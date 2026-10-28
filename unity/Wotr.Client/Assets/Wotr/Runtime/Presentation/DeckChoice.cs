using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-390 (owner's report): a step that draws from the deck the player picks — the Palantír of Anárion's extra card
    // after Denethor's token or Ruler die paid for an Event, a Palantír card's draw — is made on the decks in the hand
    // drawer. The drawer opens once with them glowing; the console names the step by its source and says what to
    // click; the decision notice, the console's plaque and the source character's Open decision lead to the drawer, not
    // to the character's region.
    public static class DeckChoice
    {
        public const string Presentation = "Deck";
        private const string Decline = "decline";

        public static bool Pending(PlayerView view) => view?.Decision?.Kind == DecisionKind.EffectChoice &&
            view.Decision.Presentation == Presentation && view.DecisionOwner == view.You && !view.Winner.HasValue;

        // This deck is one of the step's choices.
        public static bool Offers(PlayerView view, string deck) => Pending(view) &&
            view.Options.Any(o => o.Enabled && o.Kind == CommandKind.ChooseEffect && o.EligibleIds.Contains(deck));

        // "Denethor · Choose a deck".
        public static string Title(PlayerView view)
        {
            var source = view.Decision.SourceId;
            var name = source.Length == 0 ? "" : CardLibrary.Cards.TryGetValue(source, out var card) ? card.Title
                : ComponentActions.OwnerName(view, source);
            return name.Length == 0 ? "Choose a deck" : name + " · Choose a deck";
        }

        public static string Hint(PlayerView view)
        {
            if (!Pending(view))
                return "";
            var skip = view.Options.Any(o => o.Enabled && o.Kind == CommandKind.ChooseEffect && o.EligibleIds.Contains(Decline));
            return "Click a glowing deck beside your hand to draw its top card" +
                (skip ? ", or Pass to complete the action without an extra card." : ".");
        }
    }
}
