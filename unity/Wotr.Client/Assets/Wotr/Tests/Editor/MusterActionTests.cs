using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    public sealed class MusterActionTests
    {
        [Test]
        public void CombinedDieRecruitsDirectlyAndKeepsExactCost()
        {
            var fixture = new BoardFixture();
            var die = fixture.View.Dice.Single(d => d.Id == "muster");
            die.Face = DieFace.ArmyMuster;
            fixture.Refresh();
            var context = new ActionDieContext();
            context.Sync(fixture.View, die.Id);
            var draft = new TurnDraft();
            var figure = fixture.View.Units.First(u => u.Nation == "gondor" && u.Location == "reserve");
            Assert.IsTrue(MusterActions.Recruit(draft, fixture.View, context, figure.Id, "pelargir"));
            Assert.AreEqual(die.Id, draft.Payload.DieId);
            Assert.IsTrue(draft.Payload.Stepwise);
            Assert.AreEqual("reserve", figure.Location);
            Assert.AreEqual(1, draft.Payload.Placements.Count);
            Assert.IsTrue(draft.Ready(fixture.View.Options.Single(o => o.Kind == CommandKind.Recruit)));
            Assert.IsFalse(MusterActions.Recruit(draft, fixture.View, context, figure.Id, "edoras"));
            Assert.AreEqual("pelargir", draft.Payload.Placements.Single().RegionId);
        }

        [Test]
        public void PaidSequenceOnlyAllowsRemainingServerChoicesAndCannotChangeToPolitics()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            view.Decision.Kind = DecisionKind.ActionSequence;
            view.ActionSequence = new ActionSequenceView { Kind = CommandKind.Recruit, Owner = view.You, DieId = "muster", Face = DieFace.Muster, Remaining = 1 };
            var recruitment = view.Options.Single(o => o.Kind == CommandKind.Recruit);
            recruitment.Recruitment.RemoveAll(g => g.Kind == UnitKind.Elite);
            foreach (var group in recruitment.Recruitment)
                group.Targets.Remove("pelargir");
            view.Options.RemoveAll(o => o.Kind != CommandKind.Recruit);
            var context = new ActionDieContext();
            context.Sync(view, "");
            var draft = new TurnDraft();
            var elite = view.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Elite && u.Location == "reserve");
            Assert.IsFalse(MusterActions.Recruit(draft, view, context, elite.Id, "minas-tirith"));
            Assert.IsFalse(MusterActions.Politics(draft, view, context, "elves"));
            Assert.IsFalse(MusterActions.Targets(view, draft, context).Contains("pelargir"));
            var leader = view.Units.First(u => u.Nation == "gondor" && u.Kind == UnitKind.Leader && u.Location == "reserve");
            Assert.IsTrue(MusterActions.Recruit(draft, view, context, leader.Id, "dol-amroth"));
            Assert.IsTrue(MusterActions.OrphanLeader(view, UnitKind.Leader, "dol-amroth"));
            Assert.IsFalse(MusterActions.OrphanLeader(view, UnitKind.Leader, "minas-tirith"));
        }

        [Test]
        public void SpecialTokensAndAbilitiesKeepTheirOwnRecruitmentContract()
        {
            var fixture = new BoardFixture();
            var context = new ActionDieContext();
            context.Sync(fixture.View, "muster");
            var draft = new TurnDraft();
            draft.Change(p => p.RulerTokenId = "ruler-token");
            Assert.IsFalse(MusterActions.Active(fixture.View, draft, context));
            draft.Clear();
            draft.Choose(CommandKind.UseCharacterAbility, "muster");
            Assert.IsFalse(MusterActions.Active(fixture.View, draft, context));
        }
    }
}
