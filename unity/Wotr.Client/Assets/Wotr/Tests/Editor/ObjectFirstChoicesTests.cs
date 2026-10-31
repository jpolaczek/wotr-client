using System.Collections.Generic;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
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
        private const string AnotherWay = "base.fp.character.10";
        private const string Staff = "base.fp.character.08";
        private const string Mithril = "base.fp.character.05";
        private const string Balrog = "base.sh.character.17";

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
            var ids = new List<string>(tableCards) { NazgulStrikeChoice.HuntRoll, FinishingOption.StrikeFinish };
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

        // The payload the old console buttons ("Roll for the Hunt", "Finish") and the table card's × sent, recorded from those
        // buttons on a real server at c7d38fd5 (the shadow-strike match; the option's id, every other field at its default).
        internal static string RecordedPayload(string option) =>
            "{\"Stepwise\":false,\"DieId\":\"\",\"RingId\":\"\",\"RulerTokenId\":\"\",\"CouncilTokenId\":\"\",\"Deck\":\"\",\"Face\":null," +
            "\"Ids\":[\"" + option + "\"],\"NationId\":\"\",\"Placements\":[],\"MovementCost\":1,\"Moves\":[],\"CardId\":\"\",\"CardCost\":4," +
            "\"ReactionId\":\"\",\"GuideId\":\"\",\"Route\":[],\"CompanionMoves\":[],\"HuntResponse\":0,\"Losses\":[],\"RegionId\":\"\"," +
            "\"AutoDecision\":null,\"CharacterId\":\"\",\"AbilityId\":\"\"}";

        [Test]
        public void TheRegistryHoldsTheFinishesThatEndACardAndNoDecline()
        {
            Assert.IsTrue(FinishingOption.Finishes(FinishingOption.StrikeFinish));
            Assert.IsTrue(FinishingOption.Finishes(FinishingOption.AnotherWayStay));
            // A decline of a response or an optional effect stays the Pass / Skip effect (UI-419, UI-423).
            Assert.IsFalse(FinishingOption.Finishes("black-breath.decline"));
            Assert.IsFalse(FinishingOption.Finishes(NazgulStrikeChoice.HuntRoll));
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
                FinishingOption.Option(Decision(FellowshipCardActions.Presentation, AnotherWay, FinishingOption.AnotherWayStay,
                    FellowshipCardActions.Hide), Draft()).Tooltip);
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

        // The console's interaction lights the Hunt Box while the Strike offers its roll and puts it out afterwards.
        [Test]
        public void TheHuntBoxGlowsOnlyWhileTheStrikeOffersItsRoll()
        {
            var board = new BoardScreen();
            var sent = new List<ConsoleOption>();
            using var interaction = new ConsoleOptionsInteraction(board, sent.Add);
            var view = StrikeDecision(Staff);
            interaction.Refresh(view, Draft(), true);
            Assert.IsTrue(board.Hunt.Offered);
            Assert.IsTrue(board.Hunt.Tab.ClassListContains(ChoiceGlow.ClassName), "The Hunt Box glows.");
            Assert.AreEqual(NazgulStrikeChoice.RollTooltip(view), board.Hunt.Tab.tooltip);
            interaction.Refresh(view, Draft(), false);
            Assert.IsFalse(board.Hunt.Offered, "No offer while the board takes no input.");
            Assert.IsFalse(board.Hunt.Tab.ClassListContains(ChoiceGlow.ClassName));
            Assert.AreEqual(HuntPanel.PlaqueTooltip, board.Hunt.Tab.tooltip);
            interaction.Refresh(Decision("CaptainOrder", Strike, "captain.fly"), Draft(), true);
            Assert.IsFalse(board.Hunt.Offered, "Another card's choice leaves the Hunt Box as it was.");
            CollectionAssert.IsEmpty(sent, "Lighting sends nothing.");
        }

        // The Hunt Box and the ✓ send through TurnInteraction.ChooseConsoleOption, the path of the old console buttons: the
        // command each builds equals the old button's, and its payload is the one recorded from that button on a real server.
        [Test]
        public void TheHuntBoxAndTheConfirmSendTheOldButtonsCommands()
        {
            var view = StrikeDecision(Staff);
            AssertSendsTheOldButtonsCommand(view, NazgulStrikeChoice.Roll(view, Draft()), NazgulStrikeChoice.HuntRoll);
            AssertSendsTheOldButtonsCommand(view, FinishingOption.Option(view, Draft()), FinishingOption.StrikeFinish);
        }

        private static void AssertSendsTheOldButtonsCommand(PlayerView view, ConsoleOption choice, string id)
        {
            // The old console button: CardConsoleOptions' effect option before UI-430.
            var button = new ConsoleOption { Id = id, Name = "effect-choice-" + id, Kind = ConsoleOptionKind.Effect };
            var sent = Command(view, choice);
            var old = Command(view, button);
            Assert.AreEqual(CommandKind.ChooseEffect, sent.Kind);
            Assert.AreEqual(old.DecisionId, sent.DecisionId);
            Assert.AreEqual(JsonConvert.SerializeObject(old.Payload), JsonConvert.SerializeObject(sent.Payload), id + ": the old button's payload.");
            Assert.AreEqual(RecordedPayload(id), JsonConvert.SerializeObject(sent.Payload), id + ": the payload recorded on a real server.");
        }

        // What TurnInteraction.ChooseConsoleOption sends for a card's effect option (UI-214): the id chosen on the
        // ChooseEffect draft, which completes the decision, so it is sent at once.
        private static GameCommand Command(PlayerView view, ConsoleOption choice)
        {
            Assert.AreEqual(ConsoleOptionKind.Effect, choice.Kind, "The path of a card's effect option.");
            var draft = Draft();
            var option = DraftOptions.For(view, draft);
            CardDraft.Choose(draft, option, choice.Id);
            Assert.IsTrue(draft.Ready(option), "The choice completes the decision.");
            return draft.Command(view);
        }

        [Test]
        public void ARedTableCardNamesTheEffectThatOffersItsDiscard()
        {
            var view = StrikeDecision(Staff, Mithril);
            var tables = CardConsoleOptions.TableCards(view, Draft());
            Assert.AreEqual("Discard Wizard's Staff · Click to discard it because of The Nazgûl Strike!: a Nazgûl is in the Fellowship's region",
                tables[Staff]);
            Assert.AreEqual("Discard Balrog of Moria · Click to discard it and use its reaction · The reaction's detail.",
                TableDiscardText.Reaction(Balrog, "The reaction's detail."));
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

        // The base tray's hint names the lit objects (the console shows it), and so does the console's own guidance.
        [Test]
        public void TheTrayAndTheConsoleNameTheLitObjects()
        {
            var strike = StrikeDecision(Staff);
            Assert.AreEqual("Discard: red card in Effects · Roll: Hunt Box · ✓ finishes", TrayHint(strike));
            Assert.AreEqual("Discard: red card in Effects · Roll: Hunt Box · ✓ finishes",
                TurnConsoleState.For(strike, Draft(), new ActionDieContext(), id => id).Guidance);
            var anotherWay = Decision(FellowshipCardActions.Presentation, AnotherWay, FinishingOption.AnotherWayStay, FellowshipCardActions.Hide);
            anotherWay.You = Side.FreePeoples;
            anotherWay.DecisionOwner = Side.FreePeoples;
            anotherWay.Fellowship = new FellowshipView { RegionId = "rivendell", Guide = "base.character.gollum" };
            Assert.AreEqual("Hide: click the Fellowship's glowing marker on its track · ✓ finishes after healing", TrayHint(anotherWay));
        }

        private static string TrayHint(PlayerView view)
        {
            var hint = "";
            var tray = new DecisionTray(new VisualElement());
            tray.HintChanged += (text, detail) => hint = text;
            tray.Render(view, Draft(), false, false);
            return hint;
        }
    }
}
