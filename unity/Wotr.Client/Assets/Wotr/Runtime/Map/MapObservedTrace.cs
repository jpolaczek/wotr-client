using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-306 (OPPONENT_ACTIONS_UX §5): the observed trace on the map — a 2 pt pale-blue ring around a force's marker or a
    // settlement, a region's outline and a small delta badge. A set of them appears together (200 ms fade), follows the
    // camera and the markers, and fades out together (400 ms); none takes the pointer, a tooltip or a cursor, so the objects
    // under it keep their own inspection. It is never a legal target, a choice glow, a selection or a ghost.
    public sealed class MapObservedTrace
    {
        public const long FadeOutMilliseconds = 400;
        private const float RingGap = 3;
        private const float SettlementRing = 15;
        private const float PortraitGap = 6;
        private readonly MapCanvas map;
        private readonly List<TraceSet> sets = new List<TraceSet>();
        private readonly IVisualElementScheduledItem layout;
        public VisualElement Root { get; } = new VisualElement { name = "map-observed-trace", pickingMode = PickingMode.Ignore };
        // UI-309: the plates — badges, deltas, portraits and identities — stand above every marker (markers join the map
        // as they are created, after the rings' layer), so a neighbouring stack never covers a name.
        public VisualElement Plates { get; } = new VisualElement { name = "map-observed-plates", pickingMode = PickingMode.Ignore };
        public int Count => sets.Count;

        public MapObservedTrace(MapCanvas map)
        {
            this.map = map;
            foreach (var layer in new[] { Root, Plates })
            {
                layer.AddToClassList("map-observed-trace");
                layer.StretchToParentSize();
                map.Add(layer);
            }
            layout = Root.schedule.Execute(Layout).Every(16);
            layout.Pause();
        }

        public TraceSet Begin()
        {
            var set = new TraceSet(this);
            sets.Add(set);
            Root.Add(set.Root);
            Plates.Add(set.Plates);
            Plates.BringToFront();
            layout.Resume();
            return set;
        }

        public void Clear()
        {
            foreach (var set in sets.ToList())
                set.End();
        }

        private void Ended(TraceSet set)
        {
            sets.Remove(set);
            if (sets.Count == 0)
                layout.Pause();
        }

        private void Layout()
        {
            if (Plates.parent == map && map.IndexOf(Plates) != map.childCount - 1)
                Plates.BringToFront();
            foreach (var set in sets)
                set.Layout();
        }

        private Vector2 Anchor(string region)
        {
            var anchor = map.Data.Shapes[region].Anchor;
            return map.Camera.ToScreen(new Vector2(anchor[0], anchor[1]));
        }

        public sealed class TraceSet
        {
            private readonly MapObservedTrace owner;
            private readonly List<(VisualElement Element, string Region, Side Side, bool Garrison)> forces =
                new List<(VisualElement, string, Side, bool)>();
            private readonly List<(VisualElement Element, string Region)> settlements = new List<(VisualElement, string)>();
            private readonly List<(VisualElement Element, string Region)> badges = new List<(VisualElement, string)>();
            private readonly List<(VisualElement Element, string Region, Side Side, bool Garrison)> deltas =
                new List<(VisualElement, string, Side, bool)>();
            private readonly List<string> regions = new List<string>();
            private readonly List<(VisualElement Element, string Region, Side Side, bool Garrison, Rect Safe)> portraits =
                new List<(VisualElement, string, Side, bool, Rect)>();
            private readonly List<(VisualElement Element, string Region)> fellowships = new List<(VisualElement, string)>();
            private bool ended;
            public VisualElement Root { get; } = new VisualElement { pickingMode = PickingMode.Ignore };
            public VisualElement Plates { get; } = new VisualElement { pickingMode = PickingMode.Ignore };
            public IReadOnlyList<string> Regions => regions;

            internal TraceSet(MapObservedTrace owner)
            {
                this.owner = owner;
                foreach (var layer in new[] { Root, Plates })
                {
                    layer.AddToClassList("observed-set");
                    layer.StretchToParentSize();
                }
                Root.generateVisualContent += DrawOutlines;
                // The next frame, so the fade-in has a start.
                Root.schedule.Execute(() => Shown(true));
            }

            private void Shown(bool shown)
            {
                Root.EnableInClassList("shown", shown);
                Plates.EnableInClassList("shown", shown);
            }

            public TraceSet Force(string region, Side side, bool garrison)
            {
                forces.Add((NewRing(), region, side, garrison));
                Layout();
                return this;
            }

            public TraceSet Settlement(string region)
            {
                settlements.Add((NewRing(), region));
                Layout();
                return this;
            }

            public TraceSet Region(string region)
            {
                if (owner.map.Data.Shapes.ContainsKey(region) && !regions.Contains(region))
                    regions.Add(region);
                Root.MarkDirtyRepaint();
                return this;
            }

            // A signed count beside the region's settlement: "+2 VP" in the enamel of the side it counts for.
            public TraceSet Badge(string region, string text, Side side)
            {
                var badge = new Label(text) { name = "observed-badge", pickingMode = PickingMode.Ignore };
                badge.AddToClassList("observed-badge");
                badge.AddToClassList(side == Side.FreePeoples ? "observed-badge-free" : "observed-badge-shadow");
                Plates.Add(badge);
                badges.Add((badge, region));
                Layout();
                return this;
            }

            // UI-307: a delta badge beside a force — per entry its figure art (an upgrade's two with an arrow), the nation's
            // crest and a signed count — so region, unit type, nation and amount read from the picture.
            public TraceSet Delta(string region, Side side, bool garrison, IEnumerable<FigureDelta> entries)
            {
                var badge = ObservedDelta.Plate(entries);
                Plates.Add(badge);
                deltas.Add((badge, region, side, garrison));
                Layout();
                return this;
            }

            // UI-309: a character's larger moment above its force (below it when it would leave the safe area, given in
            // map-local points): each portrait in its side's medallion over its title.
            public TraceSet Portraits(string region, Side side, bool garrison, IEnumerable<CharacterMark> marks, Rect safe)
            {
                var plate = new VisualElement { name = "observed-portraits", pickingMode = PickingMode.Ignore };
                plate.AddToClassList("observed-portraits");
                foreach (var mark in marks)
                    plate.Add(Portrait(mark));
                Plates.Add(plate);
                portraits.Add((plate, region, side, garrison, safe));
                Layout();
                return this;
            }

            // UI-309: who moves, beside the force where it lands, as the delta badge stands: small portraits with short
            // names, identical figures with their count.
            public TraceSet Identities(string region, Side side, bool garrison, IEnumerable<CharacterMark> marks)
            {
                var strip = new VisualElement { name = "observed-identities", pickingMode = PickingMode.Ignore };
                strip.AddToClassList("observed-identities");
                foreach (var mark in marks)
                    strip.Add(Identity(mark));
                Plates.Add(strip);
                deltas.Add((strip, region, side, garrison));
                Layout();
                return this;
            }

            // UI-309: the ring around the Fellowship's figure on its region (a separation's public origin).
            public TraceSet Fellowship(string region)
            {
                fellowships.Add((NewRing(), region));
                Layout();
                return this;
            }

            public IEnumerable<VisualElement> Deltas => deltas.Select(d => d.Element);

            public IEnumerable<VisualElement> Rings => forces.Select(f => f.Element).Concat(settlements.Select(s => s.Element));
            public IEnumerable<Label> Badges => badges.Select(b => (Label)b.Element);

            public void End()
            {
                if (ended)
                    return;
                ended = true;
                owner.Ended(this);
                Shown(false);
                Root.schedule.Execute(() =>
                {
                    Root.RemoveFromHierarchy();
                    Plates.RemoveFromHierarchy();
                }).StartingIn(FadeOutMilliseconds);
            }

            internal void Layout()
            {
                foreach (var (element, region, side, garrison) in forces)
                    Place(element, owner.map.MarkerBounds(region, side, garrison));
                foreach (var (element, region) in settlements)
                {
                    var anchor = owner.Anchor(region);
                    Place(element, new Rect(anchor.x - SettlementRing, anchor.y - SettlementRing, 2 * SettlementRing, 2 * SettlementRing));
                }
                foreach (var (element, region) in badges)
                {
                    var anchor = owner.Anchor(region);
                    element.style.left = anchor.x + SettlementRing + 4;
                    element.style.top = anchor.y - SettlementRing - 6;
                }
                foreach (var (element, region, side, garrison) in deltas)
                {
                    var marker = owner.map.MarkerBounds(region, side, garrison);
                    var anchor = owner.Anchor(region);
                    element.style.left = marker.HasValue ? marker.Value.xMax + 4 : anchor.x + 12;
                    element.style.top = marker.HasValue ? marker.Value.y : anchor.y - 14;
                }
                foreach (var (element, region, side, garrison, safe) in portraits)
                {
                    var anchor = owner.Anchor(region);
                    Above(element, owner.map.MarkerBounds(region, side, garrison) ?? new Rect(anchor.x - 12, anchor.y - 12, 24, 24), safe);
                }
                foreach (var (element, region) in fellowships)
                    Place(element, owner.map.FellowshipMarker.Bounds(region));
                Root.MarkDirtyRepaint();
            }

            // Centred above its subject, kept inside the safe area: below the subject when there is no room above.
            private static void Above(VisualElement element, Rect subject, Rect safe)
            {
                var width = Resolved(element.resolvedStyle.width);
                var height = Resolved(element.resolvedStyle.height);
                var top = subject.yMin - PortraitGap - height;
                element.style.top = top < safe.yMin ? subject.yMax + PortraitGap : top;
                element.style.left = Mathf.Clamp(subject.center.x - width / 2, safe.xMin, Mathf.Max(safe.xMin, safe.xMax - width));
            }

            private static float Resolved(float value) => float.IsNaN(value) ? 0 : value;

            private static VisualElement Portrait(CharacterMark mark)
            {
                var item = new VisualElement { pickingMode = PickingMode.Ignore };
                item.AddToClassList("observed-portrait");
                item.EnableInClassList("observed-portrait-removed", mark.Removed);
                item.Add(Picture(mark, "observed-portrait-picture"));
                item.Add(new Label(mark.Text) { name = "observed-portrait-name", pickingMode = PickingMode.Ignore, enableRichText = false });
                return item;
            }

            private static VisualElement Identity(CharacterMark mark)
            {
                var row = new VisualElement { pickingMode = PickingMode.Ignore };
                row.AddToClassList("observed-identity");
                row.Add(Picture(mark, "observed-identity-picture"));
                row.Add(new Label(mark.Text) { name = "observed-identity-name", pickingMode = PickingMode.Ignore, enableRichText = false });
                return row;
            }

            // A named character stands in its side's medallion; identical figures show their figure art alone.
            private static Image Picture(CharacterMark mark, string className)
            {
                var image = new Image { sprite = mark.Picture, scaleMode = ScaleMode.ScaleToFit, pickingMode = PickingMode.Ignore };
                image.AddToClassList(className);
                if (mark.DefinitionId.Length > 0)
                    image.AddToClassList(mark.Side == Side.Shadow ? "observed-medallion-shadow" : "observed-medallion-free");
                if (mark.Removed)
                    image.tintColor = new Color(.55f, .55f, .55f, 1);
                return image;
            }

            private VisualElement NewRing()
            {
                var ring = new VisualElement { pickingMode = PickingMode.Ignore };
                ring.AddToClassList("observed-ring");
                Root.Add(ring);
                return ring;
            }

            private static void Place(VisualElement ring, Rect? bounds)
            {
                ring.style.display = bounds.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
                if (!bounds.HasValue)
                    return;
                var rect = bounds.Value;
                var radius = Mathf.Min(rect.width, rect.height) / 2 + RingGap;
                ring.style.left = rect.x - RingGap;
                ring.style.top = rect.y - RingGap;
                ring.style.width = rect.width + 2 * RingGap;
                ring.style.height = rect.height + 2 * RingGap;
                ring.style.borderTopLeftRadius = Mathf.Min(radius, 10);
                ring.style.borderTopRightRadius = Mathf.Min(radius, 10);
                ring.style.borderBottomLeftRadius = Mathf.Min(radius, 10);
                ring.style.borderBottomRightRadius = Mathf.Min(radius, 10);
            }

            private void DrawOutlines(MeshGenerationContext context)
            {
                if (regions.Count == 0)
                    return;
                var painter = context.painter2D;
                painter.strokeColor = new Color(146 / 255f, 212 / 255f, 242 / 255f, .85f);
                painter.lineWidth = 2;
                foreach (var id in regions)
                    foreach (var ring in owner.map.Data.Shapes[id].Rings)
                    {
                        painter.BeginPath();
                        for (var index = 0; index < ring.Points.Length; index++)
                        {
                            var point = owner.map.Camera.ToScreen(new Vector2(ring.Points[index][0], ring.Points[index][1]));
                            if (index == 0)
                                painter.MoveTo(point);
                            else
                                painter.LineTo(point);
                        }
                        painter.ClosePath();
                        painter.Stroke();
                    }
            }
        }
    }
}
