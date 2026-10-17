using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-242: hints name what the player clicks instead of "components".
    public sealed class DecisionObjectsTests
    {
        [Test]
        public void HintsNameTheObjectsOfTheStep()
        {
            var view = new PlayerView { Decision = new DecisionView { Kind = DecisionKind.Action } };
            Assert.AreEqual("Select the highlighted armies and regions, then confirm.", DecisionObjects.Confirm(view, Draft(CommandKind.MoveArmies)));
            Assert.AreEqual("Select the highlighted units", DecisionObjects.Prompt(view, Draft(CommandKind.TakeBattleLosses)));
            Assert.AreEqual("Select the highlighted characters", DecisionObjects.Prompt(view, Draft(CommandKind.MoveCompanions)));
            view.Decision = new DecisionView { Kind = DecisionKind.EffectChoice, Presentation = "Region" };
            Assert.AreEqual("Select the highlighted regions", DecisionObjects.Prompt(view, Draft(CommandKind.ChooseEffect)));
            Assert.AreEqual("That die cannot be used here.", DecisionObjects.Unusable(new DraftToken(DraftObject.Die, "d")));
            Assert.AreEqual("That cannot be used here.", DecisionObjects.Unusable(null));
            foreach (var kind in System.Enum.GetValues(typeof(CommandKind)))
                StringAssert.DoesNotContain("component", DecisionObjects.Confirm(view, Draft((CommandKind)kind)));
        }

        private static TurnDraft Draft(CommandKind kind)
        {
            var draft = new TurnDraft();
            draft.Choose(kind);
            return draft;
        }
    }
}
