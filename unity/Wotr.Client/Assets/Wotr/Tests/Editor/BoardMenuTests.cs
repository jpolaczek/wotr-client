using System;
using System.IO;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    public class BoardMenuTests
    {
        private string directory;
        private ControlledRecoveryStorage files;

        [SetUp]
        public void SetUp()
        {
            directory = Path.Combine(Path.GetTempPath(), "wotr-menu-" + Guid.NewGuid().ToString("N"));
            files = new ControlledRecoveryStorage(new LocalMatchFiles(directory, "https://server/", "account", "match"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(directory))
                Directory.Delete(directory, true);
        }

        [Test]
        public async Task SettledDraftErrorKeepsExitBlockedUntilLocalRetryDurablySavesTheLatestChoice()
        {
            var local = await LocalMatchPersistence.Open(files);
            files.FailName = "draft";
            local.SaveDraft(Draft("last-choice"));
            await local.Flushed;
            Assert.AreEqual(TaskStatus.RanToCompletion, local.Flushed.Status);
            var exit = new BoardExit(local);
            StringAssert.StartsWith("Draft", await exit.Prepare(false));
            Assert.IsFalse(exit.Ready);
            Assert.IsNull(local.Draft);
            files.FailName = "";
            Assert.IsEmpty(await exit.Prepare(true));
            Assert.IsTrue(exit.Ready);
            Assert.AreEqual("last-choice", (await LocalMatchPersistence.Open(files)).Draft.Origin);
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task BothJournalErrorsRemainObservableAndRetryKeepsTheOriginalCommand(bool policy)
        {
            var local = await LocalMatchPersistence.Open(files);
            var journal = policy ? local.Policy : local.Actions;
            files.FailName = policy ? "policy" : "action";
            var command = LocalRecoveryTests.Command("original");
            if (policy)
                command.Kind = CommandKind.SetCombatCardPolicy;
            var expected = JsonConvert.SerializeObject(command);
            await LocalRecoveryTests.Fails<LocalSaveException>(journal.Save(command));
            command.Payload.DieId = "must-not-replace-physical-cost";
            await journal.Flushed;
            Assert.AreEqual(TaskStatus.RanToCompletion, journal.Flushed.Status);
            var exit = new BoardExit(local);
            Assert.IsNotEmpty(await exit.Prepare(false));
            Assert.IsFalse(exit.Ready);
            files.FailName = "";
            Assert.IsEmpty(await exit.Prepare(true));
            Assert.AreEqual(expected, JsonConvert.SerializeObject(journal.Pending));
            var reopened = await LocalMatchPersistence.Open(files);
            Assert.AreEqual(expected, JsonConvert.SerializeObject((policy ? reopened.Policy : reopened.Actions).Pending));
        }

        [Test]
        public async Task ReceiptCleanupFailureIsRetriedLocallyAndCannotClearAnotherCommand()
        {
            var local = await LocalMatchPersistence.Open(files);
            await local.Actions.Save(LocalRecoveryTests.Command("committed"));
            files.FailName = "action-complete";
            await LocalRecoveryTests.Fails<LocalSaveException>(local.Actions.Complete("committed"));
            var exit = new BoardExit(local);
            Assert.IsNotEmpty(await exit.Prepare(false));
            Assert.AreEqual("committed", local.Actions.Pending.CommandId);
            files.FailName = "";
            Assert.IsEmpty(await exit.Prepare(true));
            Assert.IsNull((await LocalMatchPersistence.Open(files)).Actions.Pending);
            await local.Actions.Save(LocalRecoveryTests.Command("next"));
            Assert.IsEmpty(await exit.Prepare(true));
            Assert.AreEqual("next", local.Actions.Pending.CommandId);
        }

        [Test]
        public async Task ExitWaitsForWritesAddedWhileAnEarlierDraftIsStillSaving()
        {
            var local = await LocalMatchPersistence.Open(files);
            var draftGate = files.Pause("draft");
            local.SaveDraft(Draft("first"));
            await draftGate.Started;
            var exit = new BoardExit(local);
            var leaving = exit.Prepare(false);
            Assert.IsFalse(leaving.IsCompleted);
            local.SaveDraft(Draft("latest"));
            var journalGate = files.Pause("action");
            var action = local.Actions.Save(LocalRecoveryTests.Command("pending"));
            await journalGate.Started;
            draftGate.Release();
            await Task.Yield();
            Assert.IsFalse(exit.Ready);
            Assert.IsFalse(leaving.IsCompleted);
            journalGate.Release();
            await action;
            Assert.IsEmpty(await leaving);
            var reopened = await LocalMatchPersistence.Open(files);
            Assert.AreEqual("latest", reopened.Draft.Origin);
            Assert.AreEqual("pending", reopened.Actions.Pending.CommandId);
        }

        [TestCase(DecisionKind.CouncilVersions, CommandKind.ChooseCompanionVersions)]
        [TestCase(DecisionKind.HuntDamage, CommandKind.ResolveHuntDamage)]
        [TestCase(DecisionKind.Losses, CommandKind.TakeBattleLosses)]
        [TestCase(DecisionKind.FellowshipRoute, CommandKind.RevealFellowship)]
        public void EmptyMandatoryFormDoesNotConsumeEscapeButAnActualChoiceDoes(DecisionKind decision, CommandKind command)
        {
            var view = new PlayerView { Decision = new DecisionView { Kind = decision } };
            var draft = new TurnDraft();
            draft.Choose(command);
            Assert.IsFalse(DraftCancellation.HasSelection(view, draft));
            draft.Toggle("chosen-object");
            Assert.IsTrue(DraftCancellation.HasSelection(view, draft));
        }

        [Test]
        public void CommittedSequenceCostIsNotAChoiceButItsNextPlannedArmyIs()
        {
            var view = new PlayerView { Decision = new DecisionView { Kind = DecisionKind.ActionSequence } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveArmies, "paid-die");
            draft.Change(p => { p.Stepwise = true; p.AbilityId = "paid-ability"; });
            Assert.IsFalse(DraftCancellation.HasSelection(view, draft));
            draft.Change(p => p.Moves.Add(new ArmyMoveChoice { From = "gorgoroth" }));
            Assert.IsTrue(DraftCancellation.HasSelection(view, draft));
        }

        private static SavedTurnDraft Draft(string origin) => new SavedTurnDraft
        {
            Origin = origin,
            Context = new DraftContext { MatchId = "match" },
            Draft = new DraftSnapshot { Kind = CommandKind.DrawCard, Payload = new CommandPayload { DieId = "physical-die" } }
        };
    }
}
