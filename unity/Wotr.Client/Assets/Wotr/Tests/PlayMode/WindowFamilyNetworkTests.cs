using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class WindowFamilyNetworkTests
    {
        private const string Protection = "base.fp.strategy.02";

        [UnityTest]
        public IEnumerator ClosingAWindowClosesThePreviewItOpened() => Wait(Family());

        [UnityTest]
        public IEnumerator BattleReportFromALogEntryClosesWithThatEntryUnlessReopenedOnItsOwn() => Wait(Archive());

        private static async Task Archive()
        {
            using var room = await Connect("ui42-battles", ScenarioKind.BaseAcceptance);
            var board = room.Fp.Board;
            board.Battles.Close();
            await ActionContextNetworkTests.OpenLog(room, board);
            var old = board.Log.Model.Rows.Where(r => r.Step == null && r.Group.Action.Command == CommandKind.AttackArmy)
                .Select(r => r.Group).Last();
            await ActionContextNetworkTests.OpenEntry(room, board, old.Action.Id);
            PointerGestureEvents.ClickAt(board.Log.Popup.Q<Button>("open-archived-battle"));
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.BattleReport.Root.style.display.value);
            Assert.AreSame(board.Log.Popup, board.Family.OwnerOf(board.BattleReport.Root));
            board.Log.CloseCard();
            await room.Settle();
            Assert.AreEqual(DisplayStyle.None, board.BattleReport.Root.style.display.value, "The report closes with its log entry.");
            await ActionContextNetworkTests.OpenEntry(room, board, old.Action.Id);
            PointerGestureEvents.ClickAt(board.Log.Popup.Q<Button>("open-archived-battle"));
            await room.Settle();
            Assert.AreSame(board.Log.Popup, board.Family.OwnerOf(board.BattleReport.Root));
            // The same path as the ⚔ map control, which is hidden while this battle is still open.
            board.BattleReport.ShowFrom(board.BattleReport.Displayed, board.BattleReport.Open);
            await room.Settle();
            Assert.IsNull(board.Family.OwnerOf(board.BattleReport.Root), "The ⚔ map control opens the report independently.");
            board.Log.CloseCard();
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, board.BattleReport.Root.style.display.value);
            board.BattleReport.Close();
        }

        private static async Task Family()
        {
            using var room = await Connect("effects-inspection", ScenarioKind.BaseAcceptance);
            foreach (var actor in new[] { room.Fp, room.Sh })
            {
                var board = actor.Board;
                var revision = board.View.Revision;
                await EffectsReadTheCardWithoutASubWindow(room, board);
                await PreviewClosesWithItsOwner(room, actor);
                Assert.AreEqual(revision, board.View.Revision, "Closing windows never sends a game command.");
            }
        }

        // UI-94: Effects shows the card text in its own panel, so reading a source opens no sub-window.
        private static async Task ReadProtection(MatchTestRoom room, BoardScreen board)
        {
            if (!board.TableEffects.IsOpen)
                PointerGestureEvents.ClickAt(board.TableEffects.Tab);
            await room.Settle();
            PointerGestureEvents.ClickAt(board.TableEffects.Root.Q<Button>("effect-source-" + Protection));
            await room.Settle();
            EffectsNetworkTests.AssertCardInPanel(board, Protection);
            Assert.IsFalse(board.Family.Watching);
        }

        private static async Task EffectsReadTheCardWithoutASubWindow(MatchTestRoom room, BoardScreen board)
        {
            await ReadProtection(room, board);
            PointerGestureEvents.ClickAt(board.TableEffects.Root.Q<Button>("close-effects"));
            await room.Settle();
            Assert.IsFalse(board.TableEffects.IsOpen);
            Assert.AreEqual(DisplayStyle.None, board.Cards.Root.style.display.value);
            Assert.IsFalse(board.Family.Watching);
        }

        // A card preview opened from inside a window (the hand's choice between payments is the remaining
        // real path) is opened here directly from the Effects panel to keep the cascade under test.
        private static async Task PreviewClosesWithItsOwner(MatchTestRoom room, MountedMatchClient actor)
        {
            var board = actor.Board;
            await ReadProtection(room, board);
            board.Cards.Show(Protection, true, allowPlay: false, origin: board.TableEffects.Root.Q("effect-card"));
            await room.Settle();
            Assert.AreSame(board.TableEffects.Root, board.Family.OwnerOf(board.Cards.Root));
            Assert.IsNull(board.Cards.Root.Q<Button>("help-for-cards"), "UI-173: the preview has no Help link.");
            LayoutTests.Save(actor.Viewport, "ui89-family-open-" + board.View.You + ".png");
            board.TableEffects.Close();
            await room.Settle();
            Assert.AreEqual(DisplayStyle.None, board.Cards.Root.style.display.value, "The preview closes with its owner.");
            Assert.IsFalse(board.Family.Watching);
        }
    }
}
