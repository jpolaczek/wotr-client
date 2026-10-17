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
    public sealed class BattleResolutionNetworkTests
    {
        [UnityTest]
        public IEnumerator BothRerollSelectionsStayPrivateUntilResolutionAndSurviveReconnect() => Wait(Rerolls());

        [UnityTest]
        public IEnumerator OrderedLossPreviewReusesCasualtyAndLostReplyRecoversWithoutSecondCommand() => Wait(Losses());

        private static async Task Rerolls()
        {
            using var room = await Connect("ui45-rerolls", ScenarioKind.BaseAcceptance);
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            var option = board.View.Options.Single(o => o.Kind == CommandKind.RerollBattle);
            Assert.AreEqual(1, option.Maximum);
            Assert.AreEqual(2, option.EligibleIds.Count);
            await room.Until(() => Confirm(board).enabledInHierarchy);
            Assert.AreEqual("Keep roll", Confirm(board).text);
            var picked = option.EligibleIds[0];
            var first = board.View.Battle.Attacker.Dice.Single(d => d.Id == picked).Result;
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("battle-die-" + picked));
            await room.Until(() => Confirm(board).enabledInHierarchy && Confirm(board).text == "Reroll selected");
            Assert.IsFalse(board.Tray.Q<Button>("battle-die-" + option.EligibleIds[1]).enabledInHierarchy);
            Assert.IsTrue(board.Tray.Query<Button>(className: "battle-die").ToList()
                .Where(b => !option.EligibleIds.Contains(b.name.Substring("battle-die-".Length))).All(b => !b.enabledInHierarchy));
            Assert.AreEqual(revision, board.View.Revision);
            board.Battles.Close();
            board.Battles.Open();
            var connections = room.Fp.Connected;
            room.Fp.Connection.Reconnect();
            await room.Until(() => room.Fp.Connected > connections);
            await room.Settle();
            Assert.IsTrue(board.Tray.Q<Button>("battle-die-" + picked).ClassListContains("selected"));
            await Layouts(room, room.Fp, "rerolls");
            await Commit(room, room.Fp);
            Assert.IsEmpty(room.Sh.Board.View.Battle.Attacker.OwnRerollSelection);
            Assert.IsTrue(room.Sh.Board.View.Battle.Attacker.Dice.All(d => !d.Reroll.HasValue));
            var privateLog = await Log(room, room.Sh);
            Assert.IsFalse(privateLog.Steps.Any(s => s.Revision == revision + 1 && s.Command == CommandKind.RerollBattle));
            Assert.IsFalse(privateLog.Steps.Any(s => s.Facts.Targets.Contains(picked)));
            Assert.AreEqual("Keep roll", Confirm(room.Sh.Board).text);
            await Commit(room, room.Sh);
            var result = board.View.Battle.Attacker.Dice.Single(d => d.Id == picked);
            Assert.AreEqual(first, result.Result);
            Assert.IsTrue(result.Reroll.HasValue);
            Assert.AreEqual(1, board.View.Battle.Attacker.Dice.Count(d => d.Reroll.HasValue));
            Assert.IsTrue(board.View.Battle.Defender.Dice.All(d => !d.Reroll.HasValue));
            Assert.AreEqual(2, board.Tray.Q<Button>("battle-die-" + picked).Query<DiePips>().ToList().Count);
            Assert.AreEqual(1, board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Resolving));
        }

        private static async Task Losses()
        {
            var boundary = new UI10HttpBoundary();
            using var room = await Connect("ui45-losses", ScenarioKind.BaseAcceptance, fpHandler: boundary);
            var board = room.Fp.Board;
            boundary.MatchPath = "/api/matches/" + board.View.MatchId;
            var revision = board.View.Revision;
            var elite = board.View.Battle.Attacker.Units.Single(u => u.Kind == UnitKind.Elite).Id;
            var regular = board.View.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Regular).Id;
            await ChooseLoss(room, elite);
            await ChooseLoss(room, regular);
            await room.Until(() => Confirm(board).enabledInHierarchy);
            StringAssert.Contains("No regular available", board.Tray.Q<Label>(className: "draft-status").text);
            await Layouts(room, room.Fp, "missing-regular");
            await ChooseLoss(room, elite); // Remove: too many hits.
            Assert.IsFalse(Confirm(board).enabledInHierarchy);
            await ChooseLoss(room, elite); // Keep.
            await ChooseLoss(room, elite); // Downgrade last: now the regular supplies it.
            await room.Until(() => Confirm(board).enabledInHierarchy);
            StringAssert.Contains("Elite replaced by a regular", board.Tray.Q<Label>(className: "draft-status").text);
            StringAssert.Contains("Regular from casualties", board.Tray.Q<Button>("loss-" + elite).tooltip);
            Assert.AreEqual("Assigned 2 / 2 · Remaining 0", board.Tray.Q<Label>("battle-loss-budget").text);
            Assert.AreEqual(revision, board.View.Revision);
            await Layouts(room, room.Fp, "ordered-losses");
            boundary.HoldNextCommand = true;
            var apply = Confirm(board);
            PointerGestureEvents.ClickAt(apply);
            PointerGestureEvents.ClickAt(apply, 2);
            await room.Until(() => boundary.CommandHeld);
            await room.At(revision + 1);
            Assert.AreEqual(1, boundary.Commands);
            boundary.ReleaseCommand();
            await room.Until(() => Recovery(board)?.enabledInHierarchy == true);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery(board));
            await room.Until(() => boundary.ReceiptLookups == 1 && Recovery(board) == null);
            Assert.AreEqual(1, boundary.Commands);
            Assert.AreEqual("casualties", board.View.Units.Single(u => u.Id == elite).Location);
            Assert.AreEqual("minas-tirith", board.View.Units.Single(u => u.Id == regular).Location);
            var log = await Log(room, room.Sh);
            var details = log.Steps.Single(s => s.Revision == revision + 1 && s.Kind == "BattleLosses").Facts.LossDetails;
            CollectionAssert.AreEqual(new[] { regular, elite }, details.Select(l => l.Figure.Id),
                "Recorded: " + string.Join(", ", details.Select(l => l.Figure.Id + " " + l.Operation)));
            Assert.AreEqual(regular, details[1].ReplacementId);
            Assert.AreEqual("casualties", details[1].ReplacementFrom);
            Assert.AreEqual(2, details.Sum(l => l.Hits));
            await room.Direct(room.Fp, "action-StopBattle");
            Assert.AreEqual(1, board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
        }

        private static async Task ChooseLoss(MatchTestRoom room, string id)
        {
            PointerGestureEvents.ClickAt(room.Fp.Board.Tray.Q<Button>("loss-" + id));
            await room.Settle();
        }

        private static async Task Commit(MatchTestRoom room, MountedMatchClient client)
        {
            await room.Until(() => Confirm(client.Board)?.enabledInHierarchy == true);
            await room.Settle();
            var revision = client.Board.View.Revision;
            PointerGestureEvents.ClickAt(Confirm(client.Board));
            await room.At(revision + 1);
        }

        private static Button Confirm(BoardScreen board) => board.Tray.Q<Button>("confirm-decision");
        private static Button Recovery(BoardScreen board) => board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");
        private static Task<ActionLogPage> Log(MatchTestRoom room, MountedMatchClient client) =>
            client.Api.Get<ActionLogPage>("api/matches/" + client.Board.View.MatchId + "/actions?limit=50", room.Token);

        private static async Task Layouts(MatchTestRoom room, MountedMatchClient client, string phase)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    try
                    {
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        await room.Settle();
                        var board = client.Board;
                        Assert.Greater(board.Map.worldBound.height, board.Root.worldBound.height / 2);
                        foreach (var control in board.Tray.Query<Button>().ToList().Where(b => b.enabledInHierarchy && b.worldBound.height > 0))
                        {
                            var hit = control.panel.Pick(control.worldBound.center);
                            Assert.IsTrue(hit == control || control.Contains(hit), control.name + " covered by " + hit?.name);
                            Assert.LessOrEqual(control.worldBound.yMax, board.Map.worldBound.yMax + 1, control.name);
                        }
                        LayoutTests.AssertNoDecisionScroll(board);
                        LayoutTests.Save(target, $"ui45-{phase}-{width}-{scale:0.00}.png");
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        client.Panel.scale = 1;
                        target.Release();
                        Object.Destroy(target);
                    }
                }
            await room.Settle();
        }
    }
}
