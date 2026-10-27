using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-366: a character's political ability is used on its nation's track. A die that pays for it lights Politics,
    // the nation's token, arrow and station glow with a tooltip naming the character, where he stands and the effect,
    // and their click sends the ability with that die. The server offers the ability by character and region; the
    // nation it moves is the printed ability's (the server's CharacterAbilities.PoliticalAbilities).
    public static class CharacterPolitics
    {
        private static readonly IReadOnlyDictionary<string, string> Nations = new Dictionary<string, string>
        {
            ["politics-dwarves-at-erebor"] = "dwarves",
            ["politics-gondor-at-city-or-stronghold"] = "gondor",
            ["politics-elves-at-stronghold"] = "elves"
        };

        // The abilities that also activate a passive nation before its step.
        private static readonly HashSet<string> Activating = new HashSet<string>();

        public sealed class Offer
        {
            public string Nation = "";
            public CharacterActionOption Choice;
            public bool Activates;
        }

        public static bool IsPolitical(string abilityId) => Nations.ContainsKey(abilityId ?? "");

        // The political abilities the selected die pays for, one per nation; none outside the owner's action.
        public static IReadOnlyDictionary<string, Offer> For(PlayerView view, ActionDieContext die)
        {
            var offers = new Dictionary<string, Offer>();
            if (view?.Decision?.Kind != DecisionKind.Action || view.DecisionOwner != view.You || die == null || die.DieId.Length == 0)
                return offers;
            foreach (var choice in Choices(view).Where(c => c.Dice.Contains(die.DieId)))
                if (Nations.TryGetValue(choice.AbilityId, out var nation) && !offers.ContainsKey(nation))
                    offers[nation] = new Offer { Nation = nation, Choice = choice, Activates = Activating.Contains(choice.AbilityId) };
            return offers;
        }

        public static bool Pays(PlayerView view, ActionDieContext die) => For(view, die).Count > 0;

        // An ability that activates a passive nation does more than the die's own step, so its click uses the ability.
        public static bool Preferred(PlayerView view, ActionDieContext die, string nation) =>
            For(view, die).TryGetValue(nation, out var offer) && offer.Activates &&
            view.Nations.Any(n => n.Id == nation && !n.Active);

        // The ability's command with the die: the character, the ability and his region; complete with the click.
        public static bool Use(TurnDraft draft, PlayerView view, ActionDieContext die, string nation)
        {
            if (!For(view, die).TryGetValue(nation, out var offer))
                return false;
            draft.Choose(CommandKind.UseCharacterAbility, die.DieId);
            CharacterDraft.Select(draft, view, offer.Choice);
            draft.Change(p => p.DieId = die.DieId);
            return true;
        }

        // "Gimli in Erebor · Dwarf of Erebor: click to spend this die to move the Dwarves one step toward war."
        public static string Hint(PlayerView view, Offer offer, Func<string, string> regionName)
        {
            var choice = offer.Choice;
            var definition = view.OwnCharacterRoster.FirstOrDefault(row => row.FigureId == choice.CharacterId)?.CurrentDefinitionId
                ?? choice.CharacterId;
            var title = CharacterAbilityLibrary.For(definition).FirstOrDefault(a => a.Id == choice.AbilityId)?.Title ?? choice.Title;
            var region = choice.Targets.Count > 0 && regionName != null ? regionName(choice.Targets[0]) : "";
            var nation = NationPhrase(offer.Nation);
            var effect = offer.Activates ? "activate " + nation + " and move it one step toward war" : "move " + nation + " one step toward war";
            return CharacterLibrary.Name(definition) + (region.Length > 0 ? " in " + region : "") + " · " + title +
                ": click to spend this die to " + effect + ".";
        }

        // "the Dwarves", "the Elves", "the North", but "Gondor" and "Rohan".
        public static string NationPhrase(string nation)
        {
            var name = FigureGrid.Name(nation);
            return nation is "gondor" or "rohan" ? name : "the " + name;
        }

        private static IEnumerable<CharacterActionOption> Choices(PlayerView view) => view.Options
            .Where(o => o.Enabled && o.Kind == CommandKind.UseCharacterAbility).SelectMany(o => o.CharacterActions);
    }
}
