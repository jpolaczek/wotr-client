using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public sealed class ActionNoticeTests
    {
        [Test]
        public void OpponentStepsAreGroupedByBattleOrActionAndRepeatedPagesAreHarmless()
        {
            var model = Model();
            var page = ActionLogModelTests.Page(0, 4);
            page.Steps[0].Actor = Side.FreePeoples;
            page.Steps[1].Actor = null;
            foreach (var step in page.Steps.Skip(2)) { step.ActionId = "attack"; step.Context.BattleId = "battle"; }
            Assert.IsTrue(model.Merge(page));
            Assert.AreEqual(1, model.Groups.Count);
            Assert.AreEqual(2, model.Groups[0].Count);
            Assert.AreEqual("s3", model.Groups[0].Latest.StepId);
            Assert.IsFalse(model.Merge(page));
            var next = ActionLogModelTests.Page(4, 5);
            next.Steps[0].ActionId = "attack";
            next.Steps[0].Context.BattleId = "second-battle";
            model.Merge(next);
            Assert.AreEqual(2, model.Groups.Count, "Two battles in one action remain separate notifications.");
        }

        [Test]
        public void DisplayingAnOlderVersionLeavesTheNewStepUnreadEvenWhenItsPageArrivesLater()
        {
            var store = new NotificationReadStore();
            var model = new ActionNoticeModel(store, BoardPresentation.Load()) { You = Side.FreePeoples };
            var page = ActionLogModelTests.Page(0, 3);
            foreach (var step in page.Steps) step.ActionId = "one";
            model.Merge(page);
            var opened = new ActionLogView { Steps = page.Steps.Take(2).ToList() };
            model.Displayed(opened);
            Assert.AreEqual(1, model.StepCount);
            Assert.AreEqual("s2", model.Groups.Single().Latest.StepId);
            model.Displayed(new ActionLogView { Steps = { new ActionLogStep { Id = "s3", Actor = Side.Shadow } } });
            model.Merge(ActionLogModelTests.Page(3, 4));
            Assert.AreEqual(1, model.StepCount, "A delayed list cannot resurrect a displayed step.");
            CollectionAssert.AreEquivalent(new[] { "s0", "s1", "s3" }, store.Steps);
        }

        [Test]
        public void GroupOpeningCapturesTheExactStepAndUsesTheFullActionContext()
        {
            var model = Model();
            var page = ActionLogModelTests.Page(0, 2);
            page.Steps[0].Context.BattleId = "old-battle";
            model.Merge(page);
            var battle = model.Groups.Single(g => g.Latest.BattleId.Length > 0).Latest.Request;
            Assert.AreEqual("a0", battle.ActionId);
            Assert.AreEqual("s0", battle.Version);
            Assert.AreEqual("s0", battle.StepId);
            Assert.AreEqual(ActionLogFilter.All, battle.Filter);
            Assert.AreEqual("", model.Groups[0].Latest.Request.StepId);
        }

        [Test]
        public void NoticeDescribesRecordedCompositionAndEveryCommandHasSpecificVocabulary()
        {
            var step = ActionLogModelTests.Page(0, 1).Steps[0];
            step.Movements.Add(new ActionMovement { From = "morannon", To = "dagorlad", Participants =
            {
                new ActionParticipant { UnitKind = UnitKind.Regular }, new ActionParticipant { UnitKind = UnitKind.Regular },
                new ActionParticipant { UnitKind = UnitKind.Elite }, new ActionParticipant { UnitKind = UnitKind.Elite },
                new ActionParticipant { UnitKind = UnitKind.Elite }
            } });
            var data = BoardPresentation.Load();
            var text = ActionNoticeText.Short(step, data);
            StringAssert.Contains("2 Regular + 3 Elite", text);
            StringAssert.Contains("Morannon → Dagorlad", text);
            foreach (CommandKind kind in Enum.GetValues(typeof(CommandKind)))
            {
                step.Command = kind;
                step.Movements.Clear();
                Assert.IsNotEmpty(ActionNoticeText.Short(step, data));
                StringAssert.DoesNotContain("Opponent acted", ActionNoticeText.Short(step, data));
            }
        }

        [Test]
        public void NoticeNamesPublicTilesCardsAndCharactersWithoutTreatingEverySourceAsACharacter()
        {
            var data = BoardPresentation.Load();
            var step = new ActionLogStep
            {
                Actor = Side.FreePeoples,
                Command = CommandKind.RespondEffect,
                Context = new ActionContext { Kind = ActionContextKind.Cards, HuntChainId = "recorded-chain" }
            };
            step.Context.DefinitionId = "base.hunt.special-sh.die.0.1";
            StringAssert.EndsWith("Hunt tile · Die", ActionNoticeText.Short(step, data));
            StringAssert.DoesNotContain(step.Context.DefinitionId, ActionNoticeText.Short(step, data));
            step.Context.DefinitionId = "base.fp.character.05";
            StringAssert.EndsWith("Mithril Coat and Sting", ActionNoticeText.Short(step, data));
            step.Context.DefinitionId = "base.character.gandalf-grey";
            StringAssert.EndsWith("Gandalf the Grey", ActionNoticeText.Short(step, data));
            step.Context.DefinitionId = "future.hunt.tile";
            StringAssert.EndsWith(" · Hunt", ActionNoticeText.Short(step, data));
            StringAssert.DoesNotContain("future", ActionNoticeText.Short(step, data));
        }

        [Test]
        public void NoticeUsesOnlyRecordedDieFacesAndKeepsUnknownSourcesNeutral()
        {
            var data = BoardPresentation.Load();
            var step = new ActionLogStep
            {
                Actor = Side.Shadow,
                Command = CommandKind.DiscardDie,
                Context = new ActionContext { Kind = ActionContextKind.Dice, DefinitionId = "physical-shadow-die-7" }
            };
            StringAssert.EndsWith(" · Action die", ActionNoticeText.Short(step, data));
            step.Facts.Dice.Add(new ActionDieChange { Id = step.Context.DefinitionId, Before = DieFace.Army, To = DieLocation.Spent });
            StringAssert.EndsWith(" · Army die", ActionNoticeText.Short(step, data));
            step.Facts.Dice[0].After = DieFace.Character;
            StringAssert.EndsWith(" · Character die", ActionNoticeText.Short(step, data));
            StringAssert.DoesNotContain(step.Context.DefinitionId, ActionNoticeText.Short(step, data));
            step.Context.Kind = ActionContextKind.Cards;
            step.Context.DefinitionId = "future.card.effect";
            StringAssert.EndsWith(" · Card effect", ActionNoticeText.Short(step, data));
            step.Context.Kind = ActionContextKind.Characters;
            step.Context.DefinitionId = "future.character.unknown";
            StringAssert.EndsWith(" · Character", ActionNoticeText.Short(step, data));
        }

        [Test]
        public void GoldDescriptionComesFromCurrentOwnDecisionAndRequiredCounts()
        {
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { RequiredCount = 2, SourceId = "base.fp.strategy.01" } };
            foreach (DecisionKind kind in Enum.GetValues(typeof(DecisionKind)))
            {
                view.Decision.Kind = kind;
                Assert.IsNotEmpty(DecisionNoticeText.Describe(view), kind.ToString());
            }
            view.Decision.Kind = DecisionKind.Losses;
            Assert.AreEqual("Assign 2 hits", DecisionNoticeText.Describe(view));
            view.Decision.Kind = DecisionKind.Action;
            Assert.AreEqual("Choose an action", DecisionNoticeText.Describe(view));
            view.Decision.Kind = DecisionKind.HuntDamage;
            view.Hunt = new HuntView { Damage = 3 };
            Assert.AreEqual("Resolve 3 Hunt damage", DecisionNoticeText.Describe(view));
            view.DecisionOwner = Side.Shadow;
            Assert.IsNull(DecisionAttention.For(view));
            view.DecisionOwner = Side.FreePeoples;
            view.Winner = Side.Shadow;
            Assert.IsNull(DecisionAttention.For(view));
        }

        [Test]
        public async Task DisposedMatchIgnoresAnUncancelableHistoryResponse()
        {
            using var handler = new ActionLogModelTests.Delayed();
            using var api = new GameApi("https://example.invalid", handler);
            var board = new BoardScreen();
            using var notices = new ActionNoticeController(api, "match", board);
            notices.Update(new PlayerView { MatchId = "match", You = Side.FreePeoples });
            notices.SetConnection("Connected");
            Assert.AreEqual(1, handler.Pending.Count);
            notices.Dispose();
            handler.Complete(0, ActionLogModelTests.Page(0, 2));
            await Task.Yield();
            Assert.IsFalse(notices.Loaded);
            Assert.AreEqual(0, notices.Model.StepCount);
        }

        private static ActionNoticeModel Model() => new ActionNoticeModel(new NotificationReadStore(), BoardPresentation.Load())
            { You = Side.FreePeoples };
    }
}
