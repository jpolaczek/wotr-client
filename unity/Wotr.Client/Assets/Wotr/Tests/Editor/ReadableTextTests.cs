using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Wotr.Client.Tests
{
    // UI-331 (owner rule 2026-10-25): a recorded or server text never shows an identifier; every one reads as its name.
    public sealed class ReadableTextTests
    {
        private static readonly BoardPresentation Data = BoardPresentation.Load();
        private static readonly Regex Code = new Regex(@"\b(?:base|lords|kings|fate|council)\.[A-Za-z0-9]|\bFreePeoples\b");

        [Test]
        public void TheOwnersScreenshotReadsWithTheCardAndItsTile()
        {
            var text = ReadableText.Of("base.sh.character.04: base.hunt.special-sh.1.1.1 is in play.", Data);
            Assert.AreEqual(CardLibrary.Cards["base.sh.character.04"].Title + ": Shadow special tile 1 is in play.", text);
        }

        [TestCase("Drew base.fp.character.02.", "Drew {card:base.fp.character.02}.")]
        [TestCase("FreePeoples revealed base.fp.strategy.06.", "Free Peoples revealed {card:base.fp.strategy.06}.")]
        [TestCase("base.sh.strategy.20: recruited sauron Regular in dol-guldur.",
            "{card:base.sh.strategy.20}: recruited Sauron Regular in {region:dol-guldur}.")]
        [TestCase("Battle at fords-of-isen ended after 3 round(s).", "Battle at {region:fords-of-isen} ended after 3 round(s).")]
        [TestCase("base.hunt.standard.2.0.1: Hunt tile drawn.", "Standard tile 2: Hunt tile drawn.")]
        [TestCase("Selected base.character.gandalf-grey.", "Selected {character:base.character.gandalf-grey}.")]
        [TestCase("FreePeoples attacked lossarnach from minas-tirith.",
            "Free Peoples attacked {region:lossarnach} from {region:minas-tirith}.")]
        [TestCase("base.sh.character.16: additionally eliminated base.gondor.regular.03.",
            "{card:base.sh.character.16}: additionally eliminated Gondor Regular.")]
        public void EveryIdentifierReadsAsItsName(string recorded, string expected)
        {
            Assert.AreEqual(Expand(expected), ReadableText.Of(recorded, Data));
        }

        [Test]
        public void ASovereignAndUnknownIdentifiersReadAsWords()
        {
            StringAssert.DoesNotContain("kings.", ReadableText.Of("kings.sovereign.dain recruited.", Data));
            Assert.AreEqual("a card: effect ended.", ReadableText.Of("base.sh.character.99: effect ended.", Data));
            Assert.AreEqual("a Hunt tile drawn.", ReadableText.Of("base.hunt.unknown.9.9.9 drawn.", Data));
            Assert.AreEqual("A sentence without codes stays as it is.",
                ReadableText.Of("A sentence without codes stays as it is.", Data));
        }

        [Test]
        public void CardLibraryNeverShowsAnUnknownDottedIdentifier()
        {
            Assert.AreEqual("a Hunt tile", CardLibrary.Title("base.hunt.special-sh.1.1.1"));
            Assert.AreEqual("Gondor", CardLibrary.Title("gondor"), "A plain word keeps its words.");
        }

        // The guard: every recorded text of the client's wire fixtures reads without a single code.
        [Test]
        public void NoRecordedTextOfTheFixturesShowsACode()
        {
            var texts = Fixtures().SelectMany(Texts).Distinct().ToList();
            Assert.Greater(texts.Count, 20, "The fixtures hold recorded texts.");
            var leaks = texts.Select(text => (text, readable: ReadableText.Of(text, Data)))
                .Where(pair => Code.IsMatch(pair.readable)).Select(pair => pair.text + " → " + pair.readable).ToList();
            Assert.IsEmpty(leaks, string.Join("\n", leaks.Take(20)));
        }

        private static IEnumerable<string> Fixtures() =>
            Directory.GetFiles(Path.Combine(Application.dataPath, "Wotr", "Tests"), "*.json", SearchOption.AllDirectories);

        // The "text" of every recorded step and history entry in one fixture.
        private static IEnumerable<string> Texts(string file)
        {
            JToken root;
            try
            {
                root = JToken.Parse(File.ReadAllText(file));
            }
            catch (Newtonsoft.Json.JsonException)
            {
                yield break;
            }
            foreach (var property in root.SelectTokens("$..*").OfType<JValue>().Select(value => value.Parent).OfType<JProperty>())
                if (property.Name.ToLowerInvariant() == "text" && property.Value.Type == JTokenType.String)
                    yield return (string)property.Value;
        }

        private static string Expand(string expected) => Regex.Replace(expected, @"\{(card|region|character):([^}]+)\}", match =>
        {
            var id = match.Groups[2].Value;
            return match.Groups[1].Value switch
            {
                "card" => CardLibrary.Cards[id].Title,
                "region" => Data.Regions[id].Name,
                _ => CharacterLibrary.Name(id)
            };
        });
    }
}
