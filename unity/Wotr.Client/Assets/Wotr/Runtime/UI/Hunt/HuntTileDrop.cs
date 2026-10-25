using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-332 (owner 2026-10-25: "in this and analogous cases let the tile be presented — its artwork, without broader
    // explanations — and let it drop into the Hunt tab with an animation"): when the Hunt pool gains tiles in a new view — a
    // special tile put into play, the prepared tiles joining the pool at the Mordor entry, a tile returned — each gained
    // tile's face (at most three, side by side) shows large in the middle of the board, then drops, eased and shrinking, into
    // the console's Hunt plaque, which lights as it lands. Nothing takes the pointer or waits for it. Under Skip animations,
    // or on a quiet view, nothing is drawn: the view only becomes the new point of comparison.
    public sealed class HuntTileDrop
    {
        public const string TileClass = "hunt-tile-drop";
        public const int MostTiles = 3;
        public const double ShowSeconds = .25;
        public const double HoldSeconds = .55;
        public const double DropSeconds = .6;
        public const double StaggerSeconds = .15;
        private const float Size = 92;
        private const float Gap = 18;
        private const long LitMilliseconds = 450;
        private readonly Func<Rect?> middle;
        private readonly Func<VisualElement> plaque;
        private readonly Func<bool> reduced;
        private Dictionary<string, int> known;
        public VisualElement Root { get; } = new VisualElement { name = "hunt-tile-drops", pickingMode = PickingMode.Ignore };
        public int Shown => Root.Query(className: TileClass).ToList().Count;

        public HuntTileDrop(Func<Rect?> middle, Func<VisualElement> plaque, Func<bool> reduced)
        {
            this.middle = middle;
            this.plaque = plaque;
            this.reduced = reduced;
            Root.AddToClassList("hunt-tile-drops");
        }

        // A new view: the tiles the pool or the Mordor section gained since the last one drop into the Hunt plaque.
        public void Settle(HuntPoolView pool, bool quiet)
        {
            var counts = Counts(pool);
            var gained = known == null ? new List<HuntTileFaceView>() : Gains(known, pool);
            known = counts;
            if (quiet || gained.Count == 0 || reduced() || Root.panel == null)
                return;
            Play(gained.Take(MostTiles).ToList());
        }

        // Skip shows the latest state: nothing drops on after it.
        public void Clear() => Root.Clear();

        private void Play(IReadOnlyList<HuntTileFaceView> faces)
        {
            Root.BringToFront();
            var area = middle() ?? Root.worldBound;
            var width = faces.Count * Size + (faces.Count - 1) * Gap;
            for (var index = 0; index < faces.Count; index++)
            {
                var centre = new Vector2(area.center.x - width / 2 + Size / 2 + index * (Size + Gap), area.center.y);
                var tile = Tile(faces[index], Root.WorldToLocal(centre));
                Root.Add(tile);
                Animate(tile, index * StaggerSeconds);
            }
        }

        private static VisualElement Tile(HuntTileFaceView face, Vector2 centre)
        {
            var tile = new VisualElement { pickingMode = PickingMode.Ignore };
            tile.AddToClassList(TileClass);
            tile.style.left = centre.x - Size / 2;
            tile.style.top = centre.y - Size / 2;
            tile.style.width = Size;
            tile.style.height = Size;
            tile.style.opacity = 0;
            var art = new HuntTileFace("show");
            art.Apply(face);
            tile.Add(art.Root);
            return tile;
        }

        // It grows in, holds, then drops into the plaque where the plaque stands when it sets off; there it lands.
        private void Animate(VisualElement tile, double delay)
        {
            var start = Time.realtimeSinceStartupAsDouble + delay;
            Vector2? travel = null;
            var scale = 1f;
            IVisualElementScheduledItem tick = null;
            tick = tile.schedule.Execute(() =>
            {
                var time = Time.realtimeSinceStartupAsDouble - start;
                if (time < 0)
                    return;
                if (time < ShowSeconds)
                {
                    var shown = Eased(time / ShowSeconds);
                    tile.style.opacity = shown;
                    tile.style.scale = new Scale(Vector2.one * Mathf.Lerp(.6f, 1, shown));
                    return;
                }
                if (time < ShowSeconds + HoldSeconds)
                {
                    tile.style.opacity = 1;
                    tile.style.scale = new Scale(Vector2.one);
                    return;
                }
                var target = plaque();
                if (!travel.HasValue)
                {
                    travel = target.worldBound.center - tile.worldBound.center;
                    scale = Mathf.Clamp(target.worldBound.height / Size, .2f, 1);
                }
                var dropped = Eased((time - ShowSeconds - HoldSeconds) / DropSeconds);
                tile.style.translate = new Translate(travel.Value.x * dropped, travel.Value.y * dropped);
                tile.style.scale = new Scale(Vector2.one * Mathf.Lerp(1, scale, dropped));
                tile.style.opacity = Mathf.Lerp(1, .7f, dropped);
                if (dropped < 1)
                    return;
                tick.Pause();
                tile.RemoveFromHierarchy();
                Land(target);
            }).Every(16);
        }

        private static void Land(VisualElement target)
        {
            PresentationGlow.On(target);
            target.schedule.Execute(() => PresentationGlow.Off(target)).ExecuteLater(LitMilliseconds);
        }

        private static float Eased(double t)
        {
            var x = Mathf.Clamp01((float)t);
            return x < .5f ? 4 * x * x * x : 1 - Mathf.Pow(-2 * x + 2, 3) / 2;
        }

        // The pool's faces and the Mordor section's, apart: a prepared tile joining the pool is gained by the pool.
        private static Dictionary<string, int> Counts(HuntPoolView pool)
        {
            var counts = new Dictionary<string, int>();
            foreach (var (section, face) in Faces(pool))
                counts[section + Key(face)] = counts.TryGetValue(section + Key(face), out var count) ? count + face.Count : face.Count;
            return counts;
        }

        private static List<HuntTileFaceView> Gains(IReadOnlyDictionary<string, int> before, HuntPoolView pool)
        {
            var gained = new List<HuntTileFaceView>();
            foreach (var group in Faces(pool).GroupBy(pair => pair.Section + Key(pair.Face)))
            {
                var now = group.Sum(pair => pair.Face.Count);
                var earlier = before.TryGetValue(group.Key, out var count) ? count : 0;
                for (var extra = 0; extra < now - earlier; extra++)
                    gained.Add(group.First().Face);
            }
            return gained;
        }

        private static IEnumerable<(string Section, HuntTileFaceView Face)> Faces(HuntPoolView pool) =>
            (pool?.Tiles ?? new List<HuntTileFaceView>()).Select(face => ("pool|", face))
                .Concat((pool?.Prepared ?? new List<HuntTileFaceView>()).Select(face => ("mordor|", face)));

        private static string Key(HuntTileFaceView face) =>
            face.Source + ":" + face.Pool + ":" + face.Value + ":" + face.Reveal + ":" + face.Stop;
    }
}
