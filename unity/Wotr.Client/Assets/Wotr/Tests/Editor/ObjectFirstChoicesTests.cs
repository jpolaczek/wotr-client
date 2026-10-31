using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-430 (owner's rule, 2026-10-30): no button for an option the board draws. A card's Finish is the console's ✓
    // (FinishingOption), The Nazgûl Strike!'s Hunt roll the glowing Hunt Box and its table cards red in the Effects panel;
    // every lit object's tooltip names the action, the effect that offers it and what it does.
    public sealed class ObjectFirstChoicesTests
    {
        private const string Strike = "base.sh.character.11";
        private const string Staff = "base.fp.character.08";
        private const string Mithril = "base.fp.character.05";

        private static PlayerView Decision(string presentation, string source, params string[] ids)
        {
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            foreach (var id in ids)
            {
                option.EffectChoices.Add(new EffectChoiceView { Id = id, SourceId = id.StartsWith("base.fp.") ? id : "" });
                option.EligibleIds.Add(id);
            }
            return new PlayerView
            {
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "d", Kind = DecisionKind.EffectChoice, Presentation = presentation, SourceId = source },
                Options = { option }
            };
        }

        private static PlayerView StrikeDecision(params string[] tableCards)
        {
            var ids = new System.Collections.Generic.List<string>(tableCards) { NazgulStrikeChoice.HuntRoll, FinishingOption.StrikeFinish };
            var view = Decision(NazgulStrikeChoice.Presentation, Strike, ids.ToArray());
            view.Dice.Add(new DieView { Id = "eye-1", Owner = Side.Shadow, Location = DieLocation.Hunt, Face = DieFace.Eye });
            view.Dice.Add(new DieView { Id = "eye-2", Owner = Side.Shadow, Location = DieLocation.Hunt, Face = DieFace.Eye });
            view.Dice.Add(new DieView { Id = "free-1", Owner = Side.FreePeoples, Location = DieLocation.Hunt });
            return view;
        }

        private static TurnDraft Draft()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            return draft;
        }

        [Test]
        public void TheRegistryHoldsTheFinishesThatEndACardAndNoDecline()
        {
            Assert.IsTrue(FinishingOption.Finishes(FinishingOption.StrikeFinish));
            Assert.IsTrue(FinishingOption.Finishes(FinishingOption.AnotherWayStay));
            Assert.IsTrue(FinishingOption.Finishes(FinishingOption.WindFinish));
            Assert.IsTrue(FinishingOption.Finishes(FinishingOption.BalrogFinish));
            // A decline of a response or an optional effect stays the Pass / Skip effect (UI-419, UI-423).
            Assert.IsFalse(FinishingOption.Finishes("black-breath.decline"));
            Assert.IsFalse(FinishingOption.Finishes(NazgulStrikeChoice.HuntRoll));
            Assert.IsFalse(OptionalDecline.Declines(new PlayerView(), FinishingOption.WindFinish), "Wind's Finish left the Pass.");
            Assert.IsFalse(OptionalDecline.Declines(new PlayerView(), FinishingOption.BalrogFinish), "The Balrog's Finish left the Pass.");
        }

        [Test]
        public void TheHintEndsWithWhatTheConfirmFinishes()
        {
            var view = StrikeDecision(Staff);
            Assert.AreEqual("Or ✓ to finish without discarding a table card or rolling for the Hunt.",
                FinishingOption.Hint(view, Draft()));
        }

        [Test]
        public void TheConfirmSendsTheFinishOnlyWhileNothingElseIsChosen()
        {
            var view = StrikeDecision(Staff);
            var finish = FinishingOption.Option(view, Draft());
            Assert.AreEqual(FinishingOption.StrikeFinish, finish.Id);
            Assert.AreEqual(ConsoleOptionKind.Effect, finish.Kind);
            Assert.IsTrue(finish.Enabled);
            var chosen = Draft();
            chosen.Change(p => p.Ids.Add(Staff));
            Assert.IsNull(FinishingOption.Option(view, chosen), "A chosen table card is confirmed, not finished.");
            view.DecisionOwner = Side.FreePeoples;
            Assert.IsNull(FinishingOption.Option(view, Draft()), "The opponent's decision is never this ✓.");
            Assert.IsNull(FinishingOption.Option(Decision(NazgulStrikeChoice.Presentation, Strike, Staff, NazgulStrikeChoice.HuntRoll), Draft()),
                "No Finish offered, none sent.");
        }

        [Test]
        public void EachFinishNamesItsCardAndWhatItLeavesUndone()
        {
            Assert.AreEqual("Finish · End The Nazgûl Strike! without discarding a table card or rolling for the Hunt",
                FinishingOption.Option(StrikeDecision(Staff), Draft()).Tooltip);
            Assert.AreEqual("Finish · End There is Another Way without moving or hiding the Fellowship",
                FinishingOption.Option(Decision(FellowshipCardActions.Presentation, "base.fp.character.10", FinishingOption.AnotherWayStay,
                    FellowshipCardActions.Hide), Draft()).Tooltip);
            Assert.AreEqual("Finish · End Wind from the West without inspecting the Shadow hand",
                FinishingOption.Option(Decision(CardConsoleOptions.TableCardsPresentation, "lords.fp.character.27", "wind.inspect-hand",
                    FinishingOption.WindFinish), Draft()).Tooltip);
        }

        [Test]
        public void TheHuntBoxRollsForTheHuntAndSaysWhy()
        {
            var view = StrikeDecision(Staff);
            var roll = NazgulStrikeChoice.Roll(view, Draft());
            Assert.AreEqual(NazgulStrikeChoice.HuntRoll, roll.Id);
            Assert.AreEqual(ConsoleOptionKind.Effect, roll.Kind);
            Assert.AreEqual("Roll for the Hunt · Click to start a Hunt roll now because of The Nazgûl Strike!: a Nazgûl is in the " +
                "Fellowship's region · The Fellowship does not move and no die is spent · 2 Eyes in the Hunt Box", roll.Tooltip);
            view.Dice.RemoveAll(d => d.Id == "eye-2");
            StringAssert.EndsWith(" · 1 Eye in the Hunt Box", NazgulStrikeChoice.RollTooltip(view));
            view.Dice.RemoveAll(d => d.Id == "eye-1");
            StringAssert.EndsWith(" · No Eyes in the Hunt Box", NazgulStrikeChoice.RollTooltip(view));
            Assert.IsNull(NazgulStrikeChoice.Roll(Decision("CaptainOrder", Strike, NazgulStrikeChoice.HuntRoll), Draft()),
                "Only The Nazgûl Strike! rolls from the Hunt Box.");
        }

        [Test]
        public void ARedTableCardNamesTheEffectThatOffersItsDiscard()
        {
            var view = StrikeDecision(Staff, Mithril);
            var tables = CardConsoleOptions.TableCards(view, Draft());
            Assert.AreEqual("Discard Wizard's Staff · Click to discard it because of The Nazgûl Strike!: a Nazgûl is in the Fellowship's region",
                tables[Staff]);
            Assert.AreEqual("Discard The Gates are Closed · Click to discard it and use its reaction · Stop the declared move.",
                TableDiscardText.Reaction("lords.sh.character.27", "Stop the declared move."));
            Assert.AreEqual("Discard Wizard's Staff · Click to discard it because of this effect", TableDiscardText.Offered(Staff, ""));
        }

        // R16: every option keeps a click path — with table cards, without them (the Hunt Box and the ✓ only).
        [Test]
        public void TheHintNamesEveryLitObjectWithAndWithoutTableCards()
        {
            Assert.AreEqual("Discard: red card in Effects · Roll: Hunt Box · ✓ finishes",
                NazgulStrikeChoice.Hint(StrikeDecision(Staff), Draft()));
            var bare = StrikeDecision();
            Assert.IsEmpty(CardConsoleOptions.TableCards(bare, Draft()));
            Assert.IsEmpty(CardConsoleOptions.For(bare, Draft()));
            Assert.IsNotNull(NazgulStrikeChoice.Roll(bare, Draft()), "The Hunt Box rolls.");
            Assert.IsNotNull(FinishingOption.Option(bare, Draft()), "The ✓ finishes.");
            Assert.AreEqual("Click the glowing Hunt Box to roll · ✓ finishes", NazgulStrikeChoice.Hint(bare, Draft()));
            Assert.AreEqual("", NazgulStrikeChoice.Hint(Decision("CaptainOrder", Strike, "captain.fly"), Draft()));
        }
    }
}
