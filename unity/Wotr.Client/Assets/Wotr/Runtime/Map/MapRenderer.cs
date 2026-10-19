using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MapRenderer : IDisposable
    {
        public VisualElement Root { get; } = new VisualElement { name = "map-geography", pickingMode = PickingMode.Ignore };
        public int BorderBuilds => borders.Sum(b => b.Builds);
        public int BorderCount => borders.Count;
        public int BorderRepaints => borders.Sum(b => b.Layer.Repaints) + (passages?.Repaints ?? 0);
        public int Repaints => layers.Sum(layer => layer.Repaints);
        public double LastUpdateMilliseconds { get; private set; }
        public double InitialLayerDrawMilliseconds => layers.Sum(layer => layer.LastRepaintMilliseconds);
        public int InputPoints { get; private set; }
        public Texture2D Terrain => terrain?.Texture;
        public Texture2D Margins => terrain?.Margins;
        private readonly List<MapBorderVisual> borders = new List<MapBorderVisual>();
        private readonly Dictionary<string, MapRegionVisual> regions = new Dictionary<string, MapRegionVisual>();
        private readonly List<MapVectorLayer> layers = new List<MapVectorLayer>();
        private MapGeometry geometry;
        private MapTerrainVisual terrain;
        private MapVectorLayer passages;
        private MapVectorLayer mordorTrack;
        private MapVectorLayer terrainLayer;
        private MapPoliticalView political;
        // UI-224: the pulsing edge glow of enterable (green) and attackable (red) regions, and its reused buffers.
        private MapTargetGlow targetGlow;
        private readonly List<string> enterable = new List<string>();
        private readonly List<string> attackable = new List<string>();
        public bool TargetsGlowing => targetGlow?.Glowing == true;
        public int TargetGlowRepaints => targetGlow?.Layer.Repaints ?? 0;
        private bool politicalActive;
        // UI-218: the illustration under the nations' colours is subdued, so the borders between them read clearly.
        public const float PoliticalTerrainOpacity = .55f;
        public bool PoliticalActive => politicalActive;
        public int PoliticalRepaints => political?.Layer.Repaints ?? 0;
        // UI-154: the ink of the Mordor track is fainter while the Fellowship is not on it.
        private const float InactiveTrackOpacity = .85f;
        // UI-157: the track's ink is a deep Mordor red on a paper underlay, so it reads over ash and differs from barriers.
        private static readonly Color TrackInk = new Color(.42f, .12f, .07f, .95f);
        private static readonly Color TrackUnderlay = new Color(.96f, .91f, .78f, .8f);

        public MapRenderer()
        {
            Root.usageHints = UsageHints.GroupTransform;
            Root.style.position = Position.Absolute;
            Root.style.left = Root.style.top = 0;
            Root.style.transformOrigin = new TransformOrigin(0, 0, 0);
        }

        public void Prepare(BoardPresentation data)
        {
            if (geometry == data.Geometry)
                return;
            Dispose();
            geometry = data.Geometry;
            var size = new Vector2(geometry.Width, geometry.Height);
            Root.style.width = size.x;
            Root.style.height = size.y;
            terrain = new MapTerrainVisual(data.Canvas);
            var bands = MapBandGeometry.Load(data.Borders);
            var byId = bands.Paths.ToDictionary(p => p.Id);
            // UI-102: water is painted into the canvas rasters, so there is no flat water layer.
            terrainLayer = new MapVectorLayer("map-terrain", size.x, size.y, context => terrain.Draw(context, size));
            Add(terrainLayer);
            // UI-218: the political view lies on the terrain, under every highlight, band and marker.
            political = new MapPoliticalView(geometry, size);
            Add(political.Layer);
            SetPoliticalView(politicalActive);
            foreach (var shape in geometry.Regions)
            {
                var region = new MapRegionVisual(shape, size);
                regions.Add(shape.Id, region);
                Add(region.Layer);
            }
            targetGlow = new MapTargetGlow(geometry, size);
            Add(targetGlow.Layer);
            AddPassages(data, bands, byId, size);
            passages = new MapVectorLayer("map-passages", size.x, size.y,
                context => DrawPassages(context.painter2D, data));
            Add(passages);
            Add(new MapVectorLayer("map-barriers", size.x, size.y, context => DrawBarriers(context.painter2D, data)));
            mordorTrack = new MapVectorLayer("map-mordor-track", size.x, size.y, context => DrawMordorTrack(context.painter2D, data));
            Add(mordorTrack);
            InputPoints = CountPoints(geometry, bands);
            UpdateMetadata(data);
        }

        private void AddPassages(BoardPresentation data, MapBandGeometry bands,
            Dictionary<string, MapBandPath> byId, Vector2 size)
        {
            foreach (var path in data.Borders.Catalog.Paths.Where(p => p.Parts.Any(part => MapBorderData.Passable(part.Kind))))
            {
                var border = new MapBorderVisual(data.Borders, path, byId[path.Id], bands.KeylineWidth, size);
                borders.Add(border);
                Add(border.Layer);
            }
        }

        private void Add(MapVectorLayer layer)
        {
            layers.Add(layer);
            Root.Add(layer);
        }

        public void UpdateMetadata(BoardPresentation data)
        {
            if (geometry == null)
                return;
            foreach (var border in borders)
                border.Update(data.Regions);
            political.Update(data);
        }

        // Switching the view only shows the layer and fades the terrain; it never repaints either.
        public void SetPoliticalView(bool active)
        {
            politicalActive = active;
            if (political == null)
                return;
            political.SetActive(active);
            terrainLayer.style.opacity = active ? PoliticalTerrainOpacity : 1;
        }

        public void Update(BoardPresentation data, CameraState camera, string selected,
            HashSet<string> sources, HashSet<string> owners, MapDraftOverlay draft, string hovered = "", string pressed = "")
        {
            var started = Stopwatch.GetTimestamp();
            Prepare(data);
            Root.style.translate = new Translate(camera.Pan.x, camera.Pan.y);
            Root.style.scale = new Scale(Vector3.one * camera.Zoom);
            enterable.Clear();
            attackable.Clear();
            foreach (var entry in regions)
            {
                var emphasis = Emphasis(entry.Key, selected, sources, owners, draft, data, hovered, pressed);
                entry.Value.Update(emphasis);
                if (draft?.AttackTargets.Contains(entry.Key) == true)
                    attackable.Add(entry.Key);
                else if (draft?.Targets.Contains(entry.Key) == true)
                    enterable.Add(entry.Key);
            }
            targetGlow.Update(enterable, attackable);
            LastUpdateMilliseconds = (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
        }

        public MapRegionEmphasis CurrentEmphasis(string id) =>
            regions.TryGetValue(id, out var region) ? region.Emphasis : MapRegionEmphasis.None;

        private static MapRegionEmphasis Emphasis(string id, string selected, HashSet<string> sources,
            HashSet<string> owners, MapDraftOverlay draft, BoardPresentation data, string hovered, string pressed)
        {
            if (id == pressed)
                return MapRegionEmphasis.Pressed;
            if (draft?.AttackTargets.Contains(id) == true)
                return MapRegionEmphasis.Attack;
            if (draft?.PlannedRegions.Contains(id) == true)
                return MapRegionEmphasis.Selected;
            if (draft?.Targets.Contains(id) == true)
                return MapRegionEmphasis.Target;
            if (id == selected)
                return MapRegionEmphasis.Selected;
            if (sources.Contains(id) || owners.Contains(id))
                return MapRegionEmphasis.Source;
            if (id == hovered)
                return MapRegionEmphasis.Hover;
            return data.ProtectedRegions.Contains(id) ? MapRegionEmphasis.Protection : MapRegionEmphasis.None;
        }

        private static void DrawPassages(Painter2D painter, BoardPresentation data)
        {
            foreach (var path in data.Borders.Catalog.Paths)
                foreach (var part in path.Parts)
                    if (MapBorderData.Passable(part.Kind))
                        MapVectorDrawing.Stroke(painter, data.Borders.Geometry[path.Id].Points, part,
                            data.Borders.Palette.Passage, data.Borders.Palette.Style.PassageWidth);
        }

        // UI-104: the board's frame (north, east and south edges and the north-east cut) is not inked, so the map's
        // paper runs into the canvas paper; coasts, lakes, rivers and barriers stay.
        public static bool Inked(MapBorderKind kind) => kind != MapBorderKind.Frame && !MapBorderData.Passable(kind);

        // Changing the track's state only fades the layer; it never repaints it.
        public void MordorTrackActive(bool active)
        {
            if (mordorTrack != null)
                mordorTrack.style.opacity = active ? 1 : InactiveTrackOpacity;
        }

        // UI-154/UI-157: the Mordor track's path and its two entrances: a paper underlay, then deep red ink. UI-283: the
        // path is drawn twice as wide, under stones twice as large.
        private static void DrawMordorTrack(Painter2D painter, BoardPresentation data)
        {
            var core = data.Borders.Palette.BarrierCoreWidth;
            var track = data.MordorTrack;
            var path = track.PathPoints.ToList();
            var entrances = track.Entrances.Select(entrance => entrance.Points.ToList()).ToList();
            Polyline(painter, path, TrackUnderlay, core * 5.2f);
            foreach (var entrance in entrances)
                Polyline(painter, entrance, TrackUnderlay, core * 2f);
            Polyline(painter, path, TrackInk, core * 2.6f);
            foreach (var entrance in entrances)
                Polyline(painter, entrance, TrackInk, core * .9f);
        }

        private static void Polyline(Painter2D painter, IReadOnlyList<Vector2> points, Color color, float width)
        {
            painter.strokeColor = color;
            painter.lineWidth = width;
            painter.lineCap = LineCap.Round;
            painter.lineJoin = LineJoin.Round;
            painter.BeginPath();
            painter.MoveTo(points[0]);
            for (var index = 1; index < points.Count; index++)
                painter.LineTo(points[index]);
            painter.Stroke();
        }

        // UI-143: all barrier shadows go down before any ink core, so crossing barriers do not shade each other's ink.
        private static void DrawBarriers(Painter2D painter, BoardPresentation data)
        {
            var palette = data.Borders.Palette;
            foreach (var (points, part) in InkedParts(data))
                if (part.Kind == MapBorderKind.Impassable)
                    MapVectorDrawing.Stroke(painter, points, part, palette.BarrierShade, palette.BarrierShadowWidth);
            foreach (var (points, part) in InkedParts(data))
            {
                var barrier = part.Kind == MapBorderKind.Impassable;
                MapVectorDrawing.Stroke(painter, points, part, barrier ? palette.BarrierInk : palette.Keyline,
                    barrier ? palette.BarrierCoreWidth : 1.15f);
            }
        }

        private static IEnumerable<(float[][] Points, MapBorderPart Part)> InkedParts(BoardPresentation data) =>
            data.Borders.Catalog.Paths.SelectMany(path => path.Parts.Where(part => Inked(part.Kind))
                .Select(part => (data.Borders.Geometry[path.Id].Points, part)));

        private static int CountPoints(MapGeometry source, MapBandGeometry bands)
        {
            var contour = source.Regions.Sum(r => r.Rings.Sum(ring => ring.Points.Length));
            var boundary = source.Boundaries.Sum(path => path.Points.Length);
            var water = bands.Water.Sum(ring => ring.Points.Length);
            var band = bands.Paths.SelectMany(p => p.Parts).Sum(p =>
                p.Positive.Concat(p.Negative).Concat(p.PositiveOutline).Concat(p.NegativeOutline)
                    .Sum(ring => ring.Points.Length));
            return contour + boundary + water + band;
        }

        public void Dispose()
        {
            Root.Clear();
            layers.Clear();
            borders.Clear();
            regions.Clear();
            geometry = null;
            passages = null;
            terrainLayer = null;
            political = null;
            targetGlow = null;
        }
    }
}
