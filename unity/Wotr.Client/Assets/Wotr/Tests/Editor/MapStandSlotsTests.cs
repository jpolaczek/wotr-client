using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-422: every region has fixed stand places. At the worst-case footprint (1366×768 at 125 %, at the lowest zoom
    // figures show at) they lie inside their own contours, no two bases overlap, and no figures cover a settlement gem,
    // a fortress drawing or a region name; a garrison stands at its fortress's gate. Checked here with MapContourMath,
    // apart from scripts/export_map_stands.py, which lays the places out.
    public sealed class MapStandSlotsTests
    {
        // The tightest regions: their whole group fits only below scale 1, so at the worst case they show tokens, and
        // figures once the zoom shrinks the footprint to their capacity (TABLETOP_FIGURES_UX, "Stand places").
        private static readonly string[] TokenRegions =
        {
            "ash-mountains", "dol-guldur", "edoras", "gladden-fields", "high-pass", "lossarnach", "minas-morgul", "moria",
            "old-ford", "old-forest-road", "rhosgobel", "south-anduin-vale", "southern-mirkwood", "western-mirkwood",
            "woodland-realm"
        };
        private const float LowestCapacity = .5f;

        private BoardPresentation data;
        private MapStandSlots stands;

        [SetUp]
        public void Load()
        {
            data = BoardPresentation.Load();
            stands = MapStandSlots.Load(data);
        }

        [Test]
        public void EveryRegionHasItsPlaces()
        {
            Assert.AreEqual(105, data.Shapes.Count);
            foreach (var shape in data.Shapes.Values)
            {
                var region = stands.Regions[shape.Id];
                var stronghold = data.Regions[shape.Id].Settlement == "Stronghold";
                var places = region.Places;
                Assert.AreEqual(1, places.Count(p => p.Kind == StandKind.Field), shape.Id);
                Assert.AreEqual(RegionStands.CharacterPlaces, places.Count(p => p.Kind == StandKind.Character), shape.Id);
                Assert.AreEqual(1, places.Count(p => p.Kind == StandKind.Fellowship), shape.Id);
                Assert.AreEqual(1, places.Count(p => p.Kind == StandKind.Planned), shape.Id);
                Assert.AreEqual(stronghold ? 1 : 0, places.Count(p => p.Kind == StandKind.Garrison), shape.Id);
                Assert.AreEqual(stronghold ? RegionStands.GarrisonCharacterPlaces : 0,
                    places.Count(p => p.Kind == StandKind.GarrisonCharacter), shape.Id);
                Assert.AreEqual(stronghold, region.Fortress.HasValue, shape.Id + " keeps its names clear of its fortress");
            }
        }

        // The footprints are the readable minimum at the worst case: figures show down to 1.2 × the fit zoom of a
        // 1092.8×614.4 point map, 0.80 points per board unit, where an army's base of 40 units is 32 points wide.
        [Test]
        public void TheFootprintsAreSizedForTheWorstCaseLayout()
        {
            var zoom = MapStandSlots.WorstCaseZoom(data.Geometry);
            Assert.AreEqual(1.2f * 1092.8f / 1632f, zoom, 1e-4f);
            Assert.AreEqual(zoom, stands.RecordedWorstCaseZoom, 1e-4f);
            Assert.AreEqual(32, StandFootprint.Army.BaseWidth * zoom, .5f);
            foreach (var footprint in new[] { StandFootprint.Army, StandFootprint.Character, StandFootprint.Fellowship })
                Assert.AreEqual(StandFootprint.Depth, footprint.BaseDepth / footprint.BaseWidth, 1e-5f, "Every base is seen at one angle.");
            Assert.Greater(StandFootprint.Army.BaseWidth, StandFootprint.Fellowship.BaseWidth);
            Assert.Greater(StandFootprint.Fellowship.BaseWidth, StandFootprint.Character.BaseWidth);
        }

        [Test]
        public void EveryBaseLiesInsideItsOwnContourAtTheWorstCaseFootprint()
        {
            var outside = Bases().Where(b => !BaseInside(data.Shapes[b.Region], b.Place.Feet, b.Place.Footprint.BaseRadii(b.Scale)))
                .Select(b => b.Region + " " + b.Place);
            CollectionAssert.IsEmpty(outside, "Every base stands inside its own region.");
        }

        [Test]
        public void NoTwoBasesOverlapWithinOrAcrossRegions()
        {
            var bases = Bases();
            var overlapping = new List<string>();
            for (var i = 0; i < bases.Count; i++)
                for (var j = i + 1; j < bases.Count; j++)
                    if (BasesOverlap(bases[i], bases[j]))
                        overlapping.Add(bases[i].Region + " " + bases[i].Place + " / " + bases[j].Region + " " + bases[j].Place);
            CollectionAssert.IsEmpty(overlapping);
        }

        [Test]
        public void FiguresCoverNoSettlementDrawingOrName()
        {
            var gems = data.Regions.Values.Where(r => r.Settlement != null && data.Shapes.ContainsKey(r.Id))
                .Select(r => (r.Id, Rect: GemRect(data.Shapes[r.Id], r.Settlement))).ToList();
            var drawings = MapLandmarks.Load().Items.Select(l => (l.Id, l.Rect)).ToList();
            var names = data.Regions.Values.Where(r => data.Shapes.ContainsKey(r.Id)).Select(r => (r.Id, Rect: NameRect(r))).ToList();
            var covered = new List<string>();
            foreach (var stand in Bases())
            {
                var figures = stand.Place.FigureRect(stand.Scale);
                var where = stand.Region + " " + stand.Place;
                covered.AddRange(gems.Where(g => figures.Overlaps(g.Rect)).Select(g => where + " covers the settlement of " + g.Id));
                covered.AddRange(drawings.Where(d => !(d.Id == stand.Region && stand.Place.AtFortress) && figures.Overlaps(d.Rect))
                    .Select(d => where + " covers the drawing of " + d.Id));
                covered.AddRange(names.Where(n => figures.Overlaps(n.Rect)).Select(n => where + " covers the name of " + n.Id));
            }
            CollectionAssert.IsEmpty(covered);
        }

        [Test]
        public void AGarrisonStandsAtItsFortressGate()
        {
            foreach (var landmark in MapLandmarks.Load().Items)
            {
                var garrison = stands.Regions[landmark.Id].Garrison;
                Assert.IsTrue(garrison.HasValue, landmark.Id + " has a garrison place");
                Assert.IsTrue(landmark.Rect.Contains(garrison.Value), landmark.Id + "'s garrison stands on its drawing");
                var fortress = stands.Regions[landmark.Id].Fortress.Value;
                Assert.IsTrue(fortress.Contains(landmark.Rect.min) && fortress.Contains(landmark.Rect.max - Vector2.one * 1e-3f),
                    landmark.Id + "'s names keep clear of its whole drawing");
            }
        }

        // A siege (Helm's Deep in the stress fixture) shows both armies: the besieger's figures and the garrison's share
        // at most a quarter of either rectangle.
        [Test]
        public void ABesiegerAndItsGarrisonBothStandInSight()
        {
            foreach (var entry in stands.Regions.Where(e => e.Value.HasGarrison))
            {
                var scale = entry.Value.ShownScale;
                var garrison = entry.Value.Places.Single(p => p.Kind == StandKind.Garrison).FigureRect(scale);
                var field = entry.Value.Places.Single(p => p.Kind == StandKind.Field).FigureRect(scale);
                var shared = Rect.MinMaxRect(Mathf.Max(garrison.xMin, field.xMin), Mathf.Max(garrison.yMin, field.yMin),
                    Mathf.Min(garrison.xMax, field.xMax), Mathf.Min(garrison.yMax, field.yMax));
                var area = shared.width > 0 && shared.height > 0 ? shared.width * shared.height : 0;
                Assert.LessOrEqual(area, .25f * Mathf.Min(garrison.width * garrison.height, field.width * field.height) + 1e-3f,
                    entry.Key + ": the besieger and the garrison stand in sight of each other");
            }
        }

        // The tight regions are named, so a change to the board or the footprints that crowds another region shows here.
        [Test]
        public void OnlyTheTightRegionsShowTokensAtTheWorstCase()
        {
            var tokens = stands.Regions.Where(e => e.Value.Capacity < 1).Select(e => e.Key).OrderBy(id => id);
            TestContext.WriteLine("Capacities below 1: " + string.Join(", ", stands.Regions.Where(e => e.Value.Capacity < 1)
                .Select(e => e.Key + " " + e.Value.Capacity)));
            CollectionAssert.AreEquivalent(TokenRegions, tokens);
            foreach (var entry in stands.Regions)
                Assert.GreaterOrEqual(entry.Value.Capacity, LowestCapacity, entry.Key + " shows figures once zoomed in");
        }

        // An expansion module's region override always gets a place: a stronghold made of a region without a garrison
        // place stands beside its settlement, and so does a region the places do not know; both show tokens.
        [Test]
        public void ARegionOverrideAlwaysGetsAPlace()
        {
            var shape = data.Shapes["dale"];
            var printed = stands.For(shape, data.Regions["dale"]);
            Assert.AreSame(stands.Regions["dale"], printed);
            Assert.IsFalse(printed.HasGarrison);
            var stronghold = stands.For(shape, new RegionDefinition { Id = "dale", Name = "Dale", Settlement = "Stronghold" });
            Assert.IsTrue(stronghold.HasGarrison, "A new stronghold gets a garrison place");
            Assert.AreEqual(0, stronghold.Capacity, "It shows tokens");
            Assert.AreEqual(printed.Field, stronghold.Field, "Its other places stay");
            var anchor = new Vector2(shape.Anchor[0], shape.Anchor[1]);
            Assert.AreEqual(anchor.y, stronghold.Garrison.Value.y, 1e-4f, "The garrison stands beside the settlement");
            Assert.AreSame(stronghold, stands.For(shape, new RegionDefinition { Id = "dale", Settlement = "Stronghold" }), "It is made once");
            var nowhere = new RegionGeometry { Id = "nowhere", Anchor = new[] { 100f, 100f } };
            var unknown = stands.For(nowhere, null);
            Assert.AreEqual(0, unknown.Capacity);
            Assert.AreEqual(RegionStands.CharacterPlaces + RegionStands.GarrisonCharacterPlaces + 4, unknown.Places.Count);
            var gem = GemRect(nowhere, "Stronghold");
            Assert.IsFalse(unknown.Places.Any(p => p.FigureRect(1).Overlaps(gem)), "Beside the settlement means clear of its gem");
            var bases = unknown.Places.Select(p => ("nowhere", p, 1f)).ToList();
            Assert.IsFalse(bases.Any(a => bases.Any(b => !a.Equals(b) && BasesOverlap(a, b))), "The places beside it do not overlap");
        }

        [Test]
        public void StalePlacesAreRejected()
        {
            var text = Resources.Load<TextAsset>(MapStandSlots.Resource).text;
            MapStandSlots.Validate(JsonConvert.DeserializeObject<MapStandSlots>(text), data.Board, data.Geometry);
            var stale = JsonConvert.DeserializeObject<MapStandSlots>(text);
            stale.SourceHashes["map-geometry.json"] = new string('0', 64);
            Assert.Throws<InvalidDataException>(() => MapStandSlots.Validate(stale, data.Board, data.Geometry));
            var renamed = JsonConvert.DeserializeObject<BoardData>(Resources.Load<TextAsset>("base-board").text);
            renamed.Regions[0].Name += " Renamed";
            Assert.Throws<InvalidDataException>(() => MapStandSlots.Validate(JsonConvert.DeserializeObject<MapStandSlots>(text),
                renamed, data.Geometry), "A renamed region needs its names placed again");
            var missing = JsonConvert.DeserializeObject<MapStandSlots>(text);
            missing.Regions.Remove("andrast");
            Assert.Throws<InvalidDataException>(() => MapStandSlots.Validate(missing, data.Board, data.Geometry));
        }

        // The ellipse lies inside the region when its centre does and no border segment comes into it: scaled to a unit
        // circle, every segment stays at least 1 from its centre.
        private static bool BaseInside(RegionGeometry shape, Vector2 centre, Vector2 radii)
        {
            if (!MapContourMath.Contains(shape, centre))
                return false;
            foreach (var ring in shape.Rings)
                for (var i = 1; i < ring.Points.Length; i++)
                {
                    var a = Scaled(ring.Points[i - 1], centre, radii);
                    var b = Scaled(ring.Points[i], centre, radii);
                    if (DistanceToOrigin(a, b) < 1)
                        return false;
                }
            return true;
        }

        private static Vector2 Scaled(float[] point, Vector2 centre, Vector2 radii) =>
            new Vector2((point[0] - centre.x) / radii.x, (point[1] - centre.y) / radii.y);

        private static float DistanceToOrigin(Vector2 a, Vector2 b)
        {
            var segment = b - a;
            var length = segment.sqrMagnitude;
            var t = length > 0 ? Mathf.Clamp01(-Vector2.Dot(a, segment) / length) : 0;
            return (a + segment * t).magnitude;
        }

        // Every place of every region, at the scale its region shows figures at in the worst case.
        private List<(string Region, StandPlace Place, float Scale)> Bases() => stands.Regions
            .SelectMany(e => e.Value.Places.Select(p => (e.Key, p, e.Value.ShownScale)))
            .ToList();

        // Bases of one aspect are circles once their depth is stretched to their width.
        private static bool BasesOverlap((string Region, StandPlace Place, float Scale) a, (string Region, StandPlace Place, float Scale) b)
        {
            var offset = b.Place.Feet - a.Place.Feet;
            var distance = new Vector2(offset.x, offset.y / StandFootprint.Depth).magnitude;
            return distance < a.Place.Footprint.BaseRadii(a.Scale).x + b.Place.Footprint.BaseRadii(b.Scale).x;
        }

        // The gem as drawn at the worst-case zoom, its gold setting included, in board units.
        private Rect GemRect(RegionGeometry shape, string settlement)
        {
            var gem = SettlementGem.For(settlement, null);
            var outline = gem.Setting ? gem.SettingOutline(Vector2.zero) : gem.Outline(Vector2.zero);
            var zoom = MapStandSlots.WorstCaseZoom(data.Geometry);
            var anchor = new Vector2(shape.Anchor[0], shape.Anchor[1]);
            var min = new Vector2(outline.Min(p => p.x), outline.Min(p => p.y)) / zoom;
            var max = new Vector2(outline.Max(p => p.x), outline.Max(p => p.y)) / zoom;
            return Rect.MinMaxRect(anchor.x + min.x, anchor.y + min.y, anchor.x + max.x, anchor.y + max.y);
        }

        private Rect NameRect(RegionDefinition region)
        {
            var point = MapRegionNames.Point(region, data.Shapes[region.Id], stands);
            return MapRegionNames.BoardRect(point, MapRegionNames.BoardHalfSize(region.Name, MapRegionNames.LetterHeightOf(region)));
        }
    }
}
