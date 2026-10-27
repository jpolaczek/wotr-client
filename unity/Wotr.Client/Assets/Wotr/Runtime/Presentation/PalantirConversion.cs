using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-278 (owner's decision 17, 2026-10-19): the Palantír of Barad-dûr's Muster-as-Event starts at the card on the
    // table, not in the cost picker. While the Shadow may convert a die this turn the card glows; its click converts the
    // selected Muster die, or lights the dice it may convert so the next die click does; the hand's cards and the decks
    // then take the die as an Event die.
    public static class PalantirConversion
    {
        public const string Card = "kings.sh.strategy.25";
        private static readonly IReadOnlyDictionary<string, string> None = new Dictionary<string, string>();

        // The dice the Palantír may convert now.
        public static IReadOnlyCollection<string> Dice(PlayerView view, TurnDraft draft) =>
            DraftOptions.All(view, draft).Where(o => o.Enabled)
                .SelectMany(o => o.DrawCosts.Concat(o.Events.SelectMany(e => e.Costs)))
                .Where(c => c.AbilityId == CardDrawDraft.Palantir).Select(c => c.DieId).Distinct().ToList();

        // The card's use and what its click does, while it converts a die.
        public static IReadOnlyDictionary<string, string> CardUses(PlayerView view, TurnDraft draft)
        {
            if (view?.Decision?.Kind != DecisionKind.Action || view.DecisionOwner != view.You || draft.Kind != null ||
                Converted(draft) || Armed(draft))
                return None;
            var dice = Dice(view, draft);
            if (dice.Count == 0)
                return None;
            var tooltip = dice.Contains(draft.Payload.DieId)
                ? "Click to spend this Muster die as an Event die · Palantír of Barad-dûr · Once this turn"
                : "Click, then a glowing Muster die, to spend it as an Event die · Palantír of Barad-dûr · Once this turn";
            return new Dictionary<string, string> { [Card] = tooltip };
        }

        // The card's click: the selected die converts at once, otherwise the dice it may convert wait for a click.
        public static void Use(PlayerView view, TurnDraft draft)
        {
            var die = Dice(view, draft).Contains(draft.Payload.DieId) ? draft.Payload.DieId : "";
            draft.Clear();
            draft.Change(p =>
            {
                p.DieId = die;
                p.Face = DieFace.Event;
                p.AbilityId = CardDrawDraft.Palantir;
            });
        }

        // The card was clicked and waits for its die.
        public static bool Armed(TurnDraft draft) => draft?.Kind == null && draft?.Payload.AbilityId == CardDrawDraft.Palantir &&
            draft.Payload.DieId.Length == 0;

        public static bool Converted(TurnDraft draft) => draft?.Kind == null && draft?.Payload.AbilityId == CardDrawDraft.Palantir &&
            draft.Payload.DieId.Length > 0;

        // What a lit die's click does while the card waits for its die.
        public static string DieHint(PlayerView view, TurnDraft draft, string id) => Armed(draft) && Dice(view, draft).Contains(id)
            ? "\nClick to spend this Muster die as an Event die · Palantír of Barad-dûr" : "";
    }
}
