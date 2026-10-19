using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-187: every ability of the base game's characters has a short form for tooltips, one line each, without the
    // title repeated in its gist. ART-04: so has every ability of the expansions' characters; a card's own form
    // ("<card id>#<ability id>") exists only for a card that has that ability.
    public sealed class CharacterAbilitySummariesTests
    {
        private const int Longest = 72;

        [Test]
        public void EveryAbilityOfEveryCharacterHasOneShortLine()
        {
            var cards = JObject.Parse(Resources.Load<TextAsset>("character-ability-text").text)["cards"].ToList();
            Assert.AreEqual(14, cards.Count(card => card.Value<string>("id").StartsWith("base.")), "The base game has fourteen character cards.");
            var used = cards.SelectMany(card => card["abilities"].Select(ability => ability.Value<string>("id"))).Distinct().ToList();
            foreach (var card in cards)
            {
                var id = card.Value<string>("id");
                var lines = CharacterAbilitySummaries.Lines(id);
                Assert.AreEqual(card["abilities"].Count(), lines.Count, id + " has a line per ability");
            }
            var summaries = JObject.Parse(Resources.Load<TextAsset>("character-ability-summaries").text)["summaries"].ToObject<JObject>();
            var shared = summaries.Properties().Select(p => p.Name).Where(name => !name.Contains("#")).ToList();
            CollectionAssert.AreEquivalent(used, shared, "Exactly the printed abilities are summarised.");
            foreach (var own in summaries.Properties().Select(p => p.Name).Where(name => name.Contains("#")))
            {
                var parts = own.Split('#');
                var card = cards.SingleOrDefault(c => c.Value<string>("id") == parts[0]);
                Assert.IsNotNull(card, own + " names a card");
                Assert.IsTrue(card["abilities"].Any(a => a.Value<string>("id") == parts[1]), own + " names one of its abilities");
            }
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
            Assert.AreEqual("Galadriel\nNenya · Gives Nenya to the Shadow to cancel a drawn standard Eye tile and redraw\n" +
                "Valour of the Elven People · May recruit in Lórien while it is besieged\n" +
                "Combat-card level · Counts as level 3 for Combat card effects\n" +
                "Bound to Lórien · Never leaves Lórien; removed if the Shadow conquers it",
                CharacterAbilitySummaries.Append("Galadriel", "lords.character.galadriel"));
            // ART-04: the Dwarven Lord's Messenger works in his own homelands, so his card has its own form.
            Assert.IsTrue(CharacterAbilitySummaries.Lines("fate.character.gimli-dwarven-lord")
                .Any(line => line.EndsWith(" · Any die in Iron Hills or Ered Luin activates and advances the Dwarves")));
            Assert.IsTrue(CharacterAbilitySummaries.Lines("lords.character.gimli")
                .Any(line => line.EndsWith(" · Any die in Erebor activates and advances the Dwarves")));
        }
    }
}
