using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public class ActionLogModelTests
    {
        [Test]
        public void PagesDeduplicateAndMergeOldActionWithoutLosingOrderExpansionOrAnchor()
        {
            var model = new ActionLogModel();
            model.Merge(Page(90, 140));
            model.Anchor = "a120";
            model.AnchorOffset = 23;
            model.Toggle("a120");
            model.Merge(Page(40, 90));
            model.Merge(Page(0, 50));
            model.Merge(Page(0, 50));
            Assert.AreEqual(140, model.StepCount);
            Assert.AreEqual("a120", model.Rows[model.AnchorIndex()].Id);
            Assert.AreEqual(23, model.AnchorOffset);
            Assert.IsTrue(model.Expanded("a120"));
            var addition = Page(140, 141);
            addition.Actions[0].Id = "a120";
            addition.Actions[0].StepCount = 2;
            addition.Steps[0].ActionId = "a120";
            model.Merge(addition);
            Assert.AreEqual("a120", model.Rows[0].Id);
            Assert.AreEqual(new[] { 120L, 140L }, model.Find("a120").Steps.Select(s => s.Revision));
            model.Merge(Page(120, 121));
            Assert.AreEqual(2, model.Find("a120").Action.StepCount, "An older response must not roll back the header.");
        }

        [Test]
        public void CardKeepsHistoricalFaceMixedArmyAndRecordedCharacterVersion()
        {
            var page = JsonConvert.DeserializeObject<ActionLogPage>(File.ReadAllText(Path.Combine(Application.dataPath,
                "Wotr/Tests/Editor/ActionLogWire.json")));
            var action = page.Actions[0];
            StringAssert.Contains("Will Of The West die → Army", ActionCardText.Cost(action.Costs[0]));
            var figures = page.Steps[0].Movements[0].Participants;
            figures.Add(new ActionParticipant { DefinitionId = "base.character.gandalf-grey", Side = Side.FreePeoples });
            figures.Add(new ActionParticipant { UnitKind = UnitKind.Regular, NationId = "isengard", Side = Side.Shadow });
            var text = string.Join("\n", ActionCardText.Participants(figures));
            StringAssert.Contains("2× ● Regular · Sauron", text);
            StringAssert.Contains("3× ◆ Elite · Sauron", text);
            StringAssert.Contains("1× ● Regular · Isengard", text);
            StringAssert.Contains("Gandalf the Grey", text);
            Assert.IsEmpty(ActionCardText.ExtraCosts(action, page.Steps[0]));
        }

        [Test]
        public void ReactionKeepsItsOwnActorAndTokenCostEvenWhenParentBelongsToOpponent()
        {
            var action = new ActionLogSummary { Initiator = Side.Shadow, Command = CommandKind.AttackArmy,
                Costs = new List<ActionCost> { new ActionCost { Kind = ActionCostKind.Die, Owner = Side.Shadow, Face = DieFace.ArmyMuster, Use = DieFace.Army } } };
            var reaction = new ActionLogStep { Actor = Side.FreePeoples, Command = CommandKind.RespondEffect,
                Costs = new List<ActionCost> { new ActionCost { Kind = ActionCostKind.Free, Owner = Side.FreePeoples },
                    new ActionCost { Kind = ActionCostKind.RulerToken, Owner = Side.FreePeoples, Source = "theoden" } } };
            Assert.AreEqual(2, ActionCardText.ExtraCosts(action, reaction).Count());
            Assert.AreEqual("Free Peoples · Free", ActionCardText.Cost(reaction.Costs[0]));
            Assert.AreEqual("Free Peoples · Ruler token · Theoden", ActionCardText.Cost(reaction.Costs[1]));
            Assert.AreEqual("Incomplete history", ActionCardText.Status(new ActionLogSummary { Legacy = true }));
            Assert.AreEqual("Earlier entry", ActionCardText.Heading(new ActionLogSummary { Legacy = true }));
        }

        [Test]
        public void PinnedCardRetainsExactlyRenderedVersionWhileModelReceivesNextStep()
        {
            var model = new ActionLogModel();
            model.Merge(Page(0, 1));
            var panel = new ActionLogPanel(BoardPresentation.Load());
            panel.Bind(model);
            panel.Request("a0", true);
            panel.ShowAction(model.Find("a0"), true);
            var next = Page(1, 2);
            next.Actions[0].Id = "a0";
            next.Actions[0].StepCount = 2;
            next.Steps[0].ActionId = "a0";
            model.Merge(next);
            Assert.AreEqual("s0", panel.Card.Displayed.Action.Version);
            Assert.AreEqual(1, panel.Card.Displayed.Steps.Count);
            Assert.AreEqual("s1", model.Find("a0").Action.Version);
            panel.Close();
            Assert.IsTrue(panel.Pinned);
        }

        [Test]
        public async Task DelayedFilterAndDisposedMatchResponsesCannotReplaceActiveHistory()
        {
            using var handler = new Delayed();
            using var api = new GameApi("https://example.invalid", handler);
            var panel = new ActionLogPanel(BoardPresentation.Load());
            using var controller = new ActionLogController(api, "match", panel);
            controller.SetConnection("Connected");
            panel.Open();
            Assert.AreEqual(1, handler.Pending.Count);
            panel.ChooseFilter(ActionLogFilter.You);
            Assert.AreEqual(2, handler.Pending.Count);
            handler.Complete(1, Page(5, 7));
            await Until(() => panel.Model.Loaded);
            handler.Complete(0, Page(0, 5));
            await Task.Yield();
            Assert.AreEqual(2, panel.Model.StepCount);
            Assert.AreEqual("a6", panel.Model.Rows[0].Id);
            panel.ChooseFilter(ActionLogFilter.Opponent);
            controller.Dispose();
            handler.Complete(2, Page(9, 10));
            await Task.Yield();
            Assert.IsFalse(panel.Model.Loaded);
            Assert.AreEqual(0, panel.Model.StepCount);
        }

        [Test]
        public async Task DelayedDetailNeverReplacesNewSelectionOrClosedCard()
        {
            using var handler = new Delayed();
            using var api = new GameApi("https://example.invalid", handler);
            var panel = new ActionLogPanel(BoardPresentation.Load());
            using var controller = new ActionLogController(api, "match", panel);
            controller.SetConnection("Connected");
            panel.Open();
            var page = Page(0, 2);
            page.Actions.ForEach(a => a.StepCount = 2);
            handler.Complete(0, page);
            await Until(() => panel.Model.Loaded);
            panel.Request("a0", true);
            panel.Request("a1", true);
            Assert.AreEqual(3, handler.Pending.Count);
            handler.Complete(1, new ActionLogView { MatchId = "match", Action = page.Actions[0], Steps = new List<ActionLogStep> { page.Steps[0] } });
            await Task.Yield();
            Assert.AreEqual("a1", panel.Card.Displayed.Action.Id);
            panel.CloseCard();
            handler.Complete(2, new ActionLogView { MatchId = "match", Action = page.Actions[1], Steps = new List<ActionLogStep> { page.Steps[1] } });
            await Task.Yield();
            Assert.AreEqual("", panel.SelectedId);
            Assert.AreEqual(DisplayStyle.None, panel.Popup.style.display.value);
        }

        [Test]
        public async Task OfflineAndFailedReadsKeepCachedHistoryAndReconnectLoadsNewEntries()
        {
            using var handler = new Delayed();
            using var api = new GameApi("https://example.invalid", handler);
            var panel = new ActionLogPanel(BoardPresentation.Load());
            using var controller = new ActionLogController(api, "match", panel);
            panel.Open();
            Assert.IsEmpty(handler.Pending);
            controller.SetConnection("Connected");
            handler.Complete(0, Page(0, 2));
            await Until(() => panel.Model.Loaded);
            controller.SetConnection("Reconnecting");
            panel.Request("a1", true);
            Assert.AreEqual("a1", panel.Card.Displayed.Action.Id);
            Assert.AreEqual(1, handler.Pending.Count);
            StringAssert.Contains("Offline", panel.Root.Q<Label>("log-status").text);
            controller.SetConnection("Connected");
            handler.Fail(1);
            await Until(() => panel.Root.Q<Button>("log-retry").style.display.value == DisplayStyle.Flex);
            Assert.AreEqual(2, panel.Model.StepCount);
            controller.SetConnection("Connected");
            var next = Page(2, 3);
            next.NextCursor = "";
            handler.Complete(2, next);
            await Until(() => panel.Model.StepCount == 3);
            Assert.AreEqual("a1", panel.Card.Displayed.Action.Id);
            Assert.IsTrue(panel.Model.HasNew);
        }

        [Test]
        public void RecordedResultsIncludeDieChangesResourceTransfersAndNeutralControl()
        {
            var step = new ActionLogStep { Facts = new ActionFacts
            {
                Dice = new List<ActionDieChange> { new ActionDieChange { Owner = Side.Shadow,
                    Before = DieFace.ArmyMuster, After = DieFace.Event, To = DieLocation.Available } },
                Resources = new List<ActionResourceChange> { new ActionResourceChange { Kind = ActionCostKind.Ring,
                    From = "FreePeoples", To = "Shadow" } },
                Control = new List<ActionControlChange> { new ActionControlChange { RegionId = "osgiliath", After = null } }
            } };
            var text = string.Join("\n", ActionFactText.Lines(step, BoardPresentation.Load()));
            StringAssert.Contains("Army Muster → Event", text);
            StringAssert.Contains("Ring · Free Peoples → Shadow", text);
            StringAssert.Contains("Control: Neutral", text);
        }

        internal static ActionLogPage Page(int from, int to)
        {
            var page = new ActionLogPage { MatchId = "match", LatestCursor = "latest", NextCursor = "older" };
            for (var i = from; i < to; i++)
            {
                var id = "a" + i;
                page.Actions.Add(new ActionLogSummary { Id = id, Version = "s" + i, Initiator = Side.Shadow,
                    Command = CommandKind.MoveArmies, Turn = 1, Status = ActionLogStatus.Completed, StepCount = 1 });
                page.Steps.Add(new ActionLogStep { Id = "s" + i, ActionId = id, Revision = i, Order = 1,
                    Actor = Side.Shadow, Command = CommandKind.MoveArmies, Text = "Moved the army." });
            }
            return page;
        }

        internal static async Task Until(Func<bool> condition)
        {
            for (var i = 0; i < 100 && !condition(); i++) await Task.Delay(10);
            Assert.IsTrue(condition());
        }

        internal sealed class Delayed : HttpMessageHandler
        {
            public readonly List<TaskCompletionSource<HttpResponseMessage>> Pending = new List<TaskCompletionSource<HttpResponseMessage>>();
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken token)
            {
                var pending = new TaskCompletionSource<HttpResponseMessage>();
                Pending.Add(pending);
                return pending.Task; // Deliberately ignore cancellation to test the response generation guard.
            }
            public void Complete(int index, object data) => Pending[index].SetResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = new StringContent(JsonConvert.SerializeObject(data)) });
            public void Fail(int index) => Pending[index].SetResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                { Content = new StringContent("Unavailable") });
        }
    }
}
