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

        [Test]
        public void TheBlackCaptainsOrdersAreTwoConsoleButtonsAndNoWindow()
        {
            var view = Card("CaptainOrder",
                new EffectChoiceView { Id = "captain.recruit", Title = "Recruit · 2 Nazgûl", Detail = "Recruit available Nazgûl with the Witch-king." },
                new EffectChoiceView { Id = "captain.fly", Title = "Move Nazgûl", Detail = "Move any Nazgûl, including the Witch-king." });
            var draft = Draft(CommandKind.ChooseEffect);
            var options = CardConsoleOptions.For(view, draft);
            CollectionAssert.AreEqual(new[] { "Recruit · 2 Nazgûl", "Move Nazgûl" }, options.Select(o => o.Label));
            CollectionAssert.AreEqual(new[] { "effect-choice-captain.recruit", "effect-choice-captain.fly" }, options.Select(o => o.Name));
            Assert.IsTrue(options.All(o => o.Kind == ConsoleOptionKind.Effect && o.Enabled));
            Assert.IsFalse(WindowChoices(view, draft));
        }

        [Test]
        public void TheNazgulStrikeDiscardsTableCardsInTheEffectsPanelAndKeepsThreeButtons()
        {
            var view = Card("NazgulStrike",
                new EffectChoiceView { Id = "base.fp.character.05", SourceId = "base.fp.character.05", Title = "Table card", Detail = "Discard this card." },
                new EffectChoiceView { Id = "base.fp.character.08", SourceId = "base.fp.character.08", Title = "Table card", Detail = "Discard this card." },
                new EffectChoiceView { Id = "nazgul-strike.hunt", Title = "Roll for the Hunt" },
                new EffectChoiceView { Id = "nazgul-strike.decline", Title = "Finish" });
            var draft = Draft(CommandKind.ChooseEffect);
            var options = CardConsoleOptions.For(view, draft);
            CollectionAssert.AreEqual(new[] { ConsoleOptionKind.OpenEffects, ConsoleOptionKind.Effect, ConsoleOptionKind.Effect }, options.Select(o => o.Kind));
            CollectionAssert.AreEqual(new[] { "Discard a table card", "Roll for the Hunt", "Finish" }, options.Select(o => o.Label));
            CollectionAssert.AreEquivalent(new[] { "base.fp.character.05", "base.fp.character.08" }, CardConsoleOptions.TableCards(view, draft).Keys);
            Assert.IsFalse(WindowChoices(view, draft));
        }

        [Test]
        public void LureOfTheRingNamesItsCompanionOnBothButtons()
        {
            var view = Card("LureCompanion",
                new EffectChoiceView { Id = "lure.corruption", SourceId = "base.character.merry", Title = "Corruption +1" },
                new EffectChoiceView { Id = "lure.eliminate", SourceId = "base.character.merry", Title = "Eliminate" });
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            var labels = CardConsoleOptions.For(view, Draft(CommandKind.ChooseEffect)).Select(o => o.Label).ToList();
            Assert.AreEqual(2, labels.Count);
            Assert.IsTrue(labels.All(l => l.StartsWith(CharacterLibrary.ShortName(view, "base.character.merry") + " · ")));
        }

        [Test]
        public void DreadfulSpellsAlternativesAppearOnceTheirRegionIsChosen()
        {
            var view = Card("Region",
                new EffectChoiceView { Id = "osgiliath:field", TargetId = "osgiliath", Title = "Target · Field" },
                new EffectChoiceView { Id = "osgiliath:garrison", TargetId = "osgiliath", Title = "Target · Garrison" },
                new EffectChoiceView { Id = "pelargir:field", TargetId = "pelargir", Title = "Target · Field" });
            var draft = Draft(CommandKind.ChooseEffect);
            Assert.IsEmpty(CardConsoleOptions.For(view, draft), "The regions are chosen on the map first.");
            draft.Change(p => p.RegionId = "osgiliath");
            CollectionAssert.AreEqual(new[] { "Target · Field", "Target · Garrison" }, CardConsoleOptions.For(view, draft).Select(o => o.Label));
            Assert.IsFalse(WindowChoices(view, draft));
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
            StringAssert.Contains("Gandalf the Grey · Draw · 0", ReactionSkip.Tooltip(view));
            Assert.IsFalse(WindowChoices(view, draft), "The card in hand answers with its preview's Use.");
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
