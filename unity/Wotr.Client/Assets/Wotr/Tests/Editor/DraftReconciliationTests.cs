using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class DraftReconciliationTests
    {
        [Test]
        public void UnchangedDraftReportsNoCorrectionButUndoCannotCrossTheRevision()
        {
            var view = View(CommandKind.DrawCard);
            view.Options[0].EligibleDice.Add("chosen-die");
            view.Options[0].Targets.AddRange(new[] { "character", "strategy" });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.DrawCard, "chosen-die");
            draft.Change(payload => payload.Deck = "strategy");

            Assert.IsFalse(draft.Reconcile(view));
            draft.Change(payload => payload.Deck = "character");
            draft.Back();
            Assert.AreEqual("strategy", draft.Payload.Deck);
            Assert.AreEqual("chosen-die", draft.Payload.DieId);
            draft.Back();
            Assert.IsNull(draft.Kind);
        }

        [TestCase(CommandKind.AllocateHunt)]
        [TestCase(CommandKind.DiscardCards)]
        [TestCase(CommandKind.RemoveExcess)]
        [TestCase(CommandKind.RerollBattle)]
        [TestCase(CommandKind.RerollHunt)]
        [TestCase(CommandKind.AdvanceBattle)]
        [TestCase(CommandKind.ExtendSiege)]
        public void IdSelectionsKeepAvailableChoicesAndNeverChooseAReplacement(CommandKind kind)
        {
            var view = View(kind);
            view.Options[0].EligibleIds.AddRange(new[] { "kept", "replacement" });
            var draft = new TurnDraft();
            draft.Choose(kind);
            draft.Toggle("removed");
            draft.Toggle("kept");
            Assert.IsTrue(draft.Reconcile(view));
            CollectionAssert.AreEqual(new[] { "kept" }, draft.Payload.Ids);
            Assert.IsFalse(draft.Reconcile(view));
        }

        [Test]
        public void RemovedActionClearsTheDraftAndReportsThatCorrection()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.Pass);
            var view = View(CommandKind.Pass);
            view.Options[0].Enabled = false;
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsNull(draft.Kind);
            Assert.IsFalse(draft.Reconcile(view));
        }

        [Test]
        public void LosingAnEventCostKeepsTheCardAndNationWithoutSubstitutingAnotherCost()
        {
            var view = View(CommandKind.PlayEvent);
            var card = new EventPlayOption
            {
                CardId = "stormcrow", NationTargets = { "gondor" },
                Costs = { new EventCostOption { DieId = "event", Face = DieFace.Event } }
            };
            view.Options[0].Events.Add(card);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.PlayEvent, "wild");
            draft.Payload.CardId = card.CardId;
            draft.Payload.NationId = "gondor";
            draft.Payload.Face = draft.Payload.CardCost = DieFace.Muster;
            Assert.IsTrue(draft.Reconcile(view));
            Assert.AreEqual(card.CardId, draft.Payload.CardId);
            Assert.AreEqual("gondor", draft.Payload.NationId);
            Assert.IsEmpty(draft.Payload.DieId);
            Assert.IsNull(draft.Payload.Face);
            Assert.IsFalse(draft.Ready(view.Options[0]));
        }

        [Test]
        public void EffectDependenciesDisappearTransitivelyButOtherGroupsRemainSelected()
        {
            var view = View(CommandKind.ChooseEffect);
            var option = view.Options[0];
            option.EligibleIds.AddRange(new[] { "hits", "dependent", "other" });
            option.EffectChoices.Add(new EffectChoiceView { Id = "hits", RequiresAny = { "companion" } });
            option.EffectChoices.Add(new EffectChoiceView { Id = "dependent", RequiresAny = { "hits" } });
            option.EffectChoices.Add(new EffectChoiceView { Id = "other", SelectionGroup = "independent" });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            draft.Payload.Ids.AddRange(new[] { "dependent", "hits", "companion", "other" });
            Assert.IsTrue(draft.Reconcile(view));
            CollectionAssert.AreEqual(new[] { "other" }, draft.Payload.Ids);
        }

        [Test]
        public void CharacterChoicesUseTheirOwnCostAndTargetsAndKeepOtherLegalSelections()
        {
            var view = View(CommandKind.UseCharacterAbility);
            var choice = new CharacterActionOption
            {
                CharacterId = "saruman", AbilityId = "voice", TargetCount = 2,
                Dice = { "muster" }, Targets = { "orthanc", "dunland" }
            };
            view.Options[0].CharacterActions.Add(choice);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.UseCharacterAbility, "muster");
            draft.Payload.CharacterId = choice.CharacterId;
            draft.Payload.AbilityId = choice.AbilityId;
            draft.Payload.Ids.AddRange(choice.Targets);
            Assert.IsFalse(draft.Reconcile(view));
            choice.Targets.Remove("dunland");
            Assert.IsTrue(draft.Reconcile(view));
            CollectionAssert.AreEqual(new[] { "orthanc" }, draft.Payload.Ids);
            Assert.AreEqual("muster", draft.Payload.DieId);
            Assert.IsFalse(draft.Ready(view.Options[0]));
        }

        [TestCase(CommandKind.TakeBattleLosses)]
        [TestCase(CommandKind.TakeEventLosses)]
        public void AnInvalidDowngradeIsRemovedWithoutBecomingAnElimination(CommandKind kind)
        {
            var view = View(kind);
            view.Options[0].EligibleIds.AddRange(new[] { "changed", "kept" });
            view.Units.Add(new UnitView { Id = "changed", Kind = UnitKind.Regular });
            view.Units.Add(new UnitView { Id = "kept", Kind = UnitKind.Elite });
            var draft = new TurnDraft();
            draft.Choose(kind);
            BattleDraft.Loss(draft, "changed", BattleLossKind.Downgrade);
            BattleDraft.Loss(draft, "kept", BattleLossKind.Downgrade);
            Assert.IsTrue(draft.Reconcile(view));
            Assert.AreEqual(1, draft.Payload.Losses.Count);
            Assert.AreEqual("kept", draft.Payload.Losses[0].UnitId);
            Assert.AreEqual(BattleLossKind.Downgrade, draft.Payload.Losses[0].Kind);
        }

        internal static PlayerView View(CommandKind kind) => new PlayerView
        {
            Decision = new DecisionView { Id = "same-decision", Kind = DecisionKind.Action },
            Options = { new ActionOption { Kind = kind, Maximum = 3 } }
        };
    }
}
