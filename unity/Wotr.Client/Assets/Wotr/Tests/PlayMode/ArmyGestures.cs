using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal static class ArmyGestures
    {
        public static void Die(MountedMatchClient client, string id, DieFace face = DieFace.Army)
        {
            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>(id));
        }

        public static Task All(MountedMatchClient client, string region, bool garrison = false)
        {
            return All(client.Board, region, garrison);
        }

        public static async Task All(BoardScreen board, string region, bool garrison = false)
        {
            board.Hover.Close();
            var marker = board.Map.Q<Button>("army-" + region + "-" + board.View.You + (garrison ? "-garrison" : ""));
            await MapContourProbe.ReachMarker(board.Map, marker, region);
            Assert.AreEqual(DisplayStyle.Flex, marker.resolvedStyle.display);
            // UI-127/UI-142: one click on an eligible army selects all of its figures that can move.
            PointerGestureEvents.ClickAt(marker);
        }

        public static void Click(VisualElement target, int count)
        {
            Assert.NotNull(target);
            using (var down = PointerDownEvent.GetPooled(new Event { type = EventType.MouseDown, button = 0,
                mousePosition = target.worldBound.center, clickCount = count }))
                target.SendEvent(down);
            using (var up = PointerUpEvent.GetPooled(new Event { type = EventType.MouseUp, button = 0,
                mousePosition = target.worldBound.center, clickCount = count }))
                target.SendEvent(up);
        }

        public static async Task Target(MatchTestRoom room, MountedMatchClient client, string region)
        {
            var revision = client.Board.View.Revision;
            await room.Until(() => client.Board.Map.DraftOverlay.Targets.Contains(region));
            client.Board.Hover.Close();
            await room.Settle();
            var map = client.Board.Map;
            MapContourProbe.ReachRegion(map, region);
            await room.Settle();
            var pan = map.Camera.Pan;
            var zoom = map.Camera.Zoom;
            MapContourProbe.Click(map, MapContourProbe.Terrain(map, region));
            await room.At(revision + 1);
            Assert.AreEqual(pan, map.Camera.Pan, "Resolving the army target must not navigate.");
            Assert.AreEqual(zoom, map.Camera.Zoom);
        }

        public static async Task Finish(MatchTestRoom room, MountedMatchClient client)
        {
            var revision = client.Board.View.Revision;
            await room.Until(() => client.Board.Console.Actions.Confirm.enabledInHierarchy);
            await room.Settle();
            PointerGestureEvents.ClickAt(client.Board.Console.Actions.Confirm);
            await room.At(revision + 1);
        }

        public static string ArmyDie(MountedMatchClient client) => client.Board.View.Dice
            .First(d => d.Owner == client.Board.View.You && d.Face == DieFace.Army && d.Location == DieLocation.Available).Id;
    }
}
