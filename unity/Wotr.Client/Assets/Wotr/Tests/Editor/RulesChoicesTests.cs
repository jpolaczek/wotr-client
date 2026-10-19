using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-263: the waiting room's rules strip: what each medallion and chip shows, and the configuration one click asks
    // the server for.
    public sealed class RulesChoicesTests
    {
        private static readonly string[] Everything = RulesChoices.ModuleIds.Concat(RulesChoices.VariantIds).ToArray();

        // Clicking the enabled controls from the base game reaches every one of the server's 93 legal configurations
        // (ConfigurationRules.Conflict) and never an illegal one.
        [Test]
        public void TheHostReachesAllNinetyThreeLegalConfigurationsByClicks()
        {
            var start = new MatchConfiguration();
            var seen = new Dictionary<string, MatchConfiguration> { [Key(start)] = start };
            var queue = new Queue<MatchConfiguration>(new[] { start });
            while (queue.Count > 0)
            {
                var room = Host(queue.Dequeue());
                foreach (var choice in RulesChoices.Modules(room).Concat(RulesChoices.Variants(room)).Where(c => c.Enabled))
                {
                    var next = RulesChoices.Toggle(room.Configuration, choice.Id);
                    Assert.IsTrue(Legal(next), Key(next));
                    if (seen.ContainsKey(Key(next)))
                        continue;
                    seen[Key(next)] = next;
                    queue.Enqueue(next);
                }
            }
            Assert.AreEqual(93, seen.Count);
            Assert.AreEqual(93, Enumerable.Range(0, 512).Count(bits => Legal(FromBits(bits))));
        }

        [Test]
        public void TheStripKeepsThreeMedallionsAndSixChipsInTheirPlaces()
        {
            var room = Host(new MatchConfiguration());
            CollectionAssert.AreEqual(new[] { "fate", "lords", "kings" }, RulesChoices.Modules(room).Select(c => c.Id));
            CollectionAssert.AreEqual(new[] { "new-cities", "council", "visible-corruption", "sequential-corruption", "dismay",
                "removing-ruler-tokens" }, RulesChoices.Variants(room).Select(c => c.Id));
            Assert.IsTrue(RulesChoices.Modules(room).All(c => c.Enabled && !c.On));
            Assert.IsTrue(RulesChoices.Variants(room).All(c => !c.Enabled && !c.On), "Each variant waits for its expansion.");
            StringAssert.StartsWith("Click to add Fate of Erebor to this game.", RulesChoices.Modules(room)[0].Tooltip);
            StringAssert.Contains("Both players get ready again.", RulesChoices.Modules(room)[0].Tooltip);
            StringAssert.StartsWith("Council of Rivendell needs Lords of Middle-earth.", Variant(room, "council").Tooltip);
            StringAssert.StartsWith("Removing Ruler Tokens needs Lords of Middle-earth, Kings of Middle-earth and Dismay.",
                Variant(room, "removing-ruler-tokens").Tooltip);
        }

        [TestCase("lords")]
        [TestCase("kings")]
        [TestCase("fate")]
        public void RemovingAnExpansionTakesItsVariantsWithIt(string module)
        {
            var all = new MatchConfiguration { Lords = true, Kings = true, Fate = true, Council = true, Dismay = true,
                RemovingRulerTokens = true, SequentialCorruption = true, VisibleCorruption = true, NewCities = true };
            StringAssert.Contains("Its variants go with it.", RulesChoices.Modules(Host(all)).Single(c => c.Id == module).Tooltip);
            var sent = RulesChoices.Toggle(all, module);
            Assert.IsTrue(Legal(sent));
            Assert.AreEqual(module != "lords", sent.Council);
            Assert.AreEqual(module != "kings", sent.VisibleCorruption && sent.SequentialCorruption);
            Assert.AreEqual(module != "fate", sent.NewCities);
            Assert.AreEqual(module == "fate", sent.Dismay.Value && sent.RemovingRulerTokens);
            Assert.IsTrue(all.Lords && all.Kings && all.Fate, "A click asks the server; it never changes the room's own copy.");
        }

        // Dismay is on by default with Lords and Kings; switching it off also switches off Removing Ruler Tokens.
        [Test]
        public void DismayDefaultsOnWithBothRulerExpansionsAndCarriesRemovingRulerTokens()
        {
            var lords = RulesChoices.Toggle(new MatchConfiguration(), "lords");
            Assert.IsFalse(lords.Dismay.Value);
            var both = RulesChoices.Toggle(lords, "kings");
            Assert.IsTrue(both.Dismay.Value);
            var removing = RulesChoices.Toggle(both, "removing-ruler-tokens");
            Assert.IsTrue(removing.RemovingRulerTokens);
            var noDismay = RulesChoices.Toggle(removing, "dismay");
            Assert.IsFalse(noDismay.Dismay.Value);
            Assert.IsFalse(noDismay.RemovingRulerTokens);
            Assert.IsTrue(RulesChoices.IsOn(new MatchConfiguration { Lords = true, Kings = true }, "dismay"), "Unset means the default.");
        }

        [Test]
        public void AModuleTheServerDoesNotOfferStaysInPlaceDisabledWithTheReason()
        {
            var room = Host(new MatchConfiguration { Kings = true });
            room.OfferedModules = new List<string> { "fate", "kings" };
            var modules = RulesChoices.Modules(room).ToDictionary(c => c.Id);
            Assert.IsTrue(modules["fate"].Enabled);
            Assert.IsFalse(modules["lords"].Enabled);
            Assert.AreEqual("Lords of Middle-earth · Not yet available on this server.", modules["lords"].Tooltip);
            room.OfferedModules = new List<string> { "fate" };
            var kings = RulesChoices.Modules(room).Single(c => c.Id == "kings");
            Assert.IsTrue(kings.Enabled && kings.On, "A table set up before the release narrowed can still drop the module.");
            StringAssert.StartsWith("Click to remove Kings of Middle-earth", kings.Tooltip);
        }

        // EXP-01: a server from before it names no modules; ExpansionsAvailable then offers all or none.
        [TestCase(true, true)]
        [TestCase(false, false)]
        public void AnOlderServerOffersEveryExpansionOrNone(bool available, bool offered)
        {
            var room = new RoomView { ExpansionsAvailable = available };
            Assert.IsTrue(RulesChoices.ModuleIds.All(module => RulesChoices.Offered(room, module) == offered));
        }

        [Test]
        public void TheGuestAndAStartedGameSeeTheRulesReadOnly()
        {
            var guest = Host(new MatchConfiguration { Fate = true, NewCities = true });
            guest.IsHost = false;
            var choices = RulesChoices.Modules(guest).Concat(RulesChoices.Variants(guest)).ToList();
            Assert.IsTrue(choices.All(c => !c.Enabled));
            Assert.AreEqual("Fate of Erebor · In this game. Only the host chooses the rules.", choices[0].Tooltip);
            StringAssert.StartsWith("New Cities · In this game. Only the host chooses the rules.", Variant(guest, "new-cities").Tooltip);
            var started = Host(new MatchConfiguration());
            started.Status = RoomStatus.Started;
            started.MatchId = "match";
            Assert.IsTrue(RulesChoices.Modules(started).All(c => !c.Enabled));
            StringAssert.EndsWith("The rules are fixed once the game starts.", RulesChoices.Modules(started)[1].Tooltip);
        }

        [Test]
        public void SummariesNameTheExpansionsInReleaseOrderAndTheVariants()
        {
            Assert.AreEqual("Base game", RulesChoices.Summary(new MatchConfiguration()));
            var game = new MatchConfiguration { Kings = true, Fate = true, NewCities = true, VisibleCorruption = true };
            Assert.AreEqual("Fate of Erebor + Kings of Middle-earth", RulesChoices.Summary(game));
            Assert.AreEqual("New Cities · Visible Corruption", RulesChoices.VariantSummary(game));
            Assert.AreEqual("Fate of Erebor + Kings of Middle-earth\nVariants: New Cities · Visible Corruption",
                ExpansionCrests.Tooltip(game));
            Assert.AreEqual("", ExpansionCrests.Tooltip(new MatchConfiguration()));
        }

        private static RoomView Host(MatchConfiguration configuration) => new RoomView
        {
            Id = "room", IsHost = true, Status = RoomStatus.Waiting, ExpansionsAvailable = true, Configuration = configuration,
            OfferedModules = new List<string> { "fate", "lords", "kings" }
        };

        private static RuleChoice Variant(RoomView room, string id) => RulesChoices.Variants(room).Single(c => c.Id == id);

        private static string Key(MatchConfiguration configuration) =>
            string.Concat(Everything.Select(id => RulesChoices.IsOn(configuration, id) ? "1" : "0"));

        // The server's ConfigurationRules.Conflict, with Dismay explicit.
        private static bool Legal(MatchConfiguration c)
        {
            var dismay = RulesChoices.IsOn(c, "dismay");
            return (!c.Council || c.Lords) && (!dismay || c.Lords && c.Kings) && (!c.VisibleCorruption || c.Kings) &&
                (!c.SequentialCorruption || c.Kings) && (!c.RemovingRulerTokens || c.Lords && c.Kings && dismay) &&
                (!c.NewCities || c.Fate);
        }

        private static MatchConfiguration FromBits(int bits) => new MatchConfiguration
        {
            Lords = (bits & 1) != 0, Kings = (bits & 2) != 0, Fate = (bits & 4) != 0, Council = (bits & 8) != 0,
            Dismay = (bits & 16) != 0, VisibleCorruption = (bits & 32) != 0, SequentialCorruption = (bits & 64) != 0,
            RemovingRulerTokens = (bits & 128) != 0, NewCities = (bits & 256) != 0
        };
    }
}
