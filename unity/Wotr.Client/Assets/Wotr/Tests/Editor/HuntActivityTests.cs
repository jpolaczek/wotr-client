using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public sealed class HuntActivityTests
    {
        [Test]
        public void OpponentNoticeIncludesAtomicHuntConsequencesWhileLogKeepsItsExactVersion()
        {
            var action = Recorded();
            var request = new ActionNoticeItem(action.Steps[0], BoardPresentation.Load()).Request;
            Assert.IsTrue(request.IncludeConsequences);
            Assert.AreEqual("move", request.StepId);
            var shown = ActionVersion.Through(action, request.Version, request.IncludeConsequences);
            Assert.AreEqual("complete", shown.Action.Version);
            CollectionAssert.AreEqual(new[] { "move", "start", "roll", "complete" }, shown.Steps.Select(s => s.Id));
            Assert.IsTrue(shown.Steps.Skip(1).All(s => s.Actor == null));
            Assert.AreEqual(ActionLogStatus.Completed, shown.Action.Status);
            var exact = ActionVersion.Through(action, "move");
            Assert.AreEqual("move", exact.Action.Version);
            Assert.AreEqual(1, exact.Steps.Count);
            Assert.AreEqual(5, action.Steps.Count, "Inspection cannot mutate the shared history.");
        }

        [Test]
        public void ReadingConsequencesNeverAcknowledgesTheNextOpponentDecision()
        {
            var action = Recorded();
            var reads = new NotificationReadStore();
            var model = new ActionNoticeModel(reads, BoardPresentation.Load()) { You = Side.Shadow };
            model.Merge(new ActionLogPage { Actions = { action.Action }, Steps = action.Steps.Take(4).ToList() });
            var request = model.Groups.Single().Latest.Request;
            model.Merge(new ActionLogPage { Actions = { action.Action }, Steps = { action.Steps.Last() } });
            var shown = ActionVersion.Through(action, request.Version, request.IncludeConsequences);
            model.Displayed(shown);
            Assert.AreEqual(1, model.StepCount);
            Assert.AreEqual("next", model.Groups.Single().Latest.StepId);
            CollectionAssert.AreEqual(new[] { "move" }, reads.Steps);
            Assert.IsFalse(model.Merge(new ActionLogPage { Actions = { action.Action }, Steps = shown.Steps }));
            Assert.AreEqual(1, model.StepCount);
        }

        [Test]
        public void AutomaticOwnHuntDoesNotCreateAnOpponentNotice()
        {
            var action = Recorded();
            var model = new ActionNoticeModel(new NotificationReadStore(), BoardPresentation.Load()) { You = Side.FreePeoples };
            Assert.IsFalse(model.Merge(new ActionLogPage { Actions = { action.Action }, Steps = action.Steps }));
            Assert.IsEmpty(model.Groups);
        }

        [Test]
        public void ArchiveUsesRequestedChainAndShowsKnownNoDiceResultWithoutGameplayControls()
        {
            var action = Recorded();
            action.Steps.Last().Context = new ActionContext { Kind = ActionContextKind.Cards, HuntChainId = "new" };
            action.Steps.Last().Facts.Hunt = new ActionHuntFact
            {
                ChainId = "new", Entry = new HuntReportEntryView
                {
                    HuntId = 2, Kind = HuntReportKind.TileDrawn,
                    Hunt = new HuntView { TileId = "base.hunt.standard.3.0.1", Damage = 3 },
                    Outcome = new HuntOutcomeView { Corruption = 9 }
                }
            };
            var context = ActionContextModel.For(action, "move");
            var summary = new HuntArchiveSummary(BoardPresentation.Load());
            summary.Apply(context);
            Assert.AreEqual("old", summary.Displayed.ChainId);
            StringAssert.Contains("No Hunt dice", summary.Root.Q<Label>("hunt-archive-roll").text);
            Assert.AreEqual("No tile drawn", summary.Root.Q<Label>("hunt-archive-tile").text);
            StringAssert.Contains("Corruption 0/12", summary.Root.Q<Label>("hunt-archive-result").text);
            Assert.IsEmpty(summary.Root.Query<Button>().ToList());
            Assert.IsNull(summary.Root.userData);
            var next = ActionContextModel.For(action, "next");
            Assert.AreEqual(ActionContextKind.Hunt, next.Kind, "A reaction in a card action still belongs to its recorded Hunt.");
            summary.Apply(next);
            Assert.AreEqual("new", summary.Displayed.ChainId);
            StringAssert.Contains("Damage 3", summary.Root.Q<Label>("hunt-archive-tile").text);
            StringAssert.Contains("Corruption 9/12", summary.Root.Q<Label>("hunt-archive-result").text);
            var panel = new ActionLogPanel(BoardPresentation.Load());
            panel.Request(new ActionOpenRequest("action", stepId: "next"), true);
            panel.ShowAction(action, false);
            Assert.AreEqual("new", panel.Card.HuntArchive.Displayed.ChainId,
                "Cached incomplete details must already use the selected chain.");
        }

        [Test]
        public void ArchiveDistinguishesMissingReportAndMordorFromAZeroRoll()
        {
            var summary = new HuntArchiveSummary(BoardPresentation.Load());
            summary.Apply(ActionContextModel.For(new ActionLogView()));
            Assert.IsNull(summary.Displayed);
            Assert.AreEqual(DisplayStyle.None, summary.Root.style.display.value);
            var action = Recorded();
            foreach (var step in action.Steps.Where(s => s.Facts.Hunt != null))
            {
                step.Facts.Hunt.Entry.Outcome.MordorStep = 2;
                if (step.Facts.Hunt.Entry.Hunt != null)
                    step.Facts.Hunt.Entry.Hunt.Mordor = true;
            }
            summary.Apply(ActionContextModel.For(action, "move"));
            Assert.AreEqual("Mordor · No Hunt roll", summary.Root.Q<Label>("hunt-archive-roll").text);
            StringAssert.Contains("Mordor 2/5", summary.Root.Q<Label>("hunt-archive-result").text);
        }

        [Test]
        public async Task DelayedOlderHuntCannotReplaceTheNewlySelectedAction()
        {
            using var handler = new ActionLogModelTests.Delayed();
            using var api = new GameApi("https://example.invalid", handler);
            var panel = new ActionLogPanel(BoardPresentation.Load());
            using var loader = new ActionDetailLoader(api, "match", panel, _ => panel.Model);
            loader.SetConnection(true);
            var old = Recorded();
            var next = Recorded();
            next.Action.Id = "second";
            foreach (var step in next.Steps)
                step.ActionId = "second";
            panel.Request(new ActionOpenRequest("action", "move", "move", includeConsequences: true), true);
            panel.Request(new ActionOpenRequest("second", "move", "move", includeConsequences: true), true);
            Assert.AreEqual(2, handler.Pending.Count);
            handler.Complete(1, next);
            await ActionLogModelTests.Until(() => panel.Card.Displayed?.Action.Id == "second");
            handler.Complete(0, old);
            await Task.Yield();
            Assert.AreEqual("second", panel.Card.Displayed.Action.Id);
            Assert.AreEqual("complete", panel.Card.Displayed.Action.Version);
            Assert.AreEqual("old", panel.Card.HuntArchive.Displayed.ChainId);
        }

        private static ActionLogView Recorded()
        {
            var context = new ActionContext { Kind = ActionContextKind.Hunt, HuntChainId = "old" };
            var roll = new HuntView { SourceId = "fellowship-move" };
            var outcome = new HuntOutcomeView { Hidden = true, Progress = 1 };
            var view = new ActionLogView
            {
                MatchId = "match",
                Action = new ActionLogSummary { Id = "action", Version = "next", Context = context, StepCount = 5 },
                Steps =
                {
                    new ActionLogStep { Id = "move", ActionId = "action", Revision = 1, Actor = Side.FreePeoples, Command = CommandKind.MoveFellowship, Context = context },
                    Fact("start", HuntReportKind.Started, roll, outcome),
                    Fact("roll", HuntReportKind.Rolled, roll, outcome),
                    Fact("complete", HuntReportKind.HuntCompleted, roll, outcome),
                    new ActionLogStep { Id = "next", ActionId = "action", Revision = 2, Actor = Side.FreePeoples, Command = CommandKind.ResolveHuntDamage, Context = context }
                }
            };
            view.Steps[3].Facts.Hunt.Completed = true;
            view.Steps[3].Status = ActionLogStatus.Completed;
            return view;
        }

        private static ActionLogStep Fact(string id, HuntReportKind kind, HuntView hunt, HuntOutcomeView outcome) => new ActionLogStep
        {
            Id = id, ActionId = "action", Revision = 1, Automatic = true,
            Context = new ActionContext { Kind = ActionContextKind.Hunt, HuntChainId = "old" },
            Facts = new ActionFacts
            {
                Hunt = new ActionHuntFact
                {
                    ChainId = "old", Entry = new HuntReportEntryView
                    {
                        HuntId = 1, Revision = 1, Kind = kind, SourceId = "fellowship-move", Hunt = hunt, Outcome = outcome
                    }
                }
            }
        };
    }
}
