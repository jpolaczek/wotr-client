using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    public class CardActionTests
    {
        [Test]
        public void GestureUsesTheExactWildDieWithoutASeparateSymbolChoice()
        {
            var view = View();
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            Assert.IsFalse(CardActions.Start(draft, view, context, "card"));
            context.Sync(view, "wild");
            Assert.IsTrue(CardActions.Start(draft, view, context, "card"));
            Assert.AreEqual("wild", draft.Payload.DieId);
            Assert.AreEqual(DieFace.Event, draft.Payload.CardCost);
            Assert.AreEqual("card", draft.Payload.CardId);
        }

        [Test]
        public void ExplicitPlayCostKeepsTheWillChoiceAndRejectsAnUnavailableFace()
        {
            var view = View();
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            context.Sync(view, "wild");
            Assert.IsFalse(CardActions.Start(draft, view, context, "card", DieFace.Army));
            Assert.IsNull(draft.Kind);
            Assert.IsTrue(CardActions.Start(draft, view, context, "card", DieFace.Character));
            Assert.AreEqual("wild", draft.Payload.DieId);
            Assert.AreEqual(DieFace.Character, draft.Payload.CardCost);
        }

        [Test]
        public void RequiredNationRemainsUnselectedUntilPlayerTargetsIt()
        {
            var view = View();
            view.Options[0].Events[0].NationTargets.AddRange(new[] { "north", "elves" });
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            context.Sync(view, "event");
            Assert.IsTrue(CardActions.Start(draft, view, context, "card"));
            Assert.IsFalse(draft.Ready(view.Options[0]));
            Assert.IsEmpty(draft.Payload.NationId);
            draft.Change(p => p.NationId = "north");
            Assert.IsTrue(draft.Ready(view.Options[0]));
        }

        [Test]
        public void CombatAndFreeReactionDoNotInventAnActionDie()
        {
            var view = View();
            view.Decision.Kind = DecisionKind.CombatCard;
            view.Options.Clear();
            view.Options.Add(new ActionOption { Kind = CommandKind.CommitCombatCard, EligibleIds = { "card" } });
            var draft = new TurnDraft();
            Assert.IsTrue(CardActions.Start(draft, view, new ActionDieContext(), "card"));
            Assert.AreEqual(CommandKind.CommitCombatCard, draft.Kind);
            Assert.IsEmpty(draft.Payload.DieId);
            view.Decision.Kind = DecisionKind.Reaction;
            view.Options.Clear();
            view.Options.Add(new ActionOption { Kind = CommandKind.PlayEvent, Events = {
                new EventPlayOption { CardId = "card", Costs = { new EventCostOption { Face = DieFace.Character } } } } });
            Assert.IsTrue(CardActions.Start(draft, view, new ActionDieContext(), "card"));
            Assert.IsEmpty(draft.Payload.DieId);
        }

        [Test]
        public void DrawNeedsAnAvailableDeckAndDoesNotAdoptAnotherDie()
        {
            var view = View();
            view.Options.Add(new ActionOption { Kind = CommandKind.DrawCard, EligibleDice = { "event" }, Targets = { "strategy" } });
            var context = new ActionDieContext();
            var draft = new TurnDraft();
            Assert.IsFalse(CardActions.Draw(draft, view, context, "strategy"));
            context.Sync(view, "event");
            Assert.IsFalse(CardActions.Draw(draft, view, context, "character"));
            Assert.IsNull(draft.Kind);
            Assert.IsTrue(CardActions.Draw(draft, view, context, "strategy"));
            Assert.AreEqual("event", draft.Payload.DieId);
        }

        [Test]
        public void GroupedChoicesKeepOneExplicitCommit()
        {
            var view = View();
            view.Options.Clear();
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 2, EligibleIds = { "a", "b" } };
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            draft.Toggle("a");
            Assert.IsFalse(CardActions.SingleEffect(view, draft));
            option.Maximum = 1;
            Assert.IsTrue(CardActions.SingleEffect(view, draft));
        }

        [Test]
        public void SpecialConversionAndRulerTokenRemainDifferentExplicitCosts()
        {
            var view = View();
            view.Dice[0].Face = DieFace.Muster;
            view.Options[0].Events[0].Costs[0].AbilityId = CardDrawDraft.Palantir;
            var draft = new TurnDraft();
            var context = new ActionDieContext();
            context.Sync(view, "event");
            Assert.IsFalse(CardActions.Start(draft, view, context, "card"));
            draft.Change(p => { p.DieId = "event"; p.Face = DieFace.Event; p.AbilityId = CardDrawDraft.Palantir; });
            Assert.IsTrue(CardActions.Start(draft, view, context, "card"));
            Assert.AreEqual(CardDrawDraft.Palantir, draft.Payload.AbilityId);
            Assert.AreEqual(DieFace.Event, draft.Payload.CardCost);
            draft.Clear();
            view.Options[0].Events[0].Costs[0].AbilityId = "";
            view.RulerTokens.Add(new RulerTokenView { Id = "token", Face = DieFace.Event });
            view.RulerTokenActions.Add(new RulerTokenActionOption { TokenId = "token", Action = view.Options[0] });
            draft.Change(p => { p.RulerTokenId = "token"; p.DieId = "event"; });
            Assert.IsTrue(CardActions.Start(draft, view, context, "card"));
            Assert.AreEqual("token", draft.Payload.RulerTokenId);
            Assert.IsEmpty(draft.Payload.AbilityId);
            Assert.AreEqual(DieFace.Event, draft.Payload.CardCost);
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "cards", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
            Hand = { "card" },
            Dice = {
                new DieView { Id = "event", Owner = Side.FreePeoples, Face = DieFace.Event, Location = DieLocation.Available },
                new DieView { Id = "wild", Owner = Side.FreePeoples, Face = DieFace.WillOfTheWest, Location = DieLocation.Available } },
            Options = { new ActionOption { Kind = CommandKind.PlayEvent, Events = {
                new EventPlayOption { CardId = "card", Costs = {
                    new EventCostOption { DieId = "event", Face = DieFace.Event },
                    new EventCostOption { DieId = "wild", Face = DieFace.Event },
                    new EventCostOption { DieId = "wild", Face = DieFace.Character } } } } } }
        };
    }
}
