#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    // UI-407 (TABLETOP_FIGURES_UX, "Acceptance (UI-407)"): the drag-with-edge-pan workload of map-profile-2. On a recorded
    // server projection the player's own Army die is chosen, the largest army that can march is picked up by a press that
    // crosses the drag threshold, and the carried figure rests in an edge zone of the map so the edge pan runs after its
    // dwell. When the map can pan no further that way the figure goes to the opposite edge. The gesture is the real one
    // (DraftInput, FigureLiftInteraction, MapFigureHand, MapEdgePan); only the pointer is synthetic, and nothing is sent.
    internal sealed class MapDragProfile
    {
        private enum Stage { Die, Press, Lift, Carry }

        private const float Zoom = 2f;
        private const float EdgeInset = 8f;
        private const float DragStep = 9f;
        private readonly BoardScreen board;
        private readonly PlayerView view;
        private readonly string die;
        private readonly ArmyOption army;
        private Stage stage;
        private Vector2 edge;
        private Vector2 lastPan;
        private int lifts;
        private int flips;
        private int carriedFrames;
        private int pannedFrames;
        private float pannedPoints;
        private bool panning;

        public MapDragProfile(BoardScreen board, PlayerView view)
        {
            this.board = board;
            this.view = view;
            var movement = view.Options.FirstOrDefault(option => option.Kind == CommandKind.MoveArmies) ??
                throw new InvalidOperationException("The drag profile's projection offers no army move.");
            army = movement.Armies
                .Where(option => !option.Garrison && option.UnitIds.Count > 0 && option.Targets.Any(t => t.BattleMode == null))
                .OrderByDescending(option => option.UnitIds.Count).ThenBy(option => option.RegionId, StringComparer.Ordinal)
                .FirstOrDefault() ?? throw new InvalidOperationException("The drag profile's projection has no army to carry.");
            die = view.Dice.FirstOrDefault(d => d.Owner == view.You && d.Location == DieLocation.Available &&
                    movement.EligibleDice.Contains(d.Id) && d.Face is DieFace.Army or DieFace.ArmyMuster)?.Id ??
                throw new InvalidOperationException("The drag profile's projection has no Army die.");
        }

        // The figure is carried and the edge pan has moved the map at least once: the measured frames may begin.
        public bool Ready => Carrying && panning;

        private bool Carrying => board.Map.DragTarget != null;

        private MapCanvas Map => board.Map;

        public object Evidence(int sampleFrames) => new
        {
            Side = view.You.ToString(), Army = army.RegionId, Units = army.UnitIds.Count, Die = die, SampleFrames = sampleFrames,
            CarriedFrames = carriedFrames, PannedFrames = pannedFrames, PannedPoints = pannedPoints, EdgeFlips = flips, Lifts = lifts
        };

        // The camera centred on the army a little closer than the whole map, so the edge can pan both ways.
        public void Frame()
        {
            var anchor = Map.Data.Shapes[army.RegionId].Anchor;
            Map.Camera.Zoom = Map.FitZoom * Zoom;
            Map.Camera.Pan = Map.contentRect.center - new Vector2(anchor[0], anchor[1]) * Map.Camera.Zoom;
            Map.Refresh();
            lastPan = Map.Camera.Pan;
            stage = Stage.Die;
        }

        // Counting starts with the first measured frame.
        public void StartSamples()
        {
            carriedFrames = 0;
            pannedFrames = 0;
            pannedPoints = 0;
            lastPan = Map.Camera.Pan;
        }

        public void Tick(bool sampled)
        {
            switch (stage)
            {
                case Stage.Die:
                    Click(board.OwnDice.Q<Button>(die) ?? throw new InvalidOperationException("No button for the die " + die));
                    stage = Stage.Press;
                    break;
                case Stage.Press:
                    Press();
                    stage = Stage.Lift;
                    break;
                case Stage.Lift:
                    Lift();
                    break;
                default:
                    Carry(sampled);
                    break;
            }
        }

        private void Press()
        {
            var marker = ArmyMarker();
            Send(marker, EventType.MouseMove, marker.worldBound.center, 0);
            Send(marker, EventType.MouseDown, marker.worldBound.center, 0);
        }

        // The press crosses the drag threshold; the figure lifts (FigureLiftInteraction.Begin) and goes to an edge.
        private void Lift()
        {
            var marker = ArmyMarker();
            Send(marker, EventType.MouseDrag, marker.worldBound.center + new Vector2(DragStep, 0), 0);
            if (!Carrying)
                throw new InvalidOperationException($"The army in {army.RegionId} did not lift for the drag profile: " +
                    $"die {board.ActionDie.DieId ?? "none"} (wanted {die}), lit targets {Map.DraftOverlay.Targets.Count}, " +
                    $"pointer captured {board.Root.HasPointerCapture(PointerId.mousePointerId)}, marker {ArmyMarker().worldBound}.");
            lifts++;
            edge = EdgePoint(Vector2.right) ?? EdgePoint(Vector2.left) ??
                throw new InvalidOperationException("No exposed map edge for the drag profile.");
            lastPan = Map.Camera.Pan;
            stage = Stage.Carry;
        }

        // The pointer rests in the edge zone (a pixel of jitter, as a hand holds still); when the map can pan no further
        // that way, it moves to the opposite edge. A carried figure that was dropped (a lost window focus) is lifted again.
        private void Carry(bool sampled)
        {
            if (!Carrying)
            {
                stage = Stage.Press;
                return;
            }
            var jitter = new Vector2(0, Time.frameCount % 2);
            Send(board.Root, EventType.MouseDrag, edge + jitter, 0);
            if (!Map.Camera.CanPan(MapEdgePan.VelocityAt(edge, Map.worldBound) * .02f))
                Flip();
            Observe(sampled);
        }

        private void Observe(bool sampled)
        {
            var moved = Vector2.Distance(lastPan, Map.Camera.Pan);
            lastPan = Map.Camera.Pan;
            panning |= moved > 0;
            if (!sampled)
                return;
            if (Carrying)
                carriedFrames++;
            if (moved > 0)
            {
                pannedFrames++;
                pannedPoints += moved;
            }
        }

        private void Flip()
        {
            var direction = edge.x > Map.worldBound.center.x ? Vector2.left : Vector2.right;
            if (EdgePoint(direction) is Vector2 opposite)
            {
                edge = opposite;
                flips++;
            }
        }

        // Puts the figure back (Esc) and releases the pointer, so the next workload starts from a quiet board.
        public void End()
        {
            if (!Carrying)
                return;
            using (var down = KeyDownEvent.GetPooled(new Event { type = EventType.KeyDown, keyCode = KeyCode.Escape }))
                board.Root.SendEvent(down);
            using (var up = KeyUpEvent.GetPooled(new Event { type = EventType.KeyUp, keyCode = KeyCode.Escape }))
                board.Root.SendEvent(up);
            Send(board.Root, EventType.MouseUp, edge, 0);
        }

        // A point in the map's edge zone on exposed terrain (the HUD covers parts of the edges).
        private Vector2? EdgePoint(Vector2 side)
        {
            var area = Map.worldBound;
            for (var row = .3f; row < .8f; row += .02f)
            {
                var x = side.x > 0 ? area.xMax - EdgeInset : area.xMin + EdgeInset;
                var point = new Vector2(x, area.yMin + area.height * row);
                if (Map.panel.Pick(point) == Map)
                    return point;
            }
            return null;
        }

        private Button ArmyMarker() => Map.Q<Button>("army-" + army.RegionId + "-" + view.You) ??
            throw new InvalidOperationException("No marker for the army in " + army.RegionId);

        private static void Click(VisualElement target)
        {
            var point = target.worldBound.center;
            Send(target, EventType.MouseMove, point, 0);
            Send(target, EventType.MouseDown, point, 0);
            Send(target, EventType.MouseUp, point, 0);
        }

        private static void Send(VisualElement target, EventType type, Vector2 point, int button)
        {
            var source = new Event { type = type, button = button, mousePosition = point, clickCount = 1 };
            if (type == EventType.MouseDown)
            {
                using var down = PointerDownEvent.GetPooled(source);
                target.SendEvent(down);
            }
            else if (type == EventType.MouseUp)
            {
                using var up = PointerUpEvent.GetPooled(source);
                target.SendEvent(up);
            }
            else
            {
                using var move = PointerMoveEvent.GetPooled(source);
                target.SendEvent(move);
            }
        }
    }
}
#endif
