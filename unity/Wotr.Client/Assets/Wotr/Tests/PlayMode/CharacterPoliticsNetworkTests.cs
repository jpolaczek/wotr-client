using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.ReturnTestFixture;

namespace Wotr.PlayTests
{
    // UI-366: Gimli's Dwarf of Renown in the test scenario fate-gimli-renown against the real server: the die lights
    // Politics, the Dwarves' token explains the ability, its click advances the Dwarves, and the token travels there.
    public sealed class CharacterPoliticsNetworkTests
    {
        private const string Renown = "politics-dwarves-at-fate-homelands";

        [UnityTest]
        public IEnumerator RenownIsUsedOnTheDwarvesTrack() => MatchTestRoom.Wait(UseRenown());

        private static async Task UseRenown()
        {
            var fixture = Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui366-" + Guid.NewGuid().ToString("N")));
            await ClickReady(client.Root, "open-test-scenarios", token);
            client.Root.Q<TextField>("test-scenario-search").value = "renown";
            await Task.Delay(120, token);
            await ClickReady(client.Root, "test-scenario-fate-gimli-renown", token);
            await Until(() => client.Board?.View != null && client.Board.View.You == Side.FreePeoples && client.Board.Connection.text == "Live", token);
            var board = client.Board;
            var choice = board.View.Options.Where(o => o.Kind == CommandKind.UseCharacterAbility).SelectMany(o => o.CharacterActions)
                .Single(c => c.AbilityId == Renown);
            var before = board.View.Nations.Single(n => n.Id == "dwarves").StepsToWar;
            var revision = board.View.Revision;
            CardNetworkTests.Click(board.OwnDice.Q<Button>(choice.Dice[0]));
            await Until(() => board.Nations.Root.ClassListContains("legal-target"), token);
            CardNetworkTests.Click(board.Nations.Root);
            await Task.Delay(200, token);
            var dwarves = board.Nations.Drawer.Q<Button>("nation-status-dwarves");
            Assert.IsTrue(dwarves.ClassListContains("legal-target"), "The Dwarves' token glows.");
            StringAssert.Contains("Gimli in Iron Hills · Dwarf of Renown: click to spend this die to move the Dwarves one step toward war.",
                dwarves.tooltip);
            Assert.IsFalse(board.Nations.Drawer.Q<Button>("nation-status-elves").ClassListContains("legal-target"), "Only that nation glows.");
            LayoutTests.Save(client.Panel.targetTexture, "ui366-renown-track.png");
            CardNetworkTests.Click(dwarves);
            await Until(() => board.View.Revision > revision, token);
            Assert.AreEqual(before - 1, board.View.Nations.Single(n => n.Id == "dwarves").StepsToWar, "Renown moved the Dwarves.");
            Assert.AreEqual(DisplayStyle.Flex, board.Nations.Drawer.resolvedStyle.display, "Politics stays open on the step.");
            await Task.Delay(900, token);
            LayoutTests.Save(client.Panel.targetTexture, "ui366-renown-moved.png");
        }
    }
}
