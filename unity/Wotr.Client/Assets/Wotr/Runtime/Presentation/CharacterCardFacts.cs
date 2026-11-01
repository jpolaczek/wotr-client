using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-139: one statistic of a character card shown as a symbol. Key names the art (Art/UI/characters, or the
    // nation emblem), Value is the short mark beside it, Label the statistic in words and Meaning what it is for.
    public sealed class CharacterFact
    {
        public string Key { get; set; } = "";
        public string Value { get; set; } = "";
        public string Label { get; set; } = "";
        public string Meaning { get; set; } = "";
        public string Nation { get; set; } = "";
        public string Tooltip => Meaning.Length > 0 ? Label + " · " + Meaning : Label;
    }

    // UI-139: where the figure is, as a symbol and a few words: Text in the card, Short in a roster row.
    // Tooltip keeps the complete state.
    public sealed class CharacterStatus
    {
        public string Key { get; set; } = "";
        public string Text { get; set; } = "";
        public string Short { get; set; } = "";
        public string Tooltip { get; set; } = "";
    }

    // Inspection only: the current figure's values come from the public projection, another version's from the
    // reviewed catalogue. Movement and combat keep their own level (Fate of Erebor), never folded into Level.
    public static class CharacterCardFacts
    {
        public const string AllFreePeoples = "free-peoples";

        public static IReadOnlyList<CharacterFact> For(CharacterRosterView row, string version, bool current)
        {
            var card = CharacterLibrary.Find(version);
            var level = current ? row.Level : card?.Level;
            var movement = current ? row.MovementLevel : card?.Abilities.Contains("movement-level-two") == true ? 2 : card?.Level;
            var combat = current ? row.CombatLevel : card?.Abilities.Contains("combat-card-level-three") == true ? 3 : card?.Level;
            var leadership = current ? row.Leadership : card?.Leadership;
            var flying = current ? row.UnlimitedMovement : card?.UnlimitedMovement == true;
            var nation = (current ? row.NationIcon : card?.NationIcon) ?? "";
            var facts = new List<CharacterFact>
            {
                new CharacterFact { Key = "level", Value = Number(level), Label = "Level " + Number(level) },
                flying
                    ? new CharacterFact { Key = "flight", Value = "∞", Label = "Flight", Meaning = "Unlimited movement" }
                    : new CharacterFact { Key = "movement", Value = Number(movement), Label = "Move " + Number(movement), Meaning = "Level used to move" },
                new CharacterFact { Key = "combat", Value = Number(combat), Label = "Combat level " + Number(combat), Meaning = "Level used for combat cards" },
                new CharacterFact { Key = "leadership", Value = Number(leadership), Label = "Leadership " + Number(leadership) }
            };
            // UI-420 (owner): the nation's emblem is named plainly ("Gondor", "Southrons & Easterlings").
            if (nation.Length > 0)
                facts.Add(new CharacterFact { Key = "nation", Nation = nation, Label = NationName(nation) });
            return facts;
        }

        // The statistics in words: the tooltip of the whole strip and of a roster row. UI-420 (owner): Move and Combat
        // level are named only where they differ from Level ("Level 3 · Leadership 1 · North").
        public static string Summary(IEnumerable<CharacterFact> facts)
        {
            var list = facts.ToList();
            var level = list.FirstOrDefault(f => f.Key == "level")?.Value;
            return TooltipText.Parts(list.Where(f => !RepeatsLevel(f, level)).Select(f => f.Label).ToArray());
        }

        private static bool RepeatsLevel(CharacterFact fact, string level) => fact.Key is "movement" or "combat" && fact.Value == level;

        public static string NationName(string nation) => TooltipText.Nation(nation);

        // UI-438: version is the one a roster row shows; a row of a character whose versions are not in play names it.
        public static CharacterStatus Status(CharacterRosterView row, string region, string version = "")
        {
            var state = CharacterRosterPresentation.State(row);
            if (region.Length > 0)
                return new CharacterStatus { Key = "on-board", Text = region + (row.Garrison ? " · Garrison" : ""), Short = region,
                    Tooltip = state + " · " + region };
            var (key, words) = StatusWords(row, version);
            // Beside the ring a roster row needs no "In".
            var brief = key == "fellowship" ? "Fellowship" : words;
            return new CharacterStatus { Key = key, Text = words, Short = brief, Tooltip = state };
        }

        // UI-271/UI-438: the one row of a character with versions not in play names the version it shows (Lieutenant,
        // Númenórean), which switches to the version an offered entry brings in.
        private static (string Key, string Words) StatusWords(CharacterRosterView row, string version)
        {
            var epithet = CharacterRosterPresentation.HasAlternatives(row) ? CharacterLibrary.ShortEpithet(version) : "";
            if (epithet.Length > 0)
                return ("waiting", epithet);
            return row.Lifecycle switch
            {
                CharacterRosterLifecycle.OnBoard => ("on-board", "On board"),
                CharacterRosterLifecycle.InFellowship => ("fellowship", "In Fellowship"),
                CharacterRosterLifecycle.Eliminated => ("removed", "Removed"),
                CharacterRosterLifecycle.EliminatedWithReplacement => ("may-return", "May return"),
                CharacterRosterLifecycle.AwaitingPlacement => ("waiting", "Placement"),
                CharacterRosterLifecycle.AwaitingReturn => ("waiting", "Return pending"),
                CharacterRosterLifecycle.NotEntered => ("waiting", "Not entered"),
                _ => ("waiting", "Unknown")
            };
        }

        private static string Number(int? value) => value.HasValue ? value.Value.ToString() : "?";
    }
}
