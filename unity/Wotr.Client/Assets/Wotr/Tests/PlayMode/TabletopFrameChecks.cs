using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-407 (TABLETOP_FIGURES_UX, "Acceptance (UI-407)"): what one frame of the tabletop screen matrix must show — every
    // force and character standing on its own place in its own region, nearer stands in front, and at the figures' level
    // UI-422's limits kept: no figure on a settlement's stone, no two bases into each other, a garrison and its besieger
    // sharing at most a quarter of either; and no shown region name under a stand.
    internal static class TabletopFrameChecks
    {
        // A base's top face is an ellipse 0.4 times as deep as it is wide (UI-422's footprints).
        private const float BaseDepth = .4f;
        // Drawn pictures are snapped to whole pixels at 125 %.
        private const float Pixel = 1f;
        private const float SiegeShare = .25f;

        public static void AssertStatic(MapCanvas map, bool near, string focus, string label)
        {
            Assert.AreEqual(near, map.MarkerVisibility.Detail, "The Detail level only zoomed in · " + label);
            MapStandLayoutTests.AssertOnPlaces(map, label);
            MapStandLayoutTests.AssertNearerInFront(map, label);
            AssertOnTheirRegions(map, label);
            AssertNamesClearOfStands(map, label);
            if (!near)
                return;
            AssertFigures(map, focus, label);
            AssertGemsUncovered(map, label);
            AssertBasesApart(map, label);
            AssertSiegesShowBoth(map, label);
        }

        // Every shown army and character stands with its feet inside its own region's contour.
        public static void AssertOnTheirRegions(MapCanvas map, string label)
        {
            foreach (var (name, region, stand) in Shown(map))
            {
                var point = map.Camera.ToScreen(stand.Feet);
                if (map.contentRect.Contains(point))
                    Assert.AreEqual(region, map.HitTest(point), $"{name} stands in its own region · {label}");
            }
        }

        // UI-401: a name a stand covers hides; a shown name lies clear of every shown stand.
        public static void AssertNamesClearOfStands(MapCanvas map, string label)
        {
            var stands = Shown(map).Select(shown => (shown.Name, Bounds: World(map, shown.Stand.Bounds))).ToList();
            foreach (var name in map.Names.Shown.Where(Visible))
                foreach (var (stand, bounds) in stands)
                {
                    var overlap = Intersection(name.worldBound, bounds);
                    Assert.IsTrue(overlap.width <= Pixel || overlap.height <= Pixel,
                        $"The name {name.text} lies under {stand} ({overlap.width:0.0} × {overlap.height:0.0}) · {label}");
                }
        }

        // The focused region's forces stand as figures.
        private static void AssertFigures(MapCanvas map, string focus, string label)
        {
            foreach (var army in map.Data.Armies.Where(army => army.RegionId == focus))
            {
                var button = map.Q<Button>(MapStandLayoutTests.Name(army));
                Assert.IsTrue(button.ClassListContains(MapArmyArtwork.FiguresClass), $"{button.name} shows figures · {label}");
            }
        }

        // UI-422: no figure's picture covers a settlement's stone. The plaque may reach over the stone's crown; the middle
        // of the stone stays in view.
        public static void AssertGemsUncovered(MapCanvas map, string label)
        {
            var gems = map.Data.Regions.Keys.Select(region => (Region: region, Stone: map.Settlements.Bounds(region)))
                .Where(gem => gem.Stone.HasValue).ToList();
            foreach (var (name, _, stand) in Shown(map).Where(shown => Figures(shown.Stand)))
                foreach (var (region, stone) in gems)
                    Assert.IsFalse(stand.Bounds.Contains(stone.Value.center),
                        $"{name} covers the middle of {region}'s settlement · {label}");
        }

        // UI-422: bases stand 1.5 board units clear of each other; drawn, no two top faces run into each other.
        public static void AssertBasesApart(MapCanvas map, string label)
        {
            var bases = Shown(map).Where(shown => Figures(shown.Stand))
                .Select(shown => (shown.Name, Ellipse: Base(map, shown.Stand))).Where(shown => shown.Ellipse.HasValue).ToList();
            for (var i = 0; i < bases.Count; i++)
                for (var j = i + 1; j < bases.Count; j++)
                    Assert.IsFalse(Overlap(bases[i].Ellipse.Value, bases[j].Ellipse.Value),
                        $"The bases of {bases[i].Name} and {bases[j].Name} run into each other · {label}");
        }

        // UI-422: a besieger in the field and the garrison share at most a quarter of either's picture, so both read.
        public static void AssertSiegesShowBoth(MapCanvas map, string label)
        {
            foreach (var garrison in map.Data.Armies.Where(army => army.Garrison))
                foreach (var field in map.Data.Armies.Where(army => !army.Garrison && army.RegionId == garrison.RegionId))
                {
                    if (!(map.MarkerBounds(garrison.RegionId, garrison.Side, true) is Rect front) ||
                        !(map.MarkerBounds(field.RegionId, field.Side, false) is Rect back))
                        continue;
                    var shared = Area(Intersection(front, back));
                    Assert.LessOrEqual(shared, SiegeShare * Mathf.Min(Area(front), Area(back)),
                        $"The siege of {garrison.RegionId} shows both forces · {label}");
                }
        }

        // Readability beyond UI-422's limits, measured and reported rather than asserted: whose count badge, and which
        // settlement's stone, lies under another stand (a pick at its middle takes that stand's button).
        public static IEnumerable<string> Occlusions(MapCanvas map)
        {
            var layer = map.Stands.Layer;
            foreach (var (name, _, stand) in Shown(map))
                if (stand.Content.Q<Label>("army-count") is { } count && Visible(count) && count.text.Length > 0 &&
                    Covering(map, count.worldBound.center) is { } over && over != stand.Content &&
                    layer.IndexOf(over.parent) > layer.IndexOf(stand.Root))
                    yield return $"the count of {name} is under {over.name}";
            foreach (var region in map.Data.Regions.Keys)
                if (map.Settlements.Bounds(region) is Rect stone && Covering(map, map.LocalToWorld(stone.center)) is { } over)
                    yield return $"the settlement of {region} is under {over.name}";
        }

        // The army or character button a pick at a point takes (its hit shape: a base, the figures' columns, a token).
        private static Button Covering(MapCanvas map, Vector2 point)
        {
            for (var picked = map.panel.Pick(point); picked != null && picked != map; picked = picked.parent)
                if (picked is Button button && (button.name.StartsWith("army-") || button.name.StartsWith("character-")))
                    return button;
            return null;
        }

        // The board point every force and character stands on, by its stand's name: both seats must agree.
        public static Dictionary<string, Vector2> Places(MapCanvas map) =>
            Shown(map).ToDictionary(shown => shown.Name, shown => shown.Stand.Feet);

        public static void AssertSamePlaces(Dictionary<string, Vector2> free, Dictionary<string, Vector2> shadow, string label)
        {
            var shared = free.Keys.Intersect(shadow.Keys).ToList();
            Assert.IsNotEmpty(shared, label);
            foreach (var name in shared)
                Assert.Less(Vector2.Distance(free[name], shadow[name]), .01f, $"Both seats see {name} on one place · {label}");
        }

        // Every shown army and character: its stand's name, region and stand.
        private static IEnumerable<(string Name, string Region, MapStand Stand)> Shown(MapCanvas map)
        {
            foreach (var army in map.Data.Armies)
            {
                var button = map.Q<Button>(MapStandLayoutTests.Name(army));
                if (button != null && Visible(button) && button.parent?.userData is MapStand stand)
                    yield return (button.name, army.RegionId, stand);
            }
            foreach (var character in map.Data.Characters)
                if (map.CharacterMarker(character.Id) is { } marker && Visible(marker) && marker.parent?.userData is MapStand stand)
                    yield return (marker.name, character.RegionId, stand);
        }

        // An army at the figures' level, or a character's miniature (not its medallion).
        private static bool Figures(MapStand stand) => stand.Content.ClassListContains(MapArmyArtwork.FiguresClass) ||
            stand.Content.Q("character-stand") is { } face && Visible(face);

        // The top face of a stand's base (world): centred under its feet, as wide as the drawn base.
        private static Rect? Base(MapCanvas map, MapStand stand)
        {
            var face = stand.Content.Q("army-stand") ?? stand.Content.Q("character-stand");
            if (face == null || !Visible(face))
                return null;
            var feet = map.LocalToWorld(map.Camera.ToScreen(stand.Feet));
            var width = face.worldBound.width - 2 * Pixel;
            var depth = width * BaseDepth;
            return new Rect(feet.x - width / 2, feet.y - depth / 2, width, depth);
        }

        // Whether two ellipses (inscribed in their rectangles) share any point, sampled on a polar grid of the first.
        private static bool Overlap(Rect a, Rect b)
        {
            if (!a.Overlaps(b))
                return false;
            for (var ring = 0; ring <= 6; ring++)
                for (var step = 0; step < 36; step++)
                {
                    var angle = step * Mathf.PI / 18;
                    var point = a.center + Vector2.Scale(new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)), a.size / 2) * ring / 6f;
                    if (Inside(b, point))
                        return true;
                }
            return false;
        }

        private static bool Inside(Rect ellipse, Vector2 point)
        {
            var offset = point - ellipse.center;
            var x = offset.x / (ellipse.width / 2);
            var y = offset.y / (ellipse.height / 2);
            return x * x + y * y < 1;
        }

        private static bool Visible(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden ||
                    current.resolvedStyle.opacity <= 0)
                    return false;
            return !element.ClassListContains("lifted-away");
        }

        private static Rect World(MapCanvas map, Rect local)
        {
            var min = map.LocalToWorld(local.min);
            var max = map.LocalToWorld(local.max);
            return Rect.MinMaxRect(min.x, min.y, max.x, max.y);
        }

        internal static Rect Intersection(Rect a, Rect b) => Rect.MinMaxRect(Mathf.Max(a.xMin, b.xMin), Mathf.Max(a.yMin, b.yMin),
            Mathf.Max(Mathf.Max(a.xMin, b.xMin), Mathf.Min(a.xMax, b.xMax)),
            Mathf.Max(Mathf.Max(a.yMin, b.yMin), Mathf.Min(a.yMax, b.yMax)));

        private static float Area(Rect rect) => rect.width * rect.height;
    }
}
