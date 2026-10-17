using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-211: a card's decision outside a battle and the Hunt is confirmed in the turn console, which names the card and
    // what has been chosen so far.
    public sealed class CardDecisionsTests
    {
        private static PlayerView View(DecisionKind kind, string source = "", Side owner = Side.Shadow) => new PlayerView
        {
            You = Side.Shadow,
            DecisionOwner = owner,
            Decision = new DecisionView { Id = "d", Kind = kind, SourceId = source },
            Options = { new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 0, Maximum = 3 } }
        };

        private static TurnDraft Draft(CommandKind kind)
        {
            var draft = new TurnDraft();
            draft.Choose(kind);
            return draft;
        }

        [TestCase(DecisionKind.EffectChoice, true)]
        [TestCase(DecisionKind.CharacterEvent, true)]
        [TestCase(DecisionKind.ArmyEvent, true)]
        [TestCase(DecisionKind.RemoveExcess, true)]
        [TestCase(DecisionKind.Action, false)]
        [TestCase(DecisionKind.Reaction, false)]
        public void CardEffectDecisionsBelongToTheConsole(DecisionKind kind, bool card)
        {
            Assert.AreEqual(card, CardDecisions.Owns(View(kind), Draft(CommandKind.ChooseEffect)));
        }

        [Test]
        public void TheOpponentsDecisionABattleAndAnEmptyDraftDoNot()
        {
            Assert.IsFalse(CardDecisions.Owns(View(DecisionKind.EffectChoice, owner: Side.FreePeoples), Draft(CommandKind.ChooseEffect)));
            var battle = View(DecisionKind.EffectChoice);
            battle.Battle = new BattleView();
            Assert.IsFalse(CardDecisions.Owns(battle, Draft(CommandKind.ChooseEffect)));
            Assert.IsFalse(CardDecisions.Owns(View(DecisionKind.EffectChoice), new TurnDraft()));
        }

        [Test]
        public void TheCardIsNamedByItsSource()
        {
            Assert.AreEqual("Nazgûl Search", CardDecisions.CardTitle(View(DecisionKind.CharacterEvent, "base.sh.character.09")));
            Assert.AreEqual("", CardDecisions.CardTitle(View(DecisionKind.RemoveExcess)));
        }

        [Test]
        public void ProgressCountsMovesPlacementsAndChoices()
        {
            var nazgul = Draft(CommandKind.MoveShadowCharacters);
            Assert.AreEqual("", CardDecisions.Progress(View(DecisionKind.CharacterEvent), nazgul));
            nazgul.Change(p =>
            {
                p.CompanionMoves.Add(new CompanionMoveChoice { Ids = { "nazgul-1", "nazgul-2" }, Route = { "moria" } });
                p.CompanionMoves.Add(new CompanionMoveChoice { Ids = { "witch-king" }, Route = { "angmar" } });
            });
            Assert.AreEqual("Nazgûl moved 3", CardDecisions.Progress(View(DecisionKind.CharacterEvent), nazgul));
            var recruit = Draft(CommandKind.ChooseEffect);
            recruit.Change(p => p.Placements.Add(new UnitPlacementChoice { UnitId = "sh-reserve-1", RegionId = "moria" }));
            Assert.AreEqual("Recruited 1 of 3", CardDecisions.Progress(View(DecisionKind.EffectChoice), recruit));
            var chosen = Draft(CommandKind.ChooseEffect);
            chosen.Change(p => p.Ids.Add("sh-die-1"));
            Assert.AreEqual("Chosen 1 of 3", CardDecisions.Progress(View(DecisionKind.EffectChoice), chosen));
        }
    }
}
