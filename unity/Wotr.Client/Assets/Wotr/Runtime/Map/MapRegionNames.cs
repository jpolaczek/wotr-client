using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-186: the names of the regions on the map in the board's lettering (Styles/MapNames.uss). Each name stands
    // at its point (MapRegionNamePoints), grows with the zoom up to a reading size, and is hidden while it would not
    // fit its region, lie under a force, a marker or a panel, or leave the view. It never takes the pointer.
    public sealed class MapRegionNames
    {
        public const string NameClass = "map-region-name";
        public const string MajorClass = "map-region-name-major";
        // Board units per letter height, and the screen sizes between which a name is drawn.
        private const float LetterHeight = 11;
        private const float MajorLetterHeight = 12.5f;
        private const float MinimumSize = 8;
        private const float ReadingSize = 15;
        // The width of a capital in Cinzel, in letter heights, for choosing one or two lines.
        private const float CapitalWidth = .72f;
        // The widest letter spacing and the line height of the names at their reading size (Styles/MapNames.uss).
        private const float LetterSpacing = 1.6f;
        private const float LineHeight = 1.25f;
        private static Dictionary<string, MapNamePoint> sharedPoints;
        private readonly List<Entry> entries = new List<Entry>();
        // The last layout, repeated for a name once its text is measured.
        private CameraState camera;
        private Rect viewport;
        private IReadOnlyList<Rect> occupied;

        public VisualElement Root { get; } = new VisualElement { name = "map-region-names", pickingMode = PickingMode.Ignore };

        private sealed class Entry
        {
            public Label Label;
            public MapNamePoint Point;
            public float LetterHeight;
            public bool Shown;
        }

        public MapRegionNames(BoardPresentation data)
        {
            Root.AddToClassList("map-region-names");
            Root.StretchToParentSize();
            var points = Points(data);
            foreach (var region in data.Regions.Values.OrderBy(r => r.Id))
            {
                if (!points.TryGetValue(region.Id, out var point))
                    continue;
                var major = Major(region);
                var height = LetterHeightOf(region);
                var label = new Label(Lines(region.Name, point, height)) { name = "map-name-" + region.Id, pickingMode = PickingMode.Ignore };
                label.AddToClassList(NameClass);
                label.EnableInClassList(MajorClass, major);
                label.style.visibility = Visibility.Hidden;
                Root.Add(label);
                var entry = new Entry { Label = label, Point = point, LetterHeight = height };
                label.RegisterCallback<GeometryChangedEvent>(_ => Measured(entry));
                entries.Add(entry);
            }
        }

        public IEnumerable<Label> Shown => entries.Where(entry => entry.Shown).Select(entry => entry.Label);

        // The static geometry and places give the same points to every board, so they are searched once.
        private static Dictionary<string, MapNamePoint> Points(BoardPresentation data)
        {
            if (sharedPoints != null)
                return sharedPoints;
            var stands = MapStandSlots.Load(data);
            var points = new Dictionary<string, MapNamePoint>();
            foreach (var region in data.Regions.Values)
                if (data.Shapes.TryGetValue(region.Id, out var shape))
                    points[region.Id] = Point(region, shape, stands);
            return sharedPoints = points;
        }

        // UI-363, UI-422: a name stands clear of the stand places (their figures at the scale they show at, and the
        // fortresses) unless it could never be read there — wider or taller than its room even at the closest zoom;
        // then it takes the region's roomiest point, and only a force really standing over it hides it (the occupied
        // check of Place).
        public static MapNamePoint Point(RegionDefinition region, RegionGeometry shape, MapStandSlots stands)
        {
            var height = LetterHeightOf(region);
            var half = BoardHalfSize(region.Name, height);
            var bounds = MapRegionNamePoints.Bounds(shape);
            var near = Rect.MinMaxRect(bounds.xMin - half.x, bounds.yMin - half.y, bounds.xMax + half.x, bounds.yMax + half.y);
            var clear = MapRegionNamePoints.For(shape, half, stands.NameObstacle(near));
            if (Readable(region.Name, clear, height))
                return clear;
            var roomiest = MapRegionNamePoints.For(shape);
            return Readable(region.Name, roomiest, height) ? roomiest : clear;
        }

        // Board units per letter height: fortresses and cities are lettered larger.
        public static float LetterHeightOf(RegionDefinition region) => Major(region) ? MajorLetterHeight : LetterHeight;

        private static bool Major(RegionDefinition region) => region.Settlement is "Stronghold" or "City";

        // UI-422: half of a name's largest extent in board units. Up to its reading size a name keeps its size on the
        // board, so at its largest it is as wide as its single line and, with a space to break at, as tall as two lines.
        public static Vector2 BoardHalfSize(string name, float letterHeight)
        {
            var letter = (CapitalWidth * ReadingSize + LetterSpacing) * letterHeight / ReadingSize;
            var lines = name.IndexOf(' ') >= 0 ? 2 : 1;
            return new Vector2(name.Length * letter / 2, lines * LineHeight * letterHeight / 2);
        }

        // UI-422: everything a name at its point can cover, in board units: its largest extent within its room, since
        // a name wider or taller than its room is hidden (Place).
        public static Rect BoardRect(MapNamePoint point, Vector2 halfSize)
        {
            var half = Vector2.Min(halfSize, new Vector2(point.HalfWidth, point.HalfHeight));
            return new Rect(point.Point - half, half * 2);
        }

        // Whether the name at its reading size fits the point's room at the closest zoom.
        public static bool Readable(string name, MapNamePoint point, float letterHeight)
        {
            var lines = Lines(name, point, letterHeight).Split('\n');
            var width = lines.Max(line => line.Length) * (CapitalWidth * ReadingSize + LetterSpacing);
            var height = lines.Length * ReadingSize * LineHeight;
            return width <= point.HalfWidth * 2 * CameraState.ClosestZoom && height <= point.HalfHeight * 2 * CameraState.ClosestZoom;
        }

        // A name of several words that is wider than its region breaks into two lines at the middle space.
        public static string Lines(string name, MapNamePoint point, float letterHeight)
        {
            var width = name.Length * CapitalWidth * letterHeight;
            var spaces = Enumerable.Range(0, name.Length).Where(i => name[i] == ' ').ToList();
            if (width <= point.HalfWidth * 2 * .9f || spaces.Count == 0)
                return name;
            var middle = spaces.OrderBy(i => Mathf.Abs(i - name.Length / 2f)).First();
            return name.Substring(0, middle) + "\n" + name.Substring(middle + 1);
        }

        public void Layout(CameraState camera, Rect viewport, IReadOnlyList<Rect> occupied)
        {
            this.camera = camera;
            this.viewport = viewport;
            this.occupied = occupied;
            foreach (var entry in entries)
                Place(entry, camera, viewport, occupied);
        }

        private void Measured(Entry entry)
        {
            if (camera != null)
                Place(entry, camera, viewport, occupied);
        }

        private static void Place(Entry entry, CameraState camera, Rect viewport, IReadOnlyList<Rect> occupied)
        {
            var size = entry.LetterHeight * camera.Zoom;
            var measured = entry.Label.layout.size;
            var scale = Mathf.Min(size, ReadingSize) / ReadingSize;
            var drawn = measured * scale;
            var centre = camera.ToScreen(entry.Point.Point);
            var bounds = new Rect(centre - drawn / 2, drawn);
            var show = size >= MinimumSize && measured.x > 0 &&
                drawn.x <= entry.Point.HalfWidth * 2 * camera.Zoom && drawn.y <= entry.Point.HalfHeight * 2 * camera.Zoom &&
                viewport.Contains(bounds.min) && viewport.Contains(bounds.max) && !Covered(bounds, occupied);
            if (show)
            {
                entry.Label.transform.scale = new Vector3(scale, scale, 1);
                entry.Label.transform.position = new Vector3(centre.x - measured.x / 2, centre.y - measured.y / 2, 0);
            }
            if (show != entry.Shown)
            {
                entry.Shown = show;
                entry.Label.style.visibility = show ? Visibility.Visible : Visibility.Hidden;
            }
        }

        private static bool Covered(Rect bounds, IReadOnlyList<Rect> occupied)
        {
            for (var i = 0; i < occupied.Count; i++)
                if (occupied[i].Overlaps(bounds))
                    return true;
            return false;
        }
    }
}
