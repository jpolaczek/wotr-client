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
    // UI-296 (owner's report): Book of Mazarbul played with the Event die while Gandalf the Grey guides opens his draw
    // window; the Fellowship panel opens on it, so his glowing portrait is in view, and the console's Pass names it.
    public sealed class GuideDrawNetworkTests
    {
        private const string Gandalf = "base.character.gandalf-grey";

        [UnityTest]
        public IEnumerator BookWithTheEventDieOpensTheFellowshipPanelOnGandalfsDraw() => MatchTestRoom.Wait(Book());

        private static async Task Book()
        {
            var fixture = Load();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui296-" + Guid.NewGuid().ToString("N")));
            await ClickReady(client.Root, "open-test-scenarios", token);
            await ClickReady(client.Root, "test-tab-free-peoples-cards", token);
            await ClickReady(client.Root, "test-scenario-card-base.fp.strategy.04-event", token);
            await Until(() => client.Board?.View != null && client.Board.Connection.text == "Live", token);
            var board = client.Board;
            var die = board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Available && d.Face == DieFace.Event);
            CardNetworkTests.Click(board.OwnDice.Q<Button>(die.Id));
            await Task.Delay(300, token);
            CardNetworkTests.Click(board.Hand.Q<Button>("base.fp.strategy.04"));
            await Until(() => board.View.Decision?.Window == "event.completed", token);
            await Task.Delay(1500, token);
            LayoutTests.Save(client.Panel.targetTexture, "ui296-book-gandalf-draw.png");

            Assert.IsTrue(board.Fellowship.IsOpen, "The panel opens on Gandalf's draw.");
            var portrait = board.Fellowship.Root.Q<Button>("fellowship-portrait-" + Gandalf);
            Assert.IsTrue(portrait.ClassListContains(ChoiceGlow.ClassName));
            var picked = portrait.panel.Pick(portrait.worldBound.center);
            Assert.IsTrue(picked == portrait || portrait.Contains(picked), "Gandalf's portrait can be clicked.");
            StringAssert.StartsWith("Click the Guide's glowing portrait", board.Console.Status.Hint.text);
            Assert.AreEqual("Pass · Skip Gandalf the Grey · Draw matching card", board.Console.Actions.Pass.tooltip);
            Assert.IsTrue(board.Edges.Corner.ClassListContains(BoardEdgeLayout.UsableClass));

            var hand = board.View.Hand.Count;
            var revision = board.View.Revision;
            PointerGestureEvents.ClickAt(portrait);
            await Until(() => board.View.Revision > revision, token);
            Assert.AreEqual(hand + 1, board.View.Hand.Count, "One click on the portrait draws the card.");
        }
    }
}
