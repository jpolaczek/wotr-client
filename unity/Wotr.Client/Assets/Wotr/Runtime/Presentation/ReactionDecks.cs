using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-431 (owner's rule, UI-430): a reaction's response that draws a card is its deck: the deck lights (legal-target),
    // its tooltip says why, and its click sends the response. Gandalf the Grey's draw (the deck of the Event card just
    // resolved) stays on the Guide's portrait (UI-247) whenever the roster holds it; only a draw the roster cannot show
    // would have been a console button, and that one is its deck. A response without any object keeps its console button.
    public static class ReactionDecks
    {
        public const string GreyDraw = "character.grey-draw";

        // The response a deck's click sends now, or "".
        public static string Draw(PlayerView view, string deck) => Offered(view) is { } choice && Deck(view) == deck ? choice.Id : "";

        // This response stands on its deck, so the console leaves it out.
        public static bool OnObject(PlayerView view, string reaction) => reaction == GreyDraw && Offered(view) != null && Deck(view).Length > 0;

        // TOOLTIP_AUDIT rule 1: "Draw matching card · Click to draw a Strategy card because of Gandalf the Grey".
        public static string Tooltip(PlayerView view)
        {
            var choice = Offered(view);
            if (choice == null)
                return "";
            var owner = choice.SourceId.Length > 0 ? CharacterLibrary.Name(choice.SourceId) : "this response";
            return TooltipText.Parts(choice.Title.Length > 0 ? choice.Title : "Draw a card",
                "Click to draw a " + FigureGrid.Name(Deck(view)) + " card because of " + owner);
        }

        // The deck of the Event card the window follows, or "".
        private static string Deck(PlayerView view) =>
            CardLibrary.Cards.TryGetValue(view?.Decision?.SourceId ?? "", out var card) ? card.Deck : "";

        private static EffectChoiceView Offered(PlayerView view)
        {
            if (view?.Decision?.Kind != DecisionKind.Reaction || view.DecisionOwner != view.You || view.Battle != null || view.Hunt != null ||
                FellowshipActions.ReactionOnRoster(view, GreyDraw))
                return null;
            var respond = view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.RespondEffect);
            return respond?.EligibleIds.Contains(GreyDraw) == true ? respond.EffectChoices.FirstOrDefault(c => c.Id == GreyDraw) : null;
        }
    }
}
