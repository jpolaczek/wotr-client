using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

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
                var major = region.Settlement is "Stronghold" or "City";
                var height = major ? MajorLetterHeight : LetterHeight;
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

        // The static geometry gives the same points to every board, so they are searched once.
        private static Dictionary<string, MapNamePoint> Points(BoardPresentation data)
        {
            if (sharedPoints != null)
                return sharedPoints;
            var points = new Dictionary<string, MapNamePoint>();
            foreach (var region in data.Regions.Values)
                if (data.Shapes.TryGetValue(region.Id, out var shape))
                    points[region.Id] = MapRegionNamePoints.For(shape, region.Settlement != null);
            return sharedPoints = points;
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
