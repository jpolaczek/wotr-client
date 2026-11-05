using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-432 (owner's rule, UI-430): a decline is the console's Pass and a window's Skip effect, and a window list that
    // repeats an object goes. The base battle cards' declines, Worn with Sorrow and Toil's objects and Scouts' withdrawal
    // into siege; every object sends the payload its old button sent.
    public sealed class BoardRuleDeclinesTests
    {
        private const string BlackBreath = "base.sh.character.11";
        private const string Anduril = "base.fp.character.09";
        private const string HeroicDeath = "base.fp.character.10";
        private const string FatefulStrike = "base.fp.character.12";
        private const string Worn = "base.sh.character.15";
        private const string Scouts = "base.fp.strategy.05";
        private const string Mithril = "base.fp.character.05";

        [TestCase(OptionalDecline.BlackBreathKeep, BlackBreath, "LeaderOrCompanion", "Black Breath", "Eliminate no Leader or Companion")]
        [TestCase(OptionalDecline.FatefulStrikeKeep, FatefulStrike, "NazgulOrMinion", "Fateful Strike", "Eliminate no Minion")]
        [TestCase(OptionalDecline.HeroicDeathKeep, HeroicDeath, "SacrificeAndHits", "Heroic Death", "Eliminate no figure and take every hit")]
        [TestCase(OptionalDecline.AndurilKeep, Anduril, "ConvertMisses", "Andúril",
            "Convert no miss; Aragorn still forfeits his Leadership")]
        public void ABattleCardsDeclineIsTheSkipEffectAndThePass(string decline, string card, string presentation, string effect, string what)
        {
            var view = Battle(presentation, card, decline);
            var draft = Draft();
            Assert.IsTrue(OptionalDecline.Declines(view, decline));
            var pass = OptionalDecline.Option(view, draft);
            Assert.AreEqual(decline, pass.Id);
            Assert.AreEqual("Pass · Skip " + effect + " · " + what, pass.Tooltip);
            Assert.AreEqual("Or Pass to " + char.ToLowerInvariant(what[0]) + what.Substring(1) + ".", OptionalDecline.Hint(view, draft));

            var root = new VisualElement();
            new DecisionTray(root).Render(view, draft, false, false);
            Assert.IsNull(root.Q<Button>("effect-choice-" + decline), "No button repeats the decline.");
            var skip = root.Q<Button>("battle-skip-effect");
            Assert.IsNotNull(skip, "The battle window skips it.");
            Assert.AreEqual("Skip " + effect + " · " + what, skip.tooltip);
        }

        // The Pass (TurnInteraction.ChooseDecline: the decline alone) sends what the old button's click sent, also after a
        // partial choice of the same step. Fateful Strike's Keep Minions has no seeded match (the server offers it only
        // without a Nazgûl in the battle), so this is its command-equality proof.
        [TestCase(OptionalDecline.BlackBreathKeep, BlackBreath, "LeaderOrCompanion")]
        [TestCase(OptionalDecline.FatefulStrikeKeep, FatefulStrike, "NazgulOrMinion")]
        [TestCase(OptionalDecline.HeroicDeathKeep, HeroicDeath, "SacrificeAndHits")]
        [TestCase(OptionalDecline.AndurilKeep, Anduril, "ConvertMisses")]
        public void ThePassSendsTheOldButtonsPayload(string decline, string card, string presentation)
        {
            var view = Battle(presentation, card, decline);
            var button = Draft();
            CardDraft.Choose(button, view.Options[0], decline);
            var pass = Draft();
            CardDraft.Choose(pass, view.Options[0], "figure");
            CardDraft.Choose(pass, OptionalDecline.Option(view, pass).Id);
            Assert.AreEqual(JsonConvert.SerializeObject(button.Payload), JsonConvert.SerializeObject(pass.Payload));
            CollectionAssert.AreEqual(new[] { decline }, pass.Payload.Ids);
        }

        [Test]
        public void WornsTableCardsAreRedInEffectsAndItsRandomCardIsTheHandCounter()
        {
            var view = WornDecision();
            var draft = Draft();
            var tables = WornDiscardChoice.TableCards(view, draft);
            CollectionAssert.AreEquivalent(new[] { Mithril }, tables.Keys);
            Assert.AreEqual("Discard " + CardLibrary.Title(Mithril) + " · Click to discard it because of Worn with Sorrow and Toil: a " +
                "Companion was taken as a casualty", tables[Mithril]);
            Assert.AreEqual("Discard a random card · Click to discard a random card from the Free Peoples hand because of Worn " +
                "with Sorrow and Toil · Discard a random Character card from the Free Peoples hand, if any.", WornDiscardChoice.RandomCard(view, draft));
            Assert.AreEqual(WornDiscardChoice.Hint + " · Pass discards none", WornDiscardChoice.DecisionHint(view, draft));
            Assert.IsTrue(WornDiscardChoice.OnObject(view, Mithril));
            Assert.IsTrue(WornDiscardChoice.OnObject(view, WornDiscardChoice.RandomHand));
            Assert.IsFalse(WornDiscardChoice.OnObject(view, OptionalDecline.WornKeep), "Keep cards is the Pass.");
            Assert.AreEqual("Pass · Skip Worn with Sorrow and Toil · Discard no Free Peoples Character card",
                OptionalDecline.Option(view, draft).Tooltip);

            view.DecisionOwner = Side.FreePeoples;
            CollectionAssert.IsEmpty(WornDiscardChoice.TableCards(view, draft), "The waiting side is offered nothing.");
            Assert.AreEqual("", WornDiscardChoice.RandomCard(view, draft));
        }

        // R16: with an empty Free Peoples hand the table cards remain, and without table cards the counter.
        [Test]
        public void WornsHintNamesTheObjectsOffered()
        {
            var draft = Draft();
            var tablesOnly = WornDecision(random: false);
            Assert.AreEqual(TableDiscardText.Hint + " · Pass discards none", WornDiscardChoice.DecisionHint(tablesOnly, draft));
            var randomOnly = WornDecision(tables: false);
            Assert.AreEqual("Click the red hand counter to discard a random card · Pass discards none",
                WornDiscardChoice.DecisionHint(randomOnly, draft));
        }

        [Test]
        public void TheHuntWindowListsNoneOfWornsChoicesAndSkipsKeepCards()
        {
            var view = WornDecision();
            var draft = Draft();
            var root = new VisualElement();
            new DecisionTray(root) { ContextualHunt = true }.Render(view, draft, false, false);
            CollectionAssert.IsEmpty(root.Query<Button>().ToList().Where(b => b.name.StartsWith("effect-choice-")).Select(b => b.name));
            var skip = root.Q<Button>("hunt-skip-effect");
            Assert.IsNotNull(skip, "The Hunt window skips Keep cards.");
            Assert.AreEqual("Skip Worn with Sorrow and Toil · Discard no Free Peoples Character card", skip.tooltip);
        }

        [Test]
        public void TheOfferedHandCounterIsRedPressableAndLeadsItsTooltip()
        {
            var summary = new OpponentSummary();
            var view = new PlayerView { Players = { new PlayerSummary { Side = Side.FreePeoples, HandCount = 3 } } };
            summary.Apply(view, Side.FreePeoples);
            var description = summary.Cards.tooltip;
            summary.OfferCards("Discard a random card · Click to …");
            Assert.IsTrue(summary.CardsOffered);
            Assert.IsTrue(summary.Cards.ClassListContains(OpponentSummary.DiscardTargetClass));
            Assert.IsTrue(summary.Cards.ClassListContains(PressFeedback.PressableClass));
            Assert.AreEqual("Discard a random card · Click to …\n" + description, summary.Cards.tooltip);
            summary.OfferCards("");
            Assert.IsFalse(summary.CardsOffered);
            Assert.IsFalse(summary.Cards.ClassListContains(OpponentSummary.DiscardTargetClass));
            Assert.AreEqual(description, summary.Cards.tooltip, "Withdrawn, the counter is a picture again.");
        }

        [Test]
        public void ScoutsWithdrawalIsTheBattlesRegion()
        {
            var view = ScoutsDecision();
            var draft = Draft();
            Assert.AreEqual("lossarnach", ScoutsShelter.Region(view, draft));
            Assert.AreEqual("Withdraw into siege · Click to withdraw into the stronghold because of Scouts · End this battle before " +
                "the Combat roll.", ScoutsShelter.Offers(view, draft)["lossarnach"]);
            Assert.AreEqual("Click Lossarnach to withdraw into its stronghold, or a lit region to retreat",
                ScoutsShelter.Hint(view, draft, "Lossarnach"));
            var root = new VisualElement();
            new CardChoices(root, view, draft, change => change(), BoardPresentation.Load()).Refresh();
            Assert.IsNull(root.Q<Button>("effect-choice-" + ScoutsShelter.Shelter), "No button repeats the region.");

            view.Options[0].EligibleIds.Remove(ScoutsShelter.Shelter);
            Assert.AreEqual("", ScoutsShelter.Region(view, draft), "Without a stronghold to enter, the region chooses nothing.");
        }

        // The battle region's click (BattleContextInteraction) sends what the old Withdraw into siege button sent.
        [Test]
        public void TheBattleRegionsClickSendsTheOldWithdrawButtonsPayload()
        {
            var view = ScoutsDecision();
            var button = Draft();
            CardDraft.Choose(button, view.Options[0], ScoutsShelter.Shelter);
            var region = Draft();
            var completed = false;
            var battles = new BattleContextInteraction((change, complete) =>
            {
                change();
                completed = complete;
            }, () => false);
            battles.Refresh(view, region);
            Assert.IsFalse(battles.Target("minas-tirith"), "A retreat region stays the map's own choice.");
            Assert.IsTrue(battles.Target("lossarnach"));
            Assert.IsTrue(completed, "One click completes the step.");
            Assert.AreEqual(JsonConvert.SerializeObject(button.Payload), JsonConvert.SerializeObject(region.Payload));
        }

        private static TurnDraft Draft()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            return draft;
        }

        private static PlayerView Battle(string presentation, string card, string decline)
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Decision = new DecisionView { Id = "decline", Kind = DecisionKind.EffectChoice, Presentation = presentation, SourceId = card };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            option.EffectChoices.Add(new EffectChoiceView { Id = "figure", Detail = "Eliminate this figure." });
            option.EffectChoices.Add(new EffectChoiceView { Id = decline, Title = "Keep", Exclusive = true });
            option.EligibleIds.AddRange(option.EffectChoices.Select(c => c.Id));
            view.Options.Clear();
            view.Options.Add(option);
            return view;
        }

        private static PlayerView WornDecision(bool tables = true, bool random = true)
        {
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            if (tables)
                option.EffectChoices.Add(new EffectChoiceView { Id = Mithril, SourceId = Mithril, Title = "Mithril", Detail = "Discard this card from the table." });
            if (random)
                option.EffectChoices.Add(new EffectChoiceView { Id = WornDiscardChoice.RandomHand, Title = "Random hand",
                    Detail = "Discard a random Character card from the Free Peoples hand, if any." });
            option.EffectChoices.Add(new EffectChoiceView { Id = OptionalDecline.WornKeep, Title = "Keep cards" });
            option.EligibleIds.AddRange(option.EffectChoices.Select(c => c.Id));
            return new PlayerView
            {
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Hunt = new HuntView(),
                Decision = new DecisionView { Id = "worn", Kind = DecisionKind.EffectChoice, Presentation = WornDiscardChoice.Presentation, SourceId = Worn },
                Options = { option }
            };
        }

        private static PlayerView ScoutsDecision()
        {
            var view = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Decision = new DecisionView { Id = "scouts", Kind = DecisionKind.EffectChoice, Presentation = ScoutsShelter.Presentation, SourceId = Scouts };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            option.EffectChoices.Add(new EffectChoiceView { Id = "minas-tirith", TargetId = "minas-tirith", Title = "Retreat",
                Detail = "End this battle before the Combat roll." });
            option.EffectChoices.Add(new EffectChoiceView { Id = ScoutsShelter.Shelter, Title = "Withdraw into siege",
                Detail = "End this battle before the Combat roll." });
            option.EligibleIds.AddRange(option.EffectChoices.Select(c => c.Id));
            view.Options.Clear();
            view.Options.Add(option);
            return view;
        }
    }
}
