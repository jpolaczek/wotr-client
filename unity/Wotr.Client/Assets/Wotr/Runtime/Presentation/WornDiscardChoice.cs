using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-432 (owner's rule, UI-430): Worn with Sorrow and Toil's choice (the Shadow's, during the Hunt) has no list of
    // buttons. A Free Peoples Character card on the table is red in the Effects panel, as The Nazgûl Strike!'s are
    // (TableDiscardText); the random card from the Free Peoples hand is the opponent's card counter in the top bar, red and
    // glowing; Keep cards is the console's Pass and the Hunt window's Skip effect (OptionalDecline). Each sends the
    // ChooseEffect the old button sent.
    public static class WornDiscardChoice
    {
        public const string Presentation = "CharacterCardDiscard";
        public const string RandomHand = "worn.random-character";
        // The server offers the choice only when a Companion is taken as a casualty in the Hunt (HuntCasualtyCondition).
        public const string Reason = "a Companion was taken as a casualty";
        public const string Hint = "Discard: red card in Effects · Random card: red hand counter";
        // The decline is named in the hint itself, so the console adds no "Or Pass to …" sentence after it.
        public const string PassHint = "Pass discards none";
        private static readonly IReadOnlyDictionary<string, string> NoCards = new Dictionary<string, string>();

        // A choice made on its object (a table card, the hand counter), so no list repeats it.
        public static bool OnObject(PlayerView view, string id) =>
            view?.Decision?.Presentation == Presentation && (id == RandomHand || CardLibrary.Cards.ContainsKey(id));

        // The table cards the Effects panel offers now: source id → "Discard <card> · Click to discard it because of …".
        public static IReadOnlyDictionary<string, string> TableCards(PlayerView view, TurnDraft draft)
        {
            var option = Option(view, draft);
            if (option == null)
                return NoCards;
            var effect = ConfirmSummary.Source(view);
            return option.EffectChoices.Where(choice => TableCard(option, choice))
                .ToDictionary(choice => choice.Id, choice => TableDiscardText.Offered(choice.SourceId, effect, Reason));
        }

        // The hand counter's offer while the random card can be chosen, or "". TOOLTIP_AUDIT rule 1: the action, the click
        // and the effect, then the server's detail.
        public static string RandomCard(PlayerView view, TurnDraft draft)
        {
            var option = Option(view, draft);
            var choice = option?.EffectChoices.FirstOrDefault(c => c.Id == RandomHand);
            if (choice == null || !option.EligibleIds.Contains(RandomHand))
                return "";
            return TooltipText.Parts("Discard a random card",
                "Click to discard a random card from the Free Peoples hand because of " + Effect(view), choice.Detail);
        }

        // The console's hint while the objects wait, within two lines of the status column at 1366×768, ending with the Pass
        // that keeps the cards.
        public static string DecisionHint(PlayerView view, TurnDraft draft)
        {
            var option = Option(view, draft);
            if (option == null)
                return "";
            var tables = TableCards(view, draft).Count > 0;
            var random = RandomCard(view, draft).Length > 0;
            var click = tables && random ? Hint
                : tables ? TableDiscardText.Hint
                : random ? "Click the red hand counter to discard a random card"
                : "";
            return TooltipText.Parts(click, option.EligibleIds.Contains(OptionalDecline.WornKeep) ? PassHint : "");
        }

        private static bool TableCard(ActionOption option, EffectChoiceView choice) =>
            option.EligibleIds.Contains(choice.Id) && choice.SourceId == choice.Id && CardLibrary.Cards.ContainsKey(choice.Id);

        private static string Effect(PlayerView view)
        {
            var card = ConfirmSummary.Source(view);
            return card.Length > 0 ? card : "this effect";
        }

        private static ActionOption Option(PlayerView view, TurnDraft draft) =>
            view?.Decision?.Presentation == Presentation && view.DecisionOwner == view.You && !view.Winner.HasValue &&
            draft?.Kind == CommandKind.ChooseEffect
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect)
                : null;
    }
}
