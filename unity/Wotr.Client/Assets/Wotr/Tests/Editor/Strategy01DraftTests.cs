using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class Strategy01DraftTests
    {
        [Test]
        public void RemovalReplacesOnlySameTypeAndPreservesOtherCostAcrossUndo()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.DiscardProtection,
                Minimum = 2,
                Maximum = 2,
                EligibleIds = { "army1", "army2", "character" },
                EligibleDice = { "die" },
                Targets = { "protection" },
                EffectChoices = { new EffectChoiceView { Id = "army1", SelectionGroup = "Army" },
                    new EffectChoiceView { Id = "army2", SelectionGroup = "Army" },
                    new EffectChoiceView { Id = "character", SelectionGroup = "Character" } }
            };
            var view = new PlayerView { Decision = new DecisionView { Kind = DecisionKind.Action }, Options = { option } };
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            draft.Change(p => p.DieId = "die");
            Assert.IsFalse(draft.Ready(option));
            CardDraft.Choose(draft, option, "army1");
            CardDraft.Choose(draft, option, "character");
            Assert.IsTrue(draft.Ready(option));
            Assert.IsTrue(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Card, "army2"), new DraftToken(DraftObject.Tray)));
            CollectionAssert.AreEquivalent(new[] { "army2", "character" }, draft.Payload.Ids);
            draft.Back();
            CollectionAssert.AreEquivalent(new[] { "army1", "character" }, draft.Payload.Ids);
            option.EligibleDice.Clear();
            draft.Reconcile(view);
            Assert.IsFalse(draft.Ready(option));
        }

        [Test]
        public void BookAllowsEmptyMovementWhileOrdinaryCompanionActionDoesNot()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveCompanions);
            var option = new ActionOption { Kind = CommandKind.MoveCompanions, AllowEmptySelection = true };
            Assert.IsTrue(draft.Ready(option));
            option.AllowEmptySelection = false;
            Assert.IsFalse(draft.Ready(option));
        }
    }
}
