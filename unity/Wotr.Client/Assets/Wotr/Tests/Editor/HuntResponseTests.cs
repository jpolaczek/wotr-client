using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    public sealed class HuntResponseTests
    {
        [TestCase(HuntResponse.Ring)]
        [TestCase(HuntResponse.Guide)]
        [TestCase(HuntResponse.RandomCompanion)]
        public void ResponseIsLocalAndUsesOnlyTheExplicitServerOption(HuntResponse response)
        {
            var view = View();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ResolveHuntDamage);
            Assert.IsFalse(draft.Ready(view.Options[0]));
            Assert.IsTrue(HuntResponseActions.Select(view, draft, response));
            Assert.IsTrue(draft.Ready(view.Options[0]));
            Assert.IsFalse(FellowshipActions.Immediate(draft.Kind));
            Assert.AreEqual(response, draft.Command(view).Payload.HuntResponse);
            Assert.IsEmpty(draft.Payload.Ids);
            Assert.IsEmpty(draft.Payload.CharacterId);
            Assert.IsEmpty(draft.Payload.GuideId);
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.AreEqual(4, view.Fellowship.Corruption);
            var restored = new TurnDraft();
            restored.Restore(draft.Snapshot());
            restored.Reconcile(view);
            Assert.AreEqual(response, restored.HuntChoice);
            view.Options[0].HuntResponses.Remove(response);
            restored.Reconcile(view);
            Assert.IsNull(restored.HuntChoice);
            Assert.IsFalse(HuntResponseActions.Select(view, restored, response));
            view.You = Side.Shadow;
            Assert.IsNull(HuntResponseActions.Option(view));
        }

        [Test]
        public void PreviewDistinguishesKnownDamageHobbitReturnAndUnknownVictim()
        {
            var view = View();
            StringAssert.Contains("Corruption 4 → 7", HuntResponseActions.Describe(view, HuntResponse.Ring));
            StringAssert.Contains("Damage 3 → 0", HuntResponseActions.Describe(view, HuntResponse.Guide));
            StringAssert.Contains("Lose the Guide", HuntResponseActions.Describe(view, HuntResponse.Guide));
            view.Hunt.GuideReturns = true;
            StringAssert.Contains("Hobbit returns", HuntResponseActions.Describe(view, HuntResponse.Guide));
            StringAssert.Contains("unknown", HuntResponseActions.Describe(view, HuntResponse.RandomCompanion));
            view.Hunt.DamagePending = true;
            StringAssert.Contains("not determined", HuntResponseActions.Describe(view, HuntResponse.Ring));
        }

        [Test]
        public void ConsequencesAreVisibleBeforeSelectionAndNeverPredictARandomVictim()
        {
            var view = View();
            var responses = new HuntResponsesView();
            responses.Context(view, new TurnDraft(), true);
            Assert.AreEqual("Corruption\n4/12 → 7/12", responses.Root.Q<Button>("hunt-response-Ring").Q<Label>("hunt-response-consequence").text);
            Assert.AreEqual("Lose Guide\nDamage 3 → 0", responses.Root.Q<Button>("hunt-response-Guide").Q<Label>("hunt-response-consequence").text);
            Assert.AreEqual("Random casualty\nDamage left ?", responses.Root.Q<Button>("hunt-response-RandomCompanion").Q<Label>("hunt-response-consequence").text);
            Assert.AreEqual(DisplayStyle.Flex, responses.Root.Q<Label>("hunt-guide-order").style.display.value);
            view.Hunt.GuideReturns = true;
            view.OwnCompanions[0].Level = 1;
            Assert.AreEqual("Hobbit returns\nDamage 3 → 2", HuntResponseActions.Consequence(view, HuntResponse.Guide));
            view.OwnCompanions.Clear();
            StringAssert.EndsWith("→ ?", HuntResponseActions.Consequence(view, HuntResponse.Guide));
            view.Hunt.DamagePending = true;
            StringAssert.Contains("Damage ?", HuntResponseActions.Consequence(view, HuntResponse.Ring));
        }

        [Test]
        public void ResponseObjectsStayStableAndHiddenOutsideTheirOwnDecision()
        {
            var view = View();
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ResolveHuntDamage);
            var responses = new HuntResponsesView();
            responses.Context(view, draft, true);
            var random = responses.Root.Q<Button>("hunt-response-RandomCompanion");
            HuntResponseActions.Select(view, draft, HuntResponse.RandomCompanion);
            responses.Context(view, draft, true);
            Assert.AreSame(random, responses.Root.Q<Button>(random.name));
            Assert.IsTrue(random.ClassListContains("selected"));
            Assert.IsTrue(HuntResponseActions.Read((DraftToken)random.userData, out var response));
            Assert.AreEqual(HuntResponse.RandomCompanion, response);
            Assert.IsFalse(HuntResponseActions.Read(new DraftToken(DraftObject.Ring), out _));
            view.Decision.Kind = DecisionKind.Reaction;
            responses.Context(view, draft, true);
            Assert.AreEqual(DisplayStyle.None, responses.Root.style.display.value);
            Assert.AreEqual(DisplayStyle.None, random.style.display.value);
            Assert.IsFalse(random.enabledSelf);
        }

        private static PlayerView View() => new PlayerView
        {
            You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "damage", Kind = DecisionKind.HuntDamage },
            Hunt = new HuntView { Damage = 3 },
            Fellowship = new FellowshipView { Corruption = 4, Guide = "base.character.strider" },
            OwnCompanions = { new CharacterView { Id = "base.character.strider", Level = 3 } },
            Options = { new ActionOption { Kind = CommandKind.ResolveHuntDamage,
                HuntResponses = { HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion } } }
        };
    }
}
