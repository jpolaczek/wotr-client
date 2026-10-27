using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-305 (OPPONENT_ACTIONS_UX §6.5): the camera beat before a map effect. It frames the cue's regions (source, then
    // destination) in the safe map area, and does not move when every anchor is already well inside it, whatever zoom the
    // player chose. A needed move keeps at least the readable zoom (where tokens stand for armies); both stay in view
    // when they fit at it, otherwise the camera follows the travel, source first. Under Reduced motion it
    // jumps. Passive edge pan is suspended while it travels; a wheel zoom or a drag pan by the player stops it, and the
    // camera then follows nothing more until the presentation ends. The camera stays where the last effect left it.
    public sealed class PresentationCamera : ICuePresenter, IDisposable
    {
        private const float Margin = 48;
        private const float Inset = 8;
        private const float MostZoom = 1.5f;
        private readonly BoardScreen board;
        private MapCanvas Map => board.Map;
        private bool steered;

        public PresentationCamera(BoardScreen board)
        {
            this.board = board;
            Map.Steered += Steer;
        }

        // Map effects only; battles, the Hunt, cards, costs, dice and politics play in their own windows and rows. Inside a
        // battle only the attack, a retreat, an advance and a capture return to the map (§8); its casualties and other
        // figure changes stay in the battle window.
        public static bool Frames(PresentationCue cue) => cue.Kind switch
        {
            PresentationCueKind.March or PresentationCueKind.Travel or PresentationCueKind.Attack or PresentationCueKind.Retreat or
                PresentationCueKind.Advance or PresentationCueKind.Control => true,
            // UI-344: a Kings sovereign's change plays at his region on the map.
            PresentationCueKind.Sovereign => cue.BattleId.Length == 0,
            PresentationCueKind.Recruit or PresentationCueKind.Upgrade or PresentationCueKind.Removal or PresentationCueKind.Entry or
                PresentationCueKind.Transformation or PresentationCueKind.Relocation or PresentationCueKind.Fellowship or
                PresentationCueKind.Losses => cue.BattleId.Length == 0,
            _ => false
        };

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            // The exception is for the opponent's actions; after the player's own click the camera stays theirs.
            if (cue.OwnCommand || !Frames(cue))
                return null;
            return Frame(Regions(cue), now);
        }

        // UI-309: frames these regions (an ability's source figure) unless the player took the camera over.
        public IPresentationBeat Frame(IEnumerable<string> regions, double now)
        {
            if (steered)
                return null;
            var anchors = Anchors(regions);
            var safe = SafeArea();
            if (anchors.Count == 0 || safe.width <= 2 * Margin || safe.height <= 2 * Margin || Legible(anchors, safe))
                return null;
            var legs = Legs(anchors, safe);
            return legs.Count == 0 ? null : new Travel(this, legs, safe.center, board.ReducedMotion());
        }

        // The presentation ended: the next one may move the camera again.
        public void Released() => steered = false;

        public void Dispose()
        {
            Map.Steered -= Steer;
            Map.EdgePan.Suspended = false;
        }

        // The safe area in map-local points: the map minus the HUD (top bar, notices, console, overlays and the open
        // panels the map already avoids) and the side columns.
        public Rect SafeArea()
        {
            var area = new Rect(Inset, Inset, Map.contentRect.width - 2 * Inset, Map.contentRect.height - 2 * Inset);
            var occluders = (Map.Occlusions?.Invoke() ?? Enumerable.Empty<Rect>())
                .Concat(new[] { board.Edges.Left, board.Edges.Right }.Where(Visible).Select(Local));
            return SafeMapArea.Largest(area, occluders.Select(rect => Expand(rect, Inset)));
        }

        private void Steer() => steered = true;

        // UI-309: a separation from an unknown Fellowship position is framed at its destination only, so no route from the
        // last known region is suggested.
        private IEnumerable<string> Regions(PresentationCue cue) =>
            FellowshipSeparation.HiddenOrigin(cue.Movement, board.View, Map.Data.Regions.ContainsKey) ? new[] { cue.Movement.To } : cue.Regions;

        private List<Vector2> Anchors(IEnumerable<string> regions) => regions.Distinct()
            .Where(Map.Data.Shapes.ContainsKey)
            .Select(id => new Vector2(Map.Data.Shapes[id].Anchor[0], Map.Data.Shapes[id].Anchor[1]))
            .ToList();

        private bool Legible(IEnumerable<Vector2> anchors, Rect safe)
        {
            var view = new Rect(safe.xMin + Margin, safe.yMin + Margin, safe.width - 2 * Margin, safe.height - 2 * Margin);
            return anchors.All(anchor => view.Contains(Map.Camera.ToScreen(anchor)));
        }

        // Source and destination together at a readable zoom, else each in turn.
        private List<(float Zoom, Vector2 Pan)> Legs(List<Vector2> anchors, Rect safe)
        {
            var both = Frame(anchors, safe);
            if (both.HasValue && (both.Value.Zoom >= Readable() || anchors.Count == 1))
                return new List<(float Zoom, Vector2 Pan)> { both.Value };
            return new[] { anchors.First(), anchors.Last() }.Distinct()
                .Select(anchor => Frame(new List<Vector2> { anchor }, safe))
                .Where(frame => frame.HasValue).Select(frame => frame.Value).ToList();
        }

        // The framing CameraState.TryFrame would choose, without moving the camera.
        private (float Zoom, Vector2 Pan)? Frame(List<Vector2> anchors, Rect safe)
        {
            var min = new Vector2(anchors.Min(p => p.x), anchors.Min(p => p.y));
            var max = new Vector2(anchors.Max(p => p.x), anchors.Max(p => p.y));
            var targets = Rect.MinMaxRect(min.x, min.y, max.x, max.y);
            var camera = Map.Camera;
            var zoom = camera.Zoom;
            var pan = camera.Pan;
            camera.Constrain(Map.CanvasBounds, Map.contentRect);
            var preferred = Mathf.Max(Readable(), Mathf.Min(zoom, MostZoom));
            var framed = camera.TryFrame(targets, safe, preferred);
            var result = (camera.Zoom, camera.Pan);
            camera.Zoom = zoom;
            camera.Pan = pan;
            return framed ? result : ((float, Vector2)?)null;
        }

        // The zoom from which the map stops shrinking armies to its smallest tokens.
        private float Readable() => Map.FitZoom * MapMarkerVisibility.TokensBelow;

        private void Move(float zoom, Vector2 pan)
        {
            Map.Camera.Zoom = zoom;
            Map.Camera.Pan = pan;
            Map.Refresh();
        }

        private Rect Local(VisualElement element) => new Rect(Map.WorldToLocal(element.worldBound.position), element.worldBound.size);

        private static bool Visible(VisualElement element) => element.resolvedStyle.display != DisplayStyle.None &&
            element.worldBound.width > 0 && element.worldBound.height > 0;

        private static Rect Expand(Rect rect, float by) => new Rect(rect.x - by, rect.y - by, rect.width + 2 * by, rect.height + 2 * by);

        private sealed class Travel : IPresentationBeat
        {
            private readonly PresentationCamera owner;
            private readonly List<(float Zoom, Vector2 Pan)> legs;
            private readonly Vector2 focus;
            private readonly bool jump;
            private CameraTravel leg;
            private double start;
            private int index;

            public Travel(PresentationCamera owner, List<(float Zoom, Vector2 Pan)> legs, Vector2 focus, bool jump)
            {
                this.owner = owner;
                this.legs = legs;
                this.focus = focus;
                this.jump = jump;
                owner.Map.EdgePan.Suspended = true;
            }

            public bool Advance(double now)
            {
                if (owner.steered)
                    return End();
                if (leg == null)
                {
                    var camera = owner.Map.Camera;
                    leg = new CameraTravel(camera.Zoom, camera.Pan, legs[index].Zoom, legs[index].Pan, focus);
                    start = now;
                }
                var fraction = jump ? 1 : (now - start) / leg.Duration;
                var (zoom, pan) = leg.At(fraction);
                owner.Move(zoom, pan);
                if (fraction < 1)
                    return false;
                leg = null;
                return ++index >= legs.Count && End();
            }

            // Skip or a rebase: the camera stays where it is.
            public void Finish() => End();

            private bool End()
            {
                owner.Map.EdgePan.Suspended = false;
                return true;
            }
        }
    }
}
