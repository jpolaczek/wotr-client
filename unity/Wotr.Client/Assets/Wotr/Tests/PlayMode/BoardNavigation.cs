using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    internal static class BoardNavigation
    {
        public static async Task Fellowship(BoardScreen board)
        {
            if (board.Fellowship.IsOpen)
                return;
            var marker = board.Map.FellowshipMarker.Root;
            await MapContourProbe.ReachMarker(board.Map, marker, board.View.Fellowship.RegionId);
            PointerGestureEvents.ClickAt(marker);
            await Task.Delay(80);
            Assert.IsTrue(board.Fellowship.IsOpen, "The public map marker opens Fellowship explicitly.");
        }

        public static async Task Victory(BoardScreen board, bool keyboard = false)
        {
            // UI-169: a finished game opens with its end screen; View board uncovers the board menu.
            if (board.GameEnd.IsOpen)
            {
                PointerGestureEvents.ClickAt(board.GameEnd.Root.Q<Button>("game-end-view-board"));
                await Task.Delay(80);
            }
            PointerGestureEvents.ClickAt(board.Menu.Toggle);
            await Task.Delay(80);
            var action = board.Menu.Panel.Q<Button>("board-menu-victory");
            Assert.IsNotNull(action);
            if (keyboard)
            {
                action.Focus();
                Assert.AreSame(action, board.Root.panel.focusController.focusedElement);
                Assert.IsFalse(board.Victory.IsOpen, "Focus alone does not open Victory.");
                using (var submit = NavigationSubmitEvent.GetPooled())
                    action.SendEvent(submit);
            }
            else
                PointerGestureEvents.ClickAt(action);
            await Task.Delay(80);
            Assert.IsFalse(board.Menu.IsOpen);
            Assert.IsTrue(board.Victory.IsOpen);
        }

        // UI-101: sovereign portraits are a section of the Characters panel; the player unrolls it from its tab.
        public static async Task OpenCharacters(BoardScreen board)
        {
            if (board.Characters.Root.resolvedStyle.display != DisplayStyle.None)
                return;
            PointerGestureEvents.ClickAt(board.Characters.Tab);
            await Task.Delay(260);
            Assert.AreNotEqual(DisplayStyle.None, board.Characters.Root.resolvedStyle.display, "The Characters tab unrolls its panel.");
        }

        public static async Task<Button> Sovereign(BoardScreen board, string id)
        {
            await OpenCharacters(board);
            var portrait = board.Sovereigns.Root.Q<Button>("sovereign-" + id);
            Assert.IsNotNull(portrait, id);
            Assert.IsTrue(board.Characters.Root.Contains(portrait), "The portrait stands in the Characters panel.");
            // UI-274: every sovereign keeps its fixed slot in every state.
            return portrait;
        }
    }
}
