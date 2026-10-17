using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    internal static class FullGameDesktopChecks
    {
        public static async Task Configure(MatchTestRoom room)
        {
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                client.Interface.Preferences.SetScale(1.25f);
                client.Interface.Preferences.SetReducedMotion(true);
            }
            await room.Settle();
            foreach (var client in new[] { room.Fp, room.Sh })
                client.Board.Map.Fit();
            await room.Settle();
        }

        public static void Layout(MountedMatchClient client)
        {
            FellowshipLayoutTests.Check(client.Board);
            ExpansionMatrixLayoutTests.ResourcesFit(client.Board);
            foreach (var button in client.Board.Tray.Query<Button>().ToList())
                Assert.IsTrue(button.resolvedStyle.transitionDuration.All(t => t.value == 0));
        }

        public static async Task FinishInspection(MatchTestRoom room, MountedMatchClient actor)
        {
            var card = actor.Board.Cards;
            if (card.Root.resolvedStyle.display == DisplayStyle.None)
                return;
            var revision = actor.Board.View.Revision;
            await ExposeClose(room, card);
            PointerGestureEvents.ClickAt(card.Root.Q<Button>("close-card"));
            await room.Settle();
            Assert.AreEqual(DisplayStyle.None, card.Root.resolvedStyle.display,
                "The active player finishes their previous waiting-time inspection before using map targets.");
            Assert.IsFalse(card.Pinned);
            Assert.AreEqual(revision, actor.Board.View.Revision, "Closing an inspection must not commit a decision.");
        }

        private static async Task ExposeClose(MatchTestRoom room, CardInspector card)
        {
            var close = card.Root.Q<Button>("close-card");
            var picked = close.panel.Pick(close.worldBound.center);
            if (picked == close || close.Contains(picked))
                return;
            var bounds = card.Root.worldBound;
            var wasPinned = card.Pinned;
            var header = card.Root.Q(className: "card-header");
            var point = ExposedHandle(header);
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point }))
                card.Root.panel.visualTree.SendEvent(move);
            picked = header.panel.Pick(point);
            Assert.IsTrue((picked == header || header.Contains(picked)) && FloatingWindow.IsHandle(picked),
                "The inspected card needs an exposed draggable header before its covered Close can be reached.");
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0,
                mousePosition = point, clickCount = 1 }))
                picked.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0,
                mousePosition = point, clickCount = 1 }))
                picked.SendEvent(up);
            await room.Settle();
            Assert.AreEqual(bounds, card.Root.worldBound, "Raising the card must not move the saved window.");
            Assert.AreEqual(wasPinned, card.Pinned, "Raising the card must preserve its inspection state.");
        }

        private static Vector2 ExposedHandle(VisualElement header)
        {
            var bounds = header.worldBound;
            for (var x = bounds.xMin + 8; x < bounds.xMax - 8; x += 16)
            {
                var point = new Vector2(x, bounds.center.y);
                var picked = header.panel.Pick(point);
                if ((picked == header || header.Contains(picked)) && FloatingWindow.IsHandle(picked))
                    return point;
            }
            Assert.Fail("The pinned card has no exposed draggable header; do not bypass window occlusion.");
            return default;
        }

        public sealed class Observer
        {
            private readonly MountedMatchClient client;
            private readonly Vector2 pan;
            private readonly float zoom;
            private readonly string card;

            public Observer(MountedMatchClient client)
            {
                this.client = client;
                pan = client.Board.Map.Camera.Pan;
                zoom = client.Board.Map.Camera.Zoom;
                card = client.Board.View.Hand.FirstOrDefault();
                if (card != null)
                    client.Board.Cards.Show(card, true);
            }

            public void Check()
            {
                Assert.AreEqual(pan, client.Board.Map.Camera.Pan, "Opponent's decision cannot pan this player's map.");
                Assert.AreEqual(zoom, client.Board.Map.Camera.Zoom, "Opponent's decision cannot zoom this player's map.");
                if (card != null)
                {
                    Assert.AreEqual(card, client.Board.Cards.CardId);
                    Assert.IsTrue(client.Board.Cards.Pinned);
                }
            }
        }
    }
}
