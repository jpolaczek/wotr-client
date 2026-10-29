using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-348 (EXP-15 D17, owner's decision): a Combat card's replacement after the reveal — Brand's Heir of Bard, Shadows
    // in the North for a Corrupted Brand, a Dark Chieftain's substitution (UI-277) — is a gilt chip with the arms of its
    // source beside the player's own Combat card in the Battle window; its tooltip names the card it cancels and its click
    // makes the replacement. Keep card effects is the console's Pass (OptionalDecline) and, since UI-396 (owner), a button
    // in the battle window's footer too. UI-277: once made, the replacement
    // stands as the same gilt marker beside the replaced card for both players (CombatReplacements); the opponent's card
    // stays unnamed until the reveal.
    public static class CombatReplacementChoice
    {
        public const string Presentation = "CombatReplacement";
        public const string Keep = "keep";
        private const string HeirOfBard = "heir-of-bard";
        private const string ShadowsInTheNorth = "shadows-in-the-north";
        private const string NoCard = "no card";
        // UI-397: the printed abilities that replace a Combat card's effect (KINGS: Brand, the Dark Chieftains).
        private static readonly string[] Replacing = { HeirOfBard, ShadowsInTheNorth, "red-wrath", "lord-of-the-bats", "i-command" };
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

        // UI-396: the battle window offers the skip beside the chip.
        public static bool Skippable(PlayerView view) => Option(view)?.EligibleIds.Contains(Keep) == true;

        // UI-396: what the battle window's footer says: the chip's click or the skip.
        public static string Hint(PlayerView view)
        {
            var chip = Chips(view).FirstOrDefault();
            if (chip == null)
                return "";
            var name = Short(view.Sovereigns.FirstOrDefault(s => s.Id == chip.SourceId)?.Title ??
                CharacterLibrary.Name(Definition(view, chip.SourceId)));
            var card = CardTitle(view, view.You);
            var place = card == NoCard ? "your Combat card's place" : card;
            return "Click " + name + "'s arms beside " + place + " to use the ability" +
                (Skippable(view) ? ", or Keep card effects to fight on as usual." : ".");
        }

        // The choice is a chip, so the decision's lists leave it out.
        public static bool OnChip(PlayerView view, string id) => Chips(view).Any(c => c.Id == id);

        // The nation whose arms the chip wears: the sovereign's, or the Chieftain's.
        public static string Nation(PlayerView view, EffectChoiceView choice) => Nation(view, choice.SourceId);

        public static string Nation(PlayerView view, string source)
        {
            var sovereign = view.Sovereigns.FirstOrDefault(s => s.Id == source);
            if (sovereign != null)
                return sovereign.Nation;
            return CharacterLibrary.Find(Definition(view, source))?.NationIcon ?? "";
        }

        // The replacement made for this side's card this round, or null.
        public static CombatReplacementView Applied(PlayerView view, Side side) =>
            view?.CombatReplacements?.FirstOrDefault(r => r.Side == side);

        // UI-397 (owner): the marker names the ability, gives its printed text and says what it replaces this round.
        public static string AppliedTooltip(PlayerView view, CombatReplacementView applied)
        {
            var name = Short(view.Sovereigns.FirstOrDefault(s => s.Id == applied.SourceId)?.Title ??
                CharacterLibrary.Name(Definition(view, applied.SourceId)));
            var card = CardTitle(view, applied.Side);
            var ability = Ability(view, applied.SourceId);
            var replaced = "This round it replaces " + (card == NoCard ? "the Combat card" : card) + "'s effects.";
            return ability == null ? name + " · " + replaced : name + " · " + ability.Title + "\n" + ability.Text + "\n" + replaced;
        }

        // UI-397: the printed ability a source's replacement uses: Brand's by his status, a Dark Chieftain's own; or null.
        public static CharacterAbilityText Ability(PlayerView view, string source)
        {
            var sovereign = view.Sovereigns.FirstOrDefault(s => s.Id == source);
            if (sovereign != null)
            {
                var id = sovereign.Status == SovereignStatus.Corrupted ? ShadowsInTheNorth : HeirOfBard;
                return CharacterAbilityLibrary.For(sovereign.DefinitionId).FirstOrDefault(ability => ability.Id == id);
            }
            return CharacterAbilityLibrary.For(Definition(view, source)).FirstOrDefault(ability => Replacing.Contains(ability.Id));
        }

        // "The Black Serpent — Chieftain of the Haradrim" reads "The Black Serpent" in a tooltip.
        private static string Short(string title)
        {
            var dash = title.IndexOf(" — ", System.StringComparison.Ordinal);
            return dash < 0 ? title : title.Substring(0, dash);
        }

        private static string Definition(PlayerView view, string source) =>
            view.Characters.FirstOrDefault(c => c.Id == source)?.DefinitionId ?? source;

        public static string Tooltip(PlayerView view, EffectChoiceView choice)
        {
            var own = CardTitle(view, view.You);
            var sovereign = view.Sovereigns.FirstOrDefault(s => s.Id == choice.SourceId);
            // UI-397: the chip's tooltip ends with the ability's printed text.
            var printed = Ability(view, choice.SourceId);
            var text = printed == null ? "" : "\n" + printed.Title + ": " + printed.Text;
            if (sovereign == null)
                return "Click to use " + Short(choice.Title) + " instead of " + own + "'s effects" + (choice.Detail.Length > 0 ? " · " + choice.Detail : "") + text;
            var ability = sovereign.Status == SovereignStatus.Corrupted ? ShadowsInTheNorth : HeirOfBard;
            var opponent = view.You == Side.FreePeoples ? Side.Shadow : Side.FreePeoples;
            return "Click to use " + (CharacterAbilityLibrary.Title(ability) ?? choice.Title) + " · Cancels " + CardTitle(view, opponent) +
                " at initiative 0; " + own + "'s effects are given up · Once per battle" + text;
        }

        private static string CardTitle(PlayerView view, Side side)
        {
            var force = view.Battle.Attacker?.Side == side ? view.Battle.Attacker : view.Battle.Defender;
            var id = force?.CardId ?? "";
            if (id.Length == 0)
                return NoCard;
            return CardLibrary.Cards.TryGetValue(id, out var card) && card.CombatTitle.Length > 0 ? card.CombatTitle : CardLibrary.Title(id);
        }
    }
}
