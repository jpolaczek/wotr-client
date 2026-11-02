using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-409 (owner): a roster row's tooltip in clear parts without growing: the click's offer in bright gold, the name in
    // bold gold with its edition, the state dim, the statistics plain, the entry's cost in gold and its conditions dim
    // under it, the source small and faint, each ability with its name in bold; thin gaps part
    // identity, entry and abilities. The tooltip shows it as rich text (RuntimeTooltip.FormattedClass); Plain reads it back.
    public static class CharacterRowTooltip
    {
        public sealed class Parts
        {
            public string Name = "";
            public string Edition = "";
            public string State = "";
            public string Stats = "";
            public ActionAvailabilityView Entry;
            public IReadOnlyList<string> Abilities = new List<string>();
        }

        private const string Gold = RichText.Gold;
        private const string Bright = RichText.Bright;
        private const string Dim = RichText.Dim;
        private const string Faint = RichText.Faint;
        private const string Met = RichText.Met;
        private const string Gap = RichText.Gap;

        public static string Format(string offer, Parts parts)
        {
            var lines = new List<string>();
            if (offer.Length > 0)
                lines.Add(Color(Bold(Text(offer)), Bright));
            lines.Add(Color(Bold(Text(parts.Name)), Gold) + (parts.Edition.Length > 0 ? " · " + Text(parts.Edition) : ""));
            if (parts.State.Length > 0)
                lines.Add(Color(Text(parts.State), Dim));
            if (parts.Stats.Length > 0)
                lines.Add(Text(parts.Stats));
            if (parts.Entry != null)
            {
                lines.Add(Gap);
                lines.AddRange(Entry(parts.Entry));
            }
            if (parts.Abilities.Count > 0)
            {
                lines.Add(Gap);
                lines.AddRange(parts.Abilities.Select(Ability));
            }
            return string.Join("\n", lines);
        }

        // The tooltip's words without their styles.
        public static string Plain(string formatted) => Regex.Replace(formatted ?? "", "<[^>]+>", "");

        // UI-420 (owner): the entry's line is its cost; its title ("Enter Saruman") would repeat the row's name and offer.
        private static IEnumerable<string> Entry(ActionAvailabilityView entry)
        {
            yield return Color(Text(entry.Cost.Length > 0 ? "Cost: " + entry.Cost : entry.Title), Gold);
            foreach (var condition in entry.Conditions)
                yield return "  " + (condition.Met ? Color("✓", Met) : "○") + " " + Color(Text(condition.Description), Dim);
            if (!entry.WindowOpen && entry.Timing.Length > 0)
                yield return Color(Text(entry.Timing), Dim);
            if (entry.Source.Length > 0)
                yield return "<size=11>" + Color(Text(entry.Source), Faint) + "</size>";
        }

        // "Title · text": the ability's name in bold.
        private static string Ability(string line)
        {
            var split = line.IndexOf(" · ", System.StringComparison.Ordinal);
            return split < 0 ? Text(line) : Bold(Text(line.Substring(0, split))) + Text(line.Substring(split));
        }

        private static string Bold(string text) => RichText.Bold(text);

        private static string Color(string text, string color) => RichText.Color(text, color);

        private static string Text(string text) => RichText.Text(text);
    }
}
