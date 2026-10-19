using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-248: a payment by one of several different action dice is made in the own dice pool: a character's entry or
    // ability, and the removal of Palantír of Orthanc, a protection card or Denethor's Folly (UI-272: and of the Lords'
    // Lore of the Elven Rings and The Gates are Closed). The dice the step accepts glow in the pool, the chosen one is
    // selected, and the decision window lists none of them; a Ring is paid on its socket in the console.
    public static class CostDice
    {
        private static readonly IReadOnlyCollection<string> None = new List<string>();

        public static IReadOnlyCollection<string> Offered(PlayerView view, TurnDraft draft)
        {
            if (view?.Decision == null || view.DecisionOwner != view.You || draft?.Kind == null)
                return None;
            var option = DraftOptions.For(view, draft);
            if (option == null)
                return None;
            if (draft.Kind is CommandKind.EnterCharacter or CommandKind.UseCharacterAbility)
            {
                var choice = CharacterDraft.Selected(draft, option);
                return choice != null && CharacterDraft.UniformDie(view, choice).Length == 0 ? choice.Dice : None;
            }
            return Removal(draft.Kind) && option.EligibleDice.Count > 1 ? option.EligibleDice : None;
        }

        public static bool InPool(PlayerView view, TurnDraft draft) => Offered(view, draft).Count > 0;

        // A die the player may still switch to: offered and not the chosen one.
        public static bool Glows(PlayerView view, TurnDraft draft, string id) =>
            draft?.Payload.DieId != id && Contains(Offered(view, draft), id);

        // Palantír of Orthanc and Lore: a Will of the West die pays alone, so choosing one drops a Ring chosen for another die.
        public static bool DropsRing(PlayerView view, TurnDraft draft, string id) =>
            draft?.Kind is CommandKind.DiscardPalantir or CommandKind.DiscardLore &&
            view.Dice.Exists(d => d.Id == id && d.Face == DieFace.WillOfTheWest);

        // UI-272: a removal paid with a die and a Ring waits for the Ring's click on its socket.
        public static string RingHint(PlayerView view, TurnDraft draft)
        {
            if (draft?.Kind is not (CommandKind.DiscardPalantir or CommandKind.DiscardLore) || draft.Payload.RingId.Length > 0)
                return "";
            var option = DraftOptions.For(view, draft);
            if (option == null || !option.EligibleDice.Contains(draft.Payload.DieId) || option.DiceWithoutRing.Contains(draft.Payload.DieId))
                return "";
            return "Click a lit Elven Ring to give it to the Shadow with this die · Or choose a Will of the West die";
        }

        private static bool Removal(CommandKind? kind) =>
            kind is CommandKind.DiscardPalantir or CommandKind.DiscardProtection or CommandKind.DiscardDenethor or
                CommandKind.DiscardLore or CommandKind.DiscardGates;

        private static bool Contains(IReadOnlyCollection<string> ids, string id)
        {
            foreach (var item in ids)
                if (item == id)
                    return true;
            return false;
        }
    }
}
