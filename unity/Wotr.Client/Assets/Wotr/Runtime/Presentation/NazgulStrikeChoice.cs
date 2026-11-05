using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-430 (owner, 2026-10-30): The Nazgûl Strike!'s choice has no console buttons. Its Free Peoples Character cards are
    // red on the table (TableEffectsView.Offer, TableDiscardText), its Hunt roll is the console's glowing, pulsing Hunt Box
    // and its Finish the console's ✓ (FinishingOption); only the card's committed chip stays in the console's trail.
    public static class NazgulStrikeChoice
    {
        public const string Presentation = "NazgulStrike";
        public const string HuntRoll = "nazgul-strike.hunt";
        // The server offers the choice only while a Nazgûl stands in the Fellowship's region (NazgulStrikeStep).
        public const string Reason = "a Nazgûl is in the Fellowship's region";

        // A choice the Hunt Box makes, so the console has no button for it.
        public static bool OnObject(string id) => id == HuntRoll;

        // The Hunt roll the Hunt Box sends now, or null.
        public static ConsoleOption Roll(PlayerView view, TurnDraft draft)
        {
            var option = Option(view, draft);
            if (option == null || !option.EligibleIds.Contains(HuntRoll) || !EffectChoiceDraft.CanChoose(draft, option, HuntRoll))
                return null;
            return new ConsoleOption
            {
                Id = HuntRoll,
                Name = "hunt-roll-" + HuntRoll,
                Label = "Roll for the Hunt",
                Tooltip = RollTooltip(view),
                Kind = ConsoleOptionKind.Effect
            };
        }

        // TOOLTIP_AUDIT rule 1: the action, why it is offered, what it does and does not do, and the dice it rolls.
        public static string RollTooltip(PlayerView view) => TooltipText.Parts("Roll for the Hunt",
            "Click to start a Hunt roll now because of " + Card(view) + ": " + Reason,
            "The Fellowship does not move and no die is spent", Eyes(view));

        // The console's hint names the lit objects — the red table cards, the glowing Hunt Box and the ✓ — short enough for
        // two lines of the console's status column at 1366×768 beside the card's chip; each object's tooltip says the rest.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (Option(view, draft) == null)
                return "";
            var tables = CardConsoleOptions.TableCards(view, draft).Count > 0;
            var roll = Roll(view, draft) != null;
            var click = tables && roll ? "Discard: red card in Effects · Roll: Hunt Box"
                : tables ? TableDiscardText.Hint
                : roll ? "Click the glowing Hunt Box to roll"
                : "";
            var finish = FinishingOption.Option(view, draft) != null ? "✓ finishes" : "";
            return TooltipText.Parts(click, finish);
        }

        private static string Card(PlayerView view)
        {
            var card = ConfirmSummary.Source(view);
            return card.Length > 0 ? card : "this effect";
        }

        private static string Eyes(PlayerView view)
        {
            var eyes = HuntBoxView.Eyes(view);
            return (eyes == 0 ? "No Eyes" : TooltipText.Count(eyes, "Eye")) + " in the Hunt Box";
        }

        private static ActionOption Option(PlayerView view, TurnDraft draft) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == Presentation &&
            view.DecisionOwner == view.You && !view.Winner.HasValue && draft?.Kind == CommandKind.ChooseEffect
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect)
                : null;
    }
}
