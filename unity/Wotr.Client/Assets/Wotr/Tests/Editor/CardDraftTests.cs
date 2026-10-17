using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class CardDraftTests
    {
        private static PlayerView View() => new PlayerView
        {
            Decision = new DecisionView { Id = "cards", Kind = DecisionKind.Action },
            Hand = { "base.fp.strategy.08" },
            Dice = { new DieView { Id = "wild", Face = DieFace.WillOfTheWest, Source = "standard", Location = DieLocation.Available } },
            Options = { new ActionOption { Kind = CommandKind.PlayEvent, Events = {
                new EventPlayOption { CardId = "base.fp.strategy.08", NationTargets = { "elves", "north" }, Costs = {
                    new EventCostOption { DieId = "wild", Face = DieFace.Event },
                    new EventCostOption { DieId = "wild", Face = DieFace.Muster }
                } }
            } } }
        };

        [Test]
        public void GroupedChoiceKeepsOneCompanionAndOneDieInASingleReversibleDraft()
        {
            var option = new ActionOption
            {
                Kind = CommandKind.ChooseEffect,
                Minimum = 2,
                Maximum = 2,
                EligibleIds = { "aragorn", "gimli", "die-a", "die-b" },
                EffectChoices = {
                    new EffectChoiceView { Id = "aragorn", SelectionGroup = "Companion" },
                    new EffectChoiceView { Id = "gimli", SelectionGroup = "Companion" },
                    new EffectChoiceView { Id = "die-a", SelectionGroup = "Miss" },
                    new EffectChoiceView { Id = "die-b", SelectionGroup = "Miss" }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            CardDraft.Choose(draft, option, "aragorn");
            Assert.IsFalse(CardDraft.Ready(draft, option));
            CardDraft.Choose(draft, option, "die-a");
            Assert.IsTrue(CardDraft.Ready(draft, option));
            var committed = draft.Fork();
            CardDraft.Choose(draft, option, "gimli");
            CollectionAssert.AreEquivalent(new[] { "gimli", "die-a" }, draft.Payload.Ids);
            draft.Back();
            CollectionAssert.AreEquivalent(committed.Payload.Ids, draft.Payload.Ids);
            draft.Change(p => { p.Ids.Clear(); p.Ids.AddRange(new[] { "aragorn", "gimli" }); });
            Assert.IsFalse(CardDraft.Ready(draft, option));
        }

        [Test]
        public void WildDieRequiresAnExplicitUseAndCommitCopiesTheWholeCardChoice()
        {
            var view = View();
            var draft = new TurnDraft();
            Assert.IsTrue(CardDraft.Select(draft, view, view.Hand[0]));
            Assert.IsEmpty(draft.Payload.DieId);
            CardDraft.SelectDie(draft, view.Options[0], "wild");
            Assert.IsNull(draft.Payload.Face);
            draft.Change(p => { p.Face = DieFace.Muster; p.CardCost = DieFace.Muster; p.NationId = "north"; });
            Assert.IsTrue(draft.Ready(view.Options[0]));
            Assert.AreEqual(JsonConvert.SerializeObject(draft.Payload), JsonConvert.SerializeObject(draft.Command(view).Payload));
            var copy = draft.Fork();
            draft.Back();
            Assert.IsFalse(draft.Ready(view.Options[0]));
            Assert.AreEqual("north", copy.Payload.NationId);
        }

        [Test]
        public void ReconnectDoesNotEraseValidCardCostOrReplaceAnInvalidTarget()
        {
            var view = View();
            var draft = new TurnDraft();
            CardDraft.Select(draft, view, view.Hand[0]);
            CardDraft.SelectDie(draft, view.Options[0], "wild");
            draft.Change(p => { p.Face = DieFace.Event; p.CardCost = DieFace.Event; p.NationId = "north"; });
            draft.Reconcile(view);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            view.Options[0].Events[0].NationTargets.Remove("north");
            draft.Reconcile(view);
            Assert.IsEmpty(draft.Payload.NationId);
            Assert.AreEqual("wild", draft.Payload.DieId);
            Assert.IsFalse(draft.Ready(view.Options[0]));
        }

        [Test]
        public void CardDropMatchesClickAndAnUnavailableCardCannotReplaceTheDraft()
        {
            var view = View();
            var click = new TurnDraft();
            var drag = new TurnDraft();
            CardDraft.Select(click, view, view.Hand[0]);
            Assert.IsTrue(DraftGesture.Drop(drag, view, new DraftToken(DraftObject.Card, view.Hand[0]), new DraftToken(DraftObject.Tray)));
            Assert.AreEqual(JsonConvert.SerializeObject(click.Payload), JsonConvert.SerializeObject(drag.Payload));
            Assert.IsFalse(CardDraft.Select(drag, view, "base.fp.character.19"));
            Assert.AreEqual(JsonConvert.SerializeObject(click.Payload), JsonConvert.SerializeObject(drag.Payload));
        }
    }
}
