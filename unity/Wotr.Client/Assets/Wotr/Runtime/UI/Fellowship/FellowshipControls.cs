using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class FellowshipControls
    {
        public VisualElement Root { get; } = new VisualElement { name = "fellowship-controls" };
        public event Action<CommandKind> Selected;
        // UI-98: some Fellowship action is legal now, so the top-bar plaque invites a look.
        public bool Offers { get; private set; }
        // UI-110: the move is the glowing track symbol (FellowshipStatus), not a button in this row.
        public bool MoveOffered { get; private set; }
        // UI-220: a revealed Fellowship hides from its status plaque, not from a button in this row.
        public bool HideOffered { get; private set; }
        public const string HideDescription = "Click the Ring to hide the Fellowship at its current position · No progress; this die does not enter the Hunt.";
        public string MoveDescription { get; private set; } = "";
        private readonly Dictionary<CommandKind, Button> actions = new Dictionary<CommandKind, Button>();
        private readonly Label hint = new Label { name = "fellowship-action-hint" };

        public FellowshipControls()
        {
            // UI-236: the figure on the map declares and the Mordor track's gate enters; no buttons here for them.
            var row = BoardScreen.Row("fellowship-actions");
            Add(row, CommandKind.SelectGuide, "Change guide", "fellowship-change-guide");
            Add(row, CommandKind.ContinueFellowship, "Finish phase", "fellowship-continue");
            Root.Add(row);
            Root.Add(hint);
        }

        private void Add(VisualElement row, CommandKind kind, string text, string name)
        {
            var button = BoardScreen.Button(text, () => Selected?.Invoke(kind));
            button.name = name;
            actions.Add(kind, button);
            row.Add(button);
        }

        public void Refresh(PlayerView view, TurnDraft draft, ActionDieContext context, bool enabled, bool route, bool docked = false)
        {
            var own = view.You == Side.FreePeoples;
            Root.style.display = own ? DisplayStyle.Flex : DisplayStyle.None;
            Root.SetEnabled(own);
            var inActions = view.Phase == GamePhase.ActionResolution && view.Hunt == null && view.Winner == null &&
                !FellowshipDraft.UsesRoute(draft.Kind) && draft.Kind != CommandKind.SelectGuide;
            MoveOffered = own && !route && enabled && FellowshipActions.Option(view, context, CommandKind.MoveFellowship, draft) != null;
            MoveDescription = Description(view, CommandKind.MoveFellowship);
            HideOffered = own && !route && enabled && !view.Fellowship.Hidden &&
                FellowshipActions.Option(view, context, CommandKind.HideFellowship, draft) != null;
            Offers = MoveOffered || HideOffered;
            foreach (var item in actions)
            {
                var legal = own && FellowshipActions.Option(view, context, item.Key, draft) != null;
                Offers |= legal && enabled;
                item.Value.SetEnabled(enabled && legal);
                item.Value.EnableInClassList("legal-target", legal);
                var guideAction = item.Key == CommandKind.SelectGuide;
                var guideVisible = !guideAction || view.Decision?.Kind == DecisionKind.FellowshipPhase && draft.Kind != CommandKind.SelectGuide;
                item.Value.style.display = own && !route && guideVisible && legal ? DisplayStyle.Flex : DisplayStyle.None;
                item.Value.tooltip = Description(view, item.Key);
            }
            hint.text = FellowshipActions.ActionHint(view, draft, context);
            hint.style.display = own && !route && inActions && !docked ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private static string Description(PlayerView view, CommandKind kind) => kind switch
        {
            CommandKind.MoveFellowship when view.Fellowship.MordorStep.HasValue => "Attempt to move · No Hunt roll. Draw and resolve a Hunt tile before any advance, even with no dice in the Hunt.",
            CommandKind.MoveFellowship => "Move the Fellowship · Progress +1 and resolve the Hunt; the last known region stays unchanged.",
            CommandKind.SelectGuide => "Highlight legal candidates · Choosing a guide ends the Fellowship phase.",
            _ => "Keep the current Guide and finish the Fellowship phase."
        };
    }
}
