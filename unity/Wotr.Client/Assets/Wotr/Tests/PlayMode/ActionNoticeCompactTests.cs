using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-166 (SIGNALS_UX SG01–SG04): on the server's records the opponent's action reads as its result. The pennant's
    // tooltip is the heading and first line; the card it opens is compact and never scrolls; the played card's line
    // opens its preview and Full record opens the same action in the Log with every step.
    public sealed class ActionNoticeCompactTests
    {
        [UnityTest]
        public IEnumerator ArmyMoveReadsAsItsDestinationAndForce() => Wait(Result("ui42-moves", "Army move", "→"));

        [UnityTest]
        public IEnumerator AttackReadsAsItsBattle() => Wait(Result("ui42-battles", "Attack", "Battle at"));

        [UnityTest]
        public IEnumerator FellowshipMoveReadsAsTheHunt() => Wait(Result("ui42-hunts", "Fellowship moved · Hunt", ""));

        [UnityTest]
        public IEnumerator PlayedEventOpensItsCardAndFullRecordOpensTheLog() => Wait(PlayedEvent());

        private static async Task Result(string scenario, string header, string line)
        {
            using var room = await Connect(scenario, ScenarioKind.BaseAcceptance, openPanels: false);
            await ActionNoticeNetworkTests.Loaded(room);
            var client = room.Sh;
            var groups = client.Activity.Model.Groups;
            var group = groups.FirstOrDefault(g => g.Result.Header.StartsWith(header));
            Assert.IsNotNull(group, scenario + " · " + string.Join(" | ", groups.Select(g => g.Result.Header)));
            Assert.AreEqual(groups[0].Result.Tooltip, client.Board.Notifications.Stack.Root.tooltip, "The pennant names the newest result.");
            if (line.Length > 0)
                StringAssert.Contains(line, string.Join("\n", group.Result.Lines), scenario);
            await Open(room, client, group);
            AssertCompact(client.Board, scenario);
            PointerGestureEvents.ClickAt(client.Board.Log.Popup.Q<Button>("close-action-summary"));
            await room.Settle();
            Assert.IsFalse(Visible(client.Board.Log.Popup), "One click on the cross closes the card · " + scenario);
        }

        private static async Task PlayedEvent()
        {
            using var room = await Connect("grey", ScenarioKind.Character, openPanels: false);
            await ActionNoticeNetworkTests.Loaded(room);
            var client = room.Sh;
            var board = client.Board;
            var group = client.Activity.Model.Groups.FirstOrDefault(g => g.Result.CardId.Length > 0);
            Assert.IsNotNull(group, "The Free Peoples' Event reaches the Shadow as a notice with its card.");
            StringAssert.StartsWith("Event · " + group.Result.CardTitle, group.Result.Header);
            await Open(room, client, group);
            AssertCompact(board, "event");
            var card = board.Log.Popup.Q<Button>("action-result-card");
            Assert.IsTrue(Visible(card));
            Assert.AreEqual(group.Result.CardTitle, card.text);
            PointerGestureEvents.ClickAt(card);
            await room.Settle();
            Assert.AreEqual(group.Result.CardId, board.Cards.CardId, "The card's line opens its preview.");
            Assert.IsTrue(Visible(board.Log.Popup), "The pinned card stays beside the preview.");
            board.Cards.Close();
            await room.Settle();
            PointerGestureEvents.ClickAt(board.Log.Popup.Q<Button>("action-result-full"));
            await room.Until(() => board.Log.Card.Displayed?.Action.Id == group.Latest.ActionId && !board.Log.Card.Compact);
            await room.Settle();
            Assert.IsTrue(board.Log.IsOpen, "Full record opens the Log.");
            Assert.IsTrue(Visible(board.Log.Popup.Q("action-card-steps")), "The full record lists the steps.");
            Assert.IsFalse(Visible(board.Log.Popup.Q("action-result")));
        }

        private static async Task Open(MatchTestRoom room, MountedMatchClient client, ActionNoticeGroup group)
        {
            var board = client.Board;
            board.ActionHistory.Open(group.Latest.Request);
            await room.Until(() => board.Log.Card.Displayed?.Action.Id == group.Latest.ActionId && board.Log.Card.Compact);
            await room.Settle();
        }

        // About 360 points wide, at most three result lines, nothing that scrolls, Full record on the card.
        private static void AssertCompact(BoardScreen board, string label)
        {
            var popup = board.Log.Popup;
            Assert.IsTrue(Visible(popup), label);
            Assert.IsTrue(popup.ClassListContains("compact"), label);
            Assert.LessOrEqual(popup.resolvedStyle.width, 361, label);
            Assert.IsFalse(Visible(popup.Q("action-card-steps")), "No step list · " + label);
            Assert.IsEmpty(popup.Query<ScrollView>().ToList().Where(Visible), "Nothing scrolls · " + label);
            var lines = popup.Query<Label>(className: "action-result-line").ToList().Where(Visible).ToList();
            Assert.That(lines.Count, Is.InRange(1, 3), label);
            var full = popup.Q<Button>("action-result-full");
            Assert.IsTrue(Visible(full), label);
            Assert.IsTrue(popup.worldBound.Contains(full.worldBound.center), label);
            Assert.IsTrue(board.Root.worldBound.Contains(popup.worldBound.min) && board.Root.worldBound.Contains(popup.worldBound.max), label);
            AssertClearCross(popup, label);
        }

        // UI-177: a large, clear close cross in the card's top right corner.
        private static void AssertClearCross(VisualElement popup, string label)
        {
            var close = popup.Q<Button>("close-action-summary");
            Assert.IsTrue(Visible(close), label);
            Assert.GreaterOrEqual(close.resolvedStyle.width, 30, "The cross is 30 points wide · " + label);
            Assert.GreaterOrEqual(close.resolvedStyle.height, 30, "The cross is 30 points tall · " + label);
            Assert.GreaterOrEqual(close.Q("icon-glyph").resolvedStyle.width, 22, "The cross's sign is 22 points · " + label);
            Assert.Less(popup.worldBound.xMax - close.worldBound.xMax, 24, "The cross stands in the right corner · " + label);
            Assert.Less(close.worldBound.yMin - popup.worldBound.yMin, 24, "The cross stands at the top · " + label);
            Assert.AreSame(close, close.parent.Children().Last(), "Nothing stands right of the cross · " + label);
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
