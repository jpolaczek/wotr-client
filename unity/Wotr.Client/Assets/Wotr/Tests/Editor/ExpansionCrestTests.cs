using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-263: the crests of a game's expansions on the menu's rows: in release order, the variants in the tooltip, none
    // for the base game, and rebuilt only when the expansions change.
    public sealed class ExpansionCrestTests
    {
        private static readonly MatchConfiguration KingsAndFate = new MatchConfiguration
        {
            Kings = true, Fate = true, NewCities = true, SequentialCorruption = true
        };

        [Test]
        public void AnOpenGameRowShowsItsExpansionCrestsWithTheVariantsInTheirTooltip()
        {
            var row = new PublicRoomRow("table", (_, _) => { });
            row.Apply(new PublicRoomSummary { Id = "table", Name = "Evening", Host = "Alice", Configuration = KingsAndFate, CanPlay = true });
            var crests = row.Root.Q("public-room-crests-table");
            CollectionAssert.AreEqual(new[] { "crest-fate", "crest-kings" }, crests.Children().Select(c => c.name));
            Assert.AreEqual("Fate of Erebor + Kings of Middle-earth\nVariants: New Cities · Sequential Corruption", crests.tooltip);
            StringAssert.Contains("Fate of Erebor + Kings of Middle-earth", row.Root.tooltip);
            row.Apply(new PublicRoomSummary { Id = "table", Name = "Evening", Host = "Alice", CanPlay = true });
            Assert.AreEqual(0, crests.childCount);
            Assert.IsTrue(crests.ClassListContains("base-game"));
            Assert.AreEqual("", crests.tooltip);
        }

        [Test]
        public void ASavedGameRowCarriesItsCrestsAndKeepsThemAcrossRefreshes()
        {
            var match = new MatchSummary { MatchId = "m1", Opponent = "Bob", Configuration = KingsAndFate };
            var row = new SavedMatchRow(match, _ => { });
            var crests = row.Root.Q("saved-match-crests-m1");
            var fate = crests.Q("crest-fate");
            Assert.IsNotNull(fate);
            match.Revision++;
            row.Apply(match);
            Assert.AreSame(fate, crests.Q("crest-fate"), "A refresh with the same expansions keeps the crests.");
            var lords = new MatchSummary { MatchId = "m1", Opponent = "Bob", Revision = 9, Configuration = new MatchConfiguration { Lords = true } };
            row.Apply(lords);
            CollectionAssert.AreEqual(new[] { "crest-lords" }, crests.Children().Select(c => c.name));
        }
    }
}
