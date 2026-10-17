using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    public sealed class HuntInteractionTests
    {
        [Test]
        public void ClicksBuildOneAllocationWithoutAnEyeStepAndTheLimitNeverReplacesADie()
        {
            var option = new ActionOption { Kind = CommandKind.AllocateHunt, Maximum = 2, EligibleIds = { "a", "b", "c" } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.AllocateHunt);
            Assert.IsTrue(draft.Ready(option), "Zero is allowed by this server option.");
            Assert.IsTrue(HuntActions.ToggleDie(draft, option, "b"));
            Assert.IsTrue(HuntActions.ToggleDie(draft, option, "a"));
            CollectionAssert.AreEqual(new[] { "b", "a" }, draft.Payload.Ids, "Each click adds its die to the same set.");
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsTrue(draft.Ready(option));
            Assert.IsFalse(HuntActions.ToggleDie(draft, option, "c"), "The limit never silently replaces a die.");
            Assert.IsFalse(HuntActions.Allocate(draft, option, "c"));
            CollectionAssert.AreEqual(new[] { "b", "a" }, draft.Payload.Ids);
            Assert.IsFalse(HuntActions.CanAdd(draft, option, "c"));
            StringAssert.Contains("limit", HuntActions.PoolHint(draft, option, "c"));
            StringAssert.Contains("return", HuntActions.PoolHint(draft, option, "a"));
            Assert.IsFalse(HuntActions.ToggleDie(draft, option, "forged"));
            Assert.IsEmpty(HuntActions.PoolHint(draft, option, "forged"));
            Assert.IsTrue(HuntActions.ToggleDie(draft, option, "b"));
            CollectionAssert.AreEqual(new[] { "a" }, draft.Payload.Ids, "A second click returns the die.");
            Assert.IsTrue(HuntActions.CanAdd(draft, option, "c"));
            option.Minimum = 2;
            Assert.IsFalse(draft.Ready(option));
            StringAssert.Contains("at least 2", HuntActions.Hint(draft, option));
            Assert.IsTrue(HuntActions.Allocate(draft, option, "c"), "A drop on the Eye adds the die.");
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(HuntActions.Hint(draft, option));
        }

        [Test]
        public void AllocationSurvivesSnapshotAndReconciliationKeepsOnlyLegalDice()
        {
            var view = new PlayerView { You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Kind = DecisionKind.AllocateHunt } };
            var option = new ActionOption { Kind = CommandKind.AllocateHunt, Maximum = 2, EligibleIds = { "a", "b" } };
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.AllocateHunt);
            HuntActions.ToggleDie(draft, option, "a");
            HuntActions.ToggleDie(draft, option, "b");
            var restored = new TurnDraft();
            restored.Restore(draft.Snapshot());
            restored.Reconcile(view);
            CollectionAssert.AreEqual(new[] { "a", "b" }, restored.Payload.Ids);
            option.EligibleIds.Remove("b");
            restored.Reconcile(view);
            CollectionAssert.AreEqual(new[] { "a" }, restored.Payload.Ids, "Reconciliation never selects a replacement.");
            // A snapshot saved before UI-88 may still carry the removed pending die.
            var legacy = JsonConvert.DeserializeObject<DraftSnapshot>(
                "{\"Kind\":\"AllocateHunt\",\"Payload\":{\"Ids\":[\"a\"]},\"HuntAllocationDie\":\"b\"}");
            var upgraded = new TurnDraft();
            upgraded.Restore(legacy);
            upgraded.Reconcile(view);
            Assert.AreEqual(CommandKind.AllocateHunt, upgraded.Kind);
            CollectionAssert.AreEqual(new[] { "a" }, upgraded.Payload.Ids);
        }

        [Test]
        public void ConsoleAllocationCountsTheSetAndNamesItsLimit()
        {
            // UI-162: the allocation lives in the turn console; the plaque counts the set and the hint names the limit.
            var option = new ActionOption { Kind = CommandKind.AllocateHunt, Enabled = true, Minimum = 1, Maximum = 2, EligibleIds = { "a", "b", "c" } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.AllocateHunt);
            Assert.AreEqual("0/2", HuntAllocationText.Count(draft, option));
            StringAssert.Contains("0 of 2 · At least 1", HuntAllocationText.Hint(draft, option));
            Assert.AreEqual("Allocate & roll · No dice in the Hunt", HuntAllocationText.Confirm(draft));
            HuntActions.ToggleDie(draft, option, "a");
            HuntActions.ToggleDie(draft, option, "c");
            Assert.AreEqual("2/2", HuntAllocationText.Count(draft, option));
            StringAssert.Contains("Limit reached", HuntAllocationText.Hint(draft, option));
            Assert.AreEqual("Allocate & roll · 2 dice", HuntAllocationText.Confirm(draft));
            // UI-172: the plaque confirms; ready it names the set, otherwise it says what is missing.
            Assert.AreEqual("Allocate & roll · 2 dice · Click the plaque to allocate and roll", HuntAllocationText.Plaque(draft, option, true));
            Assert.AreEqual(HuntAllocationText.Hint(draft, option), HuntAllocationText.Plaque(draft, option, false));
            StringAssert.Contains("The Hunt plaque allocates and rolls", HuntAllocationText.Hint(draft, option));
            option.Minimum = 0;
            draft.Clear();
            draft.Choose(CommandKind.AllocateHunt);
            StringAssert.Contains("Zero allowed", HuntAllocationText.Hint(draft, option));
        }

        [Test]
        public void HuntHasOneDecisionHostAndRegionalRoutesStayWithTheFellowship()
        {
            var view = new PlayerView { Hunt = new HuntView(), Decision = new DecisionView { Kind = DecisionKind.Guide } };
            Assert.IsTrue(HuntActions.UsesPanel(view));
            Assert.IsFalse(FellowshipActions.UsesPanel(view, null));
            foreach (var kind in new[] { DecisionKind.FellowshipRoute, DecisionKind.CompanionSeparation })
            {
                view.Decision.Kind = kind;
                Assert.IsFalse(HuntActions.UsesPanel(view));
                Assert.IsTrue(FellowshipActions.UsesPanel(view, null));
            }
            view.Battle = new BattleView();
            Assert.IsFalse(HuntActions.UsesPanel(view));
            Assert.IsFalse(FellowshipActions.UsesPanel(view, null));
        }

        [Test]
        public void CompletedAndLegacyReportsDoNotInventRollsOrTiles()
        {
            var view = new PlayerView();
            Assert.IsNull(HuntActions.Displayed(view));
            Assert.AreEqual("No Hunt report yet", HuntActions.DecisionText(view));
            var missed = new HuntView { Dice = { new HuntDieView { First = 1, Result = 1 } } };
            view.HuntReport = new HuntReportView { Completed = true,
                Entries = { new HuntReportEntryView { Kind = HuntReportKind.Rolled, Hunt = missed }, new HuntReportEntryView { Kind = HuntReportKind.HuntCompleted } } };
            Assert.AreSame(missed, HuntActions.Displayed(view));
            Assert.IsEmpty(HuntActions.Displayed(view).TileId);
            Assert.AreEqual("Last Hunt · Complete", HuntActions.DecisionText(view));
        }

        [Test]
        public void UnknownTileDamageAndIndependentRevealStopStayVisible()
        {
            var tile = new HuntTileView(BoardPresentation.Load());
            var hunt = new HuntView { TileId = "base.hunt.special-sh.die.0.1", DamagePending = true, Reveal = true, Stop = true };
            tile.Apply(hunt);
            Assert.AreEqual("Damage ?", tile.Root.Q<Label>("hunt-damage").text);
            Assert.AreEqual(DisplayStyle.Flex, tile.Root.Q<Label>("hunt-reveal").style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, tile.Root.Q<Label>("hunt-stop").style.display.value);
            Assert.IsNull(hunt.TileDie);
            hunt.DamagePending = false;
            hunt.Damage = 3;
            hunt.Reveal = false;
            tile.Apply(hunt);
            Assert.AreEqual("Damage 3", tile.Root.Q<Label>("hunt-damage").text);
            Assert.AreEqual(DisplayStyle.None, tile.Root.Q<Label>("hunt-reveal").style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, tile.Root.Q<Label>("hunt-stop").style.display.value);
            hunt.CorruptionOnly = true;
            tile.Apply(hunt);
            Assert.AreEqual("Corruption 3", tile.Root.Q<Label>("hunt-damage").text);
            hunt.CorruptionOnly = false;
            hunt.SeparatesCompanions = true;
            hunt.DamagePending = true;
            tile.Apply(hunt);
            Assert.AreEqual("Separate ?", tile.Root.Q<Label>("hunt-damage").text);
            hunt.DamagePending = false;
            tile.Apply(hunt);
            Assert.AreEqual("Separate 3", tile.Root.Q<Label>("hunt-damage").text);
            hunt.SeparatesCompanions = false;
            hunt.Healing = 2;
            tile.Apply(hunt);
            Assert.AreEqual("Heal 2", tile.Root.Q<Label>("hunt-damage").text);
        }

        [Test]
        public void NaturalValuesModifiersAndFailedSubsetRemainDistinct()
        {
            var hunt = new HuntView { Bonus = 5, MovementBonus = 4, EffectBonus = 1, RerollBonus = 1, RerollSources = { "stronghold", "army", "nazgul" }, Dice =
                { new HuntDieView { Id = "miss", First = 1, Result = 1 }, new HuntDieView { Id = "hit", First = 2, Result = 6, Rerolled = true, Success = true } } };
            var strip = new HuntDiceStrip();
            strip.Apply(hunt);
            StringAssert.Contains("Natural 1", strip.Root.Q<Button>("hunt-die-miss").tooltip);
            StringAssert.Contains("Miss", strip.Root.Q<Button>("hunt-die-miss").tooltip);
            StringAssert.Contains("First 2 → Reroll 6", strip.Root.Q<Button>("hunt-die-hit").tooltip);
            StringAssert.Contains("Reroll bonus +1", strip.Root.Q<Button>("hunt-die-hit").tooltip);
            StringAssert.Contains("Nazgûl", strip.Root.Q<Label>("hunt-roll-bonus").tooltip);
            Assert.AreEqual("2 dice · 1 hit", strip.Root.Q<Label>("hunt-roll-bonus").text);
            Assert.AreEqual("Earlier FP moves +4", strip.Root.Q<Label>("hunt-movement-bonus").text);
            Assert.AreEqual("Effects +1", strip.Root.Q<Label>("hunt-effect-bonus").text);
            Assert.AreEqual("Rerolls · Shadow stronghold · Shadow army · Nazgûl", strip.Root.Q<Label>("hunt-reroll-sources").text);
            StringAssert.Contains("Reroll bonus +1", strip.Root.Q<Label>("hunt-reroll-bonus").text);
            Assert.AreEqual("2 → 6", strip.Root.Q<Button>("hunt-die-hit").Q<Label>("hunt-first-result").text);
            Assert.AreEqual(1, hunt.Dice.First().Result);
        }

        [Test]
        public void RerollBonusDoesNotOfferAChoiceAndPublicDiceKeepTheirIdentity()
        {
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.Shadow,
                Hunt = new HuntView { RerollBonus = 1, Dice = { new HuntDieView { Id = "miss", Result = 1 } } } };
            var strip = new HuntDiceStrip();
            strip.Apply(view.Hunt);
            var die = strip.Root.Q<Button>("hunt-die-miss");
            var sources = strip.Root.Q<Label>("hunt-reroll-sources");
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RerollHunt);
            strip.Context(view, draft, true);
            Assert.AreEqual("No reroll sources", sources.text);
            Assert.AreEqual(DisplayStyle.None, strip.Root.Q<Label>("hunt-reroll-budget").style.display.value);
            Assert.IsFalse(die.ClassListContains("legal-target"));
            Assert.IsFalse(die.ClassListContains("selected"));
            strip.Apply(view.Hunt);
            Assert.AreSame(die, strip.Root.Q<Button>("hunt-die-miss"));
            view.Hunt.Dice.Clear();
            strip.Apply(view.Hunt);
            Assert.AreSame(sources, strip.Root.Q<Label>("hunt-reroll-sources"));
            Assert.AreEqual("0 dice · 0 hits", strip.Root.Q<Label>("hunt-roll-bonus").text);
            Assert.IsNull(strip.Root.Q<Button>("hunt-die-miss"));
        }

        [Test]
        public void AllocatedAndSpecialDiceRemainInspectableWithoutGrantingAllocation()
        {
            var view = new PlayerView { You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Kind = DecisionKind.AllocateHunt } };
            var standard = new DieView { Id = "sh-1", Owner = Side.Shadow, Source = "standard", Location = DieLocation.Unrolled };
            var keeper = new DieView { Id = "fp-vilya", Owner = Side.FreePeoples, Source = "lords.vilya",
                Location = DieLocation.Hunt, Face = DieFace.Eye, HuntOrigin = "keeper-eye", SpecialDieChosen = true };
            view.Dice.AddRange(new[] { standard, keeper });
            var option = new ActionOption { Kind = CommandKind.AllocateHunt, Minimum = 1, Maximum = 1, EligibleIds = { standard.Id } };
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.AllocateHunt);
            Assert.IsFalse(HuntActions.Allocate(draft, option, keeper.Id));
            HuntActions.Allocate(draft, option, standard.Id);
            CollectionAssert.AreEqual(new[] { standard.Id }, draft.Payload.Ids, "Only the server's eligible die is allocated.");
            StringAssert.StartsWith("Not rolled yet", DieHints.Describe(view, standard));
            var detail = DieHints.Describe(view, keeper);
            StringAssert.StartsWith("Vilya · Eye", detail);
            StringAssert.Contains("In the Hunt Box", detail);
            StringAssert.Contains("Chosen", detail);
            StringAssert.DoesNotContain("fp-vilya", detail, "UI-97: no die id in the short description.");
            view.You = Side.FreePeoples;
            Assert.IsNull(HuntActions.Allocation(view), "Only the Shadow allocates.");
        }
    }
}
