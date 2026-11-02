using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed partial class BoardCompletionLayoutTests
    {
        private const string Grey = "base.character.gandalf-grey";
        private const string Gimli = "base.character.gimli";
        private const string Strider = "base.character.strider";

        [UnityTest]
        public IEnumerator FullRosterVersionsAndHomelandsKeepBothViewsReadableInFourLayouts() => Wait(RostersAndSetup());

        private static async Task RostersAndSetup()
        {
            // A complete catalog inspection has many real focus/paint cycles.
            // Each role gets its own existing 60-second room lifetime, without
            // changing shared integration timeouts or reducing inspected cards.
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            {
                using var room = await Connect("ui66-full", ScenarioKind.Interaction, openPanels: false);
                await FourLayouts(room, "characters", InspectRoster, onlySide: side);
            }

            var configurations = new[]
            {
                new MatchConfiguration { Lords = true },
                new MatchConfiguration { Lords = true, Council = true },
                new MatchConfiguration { Lords = true, Council = true, Kings = true, Fate = true, NewCities = true }
            };
            for (var index = 0; index < configurations.Length; index++)
            {
                using var room = await CreateNormalGame(configurations[index]);
                await FourLayouts(room, new[] { "lords", "council", "fate" }[index], InspectVersions);
            }

            using (var room = await Connect("ui69-layout", ScenarioKind.Interaction, openPanels: false))
                await FourLayouts(room, "homelands", InspectHomelands);
        }

        private static async Task InspectRoster(MatchTestRoom room, MountedMatchClient client)
        {
            var board = client.Board;
            var characters = board.Characters;
            var camera = board.Map.Camera.Pan;
            characters.Close();
            await room.Settle();
            await ComponentGestures.Hover(characters.Tab);
            Assert.IsTrue(Visible(characters.Root));
            Assert.IsTrue(characters.Root.ClassListContains(LeftRailPreview.PreviewClass));
            characters.Tab.Focus();
            await room.Settle();
            Assert.IsTrue(characters.Root.ClassListContains(LeftRailPreview.PreviewClass), "Focus keeps a read-only roster preview.");
            await Press(room, characters.Tab);
            Assert.IsTrue(characters.Pinned);
            var removed = characters.Root.Q<Foldout>("characters-removed");
            if (Visible(removed) && !removed.value)
                await Press(room, removed.Q<Toggle>());

            // UI-438 (owner): one row per character, its versions switching inside its window.
            var expected = CharacterRosterPresentation.Rows(board.View).Select(row => row.FigureId).ToArray();
            var rows = characters.Root.Query<Button>().ToList()
                .Where(button => button.name.StartsWith("character-roster-")).ToArray();
            CollectionAssert.AreEquivalent(expected.Select(id => "character-roster-" + id), rows.Select(row => row.name));
            Assert.AreEqual(rows.Length, rows.Select(row => row.name).Distinct().Count());
            CheckPanel(characters.Root, board.Map.parent.worldBound);
            foreach (var row in rows)
            {
                await Press(room, row);
                var inspector = characters.Inspector;
                Assert.AreEqual(row.name.Substring("character-roster-".Length), inspector.FigureId);
                Assert.IsTrue(characters.InspectorPinned);
                CheckPanel(inspector.Root, board.Map.parent.worldBound);
                Assert.IsFalse(characters.Root.worldBound.Overlaps(inspector.Root.worldBound));
                await InspectAbilities(room, client);
                var versions = inspector.Root.Query<Button>().ToList()
                    .Where(button => button.name.StartsWith("character-version-") && Visible(button)).ToArray();
                foreach (var version in versions.Where(button => !button.ClassListContains("selected")))
                {
                    await Press(room, version);
                    Assert.AreEqual(version.name.Substring("character-version-".Length), inspector.DefinitionId);
                    await InspectAbilities(room, client);
                }
            }
            Assert.AreEqual(camera, board.Map.Camera.Pan, "Reading complete character cards never locates a figure implicitly.");
            CheckPanel(characters.Root, board.Map.parent.worldBound);
            CheckPanel(characters.Inspector.Root, board.Map.parent.worldBound);
        }

        private static async Task InspectAbilities(MatchTestRoom room, MountedMatchClient client)
        {
            var inspector = client.Board.Characters.Inspector;
            var expected = CharacterAbilitySlots.For(inspector.DefinitionId);
            CheckPanel(inspector.Root, client.Board.Map.parent.worldBound);
            var chips = inspector.Root.Q("character-abilities").Query<Button>(className: CharacterAbilityChips.ChipClass)
                .ToList().Where(Visible).ToArray();
            // UI-440 (owner): every ability (each of its variants) is a bare icon in one row, in printed order; its formatted
            // tooltip names it and shows whole.
            Assert.AreEqual(expected.Count, chips.Length, inspector.DefinitionId);
            for (var index = 0; index < chips.Length; index++)
            {
                var slot = expected[index];
                var chip = chips[index];
                Assert.AreEqual(slot.Name, chip.name);
                SkillIconChecks.Bare(chip, AbilityArt.For(inspector.DefinitionId, slot.Ability.Id), 28f);
                Assert.AreEqual(chips[0].worldBound.yMin, chip.worldBound.yMin, 0.5f, "One row: " + chip.name);
                StringAssert.StartsWith(slot.Ability.Title, SkillTooltip.Plain(chip.tooltip));
                await ReadTooltip(room, client, chip);
            }
            inspector.Root.Focus();
            await room.Settle();
        }
    }
}
