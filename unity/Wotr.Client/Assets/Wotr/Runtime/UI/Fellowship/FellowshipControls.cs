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
        public string HideTooltip { get; private set; } = HideDescription;
        private readonly Dictionary<CommandKind, Button> actions = new Dictionary<CommandKind, Button>();
        private readonly Label hint = new Label { name = "fellowship-action-hint" };

        public FellowshipControls()
        {
            // UI-236: the figure on the map declares and the Mordor track's gate enters; no buttons here for them.
            // UI-249: a glowing portrait chooses the Guide; no Change guide button.
            var row = BoardScreen.Row("fellowship-actions");
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
            // UI-249: There Is Another Way's Move and Hide use the same track symbol and Ring.
            var cardMove = FellowshipCardActions.Choice(view, CommandKind.MoveFellowship);
            var cardHide = FellowshipCardActions.Choice(view, CommandKind.HideFellowship);
            MoveOffered = own && !route && enabled &&
                (FellowshipActions.Option(view, context, CommandKind.MoveFellowship, draft) != null || cardMove.Length > 0);
            MoveDescription = cardMove.Length > 0 ? FellowshipCardActions.Description(cardMove) : Description(view, CommandKind.MoveFellowship);
            HideOffered = own && !route && enabled && !view.Fellowship.Hidden &&
                (FellowshipActions.Option(view, context, CommandKind.HideFellowship, draft) != null || cardHide.Length > 0);
            HideTooltip = cardHide.Length > 0 ? FellowshipCardActions.Description(cardHide) : HideDescription;
            Offers = MoveOffered || HideOffered;
            foreach (var item in actions)
            {
                var legal = own && FellowshipActions.Option(view, context, item.Key, draft) != null;
                Offers |= legal && enabled;
                item.Value.SetEnabled(enabled && legal);
                item.Value.EnableInClassList("legal-target", legal);
                // UI-287: Finish phase keeps its place outside the Fellowship phase, greyed out with its reason.
                item.Value.style.display = own && !route ? DisplayStyle.Flex : DisplayStyle.None;
                item.Value.tooltip = legal ? Description(view, item.Key) : Unavailable;
            }
            hint.text = FellowshipActions.ActionHint(view, draft, context);
            hint.style.display = own && !route && inActions && !docked ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public const string Unavailable = "Finish phase · Only in your Fellowship phase, to keep the current Guide.";

        private static string Description(PlayerView view, CommandKind kind) => kind switch
        {
            CommandKind.MoveFellowship when view.Fellowship.MordorStep.HasValue => "Attempt to move · No Hunt roll. Draw and resolve a Hunt tile before any advance, even with no dice in the Hunt.",
            CommandKind.MoveFellowship => "Move the Fellowship · Progress +1 and resolve the Hunt; the last known region stays unchanged.",
            _ => "Keep the current Guide and finish the Fellowship phase."
        };
    }
}
