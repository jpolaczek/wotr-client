using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    public sealed class BattleResolutionTests
    {
        [Test]
        public void LegalPreviewKeepsChosenRerollCountAndLimitVisible()
        {
            var view = BattleScreenFixture.Create(CommandKind.RerollBattle);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RerollBattle);
            var preview = new ActionPreview { Legal = true };
            view.Battle.Attacker.Hits = 2;
            // UI-420 (owner): Keep roll names the roll it keeps; Reroll selected the dice it rolls again.
            Assert.AreEqual("Keep roll · Accept your Combat roll (2 hits); use none of your 2 rerolls", BattleSummary.Describe(draft, view, preview));
            draft.Toggle(view.Options[0].EligibleIds[0]);
            Assert.AreEqual("Reroll selected · Roll 1 failed die again · 1 of 2 rerolls", BattleSummary.Describe(draft, view, preview));
        }

        [Test]
        public void NaturalResultsAndDifferentRerollBonusKeepServerHitFlags()
        {
            var battle = BattleScreenFixture.Create(CommandKind.RerollBattle).Battle;
            var die = new BattleDieView { Id = "natural-one", Result = 1, Modifier = 5, HitTarget = 5,
                Hit = false, Reroll = 6, RerollModifier = -2, RerollHit = true };
            var button = new BattleDieButton(die, "Combat", battle, null);
            Assert.AreEqual(2, button.Query<DiePips>().ToList().Count);
            Assert.AreEqual("+5 › -2", button.Q<Label>("battle-die-modifier").text);
            StringAssert.Contains("First roll 1 · Modifier +5 · Target 5+ · Miss", button.tooltip);
            StringAssert.Contains("Leader reroll 6 · Modifier -2 · Hit", button.tooltip);
            StringAssert.DoesNotContain("\n\n", button.tooltip);
            StringAssert.DoesNotEndWith("\n", button.tooltip);
            // UI-420 (owner): a modifier of zero is left out.
            die.Modifier = 0;
            StringAssert.Contains("First roll 1 · Target 5+ · Miss", new BattleDieButton(die, "Combat", battle, null).tooltip);
            die.Modifier = 5;
            Assert.AreEqual("✓", button.Q<Label>("battle-die-outcome").text);
            Assert.IsTrue(button.ClassListContains("battle-hit"));
            die.Reroll = null;
            die.RerollHit = false;
            button = new BattleDieButton(die, "Combat", battle, null);
            Assert.IsFalse(button.ClassListContains("battle-hit"), "A natural 1 cannot become a hit through client arithmetic.");
        }

        [Test]
        public void ServerReplacementIsReportedEvenWhenReusedRegularDidNotChangeLocation()
        {
            var view = BattleScreenFixture.Create(CommandKind.TakeBattleLosses);
            var elite = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Elite);
            var regular = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Regular);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.TakeBattleLosses);
            BattleDraft.Loss(draft, regular.Id, BattleLossKind.Remove);
            BattleDraft.Loss(draft, elite.Id, BattleLossKind.Downgrade);
            var preview = new ActionPreview { Legal = true, Losses = new ArmyLossPreview { RequiredHits = 2, AppliedHits = 2 } };
            preview.Losses.Operations.Add(new ActionLossFact { Figure = new ActionParticipant { Id = regular.Id, NationId = "gondor" }, Hits = 1 });
            preview.Losses.Operations.Add(new ActionLossFact { Figure = new ActionParticipant { Id = elite.Id, NationId = "gondor" },
                Operation = BattleLossKind.Downgrade, Hits = 1, ReplacementId = regular.Id, ReplacementFrom = "casualties" });
            Assert.IsEmpty(preview.Placements);
            StringAssert.Contains("Elite replaced by a regular", BattleSummary.Describe(draft, view, preview));
            // UI-420 (owner): Apply losses names the region, the chosen losses and their hits.
            StringAssert.StartsWith("Apply losses in Lossarnach · Remove 1 ", BattleSummary.Describe(draft, view, preview));
            StringAssert.Contains(", downgrade 1 Gondor Elite (2 hits) · ", BattleSummary.Describe(draft, view, preview));
            StringAssert.Contains("Regular from casualties", BattleSummary.LossDetails(view, preview));
            Assert.AreEqual("Assigned 2 / 2 · Remaining 0", BattleLossText.Budget(view, draft, preview.Losses));
            var root = new VisualElement();
            var choices = new BattleLossChoices(root, view, draft, action => action());
            choices.Preview(preview);
            var tooltip = root.Q<Button>("loss-" + elite.Id).tooltip;
            choices.Preview(preview);
            Assert.AreEqual(tooltip, root.Q<Button>("loss-" + elite.Id).tooltip, "Repeated preview must not duplicate descriptions.");
        }

        [Test]
        public void ExhaustedArmyShowsExcessHitsAndMissingRegularCostsOneHit()
        {
            var loss = new ActionLossFact { Operation = BattleLossKind.Downgrade, Hits = 1 };
            StringAssert.Contains("Elite removed · 1 hit", BattleLossText.Operation(loss));
            var preview = new ArmyLossPreview { RequiredHits = 3, AppliedHits = 1 };
            Assert.AreEqual("Assigned 1 / 3 · 2 excess hits", BattleLossText.Budget(new PlayerView(), null, preview));
        }
    }
}
