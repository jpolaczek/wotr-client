using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-272: The Lore of the Elven Rings on its objects: a lit die in the pool's row, then its new result in the
    // popover over it; the result's click sends the change. No tray button and no decision window take part.
    internal static class LoreGestures
    {
        public static async Task Die(MatchTestRoom room, MountedMatchClient client, string die)
        {
            var button = client.Board.OwnDice.Q<Button>(die);
            Assert.IsNotNull(button, die + " stands in the pool's row.");
            Assert.IsTrue(button.ClassListContains("resource-candidate"), die + " glows for Lore.");
            Assert.IsNull(client.Board.Tray.Q<Button>("effect-choice-" + die), "The die is chosen in the pool, not the tray.");
            PointerGestureEvents.ClickAt(button);
            await room.Settle();
            Assert.IsTrue(client.Board.LorePicker.IsOpen, "The die's new results open over it.");
        }

        public static Button Face(MountedMatchClient client, string id) => client.Board.LorePicker.Root.Q<Button>("ring-face-" + id);

        public static async Task Result(MatchTestRoom room, MountedMatchClient client, string id)
        {
            var face = Face(client, id);
            Assert.IsNotNull(face, id + " is a result of the chosen die.");
            StringAssert.StartsWith("Click to change the die to", face.tooltip);
            var revision = client.Board.View.Revision;
            CardNetworkTests.Click(face);
            await room.At(revision + 1);
        }
    }
}
