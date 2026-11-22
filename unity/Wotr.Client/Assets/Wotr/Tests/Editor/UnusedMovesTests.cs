using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    // UI-571 (owner, 2026-11-22): what the ✓ that ends a die's action would leave unused, in the player's terms; nothing
    // when no figure could still act, when the action is the opponent's or when nothing is left.
    public class UnusedMovesTests
    {
        [Test]
        public void ArmiesRecruitmentAndFiguresLeftAreNamed()
        {
            Assert.AreEqual("You moved 1 of 2 armies this die allows. Another army can still move.",
                UnusedMoves.Sequence(View(CommandKind.MoveArmies, 1, 1)));
            Assert.AreEqual("1 recruitment point is still unused.", UnusedMoves.Sequence(View(CommandKind.Recruit, 1, 1)));
            Assert.AreEqual("2 recruitment points are still unused.", UnusedMoves.Sequence(View(CommandKind.Recruit, 0, 2)));
            var figures = View(CommandKind.MoveShadowCharacters, 1, 3);
            var option = figures.Options[0];
            option.Travel.Add(Travel("nazgul-1", "minas-morgul", "osgiliath"));
            option.Travel.Add(Travel("nazgul-2", "minas-morgul", "minas-morgul"));
            option.Travel.Add(Travel("nazgul-3", "dol-guldur", "lorien"));
            figures.ActionSequence.UsedIds.Add("nazgul-3");
            Assert.AreEqual("1 figure that can still move has not moved.", UnusedMoves.Sequence(figures),
                "A figure that may go nowhere but where it stands, or already moved, is no unused move.");
        }

        [Test]
        public void NothingIsLeftWithoutAFigureThatCanActOrForTheOpponent()
        {
            var disabled = View(CommandKind.MoveArmies, 1, 1);
            disabled.Options[0].Enabled = false;
            Assert.IsNull(UnusedMoves.Sequence(disabled), "The server's option is off: no army can move again.");
            Assert.IsNull(UnusedMoves.Sequence(View(CommandKind.Recruit, 2, 0)));
            var opponent = View(CommandKind.MoveArmies, 1, 1);
            opponent.ActionSequence.Owner = Side.Shadow;
            Assert.IsNull(UnusedMoves.Sequence(opponent));
            var stuck = View(CommandKind.MoveCompanions, 0, 1);
            stuck.Options[0].Travel.Add(Travel("gimli", "erebor", "erebor"));
            Assert.IsNull(UnusedMoves.Sequence(stuck));
            var other = View(CommandKind.MoveArmies, 1, 1);
            other.Decision.Kind = DecisionKind.Action;
            Assert.IsNull(UnusedMoves.Sequence(other), "Only an action of several steps under way.");
            Assert.IsNull(UnusedMoves.Sequence(null));
            Assert.IsNull(UnusedMoves.Flights(other, null));
        }

        private static PlayerView View(CommandKind kind, int completed, int remaining) => new PlayerView
        {
            You = Side.FreePeoples,
            Decision = new DecisionView { Kind = DecisionKind.ActionSequence },
            ActionSequence = new ActionSequenceView { Kind = kind, Owner = Side.FreePeoples, CompletedSteps = completed, Remaining = remaining },
            Options = { new ActionOption { Kind = kind, Enabled = true }, new ActionOption { Kind = CommandKind.FinishActionSequence } }
        };

        private static TravelOption Travel(string id, string origin, params string[] targets)
        {
            var travel = new TravelOption { Id = id, Origin = origin };
            travel.Targets.AddRange(targets);
            return travel;
        }
    }
}
