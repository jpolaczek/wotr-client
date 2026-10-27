using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-348 (EXP-15 D17, owner's decision): a Combat card's replacement after the reveal — Brand's Heir of Bard, Shadows
    // in the North for a Corrupted Brand, a Dark Chieftain's substitution (UI-277) — is a gilt chip with the arms of its
    // source beside the player's own Combat card in the Battle window; its tooltip names the card it cancels and its click
    // makes the replacement. Keep card effects is the console's Pass (OptionalDecline).
    public static class CombatReplacementChoice
    {
        public const string Presentation = "CombatReplacement";
        public const string Keep = "keep";
        private const string HeirOfBard = "heir-of-bard";
        private const string ShadowsInTheNorth = "shadows-in-the-north";
        private static readonly IReadOnlyList<EffectChoiceView> None = new EffectChoiceView[0];

        public static ActionOption Option(PlayerView view) =>
            view?.Battle != null && view.Decision?.Kind == DecisionKind.EffectChoice && view.Decision.Presentation == Presentation &&
            view.DecisionOwner == view.You && !view.Winner.HasValue
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;

        // The replacements offered now, one chip each.
        public static IReadOnlyList<EffectChoiceView> Chips(PlayerView view)
        {
            var option = Option(view);
            return option == null ? None : option.EffectChoices.Where(c => c.Id != Keep && option.EligibleIds.Contains(c.Id)).ToList();
        }

        // The choice is a chip, so the decision's lists leave it out.
        public static bool OnChip(PlayerView view, string id) => Chips(view).Any(c => c.Id == id);

        // The nation whose arms the chip wears: the sovereign's, or the Chieftain's.
        public static string Nation(PlayerView view, EffectChoiceView choice)
        {
            var sovereign = view.Sovereigns.FirstOrDefault(s => s.Id == choice.SourceId);
            if (sovereign != null)
                return sovereign.Nation;
            var definition = view.Characters.FirstOrDefault(c => c.Id == choice.SourceId)?.DefinitionId ?? choice.SourceId;
            return CharacterLibrary.Find(definition)?.NationIcon ?? "";
        }

        public static string Tooltip(PlayerView view, EffectChoiceView choice)
        {
            var own = CardTitle(view, view.You);
            var sovereign = view.Sovereigns.FirstOrDefault(s => s.Id == choice.SourceId);
            if (sovereign == null)
                return "Click to use " + choice.Title + " instead of " + own + "'s effects" + (choice.Detail.Length > 0 ? " · " + choice.Detail : "");
            var ability = sovereign.Status == SovereignStatus.Corrupted ? ShadowsInTheNorth : HeirOfBard;
            var opponent = view.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            return "Click to use " + (CharacterAbilityLibrary.Title(ability) ?? choice.Title) + " · Cancels " + CardTitle(view, opponent) +
                " at initiative 0; " + own + "'s effects are given up · Once per battle";
        }

        private static string CardTitle(PlayerView view, Side side)
        {
            var force = view.Battle.Attacker?.Side == side ? view.Battle.Attacker : view.Battle.Defender;
            var id = force?.CardId ?? "";
            if (id.Length == 0)
                return "no card";
            return CardLibrary.Cards.TryGetValue(id, out var card) && card.CombatTitle.Length > 0 ? card.CombatTitle : CardLibrary.Title(id);
        }
    }
}
