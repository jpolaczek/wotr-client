using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    public sealed class PoliticalActionTests
    {
        [Test]
        public void PassiveNationMayAdvanceDirectlyWithWillAndOnlyCurrentServerTargets()
        {
            var fixture = new BoardFixture();
            var view = fixture.View;
            var north = view.Nations.Single(n => n.Id == "north");
            north.Active = false;
            north.StepsToWar = 3;
            view.Dice.Single(d => d.Id == "muster").Face = DieFace.WillOfTheWest;
            fixture.Refresh();
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            context.Sync(view, "muster");
            Assert.IsTrue(PoliticalActions.Advance(draft, view, context, "elves"));
            Assert.IsTrue(PoliticalActions.Advance(draft, view, context, north.Id));
            Assert.AreEqual(north.Id, draft.Payload.NationId);
            Assert.AreEqual("muster", draft.Payload.DieId);
            Assert.AreEqual(3, north.StepsToWar, "The gesture cannot change the authoritative state.");
            Assert.IsFalse(north.Active);
            view.Options.Single(o => o.Kind == CommandKind.AdvancePolitics).Targets.Remove(north.Id);
            Assert.IsFalse(PoliticalActions.Advance(draft, view, context, north.Id));
            view.DecisionOwner = Side.Shadow;
            Assert.IsNull(PoliticalActions.AdvanceOption(view, draft, context));
        }

        [Test]
        public void CouncilUsesTheChosenTokenAndCannotSubstituteForADie()
        {
            var view = new BoardFixture().View;
            var option = new ActionOption { Kind = CommandKind.UseCouncilToken };
            option.EligibleIds.Add(ExpansionDraft.CouncilPolitics);
            option.Targets.Add("sauron");
            view.Options.Add(option);
            var context = new ActionDieContext();
            var draft = new TurnDraft();
            Assert.IsFalse(PoliticalActions.Advance(draft, view, context, "sauron"));
            draft.Choose(CommandKind.UseCouncilToken);
            draft.Change(p => p.AbilityId = ExpansionDraft.CouncilPolitics);
            Assert.IsTrue(PoliticalActions.Advance(draft, view, context, "sauron"));
            Assert.AreEqual(ExpansionDraft.CouncilPolitics, draft.Payload.AbilityId);
            Assert.IsFalse(ComponentActions.Immediate(view, draft), "The destination gesture commits Council politics.");
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsFalse(PoliticalActions.Advance(draft, view, context, "elves"));
            option.Enabled = false;
            Assert.IsFalse(PoliticalActions.Advance(draft, view, context, "sauron"));
        }

        [Test]
        public void ActivationRequiresNaryaItsLegalNationAndAnExplicitEligibleDie()
        {
            var view = new BoardFixture().View;
            var nation = view.Nations.Single(n => n.Id == "north");
            nation.Active = false;
            nation.StepsToWar = 3;
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "muster");
            Assert.IsFalse(PoliticalActions.Activate(draft, view, nation.Id), "Muster does not activate nations.");
            var narya = new ActionOption { Kind = CommandKind.UseNarya };
            narya.EligibleDice.Add("army");
            narya.EligibleIds.Add("lords.ring.narya");
            narya.Targets.Add(nation.Id);
            view.Options.Add(narya);
            Assert.IsFalse(PoliticalActions.Activate(draft, view, nation.Id));
            draft.Change(p => p.DieId = "army");
            Assert.IsFalse(PoliticalActions.Activate(draft, view, "rohan"));
            Assert.IsTrue(PoliticalActions.Activate(draft, view, nation.Id));
            Assert.AreEqual(CommandKind.UseNarya, draft.Kind);
            Assert.AreEqual("army", draft.Payload.DieId);
            Assert.AreEqual("lords.ring.narya", draft.Payload.RingId);
            Assert.AreEqual(nation.Id, draft.Payload.NationId);
            Assert.IsFalse(nation.Active);
            Assert.AreEqual(3, nation.StepsToWar);
            Assert.IsTrue(draft.Ready(narya));
            view.DecisionOwner = Side.Shadow;
            Assert.IsFalse(PoliticalActions.CanActivate(view, draft, nation.Id));
        }
    }
}
