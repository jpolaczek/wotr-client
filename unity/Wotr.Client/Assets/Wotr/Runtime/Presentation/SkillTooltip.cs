using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-440 (owner: "skill descriptions in nicely formatted tooltips"): one form of a skill's tooltip wherever its icon
    // stands — the character and king windows, the roster row, a component button. Rich text (RuntimeTooltip.FormattedClass):
    // the skill's name in bold gold (its variant and owner after it), its gist dim on the second line, the printed text as
    // a paragraph, the source small and faint, then the state — what a click does now in bright gold, or what it waits for
    // or where it is used, dim. Thin gaps part name, text and state; the tooltip's 360-point width holds the longest text.
    public static class SkillTooltip
    {
        public sealed class Parts
        {
            public string Name = "";
            public string Variant = "";
            public string Owner = "";
            public string Gist = "";
            public string Text = "";
            public string Source = "";
            public string State = "";
            public bool Ready;
        }

        // A printed ability's name, gist, text and source as its card prints them.
        public static Parts Of(string definitionId, string abilityId)
        {
            var ability = CharacterAbilityLibrary.Find(definitionId, abilityId);
            return new Parts
            {
                Name = ability?.Title ?? "",
                Gist = CharacterAbilitySummaries.Summary(definitionId ?? "", abilityId ?? ""),
                Text = ability?.Text ?? "",
                Source = ability?.Source ?? ""
            };
        }

        public static string Format(Parts parts)
        {
            var lines = new List<string> { Heading(parts) };
            if (parts.Gist.Length > 0)
                lines.Add(RichText.Color(RichText.Text(parts.Gist), RichText.Dim));
            if (parts.Text.Length > 0)
            {
                lines.Add(RichText.Gap);
                lines.Add(RichText.Text(parts.Text));
            }
            if (parts.Source.Length > 0)
                lines.Add(RichText.Small(RichText.Color(RichText.Text(parts.Source), RichText.Faint)));
            if (parts.State.Length > 0)
            {
                lines.Add(RichText.Gap);
                lines.Add(RichText.Color(RichText.Text(parts.State), parts.Ready ? RichText.Bright : RichText.Dim));
            }
            return string.Join("\n", lines);
        }

        // The tooltip's words without their styles.
        public static string Plain(string formatted) => CharacterRowTooltip.Plain(formatted);

        private static string Heading(Parts parts) =>
            RichText.Bold(RichText.Color(RichText.Text(parts.Name), RichText.Gold)) +
            (parts.Variant.Length > 0 ? RichText.Color(" · " + RichText.Text(parts.Variant), RichText.Gold) : "") +
            (parts.Owner.Length > 0 ? RichText.Color(" · " + RichText.Text(parts.Owner), RichText.Dim) : "");
    }
}
