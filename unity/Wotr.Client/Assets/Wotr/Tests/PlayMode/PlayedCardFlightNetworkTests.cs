using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-463 (owner, 2026-11-03: "let's add an animation when playing a card makes it go to Effects") with two real
    // clients: in lords-card-we-wont-go-back-event the Shadow plays We Won't Go Back with its Character die; the card leaves
    // the hand for the Shadow's table, so its stand-in flies from its place in the hand into the Effects tab, which glows.
    public sealed class PlayedCardFlightNetworkTests
    {
        private const string Scenario = "lords-card-we-wont-go-back-event";
        private const string Card = "lords.sh.character.25";

        [UnityTest] public IEnumerator WeWontGoBackFliesFromTheHandIntoTheEffectsTab() => Wait(Play());

        private static async Task Play()
        {
            using var room = await ConnectCatalog(Scenario);
            var client = room.Sh;
            var board = client.Board;
            var die = board.View.Dice.First(d =>
                d.Owner == Side.Shadow && d.Location == DieLocation.Available && d.Face == DieFace.Character);
            CardNetworkTests.Click(board.OwnDice.Q<Button>(die.Id));
            await room.Settle();
            await CardGestures.Open(client);
            var card = board.Hand.Q<Button>(Card);
            Assert.IsNotNull(card, "We Won't Go Back stands in the Shadow's hand.");
            var place = card.worldBound;
            CardNetworkTests.Click(card);
            await room.Until(() => board.PlayedCards.Flying);
            var standIn = board.PlayedCards.Root.Q(PlayedCardFlight.StandInName);
            Assert.IsNotNull(standIn, "Its stand-in leaves the hand's place.");
            Assert.Less(Vector2.Distance(standIn.worldBound.center, place.center), place.height, "It starts where the card stood.");
            await room.Until(() => !board.PlayedCards.Flying || standIn.resolvedStyle.scale.value.x < .6f);
            LayoutTests.Save(client.Viewport, "ui463-network-flight.png");
            await room.Until(() => !board.PlayedCards.Flying);
            Assert.IsTrue(PresentationGlow.IsLit(board.TableEffects.Tab), "The Effects tab glows as it lands.");
            await Task.Delay(200, room.Token);
            LayoutTests.Save(client.Viewport, "ui463-network-landed.png");
            Assert.IsFalse(board.View.Hand.Contains(Card));
            Assert.IsTrue(board.View.Players.Single(p => p.Side == Side.Shadow).TableCards.Contains(Card), "It stays on the table.");
            Assert.AreEqual(DieLocation.Spent, board.View.Dice.Single(d => d.Id == die.Id).Location);
        }
    }
}
