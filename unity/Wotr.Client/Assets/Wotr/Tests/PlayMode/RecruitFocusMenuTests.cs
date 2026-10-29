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
    // UI-412 (owner, 2026-10-29): with a Muster die chosen, a clicked settlement lights only the reserves it can take. In
    // the Corrupted kings scenario Erebor takes Dwarven Regulars but no Elite, the Iron Hills take the Elite, Dale no North
    // Elite, and a region without a settlement lights every recruit again.
    public sealed class RecruitFocusMenuTests
    {
        [UnityTest]
        public IEnumerator AClickedSettlementLightsOnlyTheReservesItCanTake() => MatchTestRoom.Wait(Focus());

        private static async Task Focus()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui412-" + Guid.NewGuid().ToString("N")));
            await OpenScenario(client, "kings-corrupted-recruitment", token);
            await Task.Delay(500, token);
            var board = client.Board;
            var revision = board.View.Revision;
            var die = board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Muster);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(600, token);
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Elite), "Before a click, a Dwarven Elite may go to the Iron Hills.");
            Assert.IsTrue(Lit(board, "elves", UnitKind.Regular));
            await Inspect(board, "erebor", token);
            Assert.AreEqual("Reserve · Erebor", Title(board));
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Regular), "Erebor still takes Regulars.");
            Assert.IsFalse(Lit(board, "dwarves", UnitKind.Elite), "Corrupted Dáin forbids Elites in Erebor.");
            Assert.IsTrue(Token(board, "dwarves", UnitKind.Elite).enabledSelf, "An unlit Elite stays choosable for another settlement.");
            StringAssert.Contains("Erebor", Token(board, "dwarves", UnitKind.Elite).tooltip);
            Assert.IsFalse(board.Reinforcements.Root.Q("reinforcement-elves").ClassListContains("recruitable"), "The Elves' row is not lit.");
            Assert.IsFalse(Lit(board, "elves", UnitKind.Regular), "No Elf goes to Erebor.");
            LayoutTests.Save(client.Panel.targetTexture, "ui412-erebor.png");
            await Inspect(board, "iron-hills", token);
            Assert.AreEqual("Reserve · Iron Hills", Title(board));
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Elite), "The Iron Hills take a Dwarven Elite.");
            await Inspect(board, "dale", token);
            Assert.IsTrue(Lit(board, "north", UnitKind.Regular));
            Assert.IsFalse(Lit(board, "north", UnitKind.Elite), "Corrupted Brand forbids North Elites everywhere.");
            await Inspect(board, "old-forest", token);
            Assert.AreEqual("Reserve", Title(board));
            Assert.IsTrue(Lit(board, "elves", UnitKind.Regular), "A region without a settlement lights every recruit again.");
            Assert.IsTrue(Lit(board, "dwarves", UnitKind.Elite));
            Assert.AreEqual(revision, board.View.Revision, "Clicking settlements sends nothing.");
        }

        private static async Task Inspect(BoardScreen board, string region, CancellationToken token)
        {
            MapContourProbe.ClickRegion(board.Map, region);
            await Task.Delay(500, token);
            Assert.AreEqual(region, board.Hover.Panel.Region);
        }

        private static string Title(BoardScreen board) => board.Reinforcements.Root.Q<Label>(className: "title").text;

        private static Button Token(BoardScreen board, string nation, UnitKind kind)
        {
            var token = board.Reinforcements.Root.Q<Button>("reserve-token-" + nation + "-" + kind);
            Assert.IsNotNull(token, nation + " " + kind);
            return token;
        }

        private static bool Lit(BoardScreen board, string nation, UnitKind kind)
        {
            var token = Token(board, nation, kind);
            return token.enabledSelf && token.ClassListContains("eligible-token");
        }
    }
}
