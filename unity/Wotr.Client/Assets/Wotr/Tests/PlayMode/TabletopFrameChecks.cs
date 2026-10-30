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
    // sharing at most a quarter of either; and no shown region name under a stand. UI-425: at both levels no force's count
    // (its token's badge) or plaque (its figures') lies under a stand or a king's marker drawn in front of it, and no
    // token, medallion or king's marker lies on a settlement's stone. UI-426: no shown name lies on its own stone.
    internal static class TabletopFrameChecks
    {
        // A base's top face is an ellipse 0.4 times as deep as it is wide (UI-422's footprints).
        private const float BaseDepth = .4f;
        // Drawn pictures are snapped to whole pixels at 125 %.
        private const float Pixel = 1f;
        private const float SiegeShare = .25f;
        // UI-425: where a label is read — along its middle line, as shares of its width — and where a stone is looked at.
        private static readonly float[] CountSamples = { .25f, .5f, .75f };
        private static readonly float[] PlaqueSamples = { .1f, .3f, .5f, .7f, .9f };
        private static readonly float[] StoneSamples = { .2f, .5f, .8f };
        // A character figure's column in design points (MapCharacterArtwork: the footprint's 16 units at 1.35 a unit).
        private const float CharacterColumn = 21.6f;

        public static void AssertStatic(MapCanvas map, bool near, string focus, string label)
        {
            Assert.AreEqual(near, map.MarkerVisibility.Detail, "The Detail level only zoomed in · " + label);
            MapStandLayoutTests.AssertOnPlaces(map, label);
            MapStandLayoutTests.AssertNearerInFront(map, label);
            AssertOnTheirRegions(map, label);
            AssertNamesClearOfStands(map, label);
            AssertReadable(map, label);
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

        // UI-425, UI-426: every label, stone and name the frame must keep readable, each finding listed.
        public static void AssertReadable(MapCanvas map, string label)
        {
            var findings = CoveredLabels(map).Concat(CoveredStones(map)).Concat(NamesOnTheirGems(map)).ToList();
            Assert.IsEmpty(findings, string.Join("; ", findings) + " · " + label);
        }

        // UI-426: a shown region name never lies on its own settlement's stone.
        public static void AssertNamesClearOfTheirGems(MapCanvas map, string label)
        {
            var findings = NamesOnTheirGems(map).ToList();
            Assert.IsEmpty(findings, string.Join("; ", findings) + " · " + label);
        }

        // UI-425: a force's label — its token's count badge, or its figures' plaque with the emblems and the count — never
        // lies under the picture of a stand or a king's marker drawn in front of it (later in the stand layer): no point
        // read along the label's middle line is inside that picture.
        private static IEnumerable<string> CoveredLabels(MapCanvas map)
        {
            var pictures = StandPictures(map);
            foreach (var shown in pictures.Where(picture => picture.Label.HasValue))
                foreach (var front in pictures.Where(picture => picture.Depth > shown.Depth))
                    if (LabelPoints(shown).Any(front.Covers))
                        yield return $"the {(shown.Token ? "count" : "plaque")} of {shown.Name} lies under {front.Name}";
        }

        // UI-425: no token, character's medallion or king's marker lies on a settlement's stone (crown and side band).
        private static IEnumerable<string> CoveredStones(MapCanvas map)
        {
            var stones = Stones(map);
            foreach (var token in StandPictures(map).Where(picture => picture.Token))
                foreach (var (region, stone) in stones)
                    if (StonePoints(stone).Any(token.Covers))
                        yield return $"{token.Name} lies on the settlement of {region}";
        }

        private static IEnumerable<string> NamesOnTheirGems(MapCanvas map)
        {
            foreach (var name in map.Names.Shown.Where(Visible))
            {
                var region = name.name.Substring("map-name-".Length);
                if (!(map.Settlements.Bounds(region) is Rect stone))
                    continue;
                var overlap = Intersection(name.worldBound, World(map, stone));
                if (overlap.width > Pixel && overlap.height > Pixel)
                    yield return $"the name {name.text.Replace('\n', ' ')} lies on its settlement ({overlap.width:0.0} × {overlap.height:0.0})";
            }
        }

        private static IEnumerable<Vector2> LabelPoints(StandPicture picture)
        {
            var bounds = picture.Label.Value;
            var samples = picture.Token ? CountSamples : PlaqueSamples;
            return samples.Select(share => new Vector2(bounds.xMin + bounds.width * share, bounds.center.y));
        }

        private static IEnumerable<Vector2> StonePoints(Rect stone) =>
            StoneSamples.SelectMany(x => StoneSamples.Select(y => new Vector2(stone.xMin + stone.width * x, stone.yMin + stone.height * y)));

        // Every settlement's stone as drawn now (world).
        private static List<(string Region, Rect Stone)> Stones(MapCanvas map) => map.Data.Regions.Keys
            .Select(region => (Region: region, Stone: map.Settlements.Bounds(region)))
            .Where(gem => gem.Stone.HasValue && Visible(map.Settlements.Find(gem.Region)))
            .Select(gem => (gem.Region, World(map, gem.Stone.Value))).ToList();

        // UI-425: what every shown army, character and king's marker draws (world), in the stand layer's order.
        private static List<StandPicture> StandPictures(MapCanvas map)
        {
            var layer = map.Stands.Layer;
            var pictures = new List<StandPicture>();
            for (var depth = 0; depth < layer.childCount; depth++)
                if (layer[depth].userData is MapStand stand && Visible(stand.Content) && Picture(stand.Content, stand, map) is { } picture)
                {
                    picture.Depth = depth;
                    pictures.Add(picture);
                }
            return pictures;
        }

        private static StandPicture Picture(VisualElement content, MapStand stand, MapCanvas map)
        {
            if (content.name.StartsWith("army-"))
                return ArmyPicture(content, stand, map);
            if (content.ClassListContains(MapCharacterArtwork.MiniatureClass))
                return CharacterPicture(content);
            if (content.ClassListContains(MapSovereignMarkers.ClassName))
                return new StandPicture(content.name, true).Round(content.Q("sovereign-puck"));
            return null;
        }

        // A token: its puck and its count badge, the badge its label; figures: the miniature's visible part (the figures
        // over the stand down to the plaque, MapArmyArtwork.Visible, as scripts/export_map_stands.py models it), the plaque
        // its label.
        private static StandPicture ArmyPicture(VisualElement army, MapStand stand, MapCanvas map)
        {
            var count = army.Q<Label>("army-count");
            if (army.ClassListContains(MapArmyArtwork.TokenClass))
            {
                var token = new StandPicture(army.name, true).Round(army.Q("army-puck")).Box(count);
                token.Label = Visible(count) && count.text.Length > 0 ? count.worldBound : (Rect?)null;
                return token;
            }
            var figures = new StandPicture(army.name, false).Box(World(map, stand.Bounds));
            var plaque = army.Q("army-plaque");
            figures.Label = Visible(plaque) ? plaque.worldBound : (Rect?)null;
            return figures;
        }

        // A medallion: the portrait's coin and its edge; figures: the stands and each figure's column — the footprint's
        // figure, 16 board units (21.6 design points) wide, which takes a press; a wide cell's sword or shield may reach
        // past it.
        private static StandPicture CharacterPicture(VisualElement character)
        {
            if (character.ClassListContains(MapCharacterArtwork.TokenClass))
                return new StandPicture(character.name, true).Round(character.Q("character-portrait")).Round(character.Q("character-token-edge"));
            var figures = new StandPicture(character.name, false).Round(character.Q("character-stand"))
                .Round(character.Q("character-behind-stand"));
            var unit = character.worldBound.width / MapCharacterArtwork.HolderSize.x;
            foreach (var figure in character.Query<Image>().ToList())
                if (!figure.ClassListContains("character-portrait") && Visible(figure))
                    figures.Box(Column(figure.worldBound, CharacterColumn * unit));
            return figures;
        }

        private static Rect Column(Rect figure, float width) =>
            new Rect(figure.center.x - width / 2, figure.yMin, width, figure.height);

        // UI-425: one object's drawn parts (ellipses and rectangles, world) and its label.
        private sealed class StandPicture
        {
            public readonly string Name;
            public readonly bool Token;
            public int Depth;
            public Rect? Label;
            private readonly List<(Rect Bounds, bool Round)> shapes = new List<(Rect, bool)>();

            public StandPicture(string name, bool token)
            {
                Name = name;
                Token = token;
            }

            public StandPicture Round(VisualElement part) => Add(part, true);

            public StandPicture Box(VisualElement part) => Add(part, false);

            public StandPicture Box(Rect bounds)
            {
                shapes.Add((bounds, false));
                return this;
            }

            public bool Covers(Vector2 point) =>
                shapes.Any(shape => shape.Round ? Inside(shape.Bounds, point) : shape.Bounds.Contains(point));

            private StandPicture Add(VisualElement part, bool round)
            {
                if (part != null && Visible(part) && part.worldBound.width > 0)
                    shapes.Add((part.worldBound, round));
                return this;
            }
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
