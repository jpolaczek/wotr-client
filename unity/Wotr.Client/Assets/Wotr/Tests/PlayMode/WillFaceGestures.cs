using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-461 (owner, 2026-11-03): the console's face menu turns the chosen Will of the West die into a result.
    internal static class WillFaceGestures
    {
        // Enter on the slot, so the gesture needs no layout pass after the die's click showed the menu.
        public static void Turn(MountedMatchClient client, DieFace face)
        {
            var faces = client.Board.Console.Status.Faces;
            Assert.IsTrue(faces.Shown, "The face menu stands while the chosen Will die waits for its use.");
            var slot = faces[face];
            Assert.IsTrue(slot.enabledInHierarchy, face + " must have a use now.");
            slot.Focus();
            using (var submit = NavigationSubmitEvent.GetPooled())
                slot.SendEvent(submit);
            slot.Blur();
        }

        // A click on the slot, as the player makes it (the menu is laid out).
        public static void Click(MountedMatchClient client, DieFace face) =>
            PointerGestureEvents.ClickAt(client.Board.Console.Status.Faces[face]);
    }
}
