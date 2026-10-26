using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-245: a combat card's choice whose id is a battle die or a figure in the battle is made on that object in the
    // Battle window — a miss in the dice strip, a portrait or a figure stack in a force column — not in a list of
    // named buttons. The server's ids, limits and previews stay as they are; an exclusive outcome without an object
    // (Keep misses, Keep Minions) keeps its button.
    // UI-246: a choice that belongs to an object without being one sits on it: a Nazgûl Leadership point beside the
    // figure that forfeits it (the choice's SourceId) and a Heroic Death hit amount on the incoming hits. Keep hits
    // (zero) and Keep all keep their buttons. UI-347 (EXP-15 D10): Orderly Withdrawal's cancelled hit is a "−1" pip there
    // too; Keep the hit is the console's Pass (OptionalDecline).
    public static class BattleObjectChoices
    {
        public const string LeadershipPresentation = "NazgulLeadership";
        public const string SacrificePresentation = "SacrificeAndHits";
        public const string WithdrawalCancel = "withdrawal.cancel-hit";
        private const string HitsGroup = "Hits";
        // Read only: the selection of a draft that has not started a choice.
        private static readonly TurnDraft NoSelection = new TurnDraft();

        public static ActionOption Option(PlayerView view)
        {
            if (view?.Battle == null || view.Decision == null || view.DecisionOwner != view.You ||
                view.Decision.Kind is not (DecisionKind.EffectChoice or DecisionKind.CharacterEvent))
                return null;
            return view.Options.FirstOrDefault(o => o.Kind == CommandKind.ChooseEffect && o.Enabled);
        }

        // The objects of the battle window: every die of both forces and their special attacks, and every figure.
        public static bool Owns(PlayerView view, string id) => view?.Battle != null && Objects(view.Battle).Contains(id);

        public static bool Offered(PlayerView view, string id)
        {
            var option = Option(view);
            if (option == null || !option.EligibleIds.Contains(id))
                return false;
            var choice = option.EffectChoices.FirstOrDefault(c => c.Id == id);
            if (choice?.Exclusive == true)
                return false;
            return Owns(view, id) || IsPoint(view, choice) || HitAmount(view, choice) > 0;
        }

        // The Leadership points of these figures that the current step offers, in the server's order.
        public static List<string> PointsOf(PlayerView view, ICollection<string> figures)
        {
            var option = Option(view);
            if (option == null)
                return new List<string>();
            return option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id) && IsPoint(view, c) && figures.Contains(c.SourceId))
                .Select(c => c.Id).ToList();
        }

        // UI-246: a source's click takes its next Leadership point while one can be added, and gives back all of its
        // points once none can.
        public static string NextPoint(PlayerView view, TurnDraft draft, IReadOnlyList<string> points) =>
            points.FirstOrDefault(id => !Chosen(draft, id) && CanChoose(view, draft, id)) ?? "";

        public static int ChosenPoints(TurnDraft draft, IEnumerable<string> points) => points.Count(id => Chosen(draft, id));

        public static void ClickPoints(PlayerView view, TurnDraft draft, IReadOnlyList<string> points)
        {
            var next = NextPoint(view, draft, points);
            if (next.Length > 0)
            {
                Choose(view, draft, next);
                return;
            }
            foreach (var id in points.Where(id => Chosen(draft, id)).ToList())
                Choose(view, draft, id);
        }

        private static bool Chosen(TurnDraft draft, string id) => draft.Kind == CommandKind.ChooseEffect && draft.Payload.Ids.Contains(id);

        // Heroic Death's amounts of cancelled hits above zero, smallest first.
        public static List<(string Id, int Amount)> HitAmounts(PlayerView view)
        {
            var option = Option(view);
            if (option == null)
                return new List<(string, int)>();
            return option.EffectChoices.Where(c => option.EligibleIds.Contains(c.Id))
                .Select(c => (c.Id, Amount: HitAmount(view, c))).Where(c => c.Amount > 0).OrderBy(c => c.Amount).ToList();
        }

        // The amount of the hit choice in the draft, or zero.
        public static int ChosenHits(PlayerView view, TurnDraft draft)
        {
            if (draft?.Kind != CommandKind.ChooseEffect)
                return 0;
            return HitAmounts(view).Where(h => draft.Payload.Ids.Contains(h.Id)).Select(h => h.Amount).DefaultIfEmpty(0).Max();
        }

        // A draft of another kind counts as an empty selection, so a choice that depends on another (Heroic Death's
        // hits on its figure) waits for it.
        public static bool CanChoose(PlayerView view, TurnDraft draft, string id) =>
            Offered(view, id) && EffectChoiceDraft.CanChoose(draft.Kind == CommandKind.ChooseEffect ? draft : NoSelection, Option(view), id);

        public static void Choose(PlayerView view, TurnDraft draft, string id)
        {
            if (draft.Kind != CommandKind.ChooseEffect)
                DraftGesture.Choose(draft, view, CommandKind.ChooseEffect);
            CardDraft.Choose(draft, Option(view), id);
        }

        public static string Detail(PlayerView view, string id)
        {
            var choice = Option(view)?.EffectChoices.FirstOrDefault(c => c.Id == id);
            return choice == null ? "" : (choice.Title.Length > 0 ? choice.Title + " · " : "") + choice.Detail;
        }

        // The next eligible figure of one nation and kind: identical figures of a stack are equivalent.
        public static string NextInStack(PlayerView view, TurnDraft draft, IEnumerable<UnitView> stack) =>
            stack.Select(u => u.Id).FirstOrDefault(id => CanChoose(view, draft, id) && !draft.Payload.Ids.Contains(id)) ?? "";

        private static bool IsPoint(PlayerView view, EffectChoiceView choice) =>
            choice != null && view.Decision.Presentation == LeadershipPresentation && choice.SourceId.Length > 0 &&
            Owns(view, choice.SourceId);

        // The server titles an amount with one dot per cancelled hit ("− ●●"); zero is "Keep hits". Orderly Withdrawal
        // cancels one hit.
        private static int HitAmount(PlayerView view, EffectChoiceView choice)
        {
            if (choice == null)
                return 0;
            if (choice.Id == WithdrawalCancel)
                return 1;
            return view.Decision.Presentation == SacrificePresentation && choice.SelectionGroup == HitsGroup
                ? choice.Title.Count(c => c == '●') : 0;
        }

        private static HashSet<string> Objects(BattleView battle)
        {
            var ids = new HashSet<string>();
            foreach (var force in new[] { battle.Attacker, battle.Defender })
            {
                if (force == null)
                    continue;
                ids.UnionWith(force.Dice.Select(d => d.Id));
                ids.UnionWith(force.Units.Select(u => u.Id));
                ids.UnionWith(force.Characters.Select(c => c.Id));
            }
            foreach (var attack in battle.SpecialAttacks)
                ids.UnionWith(attack.Dice.Select(d => d.Id));
            return ids;
        }
    }
}
