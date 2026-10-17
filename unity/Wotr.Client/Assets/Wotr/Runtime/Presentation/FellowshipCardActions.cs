using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-249: There Is Another Way (with Gollum as Guide) moves or hides the Fellowship where its own move and hide are
    // made — the glowing track symbol or the Mordor track's next step, and the Ring — without charging another die.
    // Stay (finish after healing) keeps its console button.
    public static class FellowshipCardActions
    {
        public const string Presentation = "FellowshipAction";
        public const string Move = "fellowship.move";
        public const string Hide = "fellowship.hide";

        // The card's choice that the Fellowship's own move or hide control makes now, or "".
        public static string Choice(PlayerView view, CommandKind kind)
        {
            var id = kind switch
            {
                CommandKind.MoveFellowship => Move,
                CommandKind.HideFellowship => Hide,
                _ => ""
            };
            return id.Length > 0 && Option(view)?.EligibleIds.Contains(id) == true ? id : "";
        }

        // A choice the Fellowship's controls make, so the console does not repeat it.
        public static bool OnObject(PlayerView view, string id) => id is Move or Hide && Option(view)?.EligibleIds.Contains(id) == true;

        public static void Choose(PlayerView view, TurnDraft draft, string id)
        {
            if (draft.Kind != CommandKind.ChooseEffect)
                DraftGesture.Choose(draft, view, CommandKind.ChooseEffect);
            CardDraft.Choose(draft, Option(view), id);
        }

        // The console names the Fellowship's controls the card uses, then its button.
        public static string Guidance(PlayerView view)
        {
            var steps = new List<string>();
            if (Choice(view, CommandKind.MoveFellowship).Length > 0)
                steps.Add("Move: click the glowing track");
            if (Choice(view, CommandKind.HideFellowship).Length > 0)
                steps.Add("Hide: click the glowing Ring");
            return steps.Count == 0 ? "" : string.Join(" · ", steps) + " · or Stay to finish after healing.";
        }

        public static string Description(string id) => id == Move
            ? "There Is Another Way · Move the Fellowship and resolve the Hunt; the Event die stays spent."
            : "There Is Another Way · Hide the Fellowship.";

        private static ActionOption Option(PlayerView view) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == Presentation && view.DecisionOwner == view.You
                ? view.Options.FirstOrDefault(o => o.Kind == CommandKind.ChooseEffect && o.Enabled) : null;
    }
}
