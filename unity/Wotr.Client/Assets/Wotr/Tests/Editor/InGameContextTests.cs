using System.Linq;
using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public sealed class InGameContextTests
    {
        [Test]
        public void ExpiredEffectUsesItsSavedSourceWithoutInventingARegion()
        {
            var view = Record(new ActionLogStep { Id = "expired", Kind = "EffectExpired", Automatic = true,
                Facts = new ActionFacts { Cards = { new ActionCardFact
                    { Id = "base.sh.character.22", Use = ActionCardUse.TableRemoved, Owner = Side.Shadow } } } });
            var context = ActionContextModel.For(view, "expired");
            Assert.AreEqual(InGameActionHost.Effects, context.InGame.Host);
            Assert.AreEqual("base.sh.character.22", context.InGame.SubjectId);
            StringAssert.Contains(CardLibrary.Title("base.sh.character.22"), context.InGame.Title);
            Assert.IsEmpty(context.Regions);
        }

        [Test]
        public void SelectedCorruptionKeepsItsPublicUnknownValueAndDoesNotReadTheLaterReveal()
        {
            var view = Record(new ActionLogStep { Id = "attempt", Actor = Side.Shadow, Command = CommandKind.AttemptCorruption,
                Revision = 10, Facts = new ActionFacts { Sovereigns = { new SovereignView
                    { Id = "kings.sovereign.dain", Title = "Dáin Ironfoot", Corruption = null } } } });
            view.Steps.Add(new ActionLogStep { Id = "reveal", Actor = Side.Shadow, Command = CommandKind.ChooseCorruptionTile,
                Revision = 11, Facts = new ActionFacts { Sovereigns = { new SovereignView
                    { Id = "kings.sovereign.dain", Title = "Dáin Ironfoot", Corruption = 3 } } } });
            var context = ActionContextModel.For(view, "attempt");
            Assert.AreEqual(InGameActionHost.Sovereign, context.InGame.Host);
            Assert.AreEqual("kings.sovereign.dain", context.InGame.SubjectId);
            Assert.IsEmpty(context.Regions);
            var lines = string.Join("\n", ActionFactText.Lines(view.Steps[0], BoardPresentation.Load()));
            StringAssert.DoesNotContain("Corruption 3", lines);
            StringAssert.DoesNotContain("base.hunt", lines);
        }

        [Test]
        public void AutomaticVictoryWithinCommittedRevisionUsesRecordedReasonButNextRevisionDoesNot()
        {
            var view = Record(new ActionLogStep { Id = "damage", Actor = Side.FreePeoples, Revision = 4,
                Context = new ActionContext { Kind = ActionContextKind.Hunt, HuntChainId = "hunt" } });
            view.Steps.Add(new ActionLogStep { Id = "won", Revision = 4, Automatic = true,
                Facts = new ActionFacts { Outcome = new ActionOutcome { Winner = Side.FreePeoples, Victory = VictoryCondition.RingDestroyed } } });
            var context = ActionContextModel.For(view, "damage");
            Assert.AreEqual(InGameActionHost.Victory, context.InGame.Host);
            StringAssert.Contains("Ring destroyed", context.InGame.Title);
            StringAssert.Contains("Ring destroyed", string.Join("\n", ActionFactText.Lines(view.Steps[1], BoardPresentation.Load())));
            view.Steps[1].Revision = 5;
            Assert.AreEqual(InGameActionHost.Default, ActionContextModel.For(view, "damage").InGame.Host);
        }

        [Test]
        public void ReachingMilitaryThresholdWithoutARecordedWinnerIsNotAVictory()
        {
            var view = Record(new ActionLogStep { Facts = new ActionFacts { Outcome = new ActionOutcome { ShadowPoints = 10 } } });
            Assert.AreEqual(InGameActionHost.Default, ActionContextModel.For(view).InGame.Host);
        }

        [TestCase(Side.FreePeoples)]
        [TestCase(Side.Shadow)]
        public void ResourceReactionBelongsToItsActorEvenInsideTheOtherSidesParentAction(Side side)
        {
            var view = Record(new ActionLogStep { Id = "reaction", Actor = side, Command = CommandKind.UseDismay,
                Costs = { new ActionCost { Kind = ActionCostKind.RulerToken, Owner = side, Id = "kings.token.theoden" } } });
            view.Action.Initiator = side == Side.Shadow ? Side.FreePeoples : Side.Shadow;
            var context = ActionContextModel.For(view, "reaction");
            Assert.AreEqual(InGameActionHost.Resources, context.InGame.Host);
            Assert.AreEqual(side, context.InGame.Owner);
            Assert.IsEmpty(context.Regions);
        }

        [Test]
        public void AnEarlierRingPaymentDoesNotTurnACompletedArmyMoveIntoAPoolAction()
        {
            var view = Record(new ActionLogStep { Id = "ring", Revision = 1, Actor = Side.FreePeoples, Command = CommandKind.UseRing,
                Context = new ActionContext { Kind = ActionContextKind.Resources } });
            view.Steps.Add(new ActionLogStep { Id = "move", Revision = 2, Actor = Side.FreePeoples, Command = CommandKind.MoveArmies,
                Context = new ActionContext { Kind = ActionContextKind.Army } });
            Assert.AreEqual(InGameActionHost.Default, ActionContextModel.For(view).InGame.Host);
            Assert.AreEqual(InGameActionHost.Default, ActionContextModel.For(view, "move").InGame.Host);
            Assert.AreEqual(InGameActionHost.Resources, ActionContextModel.For(view, "ring").InGame.Host);
            foreach (var step in view.Steps)
                step.Id = "";
            Assert.AreEqual(InGameActionHost.Default, ActionContextModel.For(view).InGame.Host,
                "An omitted selection must not select a legacy or incomplete step with an empty ID.");
        }

        [TestCase(ActionCostKind.Ring, "lords.ring.nenya", Side.FreePeoples, "Nenya")]
        [TestCase(ActionCostKind.CouncilToken, "council.advance-politics", Side.Shadow, "Council · Politics")]
        [TestCase(ActionCostKind.RulerToken, "kings.token.denethor", Side.FreePeoples, "Ruler token · Denethor")]
        [TestCase(ActionCostKind.RulerToken, "kings.token.theoden", Side.Shadow, "Dismay · Theoden")]
        public void ExactRecordedResourceHasANamedCost(ActionCostKind kind, string id, Side side, string expected)
        {
            StringAssert.Contains(expected, ActionCardText.Cost(new ActionCost { Kind = kind, Id = id, Owner = side }));
        }

        [Test]
        public void KeeperAndStandardDiceRemainSeparateAndHiddenDiscardStaysUnnamed()
        {
            var step = new ActionLogStep { Facts = new ActionFacts
            {
                Dice = { new ActionDieChange { Source = "standard", Before = DieFace.Character, After = DieFace.Muster },
                    new ActionDieChange { Source = "lords.keeper.nenya", Before = DieFace.Character, After = DieFace.Muster } },
                Cards = { new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Discard, Deck = "character", Count = 1 } }
            } };
            var lines = ActionFactText.Lines(step, BoardPresentation.Load()).ToArray();
            Assert.AreEqual(2, lines.Count(line => line.StartsWith("1×")));
            Assert.AreEqual(1, lines.Count(line => line.Contains("Nenya")));
            StringAssert.Contains("1 character card", lines[0]);
            Assert.IsFalse(lines.Any(line => line.Contains("base.sh.character")));
        }

        [Test]
        public void EventCardSourceIsNotMisclassifiedAsACharacterDefinition()
        {
            Assert.AreEqual(CardLibrary.Title("base.fp.character.01"), ActionCardText.Source("base.fp.character.01"));
            Assert.AreEqual(CharacterLibrary.Name("base.character.gimli"), ActionCardText.Source("base.character.gimli"));
            Assert.AreEqual("Council token", InGameResourceText.Name(ActionCostKind.CouncilToken, ""));
        }

        [TestCase("kings.sovereign.denethor", "Denethor")]
        [TestCase("kings.sovereign.dain", "Dáin Ironfoot")]
        [TestCase("council.advance-politics", "Council · Politics")]
        [TestCase("council.move-shadow-characters", "Council · Travel")]
        public void RecordedSourcesAndDecisionNoticesUseKnownPublicNames(string id, string expected)
        {
            var step = new ActionLogStep { Facts = new ActionFacts { SourceId = id } };
            CollectionAssert.AreEqual(new[] { "Source · " + expected }, ActionFactText.Lines(step, BoardPresentation.Load()));
            var view = new PlayerView { Decision = new DecisionView { Kind = DecisionKind.EffectChoice, SourceId = id } };
            Assert.AreEqual("Resolve " + expected, DecisionNoticeText.Describe(view));
            StringAssert.DoesNotContain("Awakened", ActionCardText.Source(id));
            StringAssert.DoesNotContain("Corrupted", ActionCardText.Source(id));
        }

        private static ActionLogView Record(ActionLogStep step) => new ActionLogView
        {
            Action = new ActionLogSummary { Initiator = step.Actor, Context = step.Context }, Steps = { step }
        };
    }
}
