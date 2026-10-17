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
            "override-fate", "override-whitehand", "stress-pan", "stress-hover" };
        private readonly BoardScreen board;
        private readonly Dictionary<string, PlayerView> views;
        private readonly GameApi api = new GameApi("http://localhost");
        private readonly MatchConnection connection;
        private readonly TurnInteraction interaction;
        private readonly Event pointer = new Event();
        private string kind;
        private int frame;
        private long revision;
        private bool inspectionPending;
        public Action TickAction { get; }
        public long ExpectedBorderBuilds { get; private set; }

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
        }

        public void Prepare(string next)
        {
            kind = next;
            board.Hover.Close();
            board.Cards.Close();
            board.Map.DraftOverlay.AttackTargets.Clear();
            board.Map.SetAttention("");
            Apply(kind.StartsWith("stress-", StringComparison.Ordinal) || kind == "projection" ? "dense" :
                kind == "override-whitehand" ? "captured" : "base");
            board.Map.Fit();
            inspectionPending = kind.StartsWith("stress-", StringComparison.Ordinal);
            if (kind.StartsWith("stress-", StringComparison.Ordinal))
            {
                // The pinned card window covers the middle of the map, so the clicked region sits in the left third.
                var clear = new Vector2(board.Map.contentRect.width * .3f, board.Map.contentRect.center.y);
                Center(board.Map, "helms-deep", 1.2f, clear);
                board.Cards.Show(board.View.Hand.First(), true);
            }
            ExpectedBorderBuilds = 0;
        }

        public void SetFrame(int value) => frame = value;

        private void Tick()
        {
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
            }
        }

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
            board.Apply(view);
            interaction.Update(view);
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
