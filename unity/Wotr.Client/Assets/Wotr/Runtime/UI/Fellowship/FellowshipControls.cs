using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class FellowshipControls
    {
        public VisualElement Root { get; } = new VisualElement { name = "fellowship-controls" };
        // The row of Finish phase (UI-481: the Council setup's ✓ moved to the console).
        public VisualElement Row { get; } = BoardScreen.Row("fellowship-actions");
        public event Action<CommandKind> Selected;
        // UI-98: some Fellowship action is legal now, so the top-bar plaque invites a look.
        public bool Offers { get; private set; }
        // UI-110: the move is the glowing track symbol (FellowshipStatus), not a button in this row.
        public bool MoveOffered { get; private set; }
        // UI-220: a revealed Fellowship hides from its status plaque, not from a button in this row.
        public bool HideOffered { get; private set; }
        // UI-293: the Fellowship's marker on its track hides it. UI-420 (owner): the marker's tooltip is the shared hide.
        public const string HideDescription = FellowshipActions.HideTooltip;
        public string MoveDescription { get; private set; } = "";
        public string HideTooltip { get; private set; } = HideDescription;
        private readonly Dictionary<CommandKind, Button> actions = new Dictionary<CommandKind, Button>();
        private readonly Label hint = new Label { name = "fellowship-action-hint" };
        // UI-448: the declaration is being drawn, the board takes input, and the drawn declaration can be sent now (the
        // tray's ✓ is enabled).
        private bool declaring, inputEnabled, declareReady;
        private Button Declare => actions[CommandKind.DeclareFellowship];

        public FellowshipControls()
        {
            // UI-236: the Mordor track's gate enters; no button here for it. UI-448 (owner): Declare stands beside Finish
            // phase, in one row — it starts the declaration (and brings the camera to the Fellowship), and while the route
            // is drawn it sends it; the figure on the map still declares too.
            // UI-249: a glowing portrait chooses the Guide; no Change guide button.
            Add(Row, CommandKind.DeclareFellowship, "Declare", "fellowship-declare");
            Add(Row, CommandKind.ContinueFellowship, "Finish phase", "fellowship-continue");
            Root.Add(Row);
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
            declaring = draft.Kind == CommandKind.DeclareFellowship;
            inputEnabled = enabled;
            foreach (var item in actions)
            {
                var legal = own && !declaring && FellowshipActions.Option(view, context, item.Key, draft) != null;
                Offers |= legal && enabled;
                item.Value.SetEnabled(enabled && legal);
                item.Value.EnableInClassList("legal-target", legal);
                // UI-287: Finish phase keeps its place outside the Fellowship phase, greyed out with its reason; UI-448: so
                // does it beside Declare while the declaration is drawn.
                item.Value.style.display = own && (!route || declaring) ? DisplayStyle.Flex : DisplayStyle.None;
                item.Value.tooltip = legal ? Description(view, item.Key) : UnavailableTooltip(view, item.Key);
            }
            if (declaring)
                RefreshDeclare();
            else
                ChoiceGlow.Apply(Declare, false);
            hint.text = FellowshipActions.ActionHint(view, draft, context);
            hint.style.display = own && !route && inActions && !docked ? DisplayStyle.Flex : DisplayStyle.None;
        }

        public const string Unavailable = "Finish phase · Only in your Fellowship phase, to keep the current Guide.";
        public const string DeclareUnavailable = "Declare · Only while the hidden Fellowship may declare its position.";
        // UI-482 (owner, 2026-11-04): Declare keeps its place, greyed, when the server offers no declaration at the start.
        public const string DeclareNothingYet = "Declare · The Fellowship has not moved yet: there is nothing to declare at the start of the game";
        public const string DeclareStart = "Declare · Select the Fellowship and choose its route on the map";
        public const string DeclareSend = "Declare · The Fellowship stands at the route's end and stays hidden; its Progress returns to 0";
        public const string DeclareWaiting = "Declare · Draw the route on the map first";

        private static string UnavailableTooltip(PlayerView view, CommandKind kind)
        {
            if (kind != CommandKind.DeclareFellowship)
                return Unavailable;
            return FellowshipActions.NothingToDeclare(view) ? DeclareNothingYet : DeclareUnavailable;
        }

        // UI-448: while the declaration is drawn, Declare sends it once the tray's ✓ could.
        public void DeclareReady(bool ready)
        {
            declareReady = ready;
            if (declaring)
                RefreshDeclare();
        }

        private void RefreshDeclare()
        {
            var ready = inputEnabled && declareReady;
            Declare.SetEnabled(ready);
            Declare.EnableInClassList("legal-target", ready);
            ChoiceGlow.Apply(Declare, ready);
            Declare.tooltip = ready ? DeclareSend : DeclareWaiting;
        }

        private static string Description(PlayerView view, CommandKind kind) => kind switch
        {
            CommandKind.DeclareFellowship => DeclareStart,
            CommandKind.MoveFellowship when view.Fellowship.MordorStep.HasValue => "Click to move the Fellowship on the Mordor track · " +
                "No Hunt roll; draw and resolve a Hunt tile before any advance, even with no dice in the Hunt",
            CommandKind.MoveFellowship => FellowshipActions.MoveTooltip,
            _ => "Keep the current Guide and finish the Fellowship phase."
        };
    }
}
