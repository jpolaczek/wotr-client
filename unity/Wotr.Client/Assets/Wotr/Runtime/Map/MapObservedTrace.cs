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
        private readonly MapCanvas map;
        private readonly List<TraceSet> sets = new List<TraceSet>();
        private readonly IVisualElementScheduledItem layout;
        public VisualElement Root { get; } = new VisualElement { name = "map-observed-trace", pickingMode = PickingMode.Ignore };
        public int Count => sets.Count;

        public MapObservedTrace(MapCanvas map)
        {
            this.map = map;
            Root.AddToClassList("map-observed-trace");
            Root.StretchToParentSize();
            map.Add(Root);
            layout = Root.schedule.Execute(Layout).Every(16);
            layout.Pause();
        }

        public TraceSet Begin()
        {
            var set = new TraceSet(this);
            sets.Add(set);
            Root.Add(set.Root);
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
            private readonly List<string> regions = new List<string>();
            private bool ended;
            public VisualElement Root { get; } = new VisualElement { pickingMode = PickingMode.Ignore };
            public IReadOnlyList<string> Regions => regions;

            internal TraceSet(MapObservedTrace owner)
            {
                this.owner = owner;
                Root.AddToClassList("observed-set");
                Root.StretchToParentSize();
                Root.generateVisualContent += DrawOutlines;
                // The next frame, so the fade-in has a start.
                Root.schedule.Execute(() => Root.AddToClassList("shown"));
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
                Root.Add(badge);
                badges.Add((badge, region));
                Layout();
                return this;
            }

            public IEnumerable<VisualElement> Rings => forces.Select(f => f.Element).Concat(settlements.Select(s => s.Element));
            public IEnumerable<Label> Badges => badges.Select(b => (Label)b.Element);

            public void End()
            {
                if (ended)
                    return;
                ended = true;
                owner.Ended(this);
                Root.RemoveFromClassList("shown");
                Root.schedule.Execute(Root.RemoveFromHierarchy).StartingIn(FadeOutMilliseconds);
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
                Root.MarkDirtyRepaint();
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
