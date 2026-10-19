using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-272: the Lords cards' choices on their objects (CARD_CHOICES_UX).
    public sealed class LordsCardChoicesTests
    {
        // Lore is die → face: the chosen die's results, a special die's with the Remove star, Keep results in the console.
        [Test]
        public void LoreOffersTheChosenDiesResultsAndKeepsResultsInTheConsole()
        {
            var view = Decision(Side.Shadow, DecisionKind.EffectChoice, LoreChoice.Presentation, CommandKind.ChooseEffect,
                new EffectChoiceView { Id = LoreChoice.Keep, Title = "Keep results", Exclusive = true },
                new EffectChoiceView { Id = "balrog", SelectionGroup = LoreChoice.Group },
                new EffectChoiceView { Id = "ruler", SelectionGroup = LoreChoice.Group },
                new EffectChoiceView { Id = "lore.Muster.keep", SelectionGroup = "Result", RequiresAny = { "balrog", "ruler" } },
                new EffectChoiceView { Id = "lore.Muster.remove", SelectionGroup = "Result", RequiresAny = { "balrog" } });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsEmpty(LoreChoice.Faces(view, draft), "No die is chosen yet.");
            StringAssert.StartsWith("Click a lit die", LoreChoice.Hint(view, draft));
            draft.Change(p => p.Ids.Add("ruler"));
            CollectionAssert.AreEqual(new[] { "lore.Muster.keep" }, LoreChoice.Faces(view, draft).Select(f => f.Id));
            draft.Change(p => p.Ids[0] = "balrog");
            var faces = LoreChoice.Faces(view, draft);
            CollectionAssert.AreEqual(new[] { false, true }, faces.Select(f => f.Remove));
            Assert.IsTrue(faces.All(f => f.Face == DieFace.Muster));
            var console = CardConsoleOptions.For(view, draft);
            CollectionAssert.AreEqual(new[] { "effect-choice-" + LoreChoice.Keep }, console.Select(o => o.Name));
        }

        // With Kings, Lore before Dismay is the Lore card's click in the Effects panel, not a tray button.
        [Test]
        public void LoreBeforeDismayIsTheCardsClick()
        {
            var view = Decision(Side.Shadow, DecisionKind.Dismay, "", CommandKind.UseDismay);
            view.Options.Add(new ActionOption { Kind = CommandKind.UseLoreBeforeActions, Enabled = true });
            Assert.AreEqual(LoreChoice.CardUse, LoreChoice.CardUses(view)[LoreChoice.Card]);
            Assert.IsFalse(ExpansionDecision.Alternatives(view, new TurnDraft()).Any(o => o.Kind == CommandKind.UseLoreBeforeActions));
            view.Options.Last().Enabled = false;
            Assert.IsEmpty(LoreChoice.CardUses(view));
        }

        // We Shall Get It! in the Hunt is a response object; We Won't Go Back's cost offers the card and No Corruption.
        [Test]
        public void HuntCardResponsesStandInTheResponseRow()
        {
            var view = Decision(Side.Shadow, DecisionKind.Reaction, "", CommandKind.RespondEffect,
                new EffectChoiceView { Id = "lords.we-shall-get-it", SourceId = "lords.sh.special.05", Title = "Replace the tile" });
            view.Hunt = new HuntView();
            view.Players.Add(new PlayerSummary { Side = Side.Shadow, TableCards = { "lords.sh.special.05" } });
            var reaction = HuntCardResponses.Offered(view).Single();
            Assert.IsTrue(reaction.Reaction);
            Assert.AreEqual("Replace the tile", reaction.Consequence);
            StringAssert.StartsWith("Click to discard", reaction.Tooltip);
            view.Hunt = null;
            Assert.IsEmpty(HuntCardResponses.Offered(view), "Outside the Hunt the Effects panel's × answers.");

            var cost = Decision(Side.Shadow, DecisionKind.EffectChoice, HuntCardResponses.Smeagol, CommandKind.ChooseEffect,
                new EffectChoiceView { Id = HuntCardResponses.WeWontGoBack },
                new EffectChoiceView { Id = HuntCardResponses.NoCorruption },
                new EffectChoiceView { Id = "base.sh.character.01" });
            cost.Hunt = new HuntView();
            CollectionAssert.AreEqual(new[] { HuntCardResponses.WeWontGoBack, HuntCardResponses.NoCorruption },
                HuntCardResponses.Offered(cost).Select(r => r.Id));
            Assert.IsFalse(HuntCardResponses.Offered(cost).Any(r => r.Reaction));
        }

        // Wind from the West: the Shadow table cards at their ×, the inspected hand in the drawer.
        [Test]
        public void WindsChoicesStandOnTheTableCardsAndTheInspectedHand()
        {
            var table = Decision(Side.FreePeoples, DecisionKind.EffectChoice, "TableCards", CommandKind.ChooseEffect,
                new EffectChoiceView { Id = "lords.sh.character.27", SourceId = "lords.sh.character.27", Detail = "Discard from the table without effect." });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.IsTrue(CardConsoleOptions.Owns(table, draft));
            CollectionAssert.AreEqual(new[] { "console-option-table-cards" }, CardConsoleOptions.For(table, draft).Select(o => o.Name));
            CollectionAssert.AreEqual(new[] { "lords.sh.character.27" }, CardConsoleOptions.TableCards(table, draft).Keys);

            var hand = Decision(Side.FreePeoples, DecisionKind.EffectChoice, InspectedHand.Presentation, CommandKind.ChooseEffect,
                new EffectChoiceView { Id = "base.sh.character.01", SourceId = "base.sh.character.01" });
            CollectionAssert.AreEqual(new[] { "base.sh.character.01" }, InspectedHand.Cards(hand));
            Assert.AreEqual(DecisionHost.Hand, DecisionRoutes.Of(hand));
        }

        // Three Rings: the count left to recover, on the sockets.
        [Test]
        public void ThreeRingsCountsTheRingsLeftToRecover()
        {
            var view = Decision(Side.FreePeoples, DecisionKind.EffectChoice, RingRecovery.Presentation, CommandKind.ChooseEffect,
                new EffectChoiceView { Id = "narya" }, new EffectChoiceView { Id = "nenya" });
            view.Options[0].Minimum = view.Options[0].Maximum = 2;
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            Assert.AreEqual("Click 2 glowing Rings in the console to recover them", RingRecovery.Hint(view, draft));
            Assert.IsEmpty(CardConsoleOptions.For(view, draft), "The Rings are chosen on their sockets.");
            Assert.AreEqual(CommandKind.ChooseEffect, ExpansionActions.RingKind(view, draft));
            draft.Change(p => p.Ids.Add("narya"));
            Assert.AreEqual("Click 1 glowing Ring in the console to recover it", RingRecovery.Hint(view, draft));
        }

        private static PlayerView Decision(Side side, DecisionKind kind, string presentation, CommandKind command, params EffectChoiceView[] choices)
        {
            var option = new ActionOption { Kind = command, Enabled = true, Minimum = 1, Maximum = 1 };
            foreach (var choice in choices)
            {
                option.EffectChoices.Add(choice);
                option.EligibleIds.Add(choice.Id);
            }
            return new PlayerView
            {
                You = side, DecisionOwner = side,
                Decision = new DecisionView { Id = "d", Kind = kind, Presentation = presentation },
                Options = { option }
            };
        }
    }
}
