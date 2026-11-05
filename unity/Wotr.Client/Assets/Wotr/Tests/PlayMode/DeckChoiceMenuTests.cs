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
    // UI-390 (owner's report, 2026-10-28), as the owner played it from the test menu: Denethor's Ruler token pays for Horn of
    // Gondor, and the Palantír of Anárion's extra card follows with no Skip. The decks beside the hand glow, the console
    // names the step and its click, the decision notice leads to no army window in Minas Tirith, Denethor's window shows
    // no Open decision, and a deck's click draws.
    public sealed class DeckChoiceMenuTests
    {
        [UnityTest]
        public IEnumerator AnarionsDrawAfterHornOfGondorGlowsOnTheDecks() => MatchTestRoom.Wait(Horn());

        private static async Task Horn()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(120));
            var token = timeout.Token;
            var fixture = Load();
            using var api = await fixture.Login(Side.FreePeoples, token);
            using var client = new MountedFlowClient(api, Path.Combine(Path.GetTempPath(), "wotr-ui390-" + Guid.NewGuid().ToString("N")));
            await ClickReady(client.Root, "open-test-scenarios", token);
            client.Root.Q<TextField>("test-scenario-search").value = "kings-fp-ruler-token";
            await Task.Delay(150, token);
            await ClickReady(client.Root, "test-scenario-kings-fp-ruler-token", token);
            await Until(() => client.Board?.View != null && client.Board.Connection.text == "Live", token);
            await Task.Delay(500, token);
            var board = client.Board;
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>("ruler-token-kings.token.denethor"));
            await Task.Delay(600, token);
            var hand = board.View.Hand.Count;
            PointerGestureEvents.ClickAt(board.Hand.Q<VisualElement>("base.fp.character.07"));
            await Until(() => board.View.Decision?.Presentation == DeckChoice.Presentation, token);
            await Task.Delay(600, token);
            Assert.IsFalse(board.View.Options.Any(o => o.EligibleIds.Contains("decline")), "With his token spent, the draw is not skipped.");
            StringAssert.EndsWith("Denethor · Choose a deck", board.Console.Status.Headline.text);
            Assert.AreEqual(DeckChoice.Hint(board.View), board.Console.Status.Hint.text);
            foreach (var deck in board.Decks.Root.Query<Button>(className: "card-deck").ToList())
            {
                Assert.IsTrue(deck.ClassListContains("legal-target") && deck.ClassListContains(DieOptionGlow.LitClass), deck.name + " glows.");
                Assert.IsTrue(deck.enabledInHierarchy, deck.name + " takes the click.");
            }
            StringAssert.Contains("Denethor · Choose a deck", board.Notice.Button.text);
            LayoutTests.Save(client.Panel.targetTexture, "ui390-anarion-decks.png");
            board.Notice.Open();
            await Task.Delay(500, token);
            Assert.IsFalse(board.Hover.HoldsDecision, "No army window in Minas Tirith opens for the deck choice.");
            board.Characters.Open();
            board.Characters.Show("kings.sovereign.denethor", true);
            await Task.Delay(500, token);
            Assert.IsNull(board.Characters.Inspector.Root.Q<Button>("character-reaction-link"), "Denethor's window links to no decision.");
            board.Characters.Close();
            // The card that slid under the pointer after Horn of Gondor left stands expanded over the decks until the
            // pointer leaves the hand, as the player's does on the way to a deck.
            CardGestures.Move(board.Map);
            await Task.Delay(400, token);
            PointerGestureEvents.ClickAt(board.Decks.Root.Q<Button>("deck-character"));
            await Until(() => board.View.Decision?.Presentation != DeckChoice.Presentation, token);
            Assert.AreEqual(hand, board.View.Hand.Count, "Horn of Gondor left the hand and the Palantír's card joined it.");
        }
    }
}
