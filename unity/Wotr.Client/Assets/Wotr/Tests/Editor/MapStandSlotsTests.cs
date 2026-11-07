using System;
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
            // UI-513: placing the Fellowship before the field's recruits and characters left Narrows of the Forest and South
            // Anduin Vale room for every figure.
            "ash-mountains", "dol-guldur", "edoras", "gladden-fields", "high-pass", "lorien", "lossarnach", "minas-morgul",
            "moria", "old-ford", "old-forest-road", "rhosgobel", "southern-mirkwood", "western-mirkwood", "woodland-realm"
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

        // UI-522: a field force's or garrison's row stand at its place's reach stays inside its region, and its figures
        // (two thirds of an army's 40 units) cover no settlement, other fortress drawing or name.
        [Test]
        public void ARowStandAtItsReachStaysInItsRegionAndCoversNothing()
        {
            var gems = data.Regions.Values.Where(r => r.Settlement != null && data.Shapes.ContainsKey(r.Id))
                .Select(r => (r.Id, Rect: GemRect(data.Shapes[r.Id], r.Settlement))).ToList();
            var drawings = MapLandmarks.Load().Items.Select(l => (l.Id, l.Rect)).ToList();
            var names = data.Regions.Values.Where(r => data.Shapes.ContainsKey(r.Id)).Select(r => (r.Id, Rect: NameRect(r))).ToList();
            var problems = new List<string>();
            var wide = 0;
            foreach (var entry in stands.Regions)
                foreach (var (kind, feet, reach) in Rows(entry.Value))
                {
                    var where = entry.Key + " " + kind;
                    var round = StandFootprint.Army.BaseWidth / 2;
                    if (Mathf.Min(reach.x, reach.y) < round || Mathf.Max(reach.x, reach.y) > StandFootprint.RowReach)
                        problems.Add(where + " has no row reach");
                    if (reach.x + reach.y <= 2 * round)
                        continue;
                    wide++;
                    var centre = new Vector2(feet.x + (reach.y - reach.x) / 2, feet.y);
                    if (!BaseInside(data.Shapes[entry.Key], centre, new Vector2(reach.x + reach.y, StandFootprint.Army.BaseDepth) / 2))
                        problems.Add(where + "'s row leaves the region");
                    var row = Rect.MinMaxRect(feet.x - reach.x, feet.y - 27, feet.x + reach.y, feet.y + StandFootprint.Army.BaseDepth / 2);
                    problems.AddRange(gems.Where(g => row.Overlaps(g.Rect)).Select(g => where + " covers the settlement of " + g.Id));
                    problems.AddRange(drawings.Where(d => !(d.Id == entry.Key && kind == StandKind.Garrison) && row.Overlaps(d.Rect))
                        .Select(d => where + " covers the drawing of " + d.Id));
                    problems.AddRange(names.Where(n => row.Overlaps(n.Rect)).Select(n => where + " covers the name of " + n.Id));
                }
            TestContext.WriteLine("Row stands wider than a round base: " + wide);
            CollectionAssert.IsEmpty(problems);
        }

        private static IEnumerable<(StandKind Kind, Vector2 Feet, Vector2 Reach)> Rows(RegionStands stands)
        {
            yield return (StandKind.Field, stands.Field, stands.FieldReach);
            if (stands.Garrison is Vector2 gate)
                yield return (StandKind.Garrison, gate, stands.GarrisonReach);
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

        // UI-513 (owner, a picture of Dagorlad): the Fellowship stands well inside its region, nearer its middle than its
        // border, so where it is reads at a glance. Before, the median place stood 10 board units from the border and the
        // nearest 3.7.
        [Test]
        public void TheFellowshipStandsWellInsideItsRegion()
        {
            var distances = stands.Regions.Where(e => e.Value.FellowshipPoint != null && data.Shapes.ContainsKey(e.Key))
                .Select(e => (Region: e.Key, Distance: BorderDistance(data.Shapes[e.Key], e.Value.Fellowship)))
                .OrderBy(r => r.Distance).ToList();
            TestContext.WriteLine("Nearest the border: " + string.Join(", ", distances.Take(6).Select(r => $"{r.Region} {r.Distance:0.0}")));
            Assert.GreaterOrEqual(distances[0].Distance, 5f, distances[0].Region + " keeps its Fellowship off the border");
            Assert.GreaterOrEqual(distances[distances.Count / 2].Distance, 20f, "The median Fellowship stands well inside its region.");
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

        // UI-425: on the whole map of the worst case, where they are largest on the board, no token or medallion lies on a
        // settlement's stone (its crown, its setting and its side band as drawn there), nor does a king's marker there or
        // at the worst-case Detail zoom.
        [Test]
        public void NoTokenOrKingsMarkerLiesOnASettlement()
        {
            var covered = new List<string>();
            var whole = Stones(WholeMap);
            foreach (var (region, place, _) in Bases())
                covered.AddRange(whole.Where(s => Token(place).Any(shape => Touches(shape, s.Rect)))
                    .Select(s => $"{region} {place} lies on the settlement of {s.Id}"));
            foreach (var zoom in new[] { WholeMap, Worst })
            {
                var stones = Stones(zoom);
                foreach (var entry in stands.Regions)
                    covered.AddRange(stones.Where(s => Touches(King(entry.Value.Sovereign, zoom), s.Rect))
                        .Select(s => $"the king's marker of {entry.Key} lies on the settlement of {s.Id} at {zoom:0.00}"));
            }
            CollectionAssert.IsEmpty(covered);
        }

        // UI-425: no force's label lies under another stand of its region drawn in front of it — in a siege the other
        // side, and everywhere its own characters, the planned recruits and the Fellowship: its plaque under figures at the
        // scale they show at, its count under a token or a medallion on the whole map.
        [Test]
        public void NoForcesLabelLiesUnderAStandOfItsRegion()
        {
            var covered = new List<string>();
            foreach (var entry in stands.Regions)
            {
                var scale = entry.Value.ShownScale;
                foreach (var owner in entry.Value.Places.Where(Labelled))
                    foreach (var cover in entry.Value.Places.Where(p => !p.Equals(owner) && InFront(p.Feet, owner.Feet)))
                    {
                        if (FigurePicture(cover, scale).Overlaps(Plaque(owner, scale)))
                            covered.Add($"{entry.Key}: the plaque of {owner} lies under {cover}");
                        if (Token(cover).Any(shape => Touches(shape, Count(owner, WholeMap))))
                            covered.Add($"{entry.Key}: the count of {owner} lies under {cover}");
                    }
            }
            CollectionAssert.IsEmpty(covered);
        }

        // UI-425: a corrupted king's marker stands inside its region and, from the whole map to the closest zoom, lies on
        // no count or plaque of a force it stands in front of — its region's field, garrison and planned forces and the
        // forces around it.
        [Test]
        public void AKingsMarkerLiesOnNoForcesLabel()
        {
            var covered = new List<string>();
            var forces = stands.Regions.SelectMany(e => e.Value.Places.Where(p => Labelled(p) || p.Kind == StandKind.Planned)
                .Select(p => (Region: e.Key, Place: p))).ToList();
            foreach (var entry in stands.Regions)
            {
                var king = entry.Value.Sovereign;
                Assert.IsTrue(MapContourMath.Contains(data.Shapes[entry.Key], king), entry.Key + "'s king stands in it");
                foreach (var army in entry.Value.Places.Where(p => p.Footprint.BaseWidth == StandFootprint.Army.BaseWidth))
                    Assert.IsFalse(BasesOverlap((entry.Key, army, 1), (entry.Key, new StandPlace(StandKind.Character, 0, king), 1)),
                        $"{entry.Key}'s king stands off the base of its {army}");
                var beside = forces.Where(f => (f.Region == entry.Key || f.Place.Kind != StandKind.Planned) &&
                    InFront(king, f.Place.Feet) && Near(king, f.Place.Feet));
                foreach (var (region, force) in beside)
                    foreach (var zoom in KingZooms)
                        if (Touches(King(king, zoom), Count(force, zoom)) || zoom >= Worst && Touches(King(king, zoom), Plaque(force, At(zoom).Footprint)))
                            covered.Add($"the king's marker of {entry.Key} lies on the label of {region} {force} at {zoom:0.00}");
            }
            CollectionAssert.IsEmpty(covered);
        }

        private float Worst => MapStandSlots.WorstCaseZoom(data.Geometry);

        private float WholeMap => Worst / MapMarkerVisibility.TokensBelow;

        private float[] KingZooms => new[] { WholeMap, Worst, 1, 1.35f, 1.9f, CameraState.ClosestZoom };

        private MapStandScale At(float zoom) => new MapStandScale(zoom, Worst, MapArmyArtwork.DesignUnit);

        // A force whose count and plaque stay readable: the field force and the garrison.
        private static bool Labelled(StandPlace place) => place.Kind == StandKind.Field || place.Kind == StandKind.Garrison;

        // MapStand.CompareDepth: the lower feet, then the one further right, are drawn in front.
        private static bool InFront(Vector2 a, Vector2 b) => a.y > b.y || a.y == b.y && a.x > b.x;

        private static bool Near(Vector2 a, Vector2 b) => Mathf.Abs(a.x - b.x) < 120 && Mathf.Abs(a.y - b.y) < 120;

        // A force's figures down to its plaque (MapArmyArtwork.Visible); a character's (and the Fellowship's) column down to
        // its stand's front.
        private static Rect FigurePicture(StandPlace place, float scale)
        {
            if (place.Footprint.BaseWidth < StandFootprint.Army.BaseWidth)
                return Rect.MinMaxRect(place.Feet.x - 8 * scale, place.Feet.y - 36 * scale, place.Feet.x + 8 * scale, place.Feet.y + 6.3f * scale);
            var visible = MapArmyArtwork.Visible(false, true);
            var feet = MapArmyArtwork.Feet(false, true);
            var unit = scale / MapArmyArtwork.DesignUnit;
            return new Rect(place.Feet + (visible.position - feet) * unit, visible.size * unit);
        }

        // The plaque hanging at the stand's front (Styles/Armies.uss: 6 points in from each side, 2 above the box's foot,
        // 17 tall, in the 66 × 86 box with the feet at (33, 64)).
        private static Rect Plaque(StandPlace place, float scale)
        {
            var unit = scale / MapArmyArtwork.DesignUnit;
            return Rect.MinMaxRect(place.Feet.x - 27 * unit, place.Feet.y + 3 * unit, place.Feet.x + 27 * unit, place.Feet.y + 20 * unit);
        }

        // A token's two-digit count badge at its lower right (Styles/Armies.uss), at a zoom.
        private Rect Count(StandPlace place, float zoom) =>
            Part(place.Feet, MapArmyArtwork.Feet(place.Kind == StandKind.Garrison, false), Rect.MinMaxRect(22, 25, 44, 44),
                At(zoom).Token / zoom, false).Box;

        // What a place shows far out on the whole map: an army's puck and count, or a character's medallion; nothing for
        // the Fellowship, which has no token.
        private (Rect Box, bool Round)[] Token(StandPlace place)
        {
            var zoom = WholeMap;
            switch (place.Kind)
            {
                case StandKind.Fellowship:
                    return Array.Empty<(Rect, bool)>();
                case StandKind.Character:
                case StandKind.GarrisonCharacter:
                    return new[] { Part(place.Feet, MapCharacterArtwork.Feet(false), new Rect(0, 0, 24, 26.5f), At(zoom).CharacterToken / zoom, true) };
                default:
                    var feet = MapArmyArtwork.Feet(place.Kind == StandKind.Garrison, false);
                    var unit = At(zoom).Token / zoom;
                    return new[] { Part(place.Feet, feet, Rect.MinMaxRect(0, 2, 44, 37.2f), unit, true), (Count(place, zoom), false) };
            }
        }

        // A king's iron puck (Styles/Armies.uss: 30 × 24 points, 4 from the top) on its lower edge, at a zoom.
        private (Rect Box, bool Round) King(Vector2 place, float zoom) =>
            Part(place, MapSovereignMarkers.Feet, Rect.MinMaxRect(0, 4, 30, 28), At(zoom).Marker / zoom, true);

        private static (Rect Box, bool Round) Part(Vector2 place, Vector2 feet, Rect box, float unit, bool round) =>
            (new Rect(place + (box.position - feet) * unit, box.size * unit), round);

        // Whether a shape — an ellipse in its box, or the box — shares a point with a rectangle.
        private static bool Touches((Rect Box, bool Round) shape, Rect rect)
        {
            if (!shape.Box.Overlaps(rect))
                return false;
            if (!shape.Round)
                return true;
            var centre = shape.Box.center;
            var nearest = new Vector2(Mathf.Clamp(centre.x, rect.xMin, rect.xMax), Mathf.Clamp(centre.y, rect.yMin, rect.yMax));
            var offset = new Vector2((nearest.x - centre.x) / (shape.Box.width / 2), (nearest.y - centre.y) / (shape.Box.height / 2));
            return offset.sqrMagnitude < 1;
        }

        // Every settlement's stone as drawn at a zoom: its crown or setting at the gem scale, and the side band below it.
        private List<(string Id, Rect Rect)> Stones(float zoom)
        {
            var scale = At(zoom).Gem;
            return data.Regions.Values.Where(r => r.Settlement != null && data.Shapes.ContainsKey(r.Id)).Select(r =>
            {
                var gem = GemRect(data.Shapes[r.Id], r.Settlement);
                var anchor = new Vector2(data.Shapes[r.Id].Anchor[0], data.Shapes[r.Id].Anchor[1]);
                var factor = scale * Worst / zoom;
                var rect = Rect.MinMaxRect(anchor.x + (gem.xMin - anchor.x) * factor, anchor.y + (gem.yMin - anchor.y) * factor,
                    anchor.x + (gem.xMax - anchor.x) * factor, anchor.y + (gem.yMax - anchor.y) * factor + SideBand * scale / zoom);
                return (r.Id, rect);
            }).ToList();
        }

        // UI-406: the side band's greatest thickness below a stone, in points.
        private const float SideBand = 1.6f;

        // The ellipse lies inside the region when its centre does and no border segment comes into it: scaled to a unit
        // circle, every segment stays at least 1 from its centre.
        private static float BorderDistance(RegionGeometry shape, Vector2 point)
        {
            var best = float.MaxValue;
            foreach (var ring in shape.Rings)
                for (var i = 0; i < ring.Points.Length; i++)
                {
                    var a = new Vector2(ring.Points[i][0], ring.Points[i][1]);
                    var next = ring.Points[(i + 1) % ring.Points.Length];
                    var segment = new Vector2(next[0], next[1]) - a;
                    var along = segment.sqrMagnitude == 0 ? 0 : Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
                    best = Mathf.Min(best, Vector2.Distance(point, a + along * segment));
                }
            return best;
        }

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
