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
    public sealed class BattleEndingNetworkTests
    {
        [UnityTest]
        public IEnumerator RetreatAndPartialAdvanceRecoverLostReceiptWithOneCostAndActualControl() => Wait(Advance());
        [UnityTest]
        public IEnumerator SiegeEliteMissingReplacementAndKingsRegularHaveDistinctCosts() => Wait(Siege());
        [UnityTest]
        public IEnumerator ShelterAndDeclinedAdvanceOpenACompletedReportWithoutFabricatedRound() => Wait(Shelter());
        [UnityTest]
        public IEnumerator FullReportLoadsTwoRoundsAndKeepsItsSnapshotThroughReconnectInFourLayouts() => Wait(Report());

        private static async Task Advance()
        {
            var boundary = new UI10HttpBoundary();
            using var room = await Connect("ui46-retreat", ScenarioKind.BaseAcceptance, fpHandler: boundary);
            boundary.MatchPath = "/api/matches/" + room.Fp.Board.View.MatchId;
            await Direct(room, room.Fp, "action-ContinueBattle");
            var defender = room.Sh.Board;
            var revision = defender.View.Revision;
            Assert.IsNull(defender.Map.Q<Button>("target-minas-tirith"));
            defender.Map.Select("minas-tirith");
            await room.Settle();
            Assert.AreEqual(revision, defender.View.Revision);
            await Target(room, room.Sh, "pelargir");
            var board = room.Fp.Board;
            var battle = board.View.Battle.Id;
            var before = board.View.Control["lossarnach"];
            var regular = board.View.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Regular).Id;
            PointerGestureEvents.ClickAt(board.Tray.Q<Button>("figure-" + regular));
            await room.Settle();
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
            Assert.AreEqual(before, board.View.Control["lossarnach"]);
            var commands = boundary.Commands;
            boundary.HoldNextCommand = true;
            await Target(room, room.Fp, "lossarnach");
            await Recover(room, room.Fp, boundary, commands + 1);
            Assert.AreEqual("lossarnach", board.View.Units.Single(u => u.Id == regular).Location);
            Assert.AreEqual(3, board.View.Units.Count(u => u.Side == Side.FreePeoples && u.Location == "minas-tirith"));
            Assert.AreEqual(Side.FreePeoples, board.View.Control["lossarnach"]);
            Assert.AreEqual(1, board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
            await OpenLast(room, room.Fp);
            Assert.AreEqual(battle, board.BattleReport.Displayed.Id);
            Assert.AreEqual(BattleReportStatus.Completed, board.BattleReport.Displayed.Status);
            var advance = board.BattleReport.Displayed.Steps.SelectMany(s => s.Movements).Single(m => m.Kind == ActionMovementKind.Advance);
            CollectionAssert.AreEqual(new[] { regular }, advance.Participants.Select(p => p.Id));
            Assert.AreEqual(board.View.Control["lossarnach"], board.BattleReport.Displayed.Place.Controller);
        }

        private static async Task Siege()
        {
            foreach (var scenario in new[] { "ui46-siege", "ui46-no-regular", "ui46-weakness" })
            {
                var boundary = new UI10HttpBoundary();
                using var room = await Connect(scenario, ScenarioKind.BaseAcceptance, shHandler: boundary);
                var board = room.Sh.Board;
                boundary.MatchPath = "/api/matches/" + board.View.MatchId;
                Assert.AreEqual(BattleMode.Siege, board.View.Battle.Mode);
                Assert.AreEqual(6, board.View.Battle.Attacker.HitTarget);
                Assert.AreEqual(5, board.View.Battle.Defender.HitTarget);
                Assert.IsFalse(room.Fp.Board.View.Options.Any(o => o.Kind == CommandKind.RetreatBattle));
                var kind = scenario == "ui46-weakness" ? UnitKind.Regular : UnitKind.Elite;
                var id = board.View.Options.Single(o => o.Kind == CommandKind.ExtendSiege).EligibleIds
                    .First(candidate => board.View.Units.Single(u => u.Id == candidate).Kind == kind);
                var figure = board.Tray.Q<Button>("figure-" + id);
                await Layouts(room, room.Sh, "siege-" + kind, false);
                var revision = board.View.Revision;
                boundary.HoldNextCommand = true;
                // UI-142: one figure completes the extension, so its single click extends the siege at once.
                PointerGestureEvents.ClickAt(figure);
                await room.At(revision + 1);
                await Recover(room, room.Sh, boundary, 1);
                Assert.AreEqual("reserve", board.View.Units.Single(u => u.Id == id).Location);
                if (scenario == "ui46-no-regular")
                {
                    Assert.IsNull(board.View.Battle);
                    Assert.AreEqual(Side.FreePeoples, board.View.Control["minas-tirith"]);
                    Assert.AreEqual(1, board.View.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Spent));
                }
                else
                {
                    Assert.AreEqual(2, board.View.Battle.Round);
                    Assert.AreEqual(1, board.View.Dice.Count(d => d.Owner == Side.Shadow && d.Location == DieLocation.Resolving));
                }
            }
        }

        private static async Task Shelter()
        {
            using var room = await Connect("ui46-shelter", ScenarioKind.BaseAcceptance);
            await room.Attack(room.Sh, "osgiliath", "minas-tirith");
            await Direct(room, room.Fp, "action-RetreatIntoSiege");
            Assert.AreEqual(6, room.Fp.Board.View.Units.Count(u => u.Garrison && u.Location == "minas-tirith"));
            var battle = room.Sh.Board.View.Battle.Id;
            await Direct(room, room.Sh, "battle-stay");
            Assert.AreEqual(6, room.Fp.Board.View.Units.Count(u => u.Side == Side.FreePeoples && u.Location == "minas-tirith" && !u.Garrison));
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                await ActionContextNetworkTests.OpenLog(room, board);
                var action = board.Log.Model.Rows.First(r => r.Step == null && r.Group.Action.Context.BattleId == battle).Group.Action;
                await ActionContextNetworkTests.OpenEntry(room, board, action.Id);
                PointerGestureEvents.ClickAt(board.Log.Card.Archive.Root.Q<Button>("open-archived-battle"));
                await room.Settle();
                Assert.AreEqual(battle, board.BattleReport.Displayed.Id);
                Assert.AreEqual(BattleReportStatus.Completed, board.BattleReport.Displayed.Status);
                Assert.IsEmpty(board.BattleReport.Displayed.Rounds);
                Assert.AreEqual("No recorded Combat roll", board.BattleReport.Root.Q<Label>("battle-report-round").text);
                board.BattleReport.Close();
                board.Log.CloseCard();
                board.Log.Close();
            }
        }

        private static async Task Report()
        {
            using var room = await Connect("ui46-report", ScenarioKind.BaseAcceptance);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                var board = client.Board;
                var revision = board.View.Revision;
                await OpenLast(room, client);
                var displayed = board.BattleReport.Displayed;
                Assert.AreEqual(2, displayed.Rounds.Count);
                StringAssert.Contains("Round 2", board.BattleReport.Root.Q<Label>("battle-report-round").text);
                PointerGestureEvents.ClickAt(board.BattleReport.Root.Q<Button>("previous-battle-round"));
                await room.Settle();
                StringAssert.Contains("Round 1", board.BattleReport.Root.Q<Label>("battle-report-round").text);
                Assert.IsNull(board.BattleReport.Root.Q<Button>("confirm-decision"));
                Assert.IsTrue(board.BattleReport.Root.Query<Button>(className: "battle-die").ToList().All(b => b.userData == null));
                var connections = client.Connected;
                client.Connection.Reconnect();
                await room.Until(() => client.Connected > connections);
                Assert.AreSame(displayed, board.BattleReport.Displayed);
                Assert.AreEqual(revision, board.View.Revision);
                if (client == room.Fp) await Layouts(room, client, "report", true);
                board.BattleReport.Close();
            }
        }

        private static async Task OpenLast(MatchTestRoom room, MountedMatchClient client)
        {
            PointerGestureEvents.ClickAt(client.Board.BattleReport.Open);
            await room.Until(() => client.Board.BattleReport.Displayed != null);
            await room.Settle();
        }

        private static async Task Direct(MatchTestRoom room, MountedMatchClient client, string name)
        {
            var revision = client.Board.View.Revision;
            PointerGestureEvents.ClickAt(client.Board.Tray.Q<Button>(name));
            await room.At(revision + 1);
        }

        private static async Task Target(MatchTestRoom room, MountedMatchClient client, string id)
        {
            var revision = client.Board.View.Revision;
            client.Board.Map.Focus(id);
            await room.Settle();
            MapContourProbe.ClickRegion(client.Board.Map, id);
            await room.At(revision + 1);
        }

        private static async Task Recover(MatchTestRoom room, MountedMatchClient client, UI10HttpBoundary boundary, int expected)
        {
            await room.Until(() => boundary.CommandHeld);
            Assert.AreEqual(expected, boundary.Commands);
            boundary.ReleaseCommand();
            Button Recovery() => client.Board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");
            await room.Until(() => Recovery()?.enabledInHierarchy == true);
            await room.Settle();
            PointerGestureEvents.ClickAt(Recovery());
            await room.Until(() => boundary.ReceiptLookups == 1 && Recovery() == null);
            Assert.AreEqual(expected, boundary.Commands);
        }

        private static async Task Layouts(MatchTestRoom room, MountedMatchClient client, string phase, bool report)
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
                        var panel = report ? board.BattleReport.Root : board.Battles.Root;
                        Assert.LessOrEqual(panel.worldBound.yMax, board.Map.worldBound.yMax + 1);
                        Assert.GreaterOrEqual(panel.worldBound.yMin, board.Map.worldBound.yMin - 1);
                        var buttons = report ? new[] { panel.Q<Button>("close-battle-report"), panel.Q<Button>("next-battle-round") } :
                            panel.Query<Button>().ToList().Where(b => b.enabledInHierarchy && b.worldBound.height > 0);
                        foreach (var button in buttons)
                        {
                            var hit = button.panel.Pick(button.worldBound.center);
                            Assert.IsTrue(hit == button || button.Contains(hit), button.name + " covered by " + hit?.name);
                        }
                        LayoutTests.AssertNoDecisionScroll(board);
                        LayoutTests.Save(target, $"ui46-{phase}-{width}-{scale:0.00}.png");
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
