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
            // UI-220: hiding is the status plaque's click, not a button in this row.
            Assert.IsNull(controls.Root.Q<Button>("fellowship-state"));
            Assert.IsFalse(controls.HideOffered);

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
        }

        // UI-220: a revealed Fellowship with a legal Hide and its die chosen is hidden from the status plaque.
        [Test]
        public void HidingIsOfferedOnlyForARevealedFellowshipWithTheChosenLegalDie()
        {
            var view = new PlayerView { You = Side.FreePeoples, Phase = GamePhase.ActionResolution, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.Action }, Fellowship = new FellowshipView { Hidden = false },
                Options = { new ActionOption { Kind = CommandKind.HideFellowship, EligibleDice = { "character" } } },
                Dice = { new DieView { Id = "character", Owner = Side.FreePeoples, Source = "standard", Location = DieLocation.Available,
                    Face = DieFace.Character } } };
            var controls = new FellowshipControls();
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            controls.Refresh(view, draft, context, true, false);
            Assert.IsFalse(controls.HideOffered, "no die chosen");
            context.Sync(view, "character");
            controls.Refresh(view, draft, context, true, false);
            Assert.IsTrue(controls.HideOffered);
            Assert.IsTrue(controls.Offers, "the plaque invites a look");
            controls.Refresh(view, draft, context, false, false);
            Assert.IsFalse(controls.HideOffered, "not while a command is being sent");
            view.Fellowship.Hidden = true;
            controls.Refresh(view, draft, context, true, false);
            Assert.IsFalse(controls.HideOffered, "a hidden Fellowship has nothing to hide");
            view.Fellowship.Hidden = false;
            view.You = Side.Shadow;
            controls.Refresh(view, draft, context, true, false);
            Assert.IsFalse(controls.HideOffered, "only the Free Peoples hide");
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
