using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-187: every ability of the base game's characters has a short form for tooltips, one line each, without the
    // title repeated in its gist; nothing else is summarised.
    public sealed class CharacterAbilitySummariesTests
    {
        private const int Longest = 72;

        [Test]
        public void EveryBaseAbilityHasOneShortLine()
        {
            var cards = JObject.Parse(Resources.Load<TextAsset>("character-ability-text").text)["cards"]
                .Where(card => card.Value<string>("id").StartsWith("base.")).ToList();
            Assert.AreEqual(14, cards.Count, "The base game has fourteen character cards.");
            var used = cards.SelectMany(card => card["abilities"].Select(ability => ability.Value<string>("id"))).Distinct().ToList();
            Assert.AreEqual(22, used.Count);
            foreach (var card in cards)
            {
                var id = card.Value<string>("id");
                var lines = CharacterAbilitySummaries.Lines(id);
                Assert.AreEqual(card["abilities"].Count(), lines.Count, id + " has a line per ability");
            }
            var summaries = JObject.Parse(Resources.Load<TextAsset>("character-ability-summaries").text)["summaries"].ToObject<JObject>();
            CollectionAssert.AreEquivalent(used, summaries.Properties().Select(p => p.Name), "Only the base abilities are summarised.");
            foreach (var summary in summaries.Properties())
            {
                var text = summary.Value.Value<string>();
                Assert.LessOrEqual(text.Length, Longest, summary.Name + " stays short");
                Assert.IsFalse(text.EndsWith("."), summary.Name + " reads as a tooltip phrase, not a sentence");
                Assert.IsFalse(text.Contains("\n"), summary.Name + " stays on one line");
            }
        }

        [Test]
        public void ATooltipKeepsItsOwnLinesAndAddsTitleAndGistPerAbility()
        {
            CollectionAssert.AreEqual(new[]
            {
                "Captain of the West · +1 Combat Strength in battle (max 5 dice)",
                "Dwarf of Erebor · Any die in Erebor moves the Dwarves a step toward war"
            }, CharacterAbilitySummaries.Lines("base.character.gimli"));
            Assert.AreEqual("Gollum\nGuide · Joins the Fellowship when no Companions remain\nGuide · Unseen path · Ignores Reveal on standard numbered tiles\n" +
                "Guide · Reveal · Reveals the Fellowship to reduce Hunt damage by 1",
                CharacterAbilitySummaries.Append("Gollum", "base.character.gollum"));
            Assert.AreEqual("Galadriel", CharacterAbilitySummaries.Append("Galadriel", "lords.character.galadriel"),
                "An expansion card adds no lines.");
        }
    }
}
