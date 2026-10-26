using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal static class CardGestures
    {
        // UI-194: one click plays a card that has one way to pay.
        public static void PlayClick(MountedMatchClient client, string id) => CardNetworkTests.Click(Source(client, id));

        // UI-142: a card asked for by an excess-hand discard goes with one click.
        public static void Discard(MountedMatchClient client, string id) => CardNetworkTests.Click(Source(client, id));

        public static async Task Play(MatchTestRoom room, MountedMatchClient client, string id)
        {
            await Open(client);
            var revision = client.Board.View.Revision;
            PlayClick(client, id);
            await room.At(revision + 1);
        }

        public static async Task Draw(MatchTestRoom room, MountedMatchClient client, string deck)
        {
            await Open(client);
            var revision = client.Board.View.Revision;
            CardNetworkTests.Click(client.Board.Decks.Root.Q<Button>("deck-" + deck));
            await room.At(revision + 1);
        }

        public static async Task Open(MountedMatchClient client)
        {
            if (client.Board.View.Decision?.Kind == DecisionKind.CombatCard)
            {
                if (!client.Board.Battles.IsOpen) PointerGestureEvents.ClickAt(client.Board.Console.Status.Plaque);
                await Task.Delay(100);
                return;
            }
            await OpenHand(client);
        }

        public static async Task Preview(MountedMatchClient client, string id)
        {
            await Open(client);
            var combat = client.Board.View.Decision?.Kind == DecisionKind.CombatCard;
            if (combat)
                await OpenHand(client);
            var card = client.Board.Hand.Q<Button>(id);
            var inspected = client.Board.Cards.CardId;
            Move(card);
            await Task.Delay(280);
            Assert.AreEqual(inspected, client.Board.Cards.CardId, "Hover cannot replace the inspected card.");
            Assert.AreEqual(id, client.Board.ExpandedCard, "UI-91: hover unrolls the card in place.");
            PointerGestureEvents.ClickAt(card);
            await Task.Delay(100);
            if (combat)
            {
                client.Board.HandDrawer.Close();
                await Task.Delay(100);
            }
        }

        private static async Task OpenHand(MountedMatchClient client)
        {
            var drawer = client.Board.HandDrawer;
            var wasOpen = drawer.IsOpen;
            Move(drawer.Tab);
            await Task.Delay(280);
            Assert.AreEqual(wasOpen, drawer.IsOpen, "Hover must not change whether the hand is open.");
            if (!wasOpen)
            {
                PointerGestureEvents.ClickAt(drawer.Tab);
                await Task.Delay(100);
            }
            Assert.IsTrue(drawer.IsOpen, "The hand opens after an explicit click or its required excess-hand decision.");
        }

        public static Button Source(MountedMatchClient client, string id) =>
            client.Board.View.Decision?.Kind == DecisionKind.CombatCard
                ? client.Board.Tray.Q<Button>("combat-choice-" + id) : client.Board.Hand.Q<Button>(id);

        public static void Move(VisualElement target)
        {
            Assert.IsNotNull(target);
            Move(target, target.worldBound.center);
        }

        // The pointer moves to a point of the panel that holds the element, whatever lies there.
        public static void Move(VisualElement inPanel, Vector2 position)
        {
            var root = inPanel.panel.visualTree;
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = position });
            root.SendEvent(move);
        }
    }
}
