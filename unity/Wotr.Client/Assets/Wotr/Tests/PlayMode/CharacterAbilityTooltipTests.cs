using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    // UI-187: a character's tooltip in the region panel and among the Fellowship's portraits names the character once
    // and then lists its abilities in their short form, one line each.
    public sealed class CharacterAbilityTooltipTests
    {
        [UnityTest]
        public IEnumerator RegionPanelAndFellowshipPortraitsListTheAbilitiesInShort()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            view.You = Side.FreePeoples;
            view.Characters.Add(new CharacterView { Id = "gimli-on-board", DefinitionId = "base.character.gimli", Side = Side.FreePeoples,
                Location = "minas-tirith", Level = 2, Leadership = 1 });
            view.OwnCompanions = new[] { "gandalf-grey", "strider" }
                .Select(id => new CharacterView { Id = "base.character." + id, DefinitionId = "base.character." + id, Side = Side.FreePeoples,
                    Location = "fellowship", Level = 3, Leadership = 1 }).ToList();
            view.Fellowship.CompanionIdentities = view.OwnCompanions.Select(c => CharacterLibrary.Identity(c.Id)).ToList();
            view.Fellowship.CompanionCount = 2;
            view.Fellowship.Guide = "base.character.gandalf-grey";
            using var client = new MountedMatchClient("http://localhost", view.MatchId);
            client.Apply(view);
            yield return Settle();
            var board = client.Board;
            board.Hover.Open("minas-tirith");
            yield return Settle();
            var gimli = board.Hover.Panel.Root.Q<Button>("hover-figure-gimli-on-board").tooltip.Split('\n');
            StringAssert.StartsWith("Gimli", gimli[0]);
            StringAssert.Contains("Level 2 · Leadership 1", gimli[0]);
            CollectionAssert.AreEqual(CharacterAbilitySummaries.Lines("base.character.gimli"), gimli.Skip(1));
            board.Hover.Close();
            board.Fellowship.Open();
            yield return Settle();
            var gandalf = board.Fellowship.Root.Q<Button>("fellowship-portrait-base.character.gandalf-grey").tooltip.Split('\n');
            StringAssert.StartsWith("Gandalf", gandalf[0]);
            CollectionAssert.AreEqual(CharacterAbilitySummaries.Lines("base.character.gandalf-grey"), gandalf.Skip(1),
                "The Guide's ability is one of the lines, not a second copy.");
            Assert.IsTrue(gandalf.Any(line => line.StartsWith("Guide · ")), "Gandalf's Guide ability is listed.");
            Assert.AreEqual(gandalf.Length, gandalf.Distinct().Count(), "No line repeats.");
        }

        private static IEnumerator Settle()
        {
            for (var i = 0; i < 6; i++)
                yield return null;
        }
    }
}
