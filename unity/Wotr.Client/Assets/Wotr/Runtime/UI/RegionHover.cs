using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Owns explicit inspection and lightweight hover highlighting; never edits the action model.
    public sealed class RegionHover
    {
        public event System.Action Opening;
        public System.Func<string, bool> Excluded { get; set; }
        public System.Func<VisualElement, bool> RelatedInspection { get; set; }
        public RegionHoverPanel Panel { get; } = new RegionHoverPanel();
        private readonly VisualElement root;
        private readonly MapCanvas map;
        private PlayerView view;
        private RegionViewIndex index;
        private bool holdPanel;
        private string rendered = "";
        private string explicitRegion = "";
        // UI-103: the panel open when a map press began; a press on the terrain closes it before the click.
        private string pressedOpen = "";
        // UI-189 (user decision): away from its region, the panel and a preview opened from it, the panel closes after
        // this delay, which leaves time to cross the gap between the region and the panel.
        private const long LeaveDelay = 300;
        private IVisualElementScheduledItem leaving;
        // UI-254: the decision whose panel this is, when the console's plaque or the decision notice opened it. Such a
        // panel stays while the decision waits — no leave delay, presses elsewhere, wheel or focus loss close it — and
        // follows its region when the camera moves; Esc, the plaque, a click on a region or the resolution close it.
        private string decision = "";
        private readonly System.Collections.Generic.Dictionary<VisualElement, PickingMode> dragPicking =
            new System.Collections.Generic.Dictionary<VisualElement, PickingMode>();

        public RegionHover(VisualElement root, VisualElement stage, MapCanvas map)
        {
            this.root = root;
            this.map = map;
            stage.Add(Panel.Root);
            root.RegisterCallback<PointerMoveEvent>(Move, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(e => { if (e.target == root) Leave(); });
            root.RegisterCallback<PointerDownEvent>(PointerDown, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(_ => holdPanel = false, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCancelEvent>(_ => holdPanel = false, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape)
                    Close();
                else if (e.target == map && e.keyCode is (KeyCode.Return or KeyCode.KeypadEnter or KeyCode.Space))
                {
                    if (map.Selected.Length > 0)
                        map.Select(map.Selected);
                    e.StopImmediatePropagation();
                }
            }, TrickleDown.TrickleDown);
            root.RegisterCallback<AttachToPanelEvent>(e => { if (e.target == root) Application.focusChanged += ApplicationFocus; });
            root.RegisterCallback<DetachFromPanelEvent>(e => { if (e.target == root) { Application.focusChanged -= ApplicationFocus; Close(); } });
            Panel.Root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                if (PopoverPlacement.SizeChanged(e.oldRect, e.newRect))
                    Position();
            });
            map.RegisterCallback<GeometryChangedEvent>(_ => Position());
            stage.RegisterCallback<GeometryChangedEvent>(_ => Position());
            map.RegisterCallback<WheelEvent>(_ => { if (!HoldsDecision) Close(); }, TrickleDown.TrickleDown);
            map.CameraChanged += () => { if (HoldsDecision) Position(); };
        }

        public bool HoldsDecision => decision.Length > 0 && explicitRegion.Length > 0;

        public bool HoldsDecisionOf(string decisionId) => HoldsDecision && decision == decisionId;

        public void Apply(PlayerView latest)
        {
            if (view != null && view.MatchId != latest.MatchId)
                Close();
            view = latest;
            index = new RegionViewIndex(latest);
            if (decision.Length > 0 && latest.Decision?.Id != decision)
                Close();
            if (explicitRegion.Length > 0)
                Show();
        }

        public void Open(string region)
        {
            if (Excluded?.Invoke(region) == true) { Close(); return; }
            CancelLeave();
            decision = "";
            explicitRegion = region;
            Show();
            Panel.Root.BringToFront();
        }

        // UI-254: opens the region panel that hosts a decision and keeps it while that decision waits.
        public void OpenDecision(string region, string decisionId)
        {
            Open(region);
            decision = explicitRegion == region ? decisionId ?? "" : "";
        }

        // UI-103: a click on the region whose panel is open closes that panel; another region switches to it.
        public void Toggle(string region)
        {
            var open = pressedOpen == region ||
                explicitRegion == region && Panel.Root.style.display.value != DisplayStyle.None;
            pressedOpen = "";
            if (open)
                Close();
            else
                Open(region);
        }

        public void PreviewDrag(DraftToken source)
        {
            foreach (var entry in dragPicking)
                entry.Key.pickingMode = entry.Value;
            dragPicking.Clear();
            var active = source?.Object == DraftObject.Reserve;
            Panel.Root.style.opacity = active ? .25f : 1f;
            if (!active)
                return;
            // Keep layout/capture stable while allowing drops onto the terrain beneath.
            foreach (var element in Panel.Root.Query<VisualElement>().ToList().Prepend(Panel.Root))
            {
                dragPicking[element] = element.pickingMode;
                element.pickingMode = PickingMode.Ignore;
            }
        }

        private void PointerDown(PointerDownEvent e)
        {
            var target = e.target as VisualElement;
            pressedOpen = e.button == 0 && target != null && (target == map || map.Contains(target)) ? explicitRegion : "";
            holdPanel = e.button == 0 && target != null &&
                (target == Panel.Root || Panel.Root.Contains(target) || IsArmyMarker(target));
            if (!holdPanel && !HoldsDecision && RelatedInspection?.Invoke(target) != true)
                Close();
        }

        private bool IsArmyMarker(VisualElement target)
        {
            if (!map.Contains(target))
                return false;
            for (var current = target; current != null && current != map; current = current.parent)
                if (current is Button && current.ClassListContains("army"))
                    return true;
            return false;
        }

        private void Move(PointerMoveEvent e)
        {
            if (view == null || e.pressedButtons != 0 || map.Panning)
            {
                // A gesture never closes the panel; the next plain move decides again.
                CancelLeave();
                map.SetHovered("");
                return;
            }
            var target = root.panel?.Pick(e.position) ?? e.target as VisualElement;
            var region = RegionAt(target, e.position);
            map.SetHovered(region);
            FollowPointer(target, region);
        }

        // UI-189: the open panel stays while the pointer is on its region (terrain or army marker), on the panel or on
        // a preview opened from it; anywhere else it closes after LeaveDelay unless the pointer comes back first.
        private void FollowPointer(VisualElement target, string region)
        {
            if (explicitRegion.Length == 0)
                return;
            if (HoldsDecision || region == explicitRegion || Inside(target))
                CancelLeave();
            else
                ScheduleLeave();
        }

        private bool Inside(VisualElement target) => target != null &&
            (target == Panel.Root || Panel.Root.Contains(target) || RelatedInspection?.Invoke(target) == true);

        private void ScheduleLeave()
        {
            if (leaving != null)
                return;
            leaving = root.schedule.Execute(Close).StartingIn(LeaveDelay);
        }

        private void CancelLeave()
        {
            leaving?.Pause();
            leaving = null;
        }

        private string RegionAt(VisualElement target, Vector2 position)
        {
            if (target != map && !map.Contains(target))
                return "";
            for (var current = target; current != null && current != map; current = current.parent)
                if (current.userData is DraftToken token)
                {
                    if (token.Object == DraftObject.Fellowship)
                        return "";
                    if (token.Object is DraftObject.Region or DraftObject.Army)
                        return token.Id;
                }
            return map.HitTest(map.WorldToLocal(position)) ?? "";
        }

        private void Leave()
        {
            holdPanel = false;
            map.SetHovered("");
            // UI-189: leaving the game window is leaving the region and the panel too.
            if (explicitRegion.Length > 0 && !HoldsDecision)
                ScheduleLeave();
        }

        private void Show()
        {
            if (view == null || Excluded?.Invoke(explicitRegion) == true ||
                !map.Data.Regions.TryGetValue(explicitRegion, out var region))
            {
                Close();
                return;
            }
            Opening?.Invoke();
            var changed = rendered != region.Id;
            var opening = changed || Panel.Root.style.display.value == DisplayStyle.None;
            Panel.Show(view, region, index.Get(region.Id));
            if (opening)
                Panel.Root.BringToFront();
            rendered = region.Id;
            map.InspectRegion(region.Id);
            Position();
        }

        private void Position()
        {
            if (explicitRegion.Length == 0 || Panel.Root.parent == null ||
                !map.Data.Shapes.TryGetValue(explicitRegion, out var shape))
                return;
            var viewport = Panel.Root.parent.contentRect;
            var compact = viewport.height < 360;
            Panel.Root.EnableInClassList("compact-hover", compact);
            // UI-188: a name-only window takes the width of its text (Wotr.uss caps it).
            Panel.Root.style.width = Panel.NameOnly ? new StyleLength(StyleKeyword.Auto)
                : new StyleLength(Mathf.Min(viewport.width - 16, Panel.PreferredWidth(compact)));
            var size = Panel.Root.layout.size;
            if (size.x <= 0 || size.y <= 0 || float.IsNaN(size.x) || float.IsNaN(size.y))
                return;
            var anchor = map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1]));
            var bounds = MapPanelPlacement.Near(map, Panel.Root, anchor, markers: map.MarkerBounds(explicitRegion));
            Panel.Root.style.left = bounds.x;
            Panel.Root.style.top = bounds.y;
        }

        public void Close()
        {
            CancelLeave();
            holdPanel = false;
            rendered = "";
            explicitRegion = "";
            decision = "";
            Panel.Hide();
            map.InspectRegion("");
        }

        private void ApplicationFocus(bool focused)
        {
            if (!focused && !HoldsDecision)
                Close();
        }
    }
}
