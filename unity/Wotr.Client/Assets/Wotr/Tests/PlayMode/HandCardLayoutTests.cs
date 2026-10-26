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
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public sealed class HandCardLayoutTests
    {
        private static readonly (int Width, float Scale)[] Layouts = { (1366, 1f), (1366, 1.25f), (1920, 1f), (1920, 1.25f) };

        [UnityTest]
        public IEnumerator CardsUnrollUpwardOverTheOthersWithoutMovingThemInFourLayouts() => Wait(Unroll());

        [UnityTest]
        public IEnumerator OneClickOnTheUnrolledCardDiscardsIt() => Wait(OneClickDiscard());

        private static async Task Unroll()
        {
            using var room = await Connect("ui31-discard", ScenarioKind.BaseAcceptance);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var revision = board.View.Revision;
                foreach (var (width, scale) in Layouts)
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    try
                    {
                        client.Panel.targetTexture = texture;
                        client.Interface.Preferences.SetScale(scale);
                        await room.Settle();
                        await room.Settle();
                        await CheckLayout(room, client, texture, $"{board.View.You}-{width}-{scale:0.00}");
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        client.Interface.Preferences.SetScale(1);
                        texture.Release();
                        Object.Destroy(texture);
                    }
                }
                Assert.AreEqual(revision, board.View.Revision, "Reading cards never sends a command.");
            }
        }

        private static async Task CheckLayout(MatchTestRoom room, MountedMatchClient client, RenderTexture texture, string label)
        {
            var board = client.Board;
            Assert.IsNull(board.Edges.Right.Q("edge-card-header"), "No Cards header · " + label);
            Assert.IsFalse(Visible(board.Edges.Right.Q<Button>("help-for-cards")), "No help link above the hand · " + label);
            Assert.IsFalse(Visible(board.HandDrawer.Tab), label);
            Assert.IsTrue(board.HandDrawer.IsOpen && board.HandDrawer.Docked, label);
            var slots = board.Hand.Query<Button>(className: "hand-card").ToList();
            Assert.Greater(slots.Count, 2, label);
            var before = slots.ToDictionary(slot => slot.name, slot => slot.worldBound);
            foreach (var slot in slots)
            {
                Assert.That(Texture(slot.Q("hand-card-face")), Does.StartWith("parchment"), slot.name);
                var title = slot.Q<Label>("hand-card-title");
                Assert.LessOrEqual(title.worldBound.yMax, slot.worldBound.yMax + 1, "Collapsed title stays on its card · " + label);
            }
            var bottom = slots.OrderByDescending(slot => slot.worldBound.yMin).ThenBy(slot => slot.worldBound.xMin).First();
            var above = slots.Where(slot => slot.worldBound.yMax <= bottom.worldBound.yMin + 1)
                .OrderBy(slot => Mathf.Abs(slot.worldBound.xMin - bottom.worldBound.xMin)).ThenByDescending(slot => slot.worldBound.yMin).First();
            CardGestures.Move(bottom);
            var face = bottom.Q("hand-card-face");
            await Eventually(() => board.ExpandedCard == bottom.name && face.worldBound.yMin < bottom.worldBound.yMin - 40 &&
                Mathf.Abs(face.worldBound.width - bottom.parent.worldBound.width) < 1.5f);
            Assert.AreEqual(bottom.name, board.ExpandedCard, label);
            Assert.That(face.worldBound.yMax, Is.EqualTo(bottom.worldBound.yMax).Within(1.5f), "The bottom edge stays in place · " + label);
            Assert.Less(face.worldBound.yMin, bottom.worldBound.yMin - 40, "The card unrolls upward · " + label);
            Assert.That(face.worldBound.width, Is.EqualTo(bottom.parent.worldBound.width).Within(1.5f), "Full width for reading · " + label);
            Assert.AreEqual(bottom.parent.childCount - 1, bottom.parent.IndexOf(bottom), "Drawn over the cards above · " + label);
            foreach (var slot in slots)
            {
                Assert.That(slot.worldBound.position.x, Is.EqualTo(before[slot.name].position.x).Within(.5f), "No card moves · " + slot.name);
                Assert.That(slot.worldBound.position.y, Is.EqualTo(before[slot.name].position.y).Within(.5f), "No card moves · " + slot.name);
            }
            var full = bottom.Q("hand-card-full");
            Assert.LessOrEqual(full.worldBound.height, face.worldBound.height + 1, "The whole text is readable · " + label);
            Assert.AreEqual(CardLibrary.Title(bottom.name), bottom.Q<Label>("hand-card-full-title").text);
            Assert.IsNotEmpty(bottom.Q<Label>("hand-card-event").text);
            var duration = face.resolvedStyle.transitionDuration.FirstOrDefault();
            Assert.LessOrEqual(duration.value, duration.unit == TimeUnit.Millisecond ? 200 : .2f, "Fast unrolling · " + label);
            var picked = board.Root.panel.Pick(above.worldBound.center);
            Assert.IsTrue(picked == bottom || bottom.Contains(picked), "The unrolled card keeps the pointer over the card it covers · " + label);
            LayoutTests.Save(texture, $"ui91-hand-{label}.png");
            CardGestures.Move(above);
            await Task.Delay(260);
            Assert.AreEqual(bottom.name, board.ExpandedCard, "Reading over a covered card keeps the unrolled card · " + label);
            CardGestures.Move(board.Map);
            await Eventually(() => board.ExpandedCard.Length == 0 && Mathf.Abs(face.worldBound.height - bottom.worldBound.height) < 1.5f);
            Assert.IsEmpty(board.ExpandedCard, "Leaving the card rolls it back · " + label);
            Assert.That(face.worldBound.height, Is.EqualTo(bottom.worldBound.height).Within(1.5f), label);
            CardGestures.Move(above);
            await Eventually(() => board.ExpandedCard == above.name);
            Assert.AreEqual(above.name, board.ExpandedCard, "A visible card unrolls when the pointer reaches it · " + label);
            CardGestures.Move(board.Map);
            await Eventually(() => board.ExpandedCard.Length == 0);
        }

        // UI-142: a discard of the excess hand is one click, even on the unrolled part above the card's own slot.
        private static async Task OneClickDiscard()
        {
            using var fpBoundary = new UI10HttpBoundary();
            using var room = await Connect("ui31-discard", ScenarioKind.BaseAcceptance, fpHandler: fpBoundary);
            var fp = room.Fp;
            var board = fp.Board;
            fpBoundary.MatchPath = "/api/matches/" + board.View.MatchId;
            var revision = board.View.Revision;
            var id = board.View.Hand[0];
            var card = board.Hand.Q<Button>(id);
            CardGestures.Move(card);
            await Task.Delay(260);
            var face = card.Q("hand-card-face");
            Assert.Less(face.worldBound.yMin, card.worldBound.yMin - 20, "The card is unrolled for reading.");
            StringAssert.StartsWith("Click to discard", card.Q<Label>("hand-card-hint").text);
            card.Focus();
            NoFocusOutlineTests.Key(card, KeyCode.A);
            await room.Settle();
            NoFocusOutlineTests.NoOutline(board.Root.panel.visualTree, "on a focused card (UI-191)");
            Assert.AreEqual(0, fpBoundary.Commands, "Reading and focusing never send a command.");
            var above = new Vector2(card.worldBound.center.x, card.worldBound.yMin - 12);
            var stood = face.worldBound;
            ClickPoint(board, above);
            // UI-326: two card backs dissolve where the discarded card stood, as soon as the view without it arrives (the
            // opponent's board still presents the discard, so both revisions are awaited later).
            await room.Until(() => board.View.Revision == revision + 1);
            Assert.AreEqual(2, board.Dissolve.Shown, "Two backs dissolve.");
            var back = board.Dissolve.Root.Q(className: CardDissolve.BackClass);
            Assert.Less(Vector2.Distance(back.worldBound.center, stood.center), stood.height, "Where the card stood.");
            await Task.Delay(120);
            Assert.IsTrue(back.ClassListContains(CardDissolve.DissolvingClass), "It is dissolving.");
            LayoutTests.Save(fp.Viewport, "ui326-discard.png");
            // The opponent sees the backs dissolve out of the Free Peoples' hand count as it drops.
            var opponent = room.Sh.Board;
            await room.Until(() => opponent.Dissolve.Shown > 0);
            var count = opponent.TopBar.Opponent.Root.Q("opponent-cards").worldBound;
            var drop = opponent.Dissolve.Root.Q(className: CardDissolve.BackClass).worldBound;
            Assert.Less(Vector2.Distance(drop.center, count.center), 40, "At the opponent's hand count.");
            LayoutTests.Save(room.Sh.Viewport, "ui326-opponent-discard.png");
            await room.At(revision + 1);
            Assert.AreEqual(1, fpBoundary.Commands, "One click discards the card.");
            CollectionAssert.AreEqual(new[] { id }, fpBoundary.LastCommand.Payload.Ids);
            Assert.AreEqual(7, board.View.Hand.Count);
            await Task.Delay((int)CardDissolve.Milliseconds + 300);
            Assert.AreEqual(0, board.Dissolve.Shown, "Gone after the animation.");
            Assert.AreEqual(0, opponent.Dissolve.Shown);
        }

        // Waits for an animated state instead of a fixed delay; heavy layouts can render slower than the 140 ms transition.
        private static async Task Eventually(System.Func<bool> condition, int milliseconds = 1500)
        {
            for (var waited = 0; waited < milliseconds && !condition(); waited += 40)
                await Task.Delay(40);
            await Task.Delay(40);
        }

        private static void ClickPoint(BoardScreen board, Vector2 point)
        {
            var root = board.Root.panel.visualTree;
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = point }))
                root.SendEvent(move);
            var picked = board.Root.panel.Pick(point);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0, mousePosition = point, clickCount = 1 }))
                picked.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0, mousePosition = point, clickCount = 1 }))
                picked.SendEvent(up);
        }

        private static string Texture(VisualElement element) => element.resolvedStyle.backgroundImage.texture?.name;

        private static bool Visible(VisualElement element)
        {
            if (element == null || element.panel == null || element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return true;
        }
    }
}
