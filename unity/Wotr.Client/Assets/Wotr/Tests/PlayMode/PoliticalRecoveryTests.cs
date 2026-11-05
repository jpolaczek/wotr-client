using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public sealed class PoliticalRecoveryTests
    {
        [UnityTest]
        public IEnumerator RestoredPoliticsNeverRecreatesTheBottomTrackWithMissingOrWildcardCost() =>
            MatchTestRoom.Wait(Restore());

        private static async Task Restore()
        {
            var storage = Path.Combine(Path.GetTempPath(), "wotr-politics-" + Guid.NewGuid().ToString("N"));
            try
            {
                foreach (var cost in new[] { "", "muster", "will", "combined" })
                    await RestoreCost(storage, cost);
            }
            finally
            {
                if (Directory.Exists(storage))
                    Directory.Delete(storage, true);
            }
        }

        private static async Task RestoreCost(string storage, string cost)
        {
            var fixture = Fixture();
            var view = fixture.View;
            var saved = await Save(storage, view, cost);
            try
            {
                using var client = new MountedMatchClient("http://localhost", view.MatchId, persistence: saved);
                client.Apply(view);
                await Task.Delay(100);
                var board = client.Board;
                board.Nations.Open();
                await Task.Delay(100);
                LayoutTests.Save(client.Viewport, "ui27-restored-" + (cost.Length == 0 ? "missing" : cost) + ".png");
                AssertNoBottomPolitics(board);
                Assert.AreEqual(cost, board.ActionDie.DieId);
                Assert.AreEqual(0, board.View.Revision);
                if (cost.Length == 0)
                {
                    Assert.IsFalse(board.Nations.Drawer.Q<Button>("political-slot-elves-1").enabledSelf);
                    ArmyGestures.Die(client, "muster", DieFace.Muster);
                }
                else if (cost is "will" or "combined")
                {
                    Assert.IsTrue(board.Nations.Drawer.Q<Button>("political-slot-elves-1").enabledSelf,
                        "The restored nation gesture already determines how the chosen die is used.");
                    Assert.IsNull(board.OwnDice.Q("action-face-picker"));
                }
                board.Nations.Open();
                await Task.Delay(100);
                AssertNoBottomPolitics(board);
                // UI-144: the step shows without a selecting click on the token.
                var arrow = board.Nations.Drawer.Q<Button>("political-slot-elves-1");
                Assert.IsTrue(arrow.enabledSelf);
                // UI-410: the station stays dark; the nation's token glows.
                Assert.IsFalse(arrow.ClassListContains("legal-target"));
                Assert.IsTrue(board.Nations.Drawer.Q<Button>("nation-status-elves").ClassListContains("legal-target"));
                Assert.AreEqual(cost.Length == 0 ? "muster" : cost, board.ActionDie.DieId);
                Assert.AreEqual(0, view.Revision, "Restoring and choosing components do not submit a command.");
            }
            finally
            {
                await saved.Flushed;
            }
        }

        private static async Task<LocalMatchPersistence> Save(string storage, PlayerView view, string cost)
        {
            var files = await LocalMatchPersistence.Open(storage, "http://localhost/", "politics-fixture", view.MatchId);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.AdvancePolitics, cost);
            draft.Change(p => p.NationId = "elves");
            files.SaveDraft(new SavedTurnDraft
            {
                Draft = draft.Snapshot(), Context = DraftContext.From(view), Origin = view.Decision.Id
            });
            await files.Flushed;
            return await LocalMatchPersistence.Open(storage, "http://localhost/", "politics-fixture", view.MatchId);
        }

        internal static void AssertNoBottomPolitics(BoardScreen board)
        {
            Assert.IsEmpty(board.Tray.Query<Button>().ToList(),
                "Politics must not add a second action or nation picker below the map.");
            Assert.IsEmpty(board.Tray.Query<Label>(className: "political-track").ToList());
            Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
            Assert.AreEqual(DisplayStyle.Flex, board.Nations.Drawer.resolvedStyle.display);
            Assert.AreEqual(DisplayStyle.None, board.Information.Root.resolvedStyle.display);
        }

        private static BoardFixture Fixture()
        {
            var fixture = new BoardFixture();
            fixture.View.Dice.Add(new DieView { Id = "will", Face = DieFace.WillOfTheWest, Location = DieLocation.Available });
            fixture.View.Dice.Add(new DieView { Id = "combined", Face = DieFace.ArmyMuster, Location = DieLocation.Available });
            fixture.Refresh();
            return fixture;
        }
    }
}
