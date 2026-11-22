using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-573 (owner, 2026-11-22: "we need a better visualization of what the Free Peoples player does during the Hunt, which
    // skills of which characters they use"): what one Free Peoples answer to the Hunt was, in words the Shadow reads at a
    // glance — who or what answered (a character's portrait, a table card, the Ring), its title and the printed text of what
    // it does. Only public facts: the report's entry names the response, the reaction's source and the casualty.
    public sealed class HuntResponseCaption
    {
        public string Title { get; set; } = "";
        public string Detail { get; set; } = "";
        // The character whose portrait stands in the window, or "".
        public string Portrait { get; set; } = "";
        // The table card that answered, or "".
        public string Card { get; set; } = "";
        public bool Ring { get; set; }
    }

    public static class HuntResponseCaptions
    {
        // The caption of a Response or Reaction entry, or null for any other entry. guide: the Guide when it answered.
        public static HuntResponseCaption For(HuntReportEntryView entry, string guide)
        {
            if (entry == null)
                return null;
            if (entry.Kind == HuntReportKind.Response && entry.Response.HasValue)
                return Response(entry.Response.Value, guide ?? "");
            return entry.Kind == HuntReportKind.Reaction ? Reaction(entry.SourceId ?? "", entry.CharacterId ?? "") : null;
        }

        private static HuntResponseCaption Response(HuntResponse response, string guide) => response switch
        {
            HuntResponse.Ring => new HuntResponseCaption
            {
                Ring = true,
                Title = "The Ring-bearers take the damage",
                Detail = "The Hunt damage becomes Corruption."
            },
            HuntResponse.Guide => new HuntResponseCaption
            {
                Portrait = guide,
                Title = "The Guide is the casualty" + (guide.Length > 0 ? ": " + CharacterLibrary.Name(guide) : ""),
                Detail = "He leaves the Fellowship; his level is taken off the Hunt damage."
            },
            _ => new HuntResponseCaption
            {
                Title = "A random Companion is the casualty",
                Detail = "He leaves the Fellowship; his level is taken off the Hunt damage."
            }
        };

        private static HuntResponseCaption Reaction(string source, string character)
        {
            if (CardLibrary.Cards.TryGetValue(source, out var card))
                return new HuntResponseCaption { Card = source, Title = card.Title, Detail = card.Event };
            var holder = character.Length > 0 ? character : source;
            if (CharacterLibrary.Find(holder) == null)
                return new HuntResponseCaption { Title = "The Free Peoples answer the Hunt", Detail = "" };
            var ability = HuntAbility(holder);
            return new HuntResponseCaption
            {
                Portrait = holder,
                Title = CharacterLibrary.Name(holder) + (ability != null ? " · " + ability.Title : ""),
                Detail = ability?.Text ?? ""
            };
        }

        // The character's printed ability that answers the Hunt (the one whose text speaks of it), or null.
        private static CharacterAbilityText HuntAbility(string character) => CharacterAbilityLibrary.For(character)
            .FirstOrDefault(ability => ability.Text.IndexOf("Hunt", StringComparison.Ordinal) >= 0);

        // What the answer did, as the Hunt's next entries show it.
        public static string DamageChange(int before, int after) => "Hunt damage " + before + " → " + after;

        public static string Left(string character) => CharacterLibrary.Name(character) + " leaves the Fellowship";

        public static string Corruption(int before, int after) => "Corruption " + before + " → " + after;
    }
}
