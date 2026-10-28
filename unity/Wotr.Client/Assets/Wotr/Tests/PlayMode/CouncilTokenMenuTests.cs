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
    // UI-376 (owner): a Council token with one use — the Shadow's Politics token in lords-council-token, from the test menu —
    // opens the Politics panel; another choice (a die) closes the panel it opened; a panel the player closed stays closed
    // while its tab glows. UI-388: a token with several uses opens nothing and lights them all (ExpansionTokenNetworkTests).
    public sealed class CouncilTokenMenuTests
    {
        [UnityTest]
        public IEnumerator ASingleUseTokenOpensPoliticsAndAnotherChoiceClosesIt() => MatchTestRoom.Wait(Politics());

        private static async Task Politics()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui376-" + Guid.NewGuid().ToString("N")));
            await ClickReady(client.Root, "open-test-scenarios", token);
            client.Root.Q<TextField>("test-scenario-search").value = "lords-council-token";
            await Task.Delay(150, token);
            await ClickReady(client.Root, "test-scenario-lords-council-token", token);
            await Until(() => client.Board?.View != null && client.Board.Connection.text == "Live", token);
            await Task.Delay(400, token);
            if (client.Board.View.You != client.Board.View.DecisionOwner)
            {
                await ClickReady(client.Root, "test-switch-side", token);
                await Until(() => client.Board?.View != null && client.Board.View.You == client.Board.View.DecisionOwner &&
                    client.Board.Connection.text == "Live", token);
                await Task.Delay(400, token);
            }
            var board = client.Board;
            var pool = board.OwnDice.Q<Button>("pool-" + ExpansionDraft.CouncilPolitics);
            Assert.IsNotNull(pool, "The Shadow's Politics token stands in the pool");
            PointerGestureEvents.ClickAt(pool);
            await Task.Delay(500, token);
            Assert.IsTrue(board.Nations.IsOpen, "A token of one use opens Politics.");
            Assert.AreEqual(ExpansionDraft.CouncilPoliticsHint, board.Console.Status.Hint.text);
            var die = board.View.Dice.First(d => d.Owner == board.View.You && d.Location == DieLocation.Available);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(500, token);
            Assert.AreEqual(die.Id, board.ActionDie.DieId, "The die's click chose that die instead of the token.");
            Assert.IsFalse(board.Nations.IsOpen, "Another choice closes the panel the token opened.");
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(400, token);
            Assert.AreEqual("", board.ActionDie.DieId, "UI-386: a second click releases the die.");
            PointerGestureEvents.ClickAt(pool);
            await Task.Delay(500, token);
            Assert.IsTrue(board.Nations.IsOpen, "The token's use opens Politics again.");
            board.Nations.Close();
            await Task.Delay(400, token);
            Assert.IsFalse(board.Nations.IsOpen, "A panel the player closed stays closed.");
            Assert.IsTrue(board.Nations.Root.ClassListContains("legal-target"), "Its tab keeps glowing.");
        }
    }
}
