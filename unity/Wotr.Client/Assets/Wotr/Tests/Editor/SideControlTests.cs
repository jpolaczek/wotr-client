using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    public sealed class SideControlTests
    {
        [Test]
        public void FellowshipControlsDisappearForShadowAndRecoverWhenThePerspectiveChanges()
        {
            var view = new PlayerView { You = Side.FreePeoples, Phase = GamePhase.ActionResolution, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.Action }, Fellowship = new FellowshipView { Hidden = true } };
            var controls = new FellowshipControls();
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            controls.Refresh(view, draft, context, true, false);
            // UI-110: the move is the glowing track symbol (FellowshipStatus), not a button in this row.
            Assert.IsNull(controls.Root.Q<Button>("fellowship-progress"));
            Assert.IsFalse(controls.MoveOffered, "A missing cost offers no move.");
            var hide = controls.Root.Q<Button>("fellowship-state");
            Assert.AreEqual(DisplayStyle.Flex, hide.style.display.value);
            Assert.IsFalse(hide.enabledSelf, "A missing cost is explained to the owning player.");

            view.You = Side.Shadow;
            view.Decision = null;
            controls.Refresh(view, draft, context, true, false);
            Assert.AreEqual(DisplayStyle.None, controls.Root.style.display.value);
            foreach (var button in controls.Root.Query<Button>().ToList())
            {
                Assert.AreEqual(DisplayStyle.None, button.style.display.value);
                Assert.IsFalse(button.enabledInHierarchy);
            }

            view.You = Side.FreePeoples;
            controls.Refresh(view, draft, context, true, false);
            Assert.AreEqual(DisplayStyle.Flex, controls.Root.style.display.value);
            Assert.AreSame(hide, controls.Root.Q<Button>("fellowship-state"));
            Assert.AreEqual(DisplayStyle.Flex, hide.style.display.value);
        }

        [Test]
        public void HuntAllocationAndDamageChoicesBelongToTheProjectedDecisionOwner()
        {
            var view = new PlayerView { You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Kind = DecisionKind.AllocateHunt } };
            view.Options.Add(new ActionOption { Kind = CommandKind.AllocateHunt, EligibleIds = { "die" }, Maximum = 1 });
            var responses = new HuntResponsesView();
            var draft = new TurnDraft();
            // UI-162: the allocation is the console's, offered only to the projected owner.
            Assert.IsNotNull(HuntActions.Allocation(view));
            view.You = Side.FreePeoples;
            Assert.IsNull(HuntActions.Allocation(view));

            view.DecisionOwner = Side.FreePeoples;
            view.Decision.Kind = DecisionKind.HuntDamage;
            view.Options.Clear();
            view.Options.Add(new ActionOption { Kind = CommandKind.ResolveHuntDamage, HuntResponses = { HuntResponse.Ring } });
            responses.Context(view, draft, true);
            Assert.AreEqual(DisplayStyle.Flex, responses.Root.style.display.value);
            view.You = Side.Shadow;
            responses.Context(view, draft, true);
            Assert.AreEqual(DisplayStyle.None, responses.Root.style.display.value);
        }
    }
}
