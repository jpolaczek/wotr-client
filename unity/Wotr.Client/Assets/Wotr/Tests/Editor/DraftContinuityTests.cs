using System;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class DraftContinuityTests
    {
        [Test]
        public void EveryMandatoryWindowKeepsItsOwnDraftButNeverInheritsTheNextDecision()
        {
            foreach (DecisionKind kind in Enum.GetValues(typeof(DecisionKind)))
            {
                var before = View(kind);
                var after = View(kind);
                after.Revision++;
                Assert.IsTrue(DraftContinuity.CanCarry(before, after), kind.ToString());
                after.Decision.Id = "next";
                Assert.AreEqual(kind == DecisionKind.Action, DraftContinuity.CanCarry(before, after), kind.ToString());
            }
        }

        [Test]
        public void OrdinaryActionCannotCrossTurnOwnerSourceOrProcedureBoundaries()
        {
            Action<PlayerView>[] changes =
            {
                view => view.MatchId = "another-match",
                view => view.You = Side.Shadow,
                view => view.Turn++,
                view => view.Phase = GamePhase.Fellowship,
                view => view.DecisionOwner = Side.Shadow,
                view => view.Decision.Window = "other-window",
                view => view.Decision.SourceId = "other-source",
                view => view.Decision.Kind = DecisionKind.Reaction,
                view => view.Battle = new BattleView(),
                view => view.Hunt = new HuntView(),
                view => view.EventLoss = new EventLossView(),
                view => view.Winner = Side.Shadow,
                view => view.Decision = null
            };
            foreach (var change in changes)
            {
                var after = View(DecisionKind.Action);
                after.Decision.Id = "next";
                change(after);
                Assert.IsFalse(DraftContinuity.CanCarry(View(DecisionKind.Action), after));
            }
            Assert.IsFalse(DraftContinuity.CanCarry(null, View(DecisionKind.Action)));
        }

        private static PlayerView View(DecisionKind kind) => new PlayerView
        {
            MatchId = "match", Turn = 2, Phase = GamePhase.ActionResolution,
            You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "current", Kind = kind }
        };
    }
}
