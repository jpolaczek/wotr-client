using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-305 (OPPONENT_ACTIONS_UX §6.1): the action's die, marked once by its stable id with the observed trace over the
    // presented opponent pool, which is never touched or reflowed. When the pool no longer holds the die, a copy stands at
    // its last place (or after the pool's last die); when it was used as another face, a badge shows the arrow and the used
    // face. The mark stays as the action's source symbol until another action's cue begins or the presentation ends.
    public sealed class CostEmphasis : ICuePresenter
    {
        public const double Seconds = .35;
        public const string MarkName = "presentation-cost";
        private const float Ring = 3;
        private const long FadeOutMilliseconds = 400;
        private readonly BoardScreen board;
        private readonly PresentationLayer layer;
        private readonly Dictionary<string, Rect> anchors = new Dictionary<string, Rect>();
        private VisualElement mark;
        private string action = "";

        public CostEmphasis(BoardScreen board, PresentationLayer layer)
        {
            this.board = board;
            this.layer = layer;
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            if (cue.ActionId != action)
                Clear();
            if (cue.Kind != PresentationCueKind.Cost || cue.Cost.Kind != ActionCostKind.Die || cue.Cost.Id.Length == 0)
                return null;
            Remember();
            action = cue.ActionId;
            var shown = mark = Mark(cue.Cost);
            layer.Root.Add(shown);
            // The next frame, so the fade-in (Styles/Presentation.uss) has a start.
            shown.schedule.Execute(() => shown.AddToClassList("shown"));
            return new TimedBeat(now, Seconds);
        }

        public void Clear()
        {
            action = "";
            if (mark == null)
                return;
            var leaving = mark;
            mark = null;
            leaving.RemoveFromClassList("shown");
            leaving.schedule.Execute(leaving.RemoveFromHierarchy).StartingIn(FadeOutMilliseconds);
        }

        private void Remember()
        {
            foreach (var die in board.OpponentDice.Query<Button>(className: "die").ToList())
                if (die.worldBound.width > 0)
                    anchors[die.name] = die.worldBound;
        }

        private VisualElement Mark(ActionCost cost)
        {
            var die = board.OpponentDice.Q<Button>(cost.Id);
            var live = die != null && die.worldBound.width > 0;
            var bounds = layer.Local(live ? die.worldBound : anchors.TryGetValue(cost.Id, out var last) ? last : Slot());
            var element = new VisualElement { name = MarkName, pickingMode = PickingMode.Ignore };
            element.AddToClassList(MarkName);
            element.style.left = bounds.x - Ring;
            element.style.top = bounds.y - Ring;
            element.style.width = bounds.width + 2 * Ring;
            element.style.height = bounds.height + 2 * Ring;
            if (!live)
                element.Add(Copy(cost));
            element.Add(Trace(bounds.height / 2 + Ring));
            if (cost.Use.HasValue && cost.Use != cost.Face)
                element.Add(UsedFace(cost.Use));
            return element;
        }

        // A die no longer in the pool, never seen in it: after the pool's last die.
        private Rect Slot()
        {
            var last = board.OpponentDice.Query<Button>(className: "die").ToList().LastOrDefault(die => die.worldBound.width > 0);
            if (last != null)
                return new Rect(last.worldBound.xMax + 3, last.worldBound.y, last.worldBound.width, last.worldBound.height);
            var pool = board.OpponentDice.worldBound;
            return new Rect(pool.xMax, pool.center.y - 14.5f, 29, 29);
        }

        private static VisualElement Copy(ActionCost cost)
        {
            var copy = new VisualElement { name = "presentation-die", pickingMode = PickingMode.Ignore };
            copy.AddToClassList("presentation-die");
            copy.AddToClassList(cost.Owner == Side.FreePeoples ? "die-free" : "die-shadow");
            copy.Add(new DieFaceIcon(cost.Face) { pickingMode = PickingMode.Ignore });
            return copy;
        }

        private static VisualElement Trace(float radius)
        {
            var trace = new VisualElement { pickingMode = PickingMode.Ignore };
            trace.AddToClassList(ObservedTrace.ClassName);
            trace.style.borderTopLeftRadius = radius;
            trace.style.borderTopRightRadius = radius;
            trace.style.borderBottomLeftRadius = radius;
            trace.style.borderBottomRightRadius = radius;
            return trace;
        }

        private static VisualElement UsedFace(DieFace? used)
        {
            var badge = new VisualElement { name = "presentation-used-face", pickingMode = PickingMode.Ignore };
            badge.AddToClassList("presentation-used-face");
            badge.Add(new Label("→") { pickingMode = PickingMode.Ignore });
            badge.Add(new DieFaceIcon(used) { pickingMode = PickingMode.Ignore });
            return badge;
        }
    }
}
