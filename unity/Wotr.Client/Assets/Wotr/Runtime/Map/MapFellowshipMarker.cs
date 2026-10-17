using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MapFellowshipMarker
    {
        // UI-154: on the Mordor track the figure stands on its step, walks the path to the next one and carries its
        // Corruption, the missing-attempt mark and the drawn Hunt tile beside it.
        public const float MotionSeconds = .4f;
        // UI-185/UI-192: as large as the figures of the armies (MapArmyArtwork).
        private static readonly Vector2 RegionSize = new Vector2(64, 72);
        private static readonly Vector2 TrackDetail = new Vector2(36, 40), TrackToken = new Vector2(28, 32);
        private const float StatusWidth = 58;
        private const float TileWidth = 30;
        private readonly MapCanvas map;
        private readonly Button marker = new Button { name = "fellowship-marker" };
        private readonly VisualElement status = new VisualElement { name = "fellowship-mordor-status", pickingMode = PickingMode.Ignore };
        private readonly Label corruption = new Label { name = "fellowship-mordor-corruption", pickingMode = PickingMode.Ignore };
        private readonly VisualElement idle = new VisualElement { name = "fellowship-mordor-idle", pickingMode = PickingMode.Ignore };
        private readonly HuntTileFace tile = new HuntTileFace("small");
        private readonly IVisualElementScheduledItem motion;
        public VisualElement Root => marker;
        public VisualElement Status => status;
        public VisualElement Tile => tile.Root;
        public event System.Action Clicked;
        // UI-155: on the track a revealed Fellowship hides with one click on its figure when the server allows it.
        public event System.Action HideRequested;
        public const string HideClass = "fellowship-hide-target";
        public bool HideOffered { get; private set; }
        private string regionId = "";
        private MordorTrackState mordor = MordorTrackState.Outside;
        private bool tileShown;
        private float shownStep = -1;
        private int targetStep = -1;
        private float motionFrom;
        private double motionStart;
        private bool hovered;
        private bool inspected;
        private Rect bounds;

        public MapFellowshipMarker(MapCanvas map)
        {
            this.map = map;
            marker.clicked += () =>
            {
                if (HideOffered && mordor.Active)
                    HideRequested?.Invoke();
                else
                    Clicked?.Invoke();
            };
            marker.userData = new DraftToken(DraftObject.Fellowship);
            marker.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
            marker.RegisterCallback<PointerEnterEvent>(_ => { hovered = true; map.Refresh(); });
            marker.RegisterCallback<PointerLeaveEvent>(_ => { hovered = false; map.Refresh(); });
            marker.AddToClassList("fellowship-marker");
            // The map's own figure needs no panel glow (UI-201).
            FigureArtwork.Apply(marker, FigureAppearances.Fellowship, Side.FreePeoples, glow: false);
            marker.style.display = DisplayStyle.None;
            map.Add(marker);
            var ring = new VisualElement { name = "fellowship-mordor-ring", pickingMode = PickingMode.Ignore };
            status.Add(ring);
            status.Add(corruption);
            status.Add(idle);
            status.style.display = DisplayStyle.None;
            map.Add(status);
            tile.Root.name = "fellowship-mordor-tile";
            tile.Root.style.position = Position.Absolute;
            tile.Root.style.display = DisplayStyle.None;
            map.Add(tile.Root);
            motion = marker.schedule.Execute(Walk).Every(16);
            motion.Pause();
        }

        public bool OnTrack => mordor.Active;
        public float TrackStep => shownStep;

        public void Apply(FellowshipView fellowship, HuntView hunt = null)
        {
            mordor = MordorTrackState.For(fellowship);
            regionId = fellowship?.MordorStep.HasValue == false ? fellowship.RegionId : "";
            marker.text = fellowship?.Hidden == true ? "◇" : "◆";
            MoveOnTrack(mordor.Step);
            corruption.text = mordor.Corruption + "/12";
            idle.style.display = mordor.Attempted == false ? DisplayStyle.Flex : DisplayStyle.None;
            status.tooltip = "Corruption " + mordor.Corruption + "/12 · At 12 or more, the Shadow wins immediately." +
                (mordor.Attempted == false ? "\n" + MordorTrackState.ActivityHint : "");
            tileShown = mordor.Active && hunt?.Mordor == true && hunt.Face != null;
            if (tileShown)
                tile.Apply(hunt.Face);
            Layout();
        }

        public void OfferHide(bool offered)
        {
            HideOffered = offered;
            marker.EnableInClassList(HideClass, offered && mordor.Active);
            if (mordor.Active)
                marker.tooltip = mordor.FigureTooltip + (offered ? "\nClick to hide the Fellowship." : "");
        }

        public void Inspect(bool open)
        {
            if (inspected == open)
                return;
            inspected = open;
            map.Refresh();
        }

        public Rect? Bounds(string region) => region == regionId && marker.style.display == DisplayStyle.Flex ? bounds : null;

        public void Layout()
        {
            if (mordor.Active)
            {
                LayoutOnTrack(true);
                return;
            }
            status.style.display = DisplayStyle.None;
            tile.Root.style.display = DisplayStyle.None;
            marker.style.opacity = map.Figures?.FellowshipArriving == true ? .25f : 1;
            var visible = map.Data.Shapes.TryGetValue(regionId, out var shape);
            var anchor = visible ? map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1])) : Vector2.zero;
            visible &= map.NearView(anchor) &&
                (hovered || inspected || map.MarkerVisibility.Figures(regionId) || map.Figures?.Reveals(regionId) == true);
            marker.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                return;
            Place(map.LocalMarkerRect(anchor + new Vector2(0, -64), RegionSize), true);
            marker.tooltip = map.Data.Regions[regionId].Name + " · Last known position";
        }

        // The figure's feet stand on the step; under the step a row holds the drawn tile (zoomed in only) and the
        // Corruption badge, clear of the neighbouring steps along the path.
        private void LayoutOnTrack(bool reserve)
        {
            marker.style.opacity = 1;
            var detail = map.MarkerVisibility.Detail;
            var size = detail ? TrackDetail : TrackToken;
            var anchor = map.Camera.ToScreen(map.Data.MordorTrack.PointAt(shownStep));
            var visible = map.NearView(anchor);
            var withTile = visible && tileShown && detail;
            marker.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            status.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            tile.Root.style.display = withTile ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                return;
            var rect = map.LocalMarkerRect(anchor + new Vector2(0, 4 - size.y / 2), size);
            Place(rect, reserve);
            marker.tooltip = mordor.FigureTooltip + (HideOffered ? "\nClick to hide the Fellowship." : "");
            var measured = status.resolvedStyle.width;
            var statusWidth = float.IsNaN(measured) || measured <= 0 ? StatusWidth : measured;
            var tileWidth = withTile ? TileWidth : 0;
            var left = anchor.x - (tileWidth + statusWidth) / 2;
            var top = anchor.y + (detail ? 24 : 14);
            // On the Crack of Doom the row would cover its caption and step 4, so it stands left of the crater.
            if (shownStep >= MordorTrackGeometry.LastStep)
            {
                left = rect.x - tileWidth - statusWidth - 4;
                top = anchor.y - 8;
            }
            tile.Root.style.left = left;
            tile.Root.style.top = top - 6;
            status.style.left = left + tileWidth;
            status.style.top = top;
        }

        private void Place(Rect rect, bool reserve)
        {
            bounds = rect;
            if (reserve)
                map.ReserveMarker(rect);
            marker.style.width = rect.width;
            marker.style.height = rect.height;
            marker.style.left = rect.x;
            marker.style.top = rect.y;
        }

        // A new step walks the path from the shown position; the first sight of the track, or a jump back, places it.
        private void MoveOnTrack(int step)
        {
            if (step < 0 || shownStep < 0 || step < targetStep)
            {
                shownStep = targetStep = step;
                motion.Pause();
                return;
            }
            if (step == targetStep)
                return;
            motionFrom = shownStep;
            targetStep = step;
            motionStart = Time.realtimeSinceStartupAsDouble;
            motion.Resume();
        }

        private void Walk()
        {
            var progress = Mathf.Clamp01((float)((Time.realtimeSinceStartupAsDouble - motionStart) / MotionSeconds));
            shownStep = Mathf.Lerp(motionFrom, targetStep, Mathf.SmoothStep(0, 1, progress));
            if (progress >= 1)
            {
                shownStep = targetStep;
                motion.Pause();
            }
            // The walk only moves the figure and its badges; the next map layout reserves its place.
            LayoutOnTrack(false);
        }
    }
}
