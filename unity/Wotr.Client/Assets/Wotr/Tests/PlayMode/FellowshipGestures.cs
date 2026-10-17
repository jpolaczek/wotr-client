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
            // UI-232: the Ring icon hides the Fellowship — the panel's own while it is open (it covers the plaque), else
            // the plaque's.
            if (name == "fellowship-status")
            {
                var fellowship = client.Board.Fellowship;
                var ring = new[] { fellowship.PanelRing, fellowship.Ring }.FirstOrDefault(Reachable) ??
                    (fellowship.IsOpen ? fellowship.PanelRing : fellowship.Ring);
                Assert.IsTrue(ring.ClassListContains(ChoiceGlow.ClassName), "The Ring glows while hiding is legal.");
                PointerGestureEvents.ClickAt(ring);
                return;
            }
            client.Board.Fellowship.Open();
            CardNetworkTests.Click(client.Board.Fellowship.Root.Q<Button>(name));
        }

        // UI-232: hiding is the Ring icon. Hovering the plaque unrolls the panel over it (UI-98), so the panel's own Ring
        // is the one a player clicks; the panel is opened first when it is rolled up.
        public static async Task Hide(MountedMatchClient client)
        {
            var fellowship = client.Board.Fellowship;
            if (!Reachable(fellowship.PanelRing))
            {
                fellowship.Open();
                await Task.Delay(120);
            }
            Action(client, "fellowship-status");
        }

        // UI-236: the figure on the map declares its position; the panel has no button for it. A figure outside the
        // view is brought into it first, as a player would pan to it.
        public static async Task Declare(MountedMatchClient client)
        {
            var map = client.Board.Map;
            var marker = map.FellowshipMarker;
            Assert.IsTrue(marker.DeclareOffered, "The figure offers the declaration.");
            Assert.IsTrue(marker.Root.ClassListContains(MapFellowshipMarker.DeclareClass));
            if (!Reachable(map, marker.Root))
            {
                var anchor = map.Data.Shapes[client.Board.View.Fellowship.RegionId].Anchor;
                map.FocusOn(new UnityEngine.Vector2(anchor[0], anchor[1]), map.Camera.Zoom);
                await Task.Delay(120);
            }
            Assert.AreEqual(DisplayStyle.Flex, marker.Root.style.display.value, "The figure is in view to declare.");
            Assert.IsTrue(Reachable(map, marker.Root), "The figure can be clicked: " + marker.Root.worldBound);
            CardNetworkTests.Click(marker.Root);
        }

        private static bool Reachable(MapCanvas map, VisualElement figure) => map.worldBound.Contains(figure.worldBound.center) && Reachable(figure);

        // Shown, laid out and on top at its centre, as a player's click would find it.
        private static bool Reachable(VisualElement element)
        {
            if (element.panel == null || element.resolvedStyle.display == DisplayStyle.None || element.worldBound.width <= 0)
                return false;
            var picked = element.panel.Pick(element.worldBound.center);
            return picked == element || element.Contains(picked);
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
