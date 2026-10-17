using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    public class BattleDraftTests
    {
        [Test]
        public void EliteCycleKeepsTheOperationOrderAndAReselectionMovesItLast()
        {
            var view = BattleScreenFixture.Create(CommandKind.TakeBattleLosses);
            var elite = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Elite);
            var regular = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Regular);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.TakeBattleLosses);
            BattleDraft.CycleLoss(draft, elite);
            BattleDraft.CycleLoss(draft, regular);
            var submitted = draft.Command(view);
            BattleDraft.CycleLoss(draft, elite);
            CollectionAssert.AreEqual(new[] { elite.Id, regular.Id }, draft.Payload.Losses.Select(l => l.UnitId));
            Assert.AreEqual(3, BattleDraft.Damage(draft, view));
            Assert.AreEqual(BattleLossKind.Downgrade, submitted.Payload.Losses[0].Kind);
            BattleDraft.CycleLoss(draft, elite);
            Assert.AreEqual(1, BattleDraft.Damage(draft, view));
            BattleDraft.CycleLoss(draft, elite);
            CollectionAssert.AreEqual(new[] { regular.Id, elite.Id }, draft.Payload.Losses.Select(l => l.UnitId));
            Assert.AreEqual(BattleLossKind.Downgrade, draft.Payload.Losses[1].Kind);
            draft.Back();
            Assert.AreEqual(1, draft.Payload.Losses.Count);
        }

        [Test]
        public void RerollsAllowAnExplicitEmptySetAndNeverSelectFailedDiceAutomatically()
        {
            var view = BattleScreenFixture.Create(CommandKind.RerollBattle);
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.RerollBattle);
            Assert.IsEmpty(draft.Payload.Ids);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            foreach (var id in view.Options[0].EligibleIds.Take(view.Options[0].Maximum))
                draft.Toggle(id);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            foreach (var id in draft.Payload.Ids.ToArray())
                draft.Toggle(id);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            Assert.IsEmpty(draft.Command(view).Payload.Ids);
        }

        [Test]
        public void LossOrdersAreCopiedIntoCommandsAndUndoNeverMutatesSubmittedChoices()
        {
            var view = BattleScreenFixture.Create(CommandKind.TakeBattleLosses);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.TakeBattleLosses);
            var elite = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Elite).Id;
            var regular = view.Battle.Attacker.Units.First(u => u.Kind == UnitKind.Regular).Id;
            BattleDraft.Loss(draft, regular, BattleLossKind.Remove);
            BattleDraft.Loss(draft, elite, BattleLossKind.Downgrade);
            Assert.AreEqual(2, BattleDraft.Damage(draft, view));
            var command = draft.Command(view);
            BattleDraft.Loss(draft, elite, BattleLossKind.Remove);
            Assert.AreEqual(BattleLossKind.Downgrade, command.Payload.Losses[1].Kind);
            draft.Back();
            CollectionAssert.AreEqual(new[] { regular, elite }, draft.Payload.Losses.Select(l => l.UnitId));
            Assert.AreEqual(BattleLossKind.Downgrade, draft.Payload.Losses[1].Kind);
            draft.Reconcile(view);
            Assert.AreEqual(2, draft.Payload.Losses.Count);
            draft.Back();
            Assert.IsEmpty(draft.Payload.Losses, "Undo cannot cross an acknowledged server revision.");
        }

        [Test]
        public void CardAndAdvanceHaveNoDefaultStrategicChoiceAndDragOnlySelects()
        {
            foreach (var kind in new[] { CommandKind.CommitCombatCard, CommandKind.AdvanceBattle, CommandKind.ExtendSiege })
            {
                var view = BattleScreenFixture.Create(kind);
                var draft = new TurnDraft();
                DraftGesture.Choose(draft, view, kind);
                Assert.IsFalse(draft.Ready(view.Options[0]));
                if (kind != CommandKind.CommitCombatCard)
                    continue;
                Assert.IsTrue(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.Card, view.Hand[0]), new DraftToken(DraftObject.Tray)));
                Assert.AreEqual(view.Hand[0], draft.Payload.CardId);
                Assert.IsTrue(draft.Ready(view.Options[0]));
                Assert.AreEqual(6, view.Hand.Count);
            }
        }

        [Test]
        public void AttackClickPreservesOriginRequiresAUnitForAnyRearFigureAndIgnoresAnArmyDrop()
        {
            var view = BattleScreenFixture.Create(CommandKind.AttackArmy);
            var click = new TurnDraft();
            DraftGesture.Choose(click, view, CommandKind.AttackArmy);
            BoardDraft.SelectRegion(click, view, "minas-tirith");
            var leader = view.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Leader).Id;
            ArmyDraft.Toggle(click, new[] { leader });
            StringAssert.Contains("rearguard", AttackDraft.Problem(click, view));
            ArmyDraft.Toggle(click, new[] { leader });
            Assert.IsTrue(BoardDraft.SelectRegion(click, view, "lossarnach"));
            var drag = new TurnDraft();
            DraftGesture.Choose(drag, view, CommandKind.AttackArmy);
            var before = JsonConvert.SerializeObject(drag.Payload);
            // UI-128: attacks are clicked like moves; an army drop changes nothing.
            Assert.IsFalse(DraftGesture.Drop(drag, view, new DraftToken(DraftObject.Army, "minas-tirith"), new DraftToken(DraftObject.Region, "lossarnach")));
            Assert.AreEqual(before, JsonConvert.SerializeObject(drag.Payload));
            Assert.IsTrue(view.Units.Where(u => click.Payload.Moves[0].UnitIds.Contains(u.Id)).All(u => u.Location == "minas-tirith"));
        }

        [Test]
        public void RetreatDestinationSurvivesSnapshotAndReconcileWithoutSubstitution()
        {
            var view = BattleScreenFixture.Create(CommandKind.RetreatBattle);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RetreatBattle);
            Assert.IsFalse(draft.Ready(view.Options[0]));
            draft.Change(p => p.RegionId = "pelargir");
            Assert.AreEqual("pelargir", draft.Command(view).Payload.RegionId);
            draft.Reconcile(view);
            Assert.IsTrue(draft.Ready(view.Options[0]));
            view.Options[0].Targets.Clear();
            draft.Reconcile(view);
            Assert.IsFalse(draft.Ready(view.Options[0]));
        }
    }
}
