using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-186: the names of the regions on the map in the board's lettering (Styles/MapNames.uss). Each name stands
    // at its point (MapRegionNamePoints), grows with the zoom up to a reading size, and is hidden while it would not
    // fit its region, lie under a force, a marker or a panel, or leave the view. It never takes the pointer. UI-406: a
    // settlement's gem grows with the map up to 1.4 times its UI-195 size, so a name printed just under (or over) its own
    // settlement steps clear of the stone. UI-426: the step may go down, up or sideways, the shortest that clears the stone
    // within the name's precomputed steps (MapNameSteps: its edge inside its region, its extent clear of the stand places);
    // where none does, the name hides rather than lie under its own stone.
    public sealed class MapRegionNames
    {
        public const string NameClass = "map-region-name";
        public const string MajorClass = "map-region-name-major";
        // Board units per letter height, and the screen sizes between which a name is drawn.
        private const float LetterHeight = 11;
        private const float MajorLetterHeight = 12.5f;
        private const float MinimumSize = 8;
        private const float ReadingSize = 15;
        // UI-406: the gap a name keeps from its own settlement's gem, in points.
        private const float GemGap = 1;
        // The width of a capital in Cinzel, in letter heights, for choosing one or two lines.
        private const float CapitalWidth = .72f;
        // The widest letter spacing and the line height of the names at their reading size (Styles/MapNames.uss).
        private const float LetterSpacing = 1.6f;
        private const float LineHeight = 1.25f;
        private static Dictionary<string, MapNamePoint> sharedPoints;
        private static Dictionary<string, MapNameSteps> sharedSteps;
        private readonly List<Entry> entries = new List<Entry>();
        // The last layout, repeated for a name once its text is measured.
        private CameraState camera;
        private Rect viewport;
        private IReadOnlyList<Rect> occupied;

        public VisualElement Root { get; } = new VisualElement { name = "map-region-names", pickingMode = PickingMode.Ignore };
        // UI-406: where a region's own settlement gem lies on the map now, or null where it has none.
        public System.Func<string, Rect?> Gem { get; set; }

        private sealed class Entry
        {
            public Label Label;
            public string Region;
            public MapNamePoint Point;
            public MapNameSteps Steps;
            public float LetterHeight;
            public bool Shown;
        }

        public MapRegionNames(BoardPresentation data)
        {
            Root.AddToClassList("map-region-names");
            Root.StretchToParentSize();
            var points = Points(data);
            var steps = Steps(data, points);
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
                steps.TryGetValue(region.Id, out var step);
                var entry = new Entry { Label = label, Region = region.Id, Point = point, Steps = step, LetterHeight = height };
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

        // UI-426: how far each settlement region's name may step from its point (found once, like the points).
        private static Dictionary<string, MapNameSteps> Steps(BoardPresentation data, Dictionary<string, MapNamePoint> points)
        {
            if (sharedSteps != null)
                return sharedSteps;
            var stands = MapStandSlots.Load(data);
            var steps = new Dictionary<string, MapNameSteps>();
            foreach (var region in data.Regions.Values)
                if (region.Settlement != null && points.TryGetValue(region.Id, out var point))
                    steps[region.Id] = StepsOf(region, data.Shapes[region.Id], point, stands);
            return sharedSteps = steps;
        }

        // The steps of a name's largest extent at its point, as its lines break there.
        public static MapNameSteps StepsOf(RegionDefinition region, RegionGeometry shape, MapNamePoint point, MapStandSlots stands)
        {
            var name = BoardRect(point, LinesHalfSize(region.Name, point, LetterHeightOf(region)));
            var near = Rect.MinMaxRect(name.xMin - 60, name.yMin - 60, name.xMax + 60, name.yMax + 60);
            return MapNameSteps.For(shape, point.Point, name, stands.NameObstacle(near));
        }

        // UI-426: half of a name's extent in board units up to its reading size, on the lines it breaks into at its point.
        public static Vector2 LinesHalfSize(string name, MapNamePoint point, float letterHeight)
        {
            var lines = Lines(name, point, letterHeight).Split('\n');
            var letter = (CapitalWidth * ReadingSize + LetterSpacing) * letterHeight / ReadingSize;
            return new Vector2(lines.Max(line => line.Length) * letter / 2, lines.Length * LineHeight * letterHeight / 2);
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
                Place(entry);
        }

        private void Measured(Entry entry)
        {
            if (camera != null)
                Place(entry);
        }

        private void Place(Entry entry)
        {
            var size = entry.LetterHeight * camera.Zoom;
            var measured = entry.Label.layout.size;
            var scale = Mathf.Min(size, ReadingSize) / ReadingSize;
            var drawn = measured * scale;
            var cleared = ClearOfGem(entry, new Rect(camera.ToScreen(entry.Point.Point) - drawn / 2, drawn));
            var bounds = cleared ?? default;
            var centre = bounds.center;
            var show = cleared.HasValue && size >= MinimumSize && measured.x > 0 &&
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

        // UI-406, UI-426: a name its own settlement's gem reaches into moves just clear of it (1 point) by the shortest of
        // the four steps — down below the stone, up above it, or beside it — that its precomputed steps allow: one that keeps
        // it clear of the stand places if there is one, otherwise one that keeps it inside its region (a stand standing there
        // then hides it, as at its point); null when none does, so the name hides. One rectangle test per shown name; the
        // rest is arithmetic.
        private Rect? ClearOfGem(Entry entry, Rect bounds)
        {
            if (!(Gem?.Invoke(entry.Region) is Rect gem) || !gem.Overlaps(bounds))
                return bounds;
            if (entry.Steps == null)
                return null;
            return Step(entry.Steps, gem, bounds, true) ?? Step(entry.Steps, gem, bounds, false);
        }

        // The shortest step clear of the stone that the steps allow, clear of the stand places or only inside the region.
        private Rect? Step(MapNameSteps steps, Rect gem, Rect bounds, bool clearOfPlaces)
        {
            Rect? best = null;
            for (var way = 0; way < MapNameSteps.Ways.Length; way++)
                Consider(ref best, bounds, Clearing(gem, bounds, MapNameSteps.Ways[way]),
                    steps.Allowed(way, camera.Zoom, bounds.size / 2, clearOfPlaces));
            return best;
        }

        // The step one way that leaves the name 1 point clear of the stone.
        private static Vector2 Clearing(Rect gem, Rect bounds, Vector2 way)
        {
            if (way.y > 0)
                return new Vector2(0, gem.yMax + GemGap - bounds.yMin);
            if (way.y < 0)
                return new Vector2(0, gem.yMin - GemGap - bounds.yMax);
            return way.x > 0 ? new Vector2(gem.xMax + GemGap - bounds.xMin, 0) : new Vector2(gem.xMin - GemGap - bounds.xMax, 0);
        }

        // Keeps the shorter of a step and the best so far, if the step is within what the name may take that way.
        private static void Consider(ref Rect? best, Rect bounds, Vector2 shift, float allowed)
        {
            var length = shift.magnitude;
            if (length > allowed || best is Rect current && Vector2.Distance(current.center, bounds.center) <= length)
                return;
            best = new Rect(bounds.position + shift, bounds.size);
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
