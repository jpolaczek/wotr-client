using System.Collections.Generic;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-160: the console headline says who acts and what now, from the projection and the draft only.
    public sealed class TurnConsoleStateTests
    {
        private static string Region(string id) => id == "minas-tirith" ? "Minas Tirith" : id;

        private static PlayerView View(Side you, Side? owner, DecisionKind? kind) => new PlayerView
        {
            You = you,
            DecisionOwner = owner,
            Decision = kind.HasValue && owner == you ? new DecisionView { Id = "d", Kind = kind.Value } : null,
            Phase = GamePhase.ActionResolution,
            Dice = new List<DieView> { new DieView { Id = "fp-1", Owner = Side.FreePeoples, Face = DieFace.Army, Location = DieLocation.Available } }
        };

        [Test]
        public void TheOwnActionNamesTheChosenDieAndTheSecondMarch()
        {
            var view = View(Side.FreePeoples, Side.FreePeoples, DecisionKind.Action);
            var die = new ActionDieContext();
            Assert.AreEqual("YOUR ACTION", TurnConsoleState.For(view, new TurnDraft(), die, Region).Headline);
            die.Sync(view, "fp-1");
            var state = TurnConsoleState.For(view, new TurnDraft(), die, Region);
            Assert.AreEqual("YOUR ACTION · Army die", state.Headline);
            Assert.AreEqual(Side.FreePeoples, state.Actor);
            Assert.IsTrue(state.Own);
            view.Decision.Kind = DecisionKind.ActionSequence;
            view.ActionSequence = new ActionSequenceView { Kind = CommandKind.MoveArmies, Owner = Side.FreePeoples, DieId = "fp-1",
                Face = DieFace.Army, CompletedSteps = 1, Remaining = 1 };
            die.Sync(view, "fp-1");
            Assert.AreEqual("YOUR ACTION · Army die · March 2 of 2", TurnConsoleState.For(view, new TurnDraft(), die, Region).Headline);
        }

        [Test]
        public void WaitingNamesTheOpponentAndWhatTheyResolve()
        {
            var view = View(Side.FreePeoples, Side.Shadow, null);
            Assert.AreEqual("WAITING FOR SHADOW", TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), Region).Headline);
            view.Battle = new BattleView { RegionId = "minas-tirith" };
            var state = TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), Region);
            Assert.AreEqual("WAITING FOR SHADOW · battle at Minas Tirith", state.Headline);
            Assert.AreEqual(Side.Shadow, state.Actor);
            Assert.IsFalse(state.Own);
            view.Battle = null;
            view.Phase = GamePhase.HuntAllocation;
            StringAssert.EndsWith("· Hunt allocation", TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), Region).Headline);
        }

        // UI-236: the Fellowship phase names the map's figure and the track's gate instead of panel buttons.
        [Test]
        public void FellowshipPhaseNamesTheFigureAndTheGate()
        {
            var view = View(Side.FreePeoples, Side.FreePeoples, DecisionKind.FellowshipPhase);
            Assert.AreEqual("Continue to the Hunt.", TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), Region).Guidance);
            view.Options.Add(new ActionOption { Kind = CommandKind.DeclareFellowship, Enabled = true });
            view.Options.Add(new ActionOption { Kind = CommandKind.EnterMordor, Enabled = true });
            var guidance = TurnConsoleState.For(view, new TurnDraft(), new ActionDieContext(), Region).Guidance;
            StringAssert.StartsWith("Declare: click the Fellowship figure · Enter Mordor: click the glowing gate", guidance);
            StringAssert.EndsWith("or continue to the Hunt.", guidance);
        }

        [Test]
        public void PhasesAndOtherDecisionsKeepOneLine()
        {
            Assert.AreEqual("FELLOWSHIP PHASE", TurnConsoleState.For(View(Side.FreePeoples, Side.FreePeoples, DecisionKind.FellowshipPhase),
                new TurnDraft(), new ActionDieContext(), Region).Headline);
            Assert.AreEqual("ALLOCATE HUNT DICE", TurnConsoleState.For(View(Side.Shadow, Side.Shadow, DecisionKind.AllocateHunt),
                new TurnDraft(), new ActionDieContext(), Region).Headline);
            Assert.AreEqual("YOUR DECISION · Resolve Hunt damage", TurnConsoleState.For(View(Side.FreePeoples, Side.FreePeoples, DecisionKind.HuntDamage),
                new TurnDraft(), new ActionDieContext(), Region).Headline);
            var finished = View(Side.Shadow, null, null);
            finished.Winner = Side.FreePeoples;
            var state = TurnConsoleState.For(finished, new TurnDraft(), new ActionDieContext(), Region);
            Assert.AreEqual("GAME OVER · Free Peoples win", state.Headline);
            Assert.AreEqual(Side.FreePeoples, state.Actor);
        }

        // UI-211: a card's decision names its card in the headline.
        [Test]
        public void ACardDecisionNamesItsCard()
        {
            var view = View(Side.Shadow, Side.Shadow, DecisionKind.CharacterEvent);
            view.Decision.SourceId = "base.sh.character.09";
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveShadowCharacters);
            Assert.AreEqual("YOUR DECISION · Nazgûl Search", TurnConsoleState.For(view, draft, new ActionDieContext(), Region).Headline);
        }
    }
}
