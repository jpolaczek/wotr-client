using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class ShadowStrategyDraftTests
    {
        [Test]
        public void VoluntaryLossesPermitZeroAndKeepOrderedEliteChoicesUntilUndo()
        {
            var option = new ActionOption { Kind = CommandKind.TakeBattleLosses, Minimum = 0, Maximum = 4, EligibleIds = { "regular", "elite" } };
            var view = new PlayerView
            {
                Battle = new BattleView { VoluntaryLosses = true },
                Options = { option },
                Units = { new UnitView { Id = "regular", Kind = UnitKind.Regular }, new UnitView { Id = "elite", Kind = UnitKind.Elite } }
            };
            var draft = new TurnDraft();
            draft.Choose(option.Kind);
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual("Keep all units · No sacrifice bonus.", BattleSummary.Describe(draft, view, new ActionPreview()));
            BattleDraft.Loss(draft, "regular", BattleLossKind.Remove);
            BattleDraft.Loss(draft, "elite", BattleLossKind.Downgrade);
            Assert.AreEqual(2, BattleDraft.Damage(draft, view));
            BattleDraft.Loss(draft, "elite", BattleLossKind.Remove);
            Assert.AreEqual(3, BattleDraft.Damage(draft, view));
            draft.Back();
            Assert.AreEqual(BattleLossKind.Downgrade, draft.Payload.Losses[1].Kind);
            Assert.AreEqual("regular", draft.Payload.Losses[0].UnitId);
        }

        [Test]
        public void DenethorRemovalUsesOnlyTheServerOfferedDieAndUndoRestoresTheCost()
        {
            var option = new ActionOption { Kind = CommandKind.DiscardDenethor, EligibleDice = { "will", "army" } };
            var draft = new TurnDraft();
            draft.Choose(option.Kind);
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.DieId = "will");
            Assert.IsTrue(draft.Ready(option));
            draft.Change(p => p.DieId = "army");
            Assert.IsTrue(draft.Ready(option));
            draft.Back();
            Assert.AreEqual("will", draft.Payload.DieId);
            option.EligibleDice.Remove("will");
            Assert.IsFalse(draft.Ready(option));
            Assert.IsEmpty(draft.Payload.RingId);
        }

        [Test]
        public void SacrificePreviewDistinguishesOnslaughtFromAnArmyEliminatedBeforeCombat()
        {
            var elite = new UnitView { Id = "elite", Kind = UnitKind.Elite };
            var view = new PlayerView
            {
                You = Side.Shadow,
                Units = { elite },
                Battle = new BattleView
                {
                    VoluntaryLosses = true,
                    LossSource = "base.sh.strategy.02",
                    Attacker = new BattleForceView { Side = Side.Shadow, Units = { elite } }
                }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.TakeBattleLosses);
            BattleDraft.Loss(draft, "elite", BattleLossKind.Remove);
            var preview = new ActionPreview { RemovedIds = { "elite" } };
            Assert.That(BattleSummary.Describe(draft, view, preview), Does.Contain("Roll 2 dice · Hits on 4+"));
            view.Battle.LossSource = "base.sh.strategy.04";
            Assert.AreEqual("Army eliminated · No Combat roll.", BattleSummary.Describe(draft, view, preview));
            BattleDraft.Loss(draft, "elite", BattleLossKind.Downgrade);
            preview.Placements.Add(new UnitPlacementChoice { UnitId = "replacement", RegionId = "osgiliath" });
            Assert.AreEqual("Elite → Regular · Combat +1 · Re-roll unchanged.", BattleSummary.Describe(draft, view, preview));
        }
    }
}
