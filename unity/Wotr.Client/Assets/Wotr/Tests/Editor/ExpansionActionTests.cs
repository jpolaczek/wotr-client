using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class ExpansionActionTests
    {
        [Test]
        public void RingPreservesAnExplicitCostButNeverSubstitutesAnotherDie()
        {
            var view = View();
            var option = new ActionOption { Kind = CommandKind.UseRing };
            option.EligibleDice.AddRange(new[] { "first", "second" });
            option.EligibleIds.Add("ring");
            view.Options.Add(option);
            var draft = new TurnDraft();
            Assert.IsTrue(ExpansionActions.Ring(view, draft, "ring"));
            Assert.IsEmpty(draft.Payload.DieId);
            draft.Clear();
            draft.Change(p => p.DieId = "second");
            Assert.IsTrue(ExpansionActions.Ring(view, draft, "ring"));
            Assert.AreEqual("second", draft.Payload.DieId);
            Assert.IsNull(draft.Payload.Face);
            option.Enabled = false;
            Assert.IsFalse(ExpansionActions.Ring(view, draft, "ring"));
            Assert.AreEqual("second", draft.Payload.DieId);
        }

        [Test]
        public void EyeSelectionRequiresEligibleProvenanceAndDoesNotChooseASovereign()
        {
            var view = View();
            var option = new ActionOption { Kind = CommandKind.AttemptCorruption };
            option.EligibleDice.Add("keeper-eye");
            option.EligibleIds.Add("thranduil");
            view.Options.Add(option);
            var draft = new TurnDraft();
            Assert.IsFalse(ExpansionActions.Eye(view, draft, "captain-eye"));
            Assert.IsNull(draft.Kind);
            Assert.IsTrue(ExpansionActions.Eye(view, draft, "keeper-eye"));
            Assert.IsEmpty(draft.Payload.CharacterId);
            Assert.IsFalse(draft.Ready(option));
            Assert.IsFalse(ExpansionActions.SelectSovereign(view, draft, "denethor"));
            Assert.IsTrue(ExpansionActions.SelectSovereign(view, draft, "thranduil"));
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual("keeper-eye", draft.Payload.DieId);
        }

        [Test]
        public void AwakeningUsesTheChosenSovereignsCostAndRequiresItsActualRoute()
        {
            var view = View();
            var option = new ActionOption { Kind = CommandKind.AwakenSovereign };
            option.EligibleIds.Add("dain");
            option.EligibleDice.Add("character");
            option.CharacterActions.Add(new CharacterActionOption { CharacterId = "dain", Dice = { "character" } });
            option.Travel.Add(new TravelOption { Id = "dain", Origin = "erebor", Range = 2, Targets = { "erebor", "dale" } });
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "muster");
            Assert.IsFalse(ExpansionActions.SelectSovereign(view, draft, "dain"));
            draft.Change(p => p.DieId = "character");
            Assert.IsTrue(ExpansionActions.SelectSovereign(view, draft, "dain"));
            Assert.IsEmpty(draft.Payload.Route);
            Assert.IsFalse(draft.Ready(DraftOptions.For(view, draft)));
            Assert.IsTrue(TravelActions.End(draft, view, "erebor"));
            Assert.IsTrue(draft.Ready(DraftOptions.For(view, draft)));
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "resources", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action }
        };
    }
}
