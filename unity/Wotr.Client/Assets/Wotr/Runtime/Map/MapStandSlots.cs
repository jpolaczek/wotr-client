using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-422: what a stand takes up on the board, in board units at scale 1: a base ellipse on the ground and the
    // figures that rise from its centre. Scale 1 is the readable minimum at the worst case (MapStandSlots.WorstCaseZoom),
    // so no layout ever needs a larger footprint while figures show at the Detail level.
    public readonly struct StandFootprint
    {
        // Every base is seen at the same angle: its depth is this share of its width.
        public const float Depth = .4f;
        public static readonly StandFootprint Army = new StandFootprint(40, 40, 40);
        // UI-522: the widest row stand a field force or garrison may stand on (MapArmyArtwork.RowStandWidth at its design
        // unit), half of it on either side of the feet at most; each place records how far its own may reach on either
        // side (RegionStands.FieldRow, GarrisonRow). UI-523: 100 units, for figures at least 85 % of their stature.
        public const float ForceWidest = 100;
        public const float RowReach = ForceWidest / 2;
        public static readonly StandFootprint Character = new StandFootprint(14, 16, 36);
        public static readonly StandFootprint Fellowship = new StandFootprint(16, 18, 36);

        public readonly float BaseWidth;
        public readonly float BaseDepth;
        public readonly float FigureWidth;
        public readonly float FigureHeight;

        public StandFootprint(float baseWidth, float figureWidth, float figureHeight)
        {
            BaseWidth = baseWidth;
            BaseDepth = baseWidth * Depth;
            FigureWidth = figureWidth;
            FigureHeight = figureHeight;
        }

        // The base's semi-axes at a footprint scale.
        public Vector2 BaseRadii(float scale) => new Vector2(BaseWidth, BaseDepth) * (scale / 2);

        // From the top of the figures to the front of the base, as wide as the wider of the two.
        public Rect FigureRect(Vector2 feet, float scale)
        {
            var half = Mathf.Max(BaseWidth, FigureWidth) * scale / 2;
            var front = feet.y + BaseDepth * scale / 2;
            return Rect.MinMaxRect(feet.x - half, feet.y - FigureHeight * scale, feet.x + half, front);
        }
    }

    public enum StandKind
    {
        Garrison,
        GarrisonCharacter,
        Field,
        Character,
        Fellowship,
        Planned
    }

    // UI-422: one place of a region: what stands there and the point under its feet, in board units.
    public readonly struct StandPlace
    {
        public readonly StandKind Kind;
        public readonly int Index;
        public readonly Vector2 Feet;

        public StandPlace(StandKind kind, int index, Vector2 feet)
        {
            Kind = kind;
            Index = index;
            Feet = feet;
        }

        public StandFootprint Footprint => Kind switch
        {
            StandKind.Garrison or StandKind.Field or StandKind.Planned => StandFootprint.Army,
            StandKind.Fellowship => StandFootprint.Fellowship,
            _ => StandFootprint.Character
        };

        // A garrison and its characters stand at the gate of their fortress, in front of its drawing.
        public bool AtFortress => Kind == StandKind.Garrison || Kind == StandKind.GarrisonCharacter;

        public Rect FigureRect(float scale) => Footprint.FigureRect(Feet, scale);

        public override string ToString() =>
            Kind == StandKind.Character || Kind == StandKind.GarrisonCharacter ? Kind + "[" + Index + "]" : Kind.ToString();
    }

    // UI-422: the places of one region (Resources/MapCanvas/stand-slots.json). Its capacity is the largest footprint
    // scale at which the whole group fits; below 1 the region shows tokens at the worst case.
    public sealed class RegionStands
    {
        public const int CharacterPlaces = 6;
        public const int GarrisonCharacterPlaces = 3;
        // Clear of the largest settlement gem at the worst-case zoom (a Stronghold in its gold setting, about 12 board
        // units each way), with a margin.
        private const float SettlementClearance = 14;
        private const float Gap = 2;

        [JsonProperty("capacity")] public float Capacity { get; set; }
        // UI-522: how far (board units) the row stand of its field force and of its garrison may reach on either side of
        // their feet, [left, right], clear of everything fixed around them (scripts/export_map_stands.py, force_rooms);
        // the stands that come and go narrow it further (MapArmyRowRoom). A region the export does not cover keeps the
        // round base.
        [JsonProperty("fieldRow")] public float[] FieldRow { get; set; } = RoundRow();
        [JsonProperty("garrisonRow")] public float[] GarrisonRow { get; set; } = RoundRow();
        // A stronghold's drawing and its garrison's figures at the gate: names keep clear of it.
        [JsonProperty("fortress")] public float[] FortressBounds { get; set; }
        [JsonProperty("garrison")] public float[] GarrisonPoint { get; set; }
        [JsonProperty("garrisonCharacters")] public float[][] GarrisonCharacterPoints { get; set; }
        [JsonProperty("field")] public float[] FieldPoint { get; set; }
        [JsonProperty("characters")] public float[][] CharacterPoints { get; set; }
        [JsonProperty("fellowship")] public float[] FellowshipPoint { get; set; }
        [JsonProperty("planned")] public float[] PlannedPoint { get; set; }
        // UI-425: where a corrupted king's marker stands, beside the forces but on none of their labels and no gem.
        [JsonProperty("sovereign")] public float[] SovereignPoint { get; set; }
        private List<StandPlace> places;

        [JsonIgnore] public bool HasGarrison => GarrisonPoint != null;
        [JsonIgnore] public Vector2 FieldReach => Point(FieldRow);
        [JsonIgnore] public Vector2 GarrisonReach => Point(GarrisonRow);
        // The scale its figures show at in the worst case.
        [JsonIgnore] public float ShownScale => Mathf.Min(1, Capacity);
        [JsonIgnore] public Vector2 Field => Point(FieldPoint);
        [JsonIgnore] public Vector2 Fellowship => Point(FellowshipPoint);
        [JsonIgnore] public Vector2 Planned => Point(PlannedPoint);
        [JsonIgnore] public Vector2 Sovereign => Point(SovereignPoint);
        [JsonIgnore] public Vector2? Garrison => GarrisonPoint == null ? (Vector2?)null : Point(GarrisonPoint);

        [JsonIgnore]
        public Rect? Fortress => FortressBounds == null
            ? (Rect?)null
            : Rect.MinMaxRect(FortressBounds[0], FortressBounds[1], FortressBounds[2], FortressBounds[3]);

        // Every place of the region, the garrison group first.
        [JsonIgnore] public IReadOnlyList<StandPlace> Places => places ??= BuildPlaces();

        public Vector2 Character(int index) => Point(CharacterPoints[index]);

        public Vector2 GarrisonCharacter(int index) => Point(GarrisonCharacterPoints[index]);

        private List<StandPlace> BuildPlaces()
        {
            var result = new List<StandPlace>();
            if (HasGarrison)
            {
                result.Add(new StandPlace(StandKind.Garrison, 0, Point(GarrisonPoint)));
                for (var i = 0; i < GarrisonCharacterPoints.Length; i++)
                    result.Add(new StandPlace(StandKind.GarrisonCharacter, i, GarrisonCharacter(i)));
            }
            result.Add(new StandPlace(StandKind.Field, 0, Field));
            for (var i = 0; i < CharacterPoints.Length; i++)
                result.Add(new StandPlace(StandKind.Character, i, Character(i)));
            result.Add(new StandPlace(StandKind.Fellowship, 0, Fellowship));
            result.Add(new StandPlace(StandKind.Planned, 0, Planned));
            return result;
        }

        // UI-422: places beside the settlement, for a region the export does not cover: the field to its right with
        // the characters beyond (UI-425: and a king's marker beyond them), a garrison to its left with its characters
        // beyond, the Fellowship above it and the planned recruits below, all clear of the gem. Their capacity is 0, so the
        // region shows tokens.
        public static RegionStands Beside(Vector2 anchor)
        {
            var army = StandFootprint.Army;
            var stands = new RegionStands
            {
                Capacity = 0,
                FieldPoint = Coordinates(anchor + new Vector2(SettlementClearance + army.BaseWidth / 2, 0)),
                CharacterPoints = Row(anchor, 1, CharacterPlaces),
                FellowshipPoint = Coordinates(anchor - new Vector2(0, SettlementClearance + StandFootprint.Fellowship.BaseDepth / 2)),
                PlannedPoint = Coordinates(anchor + new Vector2(0, SettlementClearance + army.FigureHeight)),
                SovereignPoint = Row(anchor, 1, CharacterPlaces + 1)[CharacterPlaces]
            };
            stands.SetGarrisonBeside(anchor);
            return stands;
        }

        // UI-422: these places with a garrison beside the settlement, for a stronghold an expansion module makes of a
        // region the export gave no garrison place; capacity 0, so the region shows tokens.
        public RegionStands WithGarrisonBeside(Vector2 anchor)
        {
            var stands = new RegionStands
            {
                Capacity = 0,
                FortressBounds = FortressBounds,
                FieldPoint = FieldPoint,
                CharacterPoints = CharacterPoints,
                FellowshipPoint = FellowshipPoint,
                PlannedPoint = PlannedPoint,
                SovereignPoint = SovereignPoint
            };
            stands.SetGarrisonBeside(anchor);
            return stands;
        }

        private void SetGarrisonBeside(Vector2 anchor)
        {
            GarrisonPoint = Coordinates(anchor - new Vector2(SettlementClearance + StandFootprint.Army.BaseWidth / 2, 0));
            GarrisonCharacterPoints = Row(anchor, -1, GarrisonCharacterPlaces);
        }

        // Characters in a row on the settlement's line, beyond the army on one side of it.
        private static float[][] Row(Vector2 anchor, float side, int count)
        {
            var start = SettlementClearance + StandFootprint.Army.BaseWidth + Gap;
            var step = StandFootprint.Character.BaseWidth + Gap;
            return Enumerable.Range(0, count)
                .Select(i => Coordinates(anchor + new Vector2(side * (start + step * (i + .5f)), 0)))
                .ToArray();
        }

        private static Vector2 Point(float[] point) => new Vector2(point[0], point[1]);

        private static float[] RoundRow() => new[] { StandFootprint.Army.BaseWidth / 2, StandFootprint.Army.BaseWidth / 2 };

        private static float[] Coordinates(Vector2 point) => new[] { point.x, point.y };
    }

    // UI-422: the fixed stand places of every region, in board units (Resources/MapCanvas/stand-slots.json, exported by
    // scripts/export_map_stands.py with the hashes of its sources). Loaded once and shared; a stale or incomplete file
    // is rejected like the render bands (MapBandGeometry.Validate). Region names keep clear of the places
    // (NameObstacle, MapRegionNamePoints.For).
    public sealed class MapStandSlots
    {
        public const string Resource = "MapCanvas/stand-slots";
        public const string Version = "stand-slots-3";
        // The worst case the footprints are sized for: 1366×768 at 125 % is a 1092.8×614.4 point map.
        public static readonly Vector2 WorstCaseView = new Vector2(1366 / 1.25f, 768 / 1.25f);
        private const float ZoomTolerance = 1e-4f;
        private const float SizeTolerance = 1e-3f;
        private static MapStandSlots shared;

        [JsonProperty("version")] public string FormatVersion { get; set; }
        [JsonProperty("sourceHashes")] public Dictionary<string, string> SourceHashes { get; set; } = new Dictionary<string, string>();
        [JsonProperty("worstCaseZoom")] public float RecordedWorstCaseZoom { get; set; }
        [JsonProperty("footprints")]
        public Dictionary<string, FootprintRecord> Footprints { get; set; } = new Dictionary<string, FootprintRecord>();
        [JsonProperty("regions")] public Dictionary<string, RegionStands> Regions { get; set; } = new Dictionary<string, RegionStands>();
        private readonly Dictionary<string, RegionStands> fallbacks = new Dictionary<string, RegionStands>();
        private List<Rect> nameObstacles;

        public sealed class FootprintRecord
        {
            [JsonProperty("base")] public float[] Base { get; set; }
            [JsonProperty("figure")] public float[] Figure { get; set; }
        }

        // The lowest zoom figures show at on the worst-case layout: MapMarkerVisibility.TokensBelow times its fit zoom,
        // 0.80 points per board unit on the base map.
        public static float WorstCaseZoom(MapGeometry geometry) => MapMarkerVisibility.TokensBelow *
            CameraState.CoverZoom(new Rect(0, 0, geometry.Width, geometry.Height), new Rect(Vector2.zero, WorstCaseView));

        public static MapStandSlots Load(BoardPresentation data)
        {
            if (shared != null)
                return shared;
            var slots = JsonConvert.DeserializeObject<MapStandSlots>(Resources.Load<TextAsset>(Resource).text);
            Validate(slots, data.Board, data.Geometry);
            shared = slots;
            return slots;
        }

        public static void Validate(MapStandSlots data, BoardData board, MapGeometry geometry)
        {
            MapContourValidation.Require(data != null && data.FormatVersion == Version && data.SourceHashes != null &&
                data.Regions != null && data.Footprints != null, "Unsupported map stand places.");
            RequireSource(data, "map-geometry.json", ResourceHash("map-geometry"));
            RequireSource(data, "MapCanvas/landmarks.json", ResourceHash(MapLandmarks.Resource));
            RequireSource(data, "base-board.json#regions", RegionsDigest(board));
            MapContourValidation.Require(Mathf.Abs(data.RecordedWorstCaseZoom - WorstCaseZoom(geometry)) < ZoomTolerance,
                "Stale map stand places: worst-case zoom");
            RequireFootprint(data, "army", StandFootprint.Army);
            RequireFootprint(data, "character", StandFootprint.Character);
            RequireFootprint(data, "fellowship", StandFootprint.Fellowship);
            var strongholds = board.Regions.Where(r => r.Settlement == "Stronghold").Select(r => r.Id).ToHashSet();
            MapContourValidation.Require(data.Regions.Count == geometry.Regions.Count, "Incomplete map stand places.");
            foreach (var shape in geometry.Regions)
                RequireRegion(data, shape.Id, strongholds.Contains(shape.Id));
        }

        // UI-422: a region's places for its current definition. A region the export does not cover, or a stronghold an
        // expansion module makes of a region without a garrison place, stands beside its settlement and shows tokens.
        public RegionStands For(RegionGeometry shape, RegionDefinition current)
        {
            Regions.TryGetValue(shape.Id, out var stands);
            if (stands != null && (stands.HasGarrison || current?.Settlement != "Stronghold"))
                return stands;
            if (!fallbacks.TryGetValue(shape.Id, out var fallback))
            {
                var anchor = new Vector2(shape.Anchor[0], shape.Anchor[1]);
                fallback = stands == null ? RegionStands.Beside(anchor) : stands.WithGarrisonBeside(anchor);
                fallbacks[shape.Id] = fallback;
            }
            return fallback;
        }

        // UI-422: whether a region name in this rectangle (board units) would lie under a stand's figures, at the scale
        // its region shows them at, or on a stronghold's fortress. Only the obstacles near the given bounds are kept.
        public Func<Rect, bool> NameObstacle(Rect near)
        {
            var rects = NameObstacles().Where(rect => rect.Overlaps(near)).ToArray();
            return name =>
            {
                foreach (var rect in rects)
                    if (rect.Overlaps(name))
                        return true;
                return false;
            };
        }

        private List<Rect> NameObstacles()
        {
            if (nameObstacles != null)
                return nameObstacles;
            var rects = new List<Rect>();
            foreach (var stands in Regions.Values)
            {
                if (stands.Fortress is Rect fortress)
                    rects.Add(fortress);
                rects.AddRange(stands.Places.Select(place => place.FigureRect(stands.ShownScale)));
            }
            return nameObstacles = rects;
        }

        // The regions' ids, names and settlements, the part of base-board.json the places depend on, digested as the
        // export digests it.
        public static string RegionsDigest(BoardData board)
        {
            var lines = board.Regions.OrderBy(r => r.Id, StringComparer.Ordinal)
                .Select(r => r.Id + "|" + r.Name + "|" + (r.Settlement ?? ""));
            return Hash(Encoding.UTF8.GetBytes(string.Join("\n", lines)));
        }

        private static void RequireSource(MapStandSlots data, string name, string hash) =>
            MapContourValidation.Require(data.SourceHashes.TryGetValue(name, out var expected) && expected == hash,
                "Stale map stand places: " + name);

        private static void RequireFootprint(MapStandSlots data, string name, StandFootprint footprint)
        {
            var valid = data.Footprints.TryGetValue(name, out var record) && record.Base?.Length == 2 && record.Figure?.Length == 2 &&
                Near(record.Base[0], footprint.BaseWidth) && Near(record.Base[1], footprint.BaseDepth) &&
                Near(record.Figure[0], footprint.FigureWidth) && Near(record.Figure[1], footprint.FigureHeight);
            MapContourValidation.Require(valid, "Stale map stand places: footprint " + name);
        }

        private static bool Near(float recorded, float expected) => Mathf.Abs(recorded - expected) < SizeTolerance;

        private static void RequireRegion(MapStandSlots data, string id, bool stronghold)
        {
            var complete = data.Regions.TryGetValue(id, out var stands) && stands != null && stands.Capacity > 0 &&
                Row(stands.FieldRow) && Row(stands.GarrisonRow) &&
                MapContourValidation.ValidPoint(stands.FieldPoint) && MapContourValidation.ValidPoint(stands.FellowshipPoint) &&
                MapContourValidation.ValidPoint(stands.PlannedPoint) && MapContourValidation.ValidPoint(stands.SovereignPoint) &&
                ValidPoints(stands.CharacterPoints, RegionStands.CharacterPlaces);
            MapContourValidation.Require(complete, "Incomplete map stand places: " + id);
            var garrison = MapContourValidation.ValidPoint(stands.GarrisonPoint) && stands.FortressBounds?.Length == 4 &&
                ValidPoints(stands.GarrisonCharacterPoints, RegionStands.GarrisonCharacterPlaces);
            var none = stands.GarrisonPoint == null && stands.GarrisonCharacterPoints == null && stands.FortressBounds == null;
            MapContourValidation.Require(stronghold ? garrison : none, "Map stand places disagree with the strongholds: " + id);
        }

        private static bool Row(float[] reach) => reach?.Length == 2 &&
            reach.All(side => side >= StandFootprint.Army.BaseWidth / 2 && side <= StandFootprint.RowReach);

        private static bool ValidPoints(float[][] points, int count) =>
            points != null && points.Length == count && points.All(MapContourValidation.ValidPoint);

        private static string ResourceHash(string name) => Hash(Resources.Load<TextAsset>(name).bytes);

        private static string Hash(byte[] bytes)
        {
            using var sha = SHA256.Create();
            return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }
    }
}
