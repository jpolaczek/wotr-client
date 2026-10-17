using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal static class FellowshipGestures
    {
        public static void Die(MountedMatchClient client)
        {
            var die = client.Board.View.Dice.First(d => d.Owner == client.Board.View.You && d.Location == DieLocation.Available &&
                d.Face is DieFace.Character or DieFace.WillOfTheWest);
            ArmyGestures.Die(client, die.Id, DieFace.Character);
        }

        public static void Action(MountedMatchClient client, string name)
        {
            client.Board.Fellowship.Open();
            CardNetworkTests.Click(client.Board.Fellowship.Root.Q<Button>(name));
        }

        public static void Portrait(MountedMatchClient client, string id)
        {
            if (client.Board.Tray.parent == client.Board.Hunt.DecisionHost)
            {
                client.Board.Hunt.Open();
                CardNetworkTests.Click(client.Board.Hunt.Root.Q<Button>("fellowship-portrait-" + id));
            }
            else
                Action(client, "fellowship-portrait-" + id);
        }

        public static void Figure(MountedMatchClient client, string region, string id)
        {
            client.Board.Hover.Open(region);
            CardNetworkTests.Click(client.Board.Hover.Panel.Root.Q<Button>("hover-figure-" + id));
        }

        // UI-105: a character group moves on the click of its destination; only a route the player draws step by
        // step (the Fellowship's declaration) ends with a second click on its last region.
        public static void FinishRoute(MountedMatchClient client, string region)
        {
            var drawn = client.Board.Map.DraftOverlay.IsRouteEnd(region);
            MapClick(client, region, 1);
            if (!drawn && client.Board.Map.DraftOverlay.IsRouteEnd(region))
                MapClick(client, region, 1);
        }

        public static void Region(MountedMatchClient client, string region) => MapClick(client, region, 1);

        // UI-105: one click on a highlighted destination. An event journey of several groups on one command only
        // records it on the map and waits for its shared confirmation.
        public static void Destination(MountedMatchClient client, string region) => MapClick(client, region, 1);

        private static void MapClick(MountedMatchClient client, string region, int count)
        {
            var map = client.Board.Map;
            var point = MapContourProbe.ReachRegion(map, region);
            var pan = map.Camera.Pan;
            var zoom = map.Camera.Zoom;
            MapContourProbe.Click(map, point, count);
            Assert.AreEqual(pan, map.Camera.Pan, "Choosing " + region + " must not navigate.");
            Assert.AreEqual(zoom, map.Camera.Zoom);
        }
    }
}
