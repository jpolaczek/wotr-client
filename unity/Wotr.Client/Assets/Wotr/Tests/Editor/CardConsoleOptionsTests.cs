using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-214: a card's object-less options and a reaction's responses are at most three buttons in the console; their
    // objects (a table card, a card in hand) keep the choice, and the decision window stays closed.
    public sealed class CardConsoleOptionsTests
    {
        private static PlayerView Card(string presentation, params EffectChoiceView[] choices)
        {
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            foreach (var choice in choices)
            {
                option.EffectChoices.Add(choice);
                option.EligibleIds.Add(choice.Id);
            }
            return new PlayerView
            {
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = presentation, Kind = DecisionKind.EffectChoice, Presentation = presentation },
                Options = { option }
            };
        }

        private static TurnDraft Draft(CommandKind kind)
        {
            var draft = new TurnDraft();
            draft.Choose(kind);
            return draft;
        }

        private static bool WindowChoices(PlayerView view, TurnDraft draft)
        {
            var tray = new DecisionTray(new VisualElement());
            tray.Render(view, draft, false, false);
            return tray.HasChoices;
        }

        internal static PlayerView Captain(bool reserveNazgul)
        {
            var view = Card(BlackCaptainOrder.Presentation,
                new EffectChoiceView { Id = BlackCaptainOrder.Recruit, Title = "Recruit · 2 Nazgûl", Detail = "Recruit available Nazgûl with the Witch-king." },
                new EffectChoiceView { Id = BlackCaptainOrder.Fly, Title = "Move Nazgûl", Detail = "Move any Nazgûl, including the Witch-king." });
            view.Decision.SourceId = "base.sh.character.24";
            view.Characters.Add(new CharacterView { Id = "base.character.witch-king", DefinitionId = "base.character.witch-king", Side = Side.Shadow, Location = "osgiliath" });
            view.Units.Add(new UnitView { Id = "base.sauron.nazgul.01", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "osgiliath" });
            view.Units.Add(new UnitView { Id = "base.sauron.nazgul.04", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "morannon" });
            if (reserveNazgul)
                view.Units.Add(new UnitView { Id = "base.sauron.nazgul.02", Nation = "sauron", Kind = UnitKind.Nazgul, Side = Side.Shadow, Location = "reserve" });
            return view;
        }

        // UI-431 (owner's rule, UI-430): the Black Captain's orders stand on the Reserve's Nazgûl (recruit) and the map's
        // Nazgûl (move); no console button and no window. With no Nazgûl in the reserve the recruit has no object, so it
        // keeps its button (R16), lit as the card's one option.
        [Test]
        public void TheBlackCaptainsOrdersStandOnTheNazgulAndAnEmptyReserveKeepsTheRecruitButton()
        {
            var view = Captain(reserveNazgul: true);
            var draft = Draft(CommandKind.ChooseEffect);
            Assert.IsEmpty(CardConsoleOptions.For(view, draft));
            Assert.IsTrue(CardConsoleOptions.Owns(view, draft), "The console keeps the decision, so no window opens.");
            Assert.IsFalse(WindowChoices(view, draft));
            var empty = Captain(reserveNazgul: false);
            var recruit = CardConsoleOptions.For(empty, draft).Single();
            Assert.AreEqual("effect-choice-captain.recruit", recruit.Name);
            Assert.AreEqual(BlackCaptainOrder.Recruit, recruit.Id);
            Assert.IsTrue(recruit.Lit && recruit.Enabled);
            Assert.AreEqual(CardLibrary.Title("base.sh.character.24"), recruit.Label);
        }

        // UI-347 (EXP-15 D11): the Balrog card's order stands on the Balrog's figure (alone) and his army's figures (with
        // it); Finish is the console's ✓ (UI-430), so the console has no button and no window opens.
        [Test]
        public void TheBalrogsOrderStandsOnHisFigureAndHisArmy()
        {
            var view = Card(BalrogCardOrder.Presentation,
                new EffectChoiceView { Id = BalrogCardOrder.Alone, Title = "Move the Balrog alone" },
                new EffectChoiceView { Id = BalrogCardOrder.Army, Title = "Move or attack with his army" },
                new EffectChoiceView { Id = FinishingOption.BalrogFinish, Title = "Finish" });
            view.Characters.Add(new CharacterView { Id = "lords.character.balrog", DefinitionId = "lords.character.balrog", Side = Side.Shadow, Location = "moria" });
            view.Units.Add(new UnitView { Id = "orc-1", Side = Side.Shadow, Location = "moria" });
            view.Units.Add(new UnitView { Id = "orc-2", Side = Side.Shadow, Location = "dol-guldur" });
            var draft = Draft(CommandKind.ChooseEffect);
            Assert.IsEmpty(CardConsoleOptions.For(view, draft));
            Assert.IsFalse(WindowChoices(view, draft));
            var figures = BalrogCardOrder.Figures(view);
            Assert.AreEqual(BalrogCardOrder.Alone, figures["lords.character.balrog"]);
            Assert.AreEqual(BalrogCardOrder.Army, figures["orc-1"]);
            Assert.IsFalse(figures.ContainsKey("orc-2"), "Only the army he stands with.");
            Assert.AreEqual("moria", BalrogCardOrder.Region(view));
            Assert.AreEqual("Click the Balrog's glowing army to move it with him, or his region and then his figure to move him alone.",
                BalrogCardOrder.Hint(view));
            Assert.AreEqual(FinishingOption.BalrogFinish, FinishingOption.Option(view, draft).Id);
            Assert.IsNull(OptionalDecline.Option(view, draft), "The Balrog's Finish is not the Pass.");
            view.Options[0].EligibleIds.Remove(BalrogCardOrder.Army);
            Assert.AreEqual(new[] { "lords.character.balrog" }, BalrogCardOrder.Figures(view).Keys.ToArray());
        }

        // UI-271: the Chief's Spear of Terror is used at the Character deck and He Sees, He Knows at his figure; the console
        // keeps only Decline.
        [Test]
        public void TheChiefsReactionsLeaveTheConsoleForTheirObjects()
        {
            var view = new PlayerView
            {
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "chief", Kind = DecisionKind.Reaction },
                Characters = { new CharacterView { Id = "base.character.witch-king", DefinitionId = "lords.character.witch-king", Side = Side.Shadow, Location = "minas-morgul" } },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.RespondEffect, Enabled = true,
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = ChiefReactionObjects.Spear, SourceId = "lords.character.witch-king", Title = "Spear of Terror · Draw card" },
                            new EffectChoiceView { Id = ChiefReactionObjects.Follow, SourceId = "lords.character.witch-king", Title = "He Sees, He Knows · Follow" }
                        }
                    },
                    new ActionOption { Kind = CommandKind.DeclineReaction, Enabled = true }
                }
            };
            var options = CardConsoleOptions.For(view, new TurnDraft());
            // UI-291: the console's Pass declines the window, so nothing is left in the console.
            CollectionAssert.IsEmpty(options.Select(o => o.Name));
            Assert.AreEqual(ChiefReactionObjects.Spear, ChiefReactionObjects.DeckReaction(view, "character"));
            Assert.AreEqual("", ChiefReactionObjects.DeckReaction(view, "strategy"));
            Assert.AreEqual("base.character.witch-king", ChiefReactionObjects.FollowFigure(view));
            var decks = new DeckView();
            decks.Refresh(view, new TurnDraft(), new ActionDieContext(), false);
            var character = decks.Root.Q<Button>("deck-character");
            Assert.IsTrue(character.enabledSelf && character.ClassListContains("legal-target"));
            StringAssert.StartsWith("Click to draw a Character card · Spear of Terror", character.tooltip);
            Assert.IsFalse(decks.Root.Q<Button>("deck-strategy").enabledSelf);
        }

        // UI-430 (owner): The Nazgûl Strike! has no console buttons; its table cards are red in the Effects panel, its Hunt
        // roll is the Hunt Box and its Finish the console's ✓ (ObjectFirstChoicesTests). The decision window stays closed.
        [Test]
        public void TheNazgulStrikeHasNoConsoleButtonsItsObjectsCarryItsOptions()
        {
            var view = Card(NazgulStrikeChoice.Presentation,
                new EffectChoiceView { Id = "base.fp.character.05", SourceId = "base.fp.character.05", Title = "Table card", Detail = "Discard this card." },
                new EffectChoiceView { Id = "base.fp.character.08", SourceId = "base.fp.character.08", Title = "Table card", Detail = "Discard this card." },
                new EffectChoiceView { Id = NazgulStrikeChoice.HuntRoll, Title = "Roll for the Hunt" },
                new EffectChoiceView { Id = FinishingOption.StrikeFinish, Title = "Finish" });
            var draft = Draft(CommandKind.ChooseEffect);
            Assert.IsEmpty(CardConsoleOptions.For(view, draft));
            Assert.IsTrue(CardConsoleOptions.Owns(view, draft), "The console keeps the decision, so no window opens.");
            CollectionAssert.AreEquivalent(new[] { "base.fp.character.05", "base.fp.character.08" }, CardConsoleOptions.TableCards(view, draft).Keys);
            Assert.AreEqual(NazgulStrikeChoice.HuntRoll, NazgulStrikeChoice.Roll(view, draft).Id);
            Assert.AreEqual(FinishingOption.StrikeFinish, FinishingOption.Option(view, draft).Id);
            Assert.IsFalse(WindowChoices(view, draft));
        }

        internal static PlayerView Lure()
        {
            var view = Card(LureChoice.Presentation,
                new EffectChoiceView { Id = LureChoice.Corruption, SourceId = "base.character.gimli", Title = "Corruption +2", Detail = "Gimli: keep this Companion in the Fellowship." },
                new EffectChoiceView { Id = LureChoice.Eliminate, SourceId = "base.character.gimli", Title = "Eliminate", Detail = "Gimli: eliminate this Companion; Take Them Alive still applies." });
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Decision.SourceId = "base.sh.character.13";
            view.OwnCompanions.Add(new CharacterView { Id = "base.character.gimli", DefinitionId = "base.character.gimli", Level = 2 });
            return view;
        }

        // UI-431: Lure of the Ring's choice is the companion's red portrait and the corruption marker; no buttons, no window.
        [Test]
        public void LureOfTheRingStandsOnTheCompanionsPortraitAndTheCorruptionMarker()
        {
            var view = Lure();
            var draft = Draft(CommandKind.ChooseEffect);
            Assert.IsEmpty(CardConsoleOptions.For(view, draft));
            Assert.IsFalse(WindowChoices(view, draft));
            Assert.IsTrue(LureChoice.OnObject(view, LureChoice.Eliminate) && LureChoice.OnObject(view, LureChoice.Corruption));
        }

        // UI-431: Dreadful Spells' (and Work of the Enemy's) field army and garrison are the region panel's glowing sections;
        // neither the console nor the tray has a button for them, before or after the region's click.
        [Test]
        public void ARegionsTwoForcesHaveNoButtonsTheirRegionPanelChoosesThem()
        {
            var view = Card("Region",
                new EffectChoiceView { Id = "osgiliath:field", TargetId = "osgiliath", Title = "Target · Field" },
                new EffectChoiceView { Id = "osgiliath:garrison", TargetId = "osgiliath", Title = "Target · Garrison" },
                new EffectChoiceView { Id = "pelargir:field", TargetId = "pelargir", Title = "Target · Field" });
            var draft = Draft(CommandKind.ChooseEffect);
            Assert.IsEmpty(CardConsoleOptions.For(view, draft), "The regions are chosen on the map first.");
            Assert.AreEqual("", RegionForceChoices.Waiting(view, draft));
            draft.Change(p => p.RegionId = "osgiliath");
            Assert.IsEmpty(CardConsoleOptions.For(view, draft));
            Assert.AreEqual("osgiliath", RegionForceChoices.Waiting(view, draft));
            Assert.IsFalse(WindowChoices(view, draft));
            var root = new VisualElement();
            new DecisionTray(root).Render(view, draft, false, false);
            Assert.IsNull(root.Q<Button>("effect-choice-osgiliath:garrison"), "No tray button either.");
        }

        private static PlayerView Reaction(int characters, bool hunt = false)
        {
            var respond = new ActionOption { Kind = CommandKind.RespondEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            respond.EffectChoices.Add(new EffectChoiceView { Id = "card", SourceId = "base.fp.character.01", Title = "" });
            for (var i = 0; i < characters; i++)
                respond.EffectChoices.Add(new EffectChoiceView { Id = "ability-" + i, SourceId = "base.character.gandalf-grey", Title = "Draw · " + i });
            respond.EligibleIds.AddRange(respond.EffectChoices.Select(c => c.Id));
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "reaction", Kind = DecisionKind.Reaction },
                Options = { respond, new ActionOption { Kind = CommandKind.DeclineReaction, Enabled = true } }
            };
            view.Hand.Add("base.fp.character.01");
            if (hunt)
                view.Hunt = new HuntView();
            return view;
        }

        [Test]
        public void AReactionKeepsItsCardAtTheHandAndPutsTheOtherResponsesInTheConsole()
        {
            var view = Reaction(1);
            var draft = Draft(CommandKind.RespondEffect);
            var options = CardConsoleOptions.For(view, draft);
            // UI-291: the console's Pass declines the window; no Decline option.
            CollectionAssert.AreEqual(new[] { "effect-choice-ability-0" }, options.Select(o => o.Name));
            CollectionAssert.AreEqual(new[] { ConsoleOptionKind.Reaction }, options.Select(o => o.Kind));
            StringAssert.StartsWith("Pass · Skip these responses:", ReactionSkip.Tooltip(view));
            StringAssert.Contains("Draw · 0 (Gandalf the Grey)", ReactionSkip.Tooltip(view));
            // UI-423: a window's Skip effect names each response it skips, one a line.
            Assert.AreEqual("Skip " + CardLibrary.Title("base.fp.character.01") + "\nSkip Draw · 0 (Gandalf the Grey)",
                ReactionSkip.SkipTooltip(view));
            Assert.IsFalse(WindowChoices(view, draft), "The card in hand answers with its preview's Use.");
        }

        // UI-431: a response that draws a card is its lit deck when the roster does not hold it (as Spear of Terror is the
        // Character deck, UI-271): the console has no button for it, and the deck's click sends the response the button sent
        // (RespondEffect with that reaction id: CardContextInteraction.Respond, as TurnInteraction.ChooseConsoleOption did).
        [Test]
        public void AResponsesDrawIsItsDeckNotAConsoleButton()
        {
            var respond = new ActionOption { Kind = CommandKind.RespondEffect, Enabled = true, EligibleIds = { ReactionDecks.GreyDraw } };
            respond.EffectChoices.Add(new EffectChoiceView { Id = ReactionDecks.GreyDraw, SourceId = "base.character.gandalf-grey", Title = "Draw matching card" });
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "grey", Kind = DecisionKind.Reaction, Window = "event.completed", SourceId = "base.fp.strategy.12" },
                Options = { respond, new ActionOption { Kind = CommandKind.DeclineReaction, Enabled = true } }
            };
            var draft = Draft(CommandKind.RespondEffect);
            var deck = CardLibrary.Cards["base.fp.strategy.12"].Deck;
            Assert.IsEmpty(CardConsoleOptions.For(view, draft), "No button: the deck carries the draw.");
            Assert.AreEqual(ReactionDecks.GreyDraw, ReactionDecks.Draw(view, deck), "The deck sends the old button's reaction id.");
            Assert.AreEqual("", ReactionDecks.Draw(view, deck == "strategy" ? "character" : "strategy"));
            var decks = new DeckView();
            decks.Refresh(view, draft, new ActionDieContext(), false);
            var lit = decks.Root.Q<Button>("deck-" + deck);
            Assert.IsTrue(lit.enabledSelf && lit.ClassListContains("legal-target") && lit.ClassListContains(DeckView.SpearClass));
            StringAssert.StartsWith("Draw matching card · Click to draw a " + FigureGrid.Name(deck) + " card because of Gandalf the Grey", lit.tooltip);
            // Once the Guide's portrait holds it (UI-247), the deck stays as it was.
            view.Fellowship = new FellowshipView { Guide = "base.character.gandalf-grey" };
            view.OwnCompanions.Add(new CharacterView { Id = "base.character.gandalf-grey", DefinitionId = "base.character.gandalf-grey", Level = 3 });
            Assert.AreEqual("", ReactionDecks.Draw(view, deck));
        }

        // The Ents Awake follow-up: a free Character Event from the hand, or the console's Pass (UI-291); the effect trail
        // only recalls the played card and opens no window.
        [Test]
        public void AFollowUpWithItsTrailIsSkippedWithPassWithoutAWindow()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "follow-up", Kind = DecisionKind.Reaction, Window = "ents.follow-up", SourceId = "base.fp.character.20" },
                Options = { new ActionOption { Kind = CommandKind.PlayEvent }, new ActionOption { Kind = CommandKind.DeclineReaction } }
            };
            view.EffectTrail.Add(new EffectTrailView { SourceId = "base.fp.character.20" });
            var draft = new TurnDraft();
            Assert.IsEmpty(CardConsoleOptions.For(view, draft), "No option hides the console's hint.");
            Assert.IsNotNull(ReactionSkip.Option(view));
            Assert.AreEqual("Pass · Skip the free Character Event of " + CardLibrary.Title("base.fp.character.20"), ReactionSkip.Tooltip(view));
            Assert.AreEqual("Skip the free Character Event of " + CardLibrary.Title("base.fp.character.20"), ReactionSkip.SkipTooltip(view));
            view.Decision.Window = "";
            Assert.AreEqual("Skip this response", ReactionSkip.SkipTooltip(view), "A window without named responses still reads.");
            view.Decision.Window = "ents.follow-up";
            Assert.IsFalse(WindowChoices(view, draft));
        }

        // UI-164 CC09: while the other side decides a card's step, the trail of what it did opens no window.
        [Test]
        public void TheWaitingSideSeesTheCardsTrailWithoutAWindow()
        {
            var view = new PlayerView { You = Side.Shadow, DecisionOwner = Side.FreePeoples };
            view.EffectTrail.Add(new EffectTrailView { SourceId = "base.sh.character.09" });
            var root = new VisualElement();
            var tray = new DecisionTray(root);
            tray.Render(view, new TurnDraft(), false, false);
            Assert.IsNull(root.Q(className: "committed-choice"));
            Assert.IsNotNull(EffectTrailTests.Console(view).Q(className: "committed-choice"));
            Assert.IsFalse(tray.HasChoices);
        }

        [Test]
        public void AReactionWithTooManyResponsesOrInTheHuntKeepsItsHost()
        {
            // UI-291: three responses fit the console now that Pass, not a fourth button, declines.
            Assert.IsTrue(CardConsoleOptions.Owns(Reaction(3), Draft(CommandKind.RespondEffect)), "Three responses fit the console.");
            Assert.IsFalse(CardConsoleOptions.Owns(Reaction(4), Draft(CommandKind.RespondEffect)), "Four buttons do not fit the console.");
            Assert.IsEmpty(CardConsoleOptions.For(Reaction(4), Draft(CommandKind.RespondEffect)));
            Assert.IsFalse(CardConsoleOptions.Owns(Reaction(1, hunt: true), Draft(CommandKind.RespondEffect)), "The Hunt panel hosts its reactions.");
        }
    }
}
