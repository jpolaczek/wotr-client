#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    internal sealed class MapProfileScene : IDisposable
    {
        public static readonly string[] Kinds = { "idle", "pan-zoom", "highlight", "pointer", "projection",
            "override-fate", "override-whitehand", "stress-pan", "stress-hover", "drag-edge-fp", "drag-edge-sh" };
        // UI-407: the recorded projections the drag-with-edge-pan workloads carry an army on, one per side.
        public static readonly string[] DragViews = { "drag-fp", "drag-sh" };
        private readonly BoardScreen board;
        private readonly Dictionary<string, PlayerView> views;
        private readonly GameApi api = new GameApi("http://localhost");
        private readonly MatchConnection connection;
        private TurnInteraction interaction;
        private Side seat = Side.FreePeoples;
        private readonly Event pointer = new Event();
        private string kind;
        private int frame;
        private long revision;
        private bool inspectionPending;
        private bool exposePending;
        private MapDragProfile drag;
        public Action TickAction { get; }
        public Action EndAction { get; }
        public long ExpectedBorderBuilds { get; private set; }
        // The workload's gesture is under way and the measured frames may begin (a carried figure and a running pan).
        public bool Ready => drag?.Ready ?? true;
        public object DragEvidence => drag?.Evidence(FrameProfile.SampleFrames);

        public MapProfileScene(BoardScreen board, Dictionary<string, PlayerView> views)
        {
            this.board = board;
            this.views = views;
            // Repeating stored projections needs one monotonic presentation
            // envelope. All server-produced rules, figures and legal options stay
            // intact. This connection is never started and cannot send commands.
            foreach (var view in views.Values)
                view.MatchId = "map-render-profile";
            connection = new MatchConnection(api, "map-render-profile");
            interaction = new TurnInteraction(api, connection, board);
            TickAction = Tick;
            EndAction = End;
        }

        public void Prepare(string next)
        {
            kind = next;
            board.Hover.Close();
            board.Cards.Close();
            board.Map.DraftOverlay.AttackTargets.Clear();
            board.Map.SetAttention("");
            drag = null;
            Apply(ViewOf(kind));
            board.Map.Fit();
            if (IsDrag(kind))
            {
                drag = new MapDragProfile(board, views[ViewOf(kind)]);
                drag.Frame();
            }
            exposePending = inspectionPending = kind.StartsWith("stress-", StringComparison.Ordinal);
            if (kind.StartsWith("stress-", StringComparison.Ordinal))
            {
                // The pinned card window covers the middle of the map, so the clicked region sits in the left third (and
                // moves to the side the laid-out card leaves clear, ExposeSiege).
                var clear = new Vector2(board.Map.contentRect.width * .3f, board.Map.contentRect.center.y);
                Center(board.Map, "helms-deep", 1.2f, clear);
                board.Cards.Show(board.View.Hand.First(), true);
            }
            ExpectedBorderBuilds = 0;
        }

        // The frame of the measured run (negative while the gesture is set up); counting starts at the first sample.
        public void SetFrame(int value)
        {
            frame = value;
            if (value == FrameProfile.WarmupFrames)
                drag?.StartSamples();
        }

        private static bool IsDrag(string name) => name.StartsWith("drag-", StringComparison.Ordinal);

        private static string ViewOf(string name)
        {
            if (name == "drag-edge-fp")
                return "drag-fp";
            if (name == "drag-edge-sh")
                return "drag-sh";
            if (name.StartsWith("stress-", StringComparison.Ordinal) || name == "projection")
                return "dense";
            return name == "override-whitehand" ? "captured" : "base";
        }

        private void Tick()
        {
            if (exposePending)
            {
                ExposeSiege();
                exposePending = false;
                return;
            }
            if (inspectionPending)
            {
                BoardTerrainInput.Click(board.Map, "helms-deep");
                inspectionPending = false;
                if (board.Hover.Panel.Root.style.display.value == DisplayStyle.None)
                    throw new InvalidOperationException("The dense terrain click did not open the region inspection.");
                // The inspection is laid out on the next frame; its bounds are stale until then.
                return;
            }
            var map = board.Map;
            switch (kind)
            {
                case "pan-zoom":
                case "stress-pan":
                    Center(map, kind == "stress-pan" ? "helms-deep" : "morannon",
                        .6f + (1 + Mathf.Sin(frame * .04f)) * .6f);
                    break;
                case "highlight":
                    map.DraftOverlay.AttackTargets.Clear();
                    if (frame % 2 == 0)
                        map.DraftOverlay.AttackTargets.Add("morannon");
                    map.Refresh();
                    break;
                case "pointer":
                case "stress-hover":
                    MovePointer();
                    break;
                case "projection":
                    Apply(frame % 2 == 0 ? "moved" : "dense");
                    break;
                case "override-fate":
                    Apply(frame % 2 == 0 ? "fate" : "base");
                    ExpectedBorderBuilds += 9;
                    break;
                case "override-whitehand":
                    Apply(frame % 2 == 0 ? "whitehand" : "captured");
                    ExpectedBorderBuilds += 2;
                    break;
                case "drag-edge-fp":
                case "drag-edge-sh":
                    drag.Tick(frame >= FrameProfile.WarmupFrames);
                    break;
            }
        }

        // UI-407: the pinned card has grown since MAP-17 and at 125 % covers the left third of the map, so once it is laid
        // out Helm's Deep is centred in the wider part of the map it leaves clear, a frame before the click inspecting it.
        private void ExposeSiege()
        {
            var card = board.Cards.Root.worldBound;
            var area = board.Map.worldBound;
            var left = Mathf.Max(0, card.xMin - area.xMin);
            var right = Mathf.Max(0, area.xMax - card.xMax);
            var x = left >= right ? area.xMin + left / 2 : area.xMax - right / 2;
            Center(board.Map, "helms-deep", 1.2f, board.Map.WorldToLocal(new Vector2(x, area.center.y)));
        }

        // After a workload's measured frames: a carried figure is put back and the pointer released.
        private void End() => drag?.End();

        private void MovePointer()
        {
            Vector2 position;
            if (kind == "stress-hover")
            {
                var panel = board.Hover.Panel.Root;
                if (panel.resolvedStyle.display == DisplayStyle.None)
                    throw new InvalidOperationException($"Dense clicked inspection closed during profiling at frame {frame}; " +
                        "the pointer at " + pointer.mousePosition + " was over " +
                        BoardTerrainInput.Named(board.Root.panel.Pick(pointer.mousePosition)) + ", the panel at " + panel.worldBound +
                        (Application.isFocused ? "" : "; the Player window had lost focus"));
                position = panel.worldBound.center + new Vector2(Mathf.Sin(frame * .1f) * 12, 0);
            }
            else
                position = board.Map.worldBound.center + new Vector2(Mathf.Sin(frame * .1f) * 220,
                    Mathf.Cos(frame * .07f) * 170);
            pointer.type = EventType.MouseMove;
            pointer.mousePosition = position;
            using var move = PointerMoveEvent.GetPooled(pointer);
            board.Root.panel.visualTree.SendEvent(move);
        }

        private void Apply(string name)
        {
            var view = views[name];
            view.Revision = ++revision;
            if (view.You != seat)
                Seat(view.You);
            board.Apply(view);
            interaction.Update(view);
        }

        // UI-407: each seat plays through its own interaction, as each match session has its own; the drag workloads
        // switch from the Free Peoples' projections to the Shadow's and back.
        private void Seat(Side side)
        {
            interaction.Dispose();
            interaction = new TurnInteraction(api, connection, board);
            seat = side;
        }

        public void Dispose()
        {
            interaction.Dispose();
            connection.Dispose();
            api.Dispose();
        }

        public static void Center(MapCanvas map, string region, float zoom) =>
            Center(map, region, zoom, map.contentRect.center);

        private static void Center(MapCanvas map, string region, float zoom, Vector2 at)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            map.Camera.Zoom = zoom;
            map.Camera.Pan = at - new Vector2(anchor[0], anchor[1]) * zoom;
            map.Refresh();
        }
    }
}
#endif
