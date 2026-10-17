using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public sealed class ActionContextTests
    {
        [TestCase(ActionContextKind.Army)]
        [TestCase(ActionContextKind.Battle)]
        [TestCase(ActionContextKind.Fellowship)]
        [TestCase(ActionContextKind.Hunt)]
        [TestCase(ActionContextKind.Politics)]
        [TestCase(ActionContextKind.Cards)]
        [TestCase(ActionContextKind.Characters)]
        [TestCase(ActionContextKind.Resources)]
        [TestCase(ActionContextKind.Dice)]
        [TestCase(ActionContextKind.General)]
        public void EveryHostWithoutRecordedLocationRemainsWithoutLocation(ActionContextKind kind)
        {
            var view = new ActionLogView { Action = new ActionLogSummary { Context = new ActionContext { Kind = kind } } };
            var result = ActionContextModel.For(view);
            Assert.AreEqual(kind, result.Kind);
            Assert.IsEmpty(result.Regions);
            Assert.IsEmpty(result.Movements);
        }

        [Test]
        public void SelectedStepUsesItsOwnBattleIdentityInsideOneParentAction()
        {
            var view = new ActionLogView { Action = new ActionLogSummary { Context = new ActionContext { Kind = ActionContextKind.Cards } } };
            view.Steps.Add(new ActionLogStep { Id = "first", Context = new ActionContext { BattleId = "battle1", Regions = { "moria" } } });
            view.Steps.Add(new ActionLogStep { Id = "second", Context = new ActionContext { BattleId = "battle2", Regions = { "moria" } } });
            Assert.AreEqual("battle1", ActionContextModel.For(view, "first").BattleId);
            Assert.AreEqual("battle2", ActionContextModel.For(view, "second").BattleId);
        }

        [Test]
        public void RecordedArmyAndTravelKeepEverySegmentWithoutLookingUpCurrentFigures()
        {
            var page = JsonConvert.DeserializeObject<ActionLogPage>(File.ReadAllText(Path.Combine(Application.dataPath,
                "Wotr/Tests/Editor/ActionLogWire.json")));
            var view = new ActionLogView { Action = page.Actions[0], Steps = page.Steps };
            view.Steps.Add(new ActionLogStep { Id = "travel", Movements = { new ActionMovement
            {
                Kind = ActionMovementKind.Travel, From = "hollin", To = "fangorn", Route = { "hollin", "dunland", "gap-of-rohan", "fangorn" },
                Participants = { new ActionParticipant { DefinitionId = "base.character.gandalf-grey" } }
            } } });
            var result = ActionContextModel.For(view);
            Assert.AreEqual(2, result.Movements.Count);
            Assert.AreEqual(5, result.Movements[0].Participants.Count);
            CollectionAssert.AreEqual(new[] { "hollin", "dunland", "gap-of-rohan", "fangorn" }, ActionContextModel.Route(result.Movements[1]));
            Assert.Contains("Gandalf the Grey", ActionCardText.Participants(result.Movements[1].Participants).ToArray());
        }

        [Test]
        public void HuntUsesTheRequestedChainAndNeverTheNextReport()
        {
            var view = new ActionLogView { Action = new ActionLogSummary { Context = new ActionContext
                { Kind = ActionContextKind.Hunt, HuntChainId = "old" } } };
            view.Steps.Add(new ActionLogStep { Facts = new ActionFacts { Hunt = new ActionHuntFact { ChainId = "old", Completed = true } } });
            view.Steps.Add(new ActionLogStep { Facts = new ActionFacts { Hunt = new ActionHuntFact { ChainId = "new" } } });
            var result = ActionContextModel.For(view);
            Assert.AreEqual("old", result.HuntChainId);
            Assert.AreEqual(1, result.Hunt.Count);
            Assert.IsTrue(result.Hunt[0].Completed);
        }

        [Test]
        public void DelayedActionResponseIsLimitedToTheClickedVersion()
        {
            var page = ActionLogModelTests.Page(0, 3);
            foreach (var step in page.Steps) { step.ActionId = "a0"; step.Status = ActionLogStatus.InProgress; }
            var action = page.Actions[0];
            action.Version = "s2";
            action.StepCount = 3;
            action.Status = ActionLogStatus.Completed;
            var result = ActionVersion.Through(new ActionLogView { Action = action, Steps = page.Steps }, "s1");
            Assert.AreEqual("s1", result.Action.Version);
            Assert.AreEqual(ActionLogStatus.InProgress, result.Action.Status);
            Assert.AreEqual(2, result.Steps.Count);
            Assert.AreEqual(3, page.Steps.Count);
            Assert.IsNull(ActionVersion.Through(result, "unknown"));
        }

        [Test]
        public void EarlierBattleVersionDoesNotRevealLaterRoundsOrAdvance()
        {
            var report = JsonConvert.DeserializeObject<BattleReportView>(File.ReadAllText(Path.Combine(Application.dataPath,
                "Wotr/Tests/Editor/BattleReportWire.json")));
            var start = report.Steps.First(s => s.Kind == "BattleStarted");
            var view = new ActionLogView { Action = new ActionLogSummary { Version = start.Id, Status = ActionLogStatus.InProgress },
                Steps = new List<ActionLogStep> { start } };
            var earlier = BattleReportVersion.Through(report, view);
            Assert.IsEmpty(earlier.Rounds);
            Assert.AreEqual(BattleReportStatus.InProgress, earlier.Status);
            Assert.IsFalse(earlier.Steps.Any(s => s.Kind is "BattleEnded" or "BattleAdvance"));
            var plan = ActionContextModel.For(new ActionLogView { Action = new ActionLogSummary { Context = start.Context }, Steps = view.Steps });
            Assert.AreEqual(ActionMovementKind.Attack, plan.Movements.Single().Kind);
            Assert.AreEqual(start.Facts.Battle.Attacker.RegionId, plan.Movements[0].From);
            Assert.AreEqual(start.Facts.Battle.RegionId, plan.Movements[0].To);
            Assert.AreEqual(report.Initial.Attacker.Units.Count + report.Initial.Attacker.Characters.Count, plan.Movements[0].Participants.Count);
        }

        [TestCase(CommandKind.AdvanceBattle, BattleReportStatus.Completed)]
        [TestCase(CommandKind.CommitCombatCard, BattleReportStatus.InProgress)]
        public void CommandNoticeIncludesItsAutomaticResultsButNoLaterDecision(CommandKind command, BattleReportStatus status)
        {
            var report = JsonConvert.DeserializeObject<BattleReportView>(File.ReadAllText(Path.Combine(Application.dataPath,
                "Wotr/Tests/Editor/BattleReportWire.json")));
            var selected = report.Steps.Last(s => s.Command == command);
            var action = new ActionLogView
            {
                Action = new ActionLogSummary { Version = selected.Id, Status = selected.Status },
                Steps = report.Steps.Where(s => s.Command.HasValue && s.Revision <= selected.Revision).ToList()
            };
            var result = BattleReportVersion.Through(report, action);
            Assert.AreEqual(status, result.Status);
            CollectionAssert.AreEqual(report.Steps.Where(s => s.Revision <= selected.Revision).Select(s => s.Id),
                result.Steps.Select(s => s.Id));
            Assert.IsTrue(result.Steps.Any(s => s.Revision == selected.Revision && s.Command == null));
        }

        [Test]
        public async Task UncachedBattleStaysIncompleteOfflineAndRetryLoadsOnlyTheClickedVersion()
        {
            using var handler = new ActionLogModelTests.Delayed();
            using var api = new GameApi("https://example.invalid", handler);
            var panel = new ActionLogPanel(BoardPresentation.Load());
            using var loader = new ActionDetailLoader(api, "match", panel, _ => panel.Model);
            var report = JsonConvert.DeserializeObject<BattleReportView>(File.ReadAllText(Path.Combine(Application.dataPath,
                "Wotr/Tests/Editor/BattleReportWire.json")));
            var start = report.Steps.First(s => s.Kind == "BattleStarted");
            report.MatchId = "match";
            var page = new ActionLogPage { MatchId = "match", Steps = { start }, Actions = { new ActionLogSummary
            {
                Id = start.ActionId, Version = start.Id, StepCount = 1, Context = start.Context, Status = ActionLogStatus.InProgress
            } } };
            panel.Model.Merge(page);
            var completed = 0;
            panel.Presented += (_, complete) => { if (complete) completed++; };
            panel.Request(new ActionOpenRequest(start.ActionId, start.Id), true);
            StringAssert.Contains("Offline", panel.Popup.Q<Label>("action-card-message").text);
            Assert.AreEqual(0, completed);
            Assert.IsEmpty(handler.Pending);
            loader.SetConnection(true);
            panel.Pin();
            Assert.AreEqual(1, handler.Pending.Count);
            handler.Complete(0, report);
            await ActionLogModelTests.Until(() => completed == 1);
            Assert.AreEqual(start.Id, panel.Card.Displayed.Action.Version);
            Assert.AreEqual(BattleReportStatus.InProgress, panel.Card.Archive.Displayed.Status);
            Assert.IsEmpty(panel.Card.Archive.Displayed.Rounds);
            panel.CloseCard();
            loader.SetConnection(false);
            panel.Request(new ActionOpenRequest(start.ActionId, start.Id), true);
            Assert.AreEqual(2, completed, "The exact cached version remains readable offline.");
            Assert.AreEqual(1, handler.Pending.Count);
            Assert.IsFalse(panel.IsOpen);
        }
    }
}
