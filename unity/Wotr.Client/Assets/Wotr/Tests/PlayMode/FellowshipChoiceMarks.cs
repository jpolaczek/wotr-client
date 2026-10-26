using System.Linq;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-298: a chosen companion is marked on the Fellowship roster, or in the tray when the roster does not show it;
    // the tray no longer repeats a roster portrait.
    internal static class FellowshipChoiceMarks
    {
        public static int Chosen(BoardScreen board) =>
            board.Tray.Query<Button>(className: "companion-portrait").ToList()
                .Concat(board.Fellowship.Root.Query<Button>().ToList().Where(b => b.name.StartsWith("fellowship-portrait-")))
                .Count(b => b.ClassListContains("selected"));
    }

    // UI-298: an interrupted response is recovered with the tray's "Check result", or, in the Fellowship panel, with the
    // console's glowing ✓ while the tray shows the pending message.
    internal static class PendingRecovery
    {
        public static Button Control(BoardScreen board)
        {
            var tray = board.Tray.Query<Button>().ToList().FirstOrDefault(b => b.text == "Check result");
            if (tray != null)
                return tray;
            return board.Tray.Q<Label>("pending-command-message") != null ? board.Console.Actions.Confirm : null;
        }
    }
}
