using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class CardPack04DraftTests
    {
        [Test]
        public void OptionalSingleCompanionCanBeDeselectedAndDestinationRemainsRequired()
        {
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 2, EligibleIds = { "gimli", "pelargir" } };
            option.EffectChoices.Add(new EffectChoiceView { Id = "gimli", SelectionGroup = "companions", GroupMinimum = 0 });
            option.EffectChoices.Add(new EffectChoiceView { Id = "pelargir", SelectionGroup = "destination" });
            var draft = new TurnDraft();
            draft.Choose(option.Kind);
            CardDraft.Choose(draft, option, "gimli");
            Assert.IsFalse(draft.Ready(option));
            CardDraft.Choose(draft, option, "pelargir");
            Assert.IsTrue(draft.Ready(option));
            CardDraft.Choose(draft, option, "gimli");
            Assert.IsTrue(draft.Ready(option));
            CollectionAssert.AreEqual(new[] { "pelargir" }, draft.Payload.Ids);
        }

        [Test]
        public void FreeCharacterEventCanBeClickedOrDraggedWithoutSelectingDie()
        {
            var view = new PlayerView { Decision = new DecisionView { Kind = DecisionKind.Reaction } };
            var option = new ActionOption { Kind = CommandKind.PlayEvent };
            option.Events.Add(new EventPlayOption { CardId = "card", Costs = { new EventCostOption { Face = DieFace.Character } } });
            view.Options.Add(option);
            var draft = new TurnDraft();
            Assert.IsTrue(CardDraft.Select(draft, view, "card"));
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.DieId);
            draft.Clear();
            Assert.IsTrue(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Card, "card"), new DraftToken(DraftObject.Tray)));
            Assert.IsTrue(draft.Ready(option));
        }
    }
}
