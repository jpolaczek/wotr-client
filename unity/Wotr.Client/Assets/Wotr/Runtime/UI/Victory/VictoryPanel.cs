using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class VictoryPanel
    {
        public VisualElement Root { get; } = new VisualElement { name = "victory-panel" };
        public Button Tab { get; }
        public Label Banner { get; } = new Label { name = "victory-reason" };
        public FloatingWindow Window { get; set; }
        public bool IsOpen => hover.Visible;
        public bool Pinned => hover.Pinned;
        public bool MenuOnly { get; set; }
        public event Action Opening;
        private readonly VictoryHover hover;
        private readonly VictoryShortcut shortcut;
        private readonly MapCanvas map;
        private readonly WindowLayout positions;
        private readonly Label timing = new Label { name = "victory-timing" };
        private readonly VictoryScoreColumn freePeoples, shadow;
        private PlayerView view;
        private bool visible;

        public VictoryPanel(VisualElement surface, MapCanvas map, Action fellowship, WindowLayout positions = null)
        {
            this.map = map;
            this.positions = positions;
            Root.AddToClassList("victory-panel");
            Root.style.display = DisplayStyle.None;
            Tab = BoardScreen.Button("FP —/4 · SH —/10", Open);
            Tab.name = "victory-tab";
            Tab.tooltip = "Victory · Captured settlements and victory conditions";
            Tab.AddToClassList("victory-tab");
            Tab.AddToClassList(RuntimeTooltip.RichHoverClass);
            Tab.style.display = DisplayStyle.None;
            shortcut = new VictoryShortcut(Tab);
            Banner.AddToClassList("victory-result");
            Banner.pickingMode = PickingMode.Ignore;
            Banner.style.display = DisplayStyle.None;
            var header = FloatingWindow.Header("Victory");
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-victory";
            close.tooltip = "Close Victory · Keep your selection";
            header.Add(close);
            Root.Add(header);
            timing.AddToClassList("victory-timing");
            Root.Add(timing);
            var scores = BoardScreen.Row("victory-scores");
            freePeoples = new VictoryScoreColumn(Side.FreePeoples, Locate);
            shadow = new VictoryScoreColumn(Side.Shadow, Locate);
            scores.Add(freePeoples.Root);
            scores.Add(shadow.Root);
            Root.Add(scores);
            var ring = BoardScreen.Row("victory-ring");
            var conditions = new VisualElement();
            conditions.Add(new Label("SH · Corruption reaches 12.") { name = "victory-corruption-rule" });
            conditions.Add(new Label("FP · Finish Mordor with corruption below 12.") { name = "victory-destruction-rule" });
            conditions.tooltip = "Resolve the Hunt tile first. Corruption takes priority over destroying the Ring. BASE p.43–44.";
            ring.Add(conditions);
            var openFellowship = BoardScreen.Button("Fellowship", fellowship);
            openFellowship.name = "victory-fellowship";
            openFellowship.tooltip = "Inspect the Fellowship · No camera movement";
            ring.Add(openFellowship);
            Root.Add(ring);
            hover = new VictoryHover(surface, Tab, Root, RefreshVisibility);
            Root.RegisterCallback<GeometryChangedEvent>(_ => { Position(); PlaceStatus(); });
            map.RegisterCallback<GeometryChangedEvent>(_ => { Position(); PlaceStatus(); });
            Banner.RegisterCallback<GeometryChangedEvent>(_ => PlaceStatus());
            Tab.RegisterCallback<GeometryChangedEvent>(_ => { Position(); PlaceStatus(); });
        }

        public void KeepShortcutClearOf(VisualElement window) => shortcut.Avoid(window);

        public void Apply(PlayerView latest)
        {
            if (view?.MatchId != latest.MatchId)
            {
                Close();
                freePeoples.ResetPage();
                shadow.ResetPage();
            }
            view = latest;
            Tab.style.display = MenuOnly ? DisplayStyle.None : DisplayStyle.Flex;
            Tab.text = VictoryPresentation.Summary(view);
            Tab.EnableInClassList("threshold-reached", !view.Winner.HasValue && view.MilitaryVictory.Exists(score => score.ThresholdReached));
            timing.text = VictoryPresentation.MilitaryTiming(view);
            timing.tooltip = "Military victory is checked in phase 6, after Ring victories. If both military thresholds are reached, Shadow wins. BASE p.44.";
            freePeoples.Apply(view);
            shadow.Apply(view);
            Banner.text = VictoryPresentation.Result(view);
            Banner.style.display = view.Winner.HasValue ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshVisibility();
        }

        public void Open()
        {
            if (view != null)
                hover.Open();
        }

        public void Close() => hover.Close();
        public void CloseTransient() => hover.CloseTransient();

        public void Hide()
        {
            Close();
            view = null;
            Tab.style.display = Banner.style.display = DisplayStyle.None;
        }

        private void Locate(string region)
        {
            map.Focus(region);
            // Locate is explicit navigation. Keep a pinned panel, but leave the
            // focused region unobstructed even after the player moved the window.
            if (!map.Data.Shapes.TryGetValue(region, out var shape))
                return;
            var target = MapPanelPlacement.Anchor(map, Root,
                map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1])));
            var current = Root.layout;
            if (!current.Contains(target))
                return;
            var left = target.x - Root.layout.width - 32;
            var right = target.x + 32;
            var next = new Vector2(left >= 0 ? left : Math.Min(right, Root.parent.contentRect.width - Root.layout.width),
                Math.Max(0, PanelBottom() - Root.layout.height));
            positions?.Remember(Root.name, next);
            Root.style.left = next.x;
            Root.style.top = next.y;
        }

        private void RefreshVisibility()
        {
            var open = IsOpen && view != null;
            Appearance.Display(Root, open);
            if (visible != open)
            {
                visible = open;
                if (open)
                {
                    Opening?.Invoke();
                    Root.BringToFront();
                }
            }
            Position();
            Window?.Refresh();
            PlaceStatus();
        }

        private void PlaceStatus()
        {
            // The result must remain readable when a player moves this inspector
            // over its usual bottom-right corner. Keep it in the opposite corner.
            if (view?.Winner.HasValue == true && Banner.layout.width > 0 && Banner.layout.height > 0)
            {
                var bounds = Root.parent.contentRect;
                var candidate = new Rect(Root.parent.LocalToWorld(new Vector2(bounds.xMax - Banner.layout.width - 8,
                    bounds.yMax - Banner.layout.height - 42)), Banner.worldBound.size);
                var useLeft = IsOpen && Root.worldBound.Overlaps(candidate);
                Banner.style.left = useLeft ? new StyleLength(8) : new StyleLength(StyleKeyword.Auto);
                Banner.style.right = useLeft ? new StyleLength(StyleKeyword.Auto) : new StyleLength(8);
                Banner.BringToFront();
            }
            shortcut.Refresh();
            Tab.BringToFront();
        }

        public void Position()
        {
            var viewport = Root.parent?.contentRect ?? map.contentRect;
            var pageSize = viewport.height > 0 && viewport.height < 360 ? 2 : 3;
            freePeoples.SetPageSize(pageSize);
            shadow.SetPageSize(pageSize);
            if (Window?.Restore() == true || !IsOpen || viewport.width <= 0 || Root.layout.height <= 0 ||
                float.IsNaN(Root.layout.width) || float.IsNaN(Root.layout.height))
                return;
            var bounds = viewport;
            var position = WindowLayout.Clamp(new Vector2(bounds.xMax - Root.layout.width - 8,
                PanelBottom() - Root.layout.height), Root.layout.size, bounds);
            Root.style.left = position.x;
            Root.style.top = position.y;
        }

        private float PanelBottom()
        {
            var parent = Root.parent ?? map;
            var top = parent.WorldToLocal(Tab.worldBound.min).y;
            return Tab.layout.height > 0 && !float.IsNaN(top) ? top - 8 : parent.contentRect.yMax - 52;
        }
    }
}
