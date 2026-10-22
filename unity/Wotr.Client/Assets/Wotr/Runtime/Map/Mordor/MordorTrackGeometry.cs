using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-153: where the Mordor track lies on the map (Resources/MapCanvas/mordor-track.json, map units). The track
    // is not a region: its entrances leave Morannon and Minas Morgul, step 0 joins them on the ash plain, and
    // steps 1–5 climb round Orodruin to the Crack of Doom on its summit.
    public sealed class MordorTrackGeometry
    {
        public const string Resource = "MapCanvas/mordor-track";
        public const string Version = "mordor-track-1";
        public const int LastStep = 5;
        // Steps and gates keep this far from a settlement symbol, and steps this far from each other.
        // UI-283: the step stones are twice as large as at first, so they keep twice as far from settlement symbols and
        // from each other; the gates stay at their regions' edges.
        public const float SettlementClearance = 20;
        public const float StepClearance = 40;
        public const float MinimumSpacing = 40;
        private const float Tolerance = 4;
        private static readonly string[] EntranceRegions = { "morannon", "minas-morgul" };

        [JsonProperty("version")] public string FormatVersion { get; set; }
        [JsonProperty("entrances")] public List<Entrance> Entrances { get; set; }
        [JsonProperty("steps")] public List<float[]> Steps { get; set; }
        [JsonProperty("path")] public List<float[]> Path { get; set; }

        public sealed class Entrance
        {
            [JsonProperty("region")] public string Region { get; set; }
            [JsonProperty("path")] public List<float[]> Path { get; set; }
            public Vector2 Gate => Point(Path[0]);
            public IEnumerable<Vector2> Points => Path.Select(Point);
        }

        public Vector2 Step(int step) => Point(Steps[step]);
        public IEnumerable<Vector2> PathPoints => Path.Select(Point);

        // UI-154: the point a fractional step along the drawn path, so the Fellowship walks the path between steps.
        public Vector2 PointAt(float step)
        {
            var clamped = Mathf.Clamp(step, 0, LastStep);
            var from = Mathf.Min((int)clamped, LastStep - 1);
            var leg = LegPoints(from);
            var length = 0f;
            for (var i = 1; i < leg.Count; i++)
                length += Vector2.Distance(leg[i - 1], leg[i]);
            var remaining = (clamped - from) * length;
            for (var i = 1; i < leg.Count; i++)
            {
                var segment = Vector2.Distance(leg[i - 1], leg[i]);
                if (remaining <= segment && segment > 0)
                    return Vector2.Lerp(leg[i - 1], leg[i], remaining / segment);
                remaining -= segment;
            }
            return leg[leg.Count - 1];
        }

        // The path points from one step to the next.
        private List<Vector2> LegPoints(int from)
        {
            var path = PathPoints.ToList();
            var start = path.FindIndex(p => Vector2.Distance(p, Step(from)) <= .5f);
            var end = path.FindIndex(start, p => Vector2.Distance(p, Step(from + 1)) <= .5f);
            return path.GetRange(start, end - start + 1);
        }

        public Rect Bounds
        {
            get
            {
                var points = PathPoints.Concat(Entrances.SelectMany(e => e.Points)).ToList();
                return Rect.MinMaxRect(points.Min(p => p.x), points.Min(p => p.y), points.Max(p => p.x), points.Max(p => p.y));
            }
        }

        public static MordorTrackGeometry Load(MapGeometry geometry)
        {
            var track = JsonConvert.DeserializeObject<MordorTrackGeometry>(Resources.Load<TextAsset>(Resource).text);
            Validate(track, geometry);
            return track;
        }

        public static void Validate(MordorTrackGeometry track, MapGeometry geometry)
        {
            MapContourValidation.Require(track?.FormatVersion == Version, "Unsupported Mordor track.");
            MapContourValidation.Require(track.Steps?.Count == LastStep + 1 && track.Steps.All(IsPoint) &&
                track.Path?.Count >= track.Steps.Count && track.Path.All(IsPoint), "The Mordor track needs six steps on a path.");
            MapContourValidation.Require(track.Entrances?.Select(e => e.Region).OrderBy(r => r)
                .SequenceEqual(EntranceRegions.OrderBy(r => r)) == true, "The Mordor track enters from Morannon and Minas Morgul.");
            foreach (var entrance in track.Entrances)
                MapContourValidation.Require(entrance.Path?.Count >= 2 && entrance.Path.All(IsPoint) &&
                    Vector2.Distance(Point(entrance.Path[entrance.Path.Count - 1]), track.Step(0)) <= Tolerance,
                    "Each Mordor entrance leads to step 0.");
            RequireStepsOnPath(track);
            var map = new Rect(0, 0, geometry.Width, geometry.Height);
            MapContourValidation.Require(track.PathPoints.Concat(track.Entrances.SelectMany(e => e.Points)).All(map.Contains),
                "The Mordor track leaves the map.");
            for (var step = 1; step <= LastStep; step++)
                MapContourValidation.Require(Vector2.Distance(track.Step(step - 1), track.Step(step)) >= MinimumSpacing,
                    "Mordor steps overlap.");
            var anchors = geometry.Regions.Where(r => r.Anchor.Length == 2).Select(r => Point(r.Anchor)).ToList();
            var steps = Enumerable.Range(0, LastStep + 1).Select(track.Step);
            var gates = track.Entrances.Select(e => e.Gate);
            MapContourValidation.Require(steps.All(step => Clear(step, anchors, StepClearance)) &&
                gates.All(gate => Clear(gate, anchors, SettlementClearance)), "A Mordor step covers a settlement symbol.");
        }

        private static bool Clear(Vector2 mark, IEnumerable<Vector2> anchors, float clearance) =>
            anchors.All(anchor => Vector2.Distance(mark, anchor) >= clearance);

        // The path starts at step 0, ends at the Crack of Doom and passes every step in order.
        private static void RequireStepsOnPath(MordorTrackGeometry track)
        {
            var path = track.PathPoints.ToList();
            var index = 0;
            for (var step = 0; step <= LastStep; step++)
            {
                while (index < path.Count && Vector2.Distance(path[index], track.Step(step)) > .5f)
                    index++;
                MapContourValidation.Require(index < path.Count, "The Mordor path misses a step.");
            }
            MapContourValidation.Require(Vector2.Distance(path[0], track.Step(0)) <= .5f &&
                Vector2.Distance(path[path.Count - 1], track.Step(LastStep)) <= .5f, "The Mordor path runs from step 0 to the Crack of Doom.");
        }

        private static bool IsPoint(float[] value) => value?.Length == 2;

        private static Vector2 Point(float[] value) => new Vector2(value[0], value[1]);
    }
}
