using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
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
            // UI-220: the status plaque hides the Fellowship; it sits in the corner tab, not in the panel.
            if (name == "fellowship-status")
            {
                CardNetworkTests.Click(client.Board.Fellowship.Status);
                return;
            }
            // UI-236: the figure on the map declares its position; the panel has no button for it.
            if (name == "fellowship-declare")
            {
                Declare(client);
                return;
            }
            client.Board.Fellowship.Open();
            CardNetworkTests.Click(client.Board.Fellowship.Root.Q<Button>(name));
        }

        public static void Declare(MountedMatchClient client)
        {
            var marker = client.Board.Map.FellowshipMarker;
            Assert.IsTrue(marker.DeclareOffered, "The figure offers the declaration.");
            Assert.IsTrue(marker.Root.ClassListContains(MapFellowshipMarker.DeclareClass));
            Assert.AreEqual(DisplayStyle.Flex, marker.Root.style.display.value, "The figure is in view to declare.");
            CardNetworkTests.Click(marker.Root);
        }

        // UI-236: the gate of the Mordor track enters; the view comes to the track first, as a player's would.
        public static async Task EnterMordor(MountedMatchClient client)
        {
            var map = client.Board.Map;
            map.FocusOn(map.Data.MordorTrack.Bounds.center, map.Camera.Zoom);
            await Task.Delay(120);
            var gate = map.MordorTrack.GateButton(client.Board.View.Fellowship.RegionId);
            Assert.IsTrue(gate.ClassListContains(MapMordorTrack.NextClass), "The Fellowship's gate glows.");
            CardNetworkTests.Click(gate);
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

        // UI-105: a character group moves on the click of its destination. UI-219: a Fellowship route the player draws
        // step by step moves on the step that uses all of its Progress; a shorter one is sent with ✓ once the server's
        // preview allows it.
        public static void FinishRoute(MountedMatchClient client, string region)
        {
            var overlay = client.Board.Map.DraftOverlay;
            // A route that already used all of its Progress has moved; a click then would reach the next decision.
            if (!overlay.IsRouteEnd(region) && overlay.Targets.Contains(region))
                MapClick(client, region, 1);
            var travel = client.Board.View?.Options.FirstOrDefault(o => FellowshipDraft.LocationRoute(o.Kind))?.Travel.FirstOrDefault();
            if (overlay.IsRouteEnd(region) && travel != null && overlay.RouteSteps < travel.Range)
                ConfirmWhenReady(client);
        }

        private static void ConfirmWhenReady(MountedMatchClient client)
        {
            var tray = client.Board.Tray;
            var clicked = false;
            tray.schedule.Execute(() =>
            {
                var confirm = tray.Q<Button>("confirm-decision");
                if (clicked || confirm == null || !confirm.enabledInHierarchy)
                    return;
                clicked = true;
                CardNetworkTests.Click(confirm);
            }).Every(16).Until(() => clicked);
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
