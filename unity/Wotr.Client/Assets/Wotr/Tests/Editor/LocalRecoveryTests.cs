using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class LocalRecoveryTests
    {
        private string directory;
        [SetUp] public void SetUp() => directory = Path.Combine(Path.GetTempPath(), "wotr-recovery-" + Guid.NewGuid().ToString("N"));
        [TearDown] public void TearDown() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }

        [Test]
        public async Task FilesAreScopedToServerAccountAndMatchAndNeverOverwriteAnotherPendingCommand()
        {
            var files = Files();
            var journal = await CommandJournal.Open(files);
            await journal.Save(Command("first"));
            Assert.IsNull((await CommandJournal.Open(Files(account: "other"))).Pending);
            Assert.IsNull((await CommandJournal.Open(Files(server: "https://another/"))).Pending);
            Assert.IsNull((await CommandJournal.Open(Files(match: "other"))).Pending);
            Assert.IsNull((await CommandJournal.Open(files, true)).Pending);
            await Fails<LocalSaveException>(journal.Save(Command("second")));
            await journal.Complete("unrelated");
            Assert.AreEqual("first", (await CommandJournal.Open(Files())).Pending.CommandId);
            await journal.Complete("first");
            await journal.Save(Command("second"));
            await journal.Complete("first");
            Assert.AreEqual("second", (await CommandJournal.Open(Files())).Pending.CommandId);
        }

        [Test]
        public async Task LeavingWaitsForBothCommandJournalsBeforeAnotherClientOpensTheMatch()
        {
            var local = await LocalMatchPersistence.Open(directory, "https://server/", "account", "match");
            var action = local.Actions.Save(Command("action"));
            var setting = Command("policy");
            setting.Kind = CommandKind.SetCombatCardPolicy;
            var policy = local.Policy.Save(setting);
            await local.Flushed;
            var reopened = await LocalMatchPersistence.Open(directory, "https://server/", "account", "match");
            Assert.AreEqual("action", reopened.Actions.Pending.CommandId);
            Assert.AreEqual("policy", reopened.Policy.Pending.CommandId);
            Assert.IsTrue(action.IsCompleted);
            Assert.IsTrue(policy.IsCompleted);
            await Task.WhenAll(action, policy);
        }

        [Test]
        public async Task TruncatedOrIncompatibleRecordsFailInsteadOfDiscardingAnUncertainCommand()
        {
            var journal = await CommandJournal.Open(Files());
            await journal.Save(Command("first"));
            var path = Directory.GetFiles(directory, "action.json", SearchOption.AllDirectories).Single();
            var original = File.ReadAllText(path);
            foreach (var broken in new[] { "{", original.Replace("\"Version\":1", "\"Version\":99"), original.Replace("account", "another-account") })
            {
                File.WriteAllText(path, broken);
                await Fails<LocalSaveException>(CommandJournal.Open(Files()));
                Assert.AreEqual(broken, File.ReadAllText(path));
            }
        }

        [Test]
        public async Task DraftSurvivesRestartButReconcilesItsCostAndNeverCopiesAHandOrChoices()
        {
            var view = View();
            view.Hand.Add("not-persisted-card");
            view.Decision.Choices.Add("not-persisted-private-choice");
            var session = new TurnDraftSession();
            session.Update(view);
            session.Draft.Choose(CommandKind.DrawCard, "die");
            session.Draft.Change(p => p.Deck = "strategy");
            session.Edited(view);
            var local = await LocalMatchPersistence.Open(directory, "https://server/", "account", "match");
            local.SaveDraft(session.Save());
            await local.Flushed;
            Assert.IsEmpty(local.Error);
            var restored = await LocalMatchPersistence.Open(directory, "https://server/", "account", "match");
            var json = JsonConvert.SerializeObject(restored.Draft);
            Assert.IsFalse(json.Contains("not-persisted"));
            var next = new TurnDraftSession();
            next.Restore(restored.Draft);
            next.Update(view);
            Assert.AreEqual("die", next.Draft.Payload.DieId);
            Assert.AreEqual("strategy", next.Draft.Payload.Deck);
            view.Dice[0].Location = DieLocation.Spent;
            view.Options[0].EligibleDice.Clear();
            next.Update(view);
            Assert.IsEmpty(next.Draft.Payload.DieId);
            var nextWindow = View();
            nextWindow.Decision.Id = "another-window";
            nextWindow.Decision.Kind = DecisionKind.CombatCard;
            next.Update(nextWindow);
            Assert.IsNull(next.Draft.Kind);
        }

        [Test]
        public async Task CoalescedDraftWritesKeepTheLatestSelectionAndClearDurably()
        {
            var local = await LocalMatchPersistence.Open(directory, "https://server/", "account", "match");
            for (var index = 0; index < 30; index++)
                local.SaveDraft(new SavedTurnDraft { Origin = index.ToString(), Context = DraftContext.From(View()), Draft = new DraftSnapshot() });
            await local.Flushed;
            var restored = await LocalMatchPersistence.Open(directory, "https://server/", "account", "match");
            Assert.AreEqual("29", restored.Draft.Origin);
            local.SaveDraft(null);
            await local.Flushed;
            Assert.IsNull((await LocalMatchPersistence.Open(directory, "https://server/", "account", "match")).Draft);
        }

        private LocalMatchFiles Files(string server = "https://server/", string account = "account", string match = "match") =>
            new LocalMatchFiles(directory, server, account, match);

        internal static GameCommand Command(string id) => new GameCommand
        {
            MatchId = "match", CommandId = id, DecisionId = "current", ExpectedRevision = 1,
            Kind = CommandKind.DrawCard, Payload = new CommandPayload { DieId = "die", Deck = "strategy" }
        };

        internal static async Task Fails<T>(Task task) where T : Exception
        {
            try { await task; }
            catch (T) { return; }
            Assert.Fail("Expected " + typeof(T).Name);
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "match", Revision = 1, Turn = 2, Phase = GamePhase.ActionResolution,
            You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "current", Kind = DecisionKind.Action },
            Dice = { new DieView { Id = "die", Owner = Side.FreePeoples, Face = DieFace.Event, Location = DieLocation.Available } },
            Options = { new ActionOption { Kind = CommandKind.DrawCard, EligibleDice = { "die" }, Targets = { "character", "strategy" } } }
        };
    }
}
