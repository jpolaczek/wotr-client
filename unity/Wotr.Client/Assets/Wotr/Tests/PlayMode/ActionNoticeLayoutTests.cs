using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-99/UI-100: opponent notices as a stack of small pennants, read newest first, with the action card
    // on parchment in the centre of the board. The log icon beside the stack opens the history list.
    // Reading is local; the map viewport and the game do not change.
    public sealed class ActionNoticeLayoutTests
    {
        // Pennant, two peeking pennants and the seal: an icon, not a strip.
        private const float IconStackWidth = 44;

        [UnityTest]
        public IEnumerator PennantStackReadsNewestFirstAndCentresParchmentCardsInFourLayouts() => Wait(Layouts());

        private static async Task Layouts()
        {
            using var room = await Connect("ui41-history", ScenarioKind.BaseAcceptance, openPanels: false);
            await ActionNoticeNetworkTests.Loaded(room);
            var client = room.Fp;
            var board = client.Board;
            var revision = board.View.Revision;
            Assert.IsNull(board.Root.Q(className: "map-controls"), "UI-100 removed the map control row.");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var texture = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    texture.Create();
                    try
                    {
                        client.Panel.targetTexture = texture;
                        client.Panel.scale = scale;
                        board.Windows.Clear();
                        await room.Settle();
                        await Check(room, client, texture, $"{width}-{scale:0.00}");
                    }
                    finally
                    {
                        board.Log.CloseCard();
                        board.Log.Close();
                        client.Panel.targetTexture = client.Viewport;
                        texture.Release();
                        Object.Destroy(texture);
                    }
                }
            Assert.AreEqual(revision, board.View.Revision, "Reading notices sends no command.");
        }

        private static async Task Check(MatchTestRoom room, MountedMatchClient client, RenderTexture texture, string label)
        {
            var board = client.Board;
            var bar = board.Notifications;
            var stack = bar.Stack;
            var groups = client.Activity.Model.Groups;
            Assert.GreaterOrEqual(groups.Count, 3, "The fixture keeps several unread notices · " + label);
            Assert.AreEqual(1, bar.Root.Query<Button>(className: ActionNoticeStack.ClassName).ToList().Count, "One stack, not a row of tabs · " + label);
            Assert.AreEqual("action-notice-" + groups[0].Latest.Id, stack.Root.name, "The newest notice is in front · " + label);
            Assert.AreEqual(groups.Count, stack.Count, label);
            Assert.AreEqual(groups.Count.ToString(), stack.Root.Q<Label>("notice-count").text, "The seal counts the stack · " + label);
            Assert.IsTrue(Visible(stack.Root.Q("notice-layer-1")) && Visible(stack.Root.Q("notice-layer-2")), "Older pennants peek out · " + label);
            // The pointer may still rest on the stack from the previous layout: the hover variant is the same enamel.
            Assert.That(Texture(stack.Root.Q("notice-pennant")), Does.StartWith("pennant-shadow"), "The pennant wears the opponent's enamel · " + label);
            Assert.AreEqual("wax-seal", Texture(stack.Root.Q("notice-seal")), label);
            Assert.LessOrEqual(stack.Root.worldBound.width, IconStackWidth, "A small icon, not a long banner · " + label);
            Assert.IsEmpty(stack.Root.Query<Label>().ToList().Where(l => l.name != "notice-count"), "The pennant carries no text · " + label);
            // UI-166: the newest action's result — heading and first line — without the side the pennant already shows.
            Assert.AreEqual(groups[0].Result.Tooltip, stack.Root.tooltip, "The tooltip names the newest result · " + label);
            Assert.LessOrEqual(stack.Root.tooltip.Split('\n').Length, 2, label);
            StringAssert.DoesNotContain(BoardScreen.SideName(board.View.You == Side.Shadow ? Side.FreePeoples : Side.Shadow),
                stack.Root.tooltip, label);
            Inside(stack.Root.worldBound, board.Root.worldBound, label);
            CheckLogIcon(board, label);
            LayoutTests.Save(texture, $"ui100-stack-{label}.png");

            var count = client.Activity.Model.StepCount;
            var map = board.Map.worldBound;
            var pan = board.Map.Camera.Pan;
            ActionNoticeNetworkTests.Hover(stack.Root);
            await Task.Delay(300, room.Token);
            Assert.AreEqual(count, client.Activity.Model.StepCount, "Hovering the stack reads nothing · " + label);
            Assert.AreEqual(DisplayStyle.None, board.Log.Popup.resolvedStyle.display, label);

            var first = groups[0];
            var second = groups[1];
            PointerGestureEvents.ClickAt(stack.Root);
            await room.Settle();
            Assert.AreEqual("action-notice-" + second.Latest.Id, stack.Root.name, "The opened pennant is gone at once · " + label);
            Assert.AreEqual(first.Latest.ActionId, board.Log.SelectedId, label);
            await room.Until(() => !client.Activity.Model.Groups.Any(g => g.Latest.Id == first.Latest.Id));
            await room.Settle();
            CheckCard(board, label);
            LayoutTests.Save(texture, $"ui100-card-{label}.png");

            PointerGestureEvents.ClickAt(stack.Root);
            await room.Settle();
            Assert.AreEqual(second.Latest.ActionId, board.Log.SelectedId, "A newer click replaces the previous card · " + label);
            Assert.AreEqual(1, board.Root.Query(className: "action-summary").ToList().Count(Visible), "One card at a time · " + label);
            await room.Until(() => !client.Activity.Model.Groups.Any(g => g.Latest.Id == second.Latest.Id));
            await room.Settle();
            CheckCentred(board, "Second notice · " + label);
            Assert.AreEqual(map, board.Map.worldBound, "Reading a notice cannot resize the map · " + label);
            Assert.IsFalse(board.Log.IsOpen, "The log list stays closed · " + label);
            board.Map.Camera.Pan = pan;
            board.Map.Refresh();
        }

        // The scroll icon sits in the notice strip before the stack and toggles the history list.
        private static void CheckLogIcon(BoardScreen board, string label)
        {
            var log = board.Log.Toggle;
            Assert.AreSame(board.Notifications.Root, log.parent, label);
            Assert.IsTrue(Visible(log) && log.enabledInHierarchy, label);
            Assert.AreEqual("log", Texture(log.Q(className: "notice-log-icon")), label);
            Assert.LessOrEqual(log.worldBound.xMax, board.Notifications.Stack.Root.worldBound.xMin + 1, "Log icon before the stack · " + label);
            Assert.LessOrEqual(log.worldBound.width, 30, label);
            Assert.IsEmpty(log.text, label);
            PointerGestureEvents.ClickAt(log);
            Assert.IsTrue(board.Log.IsOpen, "The icon opens the history list · " + label);
            PointerGestureEvents.ClickAt(log);
            Assert.IsFalse(board.Log.IsOpen, "A second click closes it · " + label);
        }

        // The action card on parchment with the lacquer header of the board's panels, sized to its content
        // and centred on the board whatever the notice (UI-100).
        private static void CheckCard(BoardScreen board, string label)
        {
            var card = board.Log.Popup;
            Assert.IsTrue(Visible(card), label);
            Assert.AreEqual("parchment", Texture(card), "The card is parchment · " + label);
            Assert.AreEqual("panel-header", Texture(card.Q(className: "window-header")), label);
            Inside(card.worldBound, board.Root.worldBound, label);
            Assert.LessOrEqual(card.resolvedStyle.height, 460.5f, label);
            // UI-166: the notice's card is the compact result: no step list, nothing scrolls, Full record on it.
            Assert.IsTrue(board.Log.Card.Compact, label);
            Assert.LessOrEqual(card.resolvedStyle.width, 361, label);
            Assert.IsFalse(Visible(card.Q("action-card-steps")), label);
            Assert.IsEmpty(card.Query<ScrollView>().ToList().Where(Visible), label);
            Assert.That(card.Query<Label>(className: "action-result-line").ToList().Count(Visible), Is.InRange(1, 3), label);
            Inside(card.Q<Button>("action-result-full").worldBound, card.worldBound, label);
            foreach (var labelElement in board.Log.Card.Root.Query<Label>().ToList().Where(Visible))
            {
                var color = labelElement.resolvedStyle.color;
                Assert.Less(color.r + color.g + color.b, 1.5f, "Dark ink on parchment: " + labelElement.name + " · " + label);
            }
            CheckCentred(board, label);
        }

        private static void CheckCentred(BoardScreen board, string label)
        {
            var card = board.Log.Popup.worldBound;
            var area = board.Log.Popup.parent.worldBound;
            var chrome = board.Notifications.Root.worldBound.yMax;
            Assert.AreEqual(area.center.x, card.center.x, 2, "The card is centred horizontally · " + label);
            Assert.GreaterOrEqual(card.yMin + 1, chrome, "The card leaves the notice strip readable · " + label);
            if (card.yMin > chrome + 6)
                Assert.AreEqual(area.center.y, card.center.y, 2, "The card is centred vertically · " + label);
        }

        private static string Texture(VisualElement element) => element.resolvedStyle.backgroundImage.texture?.name;

        private static void Inside(Rect inner, Rect outer, string label)
        {
            Assert.GreaterOrEqual(inner.xMin + 1, outer.xMin, label);
            Assert.LessOrEqual(inner.xMax, outer.xMax + 1, label);
            Assert.GreaterOrEqual(inner.yMin + 1, outer.yMin, label);
            Assert.LessOrEqual(inner.yMax, outer.yMax + 1, label);
        }

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
