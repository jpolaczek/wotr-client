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
        // UI-185/UI-192: as large as the figures of the armies (MapArmyArtwork). UI-401: its design size; the map scales
        // it, and it stands on its region's Fellowship place (UI-422). UI-406 (TABLETOP_FIGURES_UX, "Fellowship figure"):
        // the figure stands on a round Free Peoples base over a contact shadow, so the box runs on below its feet to hold
        // the base; the figure keeps its 64 × 72 point box above it.
        internal static readonly Vector2 RegionSize = new Vector2(64, 80);
        private const float FigureHeight = 72;
        // UI-401: design points per board unit — the figure is 36 units tall and its base 16 units wide.
        public const float DesignUnit = 1.8f;
        // The point under the figure's feet, the middle of its base's top face, on a region and on the Mordor track's step.
        internal static readonly Vector2 RegionFeet = new Vector2(32, 64);
        // UI-406: the round base (40 × 28 points of art) drawn 16 units wide; its top face's middle is .357 of its height
        // down. The figure's box is its art's, its feet on the base.
        private static readonly Vector2 BaseSize = new Vector2(16 * DesignUnit, 16 * DesignUnit * 28 / 40);
        private const float FeetOnTopFace = .357f;
        private static readonly Rect FigureBox = new Rect(2, RegionFeet.y - 62, 60, 62);
        // UI-406: the figure on its base, as the characters' miniatures (MapCharacterArtwork) stand.
        public const string MiniatureClass = "fellowship-miniature";
        // UI-283 (owner's request): on the Mordor track the figure is twice its first size, as large as on a region.
        private const float TrackDetail = 80, TrackToken = 64;
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
        // UI-155, UI-280: a revealed Fellowship hides with one click on its figure when the server allows it.
        public event System.Action HideRequested;
        public const string HideClass = "fellowship-hide-target";
        public const string HideHint = FellowshipActions.HideTooltip;
        private (string Region, bool Hidden, int Corruption, bool Declare, bool Move, bool Hide) shownTooltip;
        public bool HideOffered { get; private set; }
        // UI-236: in the Fellowship phase a click on the figure declares its position; the panel has no button for it.
        public event System.Action DeclareRequested;
        public const string DeclareClass = "fellowship-declare-target";
        public const string DeclareHint = "Click to declare the Fellowship's position · Choose the route on the map.";
        public bool DeclareOffered { get; private set; }
        // UI-282: off the Mordor track a hidden Fellowship moves with one click on its figure when the chosen die can
        // move it, as with the track symbol in its panel.
        public event System.Action MoveRequested;
        public const string MoveClass = "fellowship-move-target";
        // UI-420 (owner): the same hide and move wording as the track and the console (FellowshipActions).
        public const string MoveHint = FellowshipActions.MoveTooltip;
        public bool MoveOffered { get; private set; }
        private readonly ChoicePulse pulse;
        private string regionId = "";
        private bool hidden;
        private int corruptionLevel;
        private MordorTrackState mordor = MordorTrackState.Outside;
        private bool tileShown;
        private float shownStep = -1;
        private int targetStep = -1;
        private float motionFrom;
        private double motionStart;
        private bool hovered;
        private bool inspected;
        private readonly MapStand stand;
        // UI-427: the plaque on the base's front that shows ◇ Hidden or ◆ Revealed.
        private readonly VisualElement plaque;

        public MapFellowshipMarker(MapCanvas map)
        {
            this.map = map;
            marker.clicked += Activate;
            marker.userData = Token("");
            marker.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
            marker.RegisterCallback<PointerEnterEvent>(_ => { hovered = true; map.Refresh(); });
            marker.RegisterCallback<PointerLeaveEvent>(_ => { hovered = false; map.Refresh(); });
            plaque = Dress(marker, true);
            marker.style.display = DisplayStyle.None;
            stand = new MapStand(marker, RegionSize);
            map.Stands.Add(stand);
            var ring = new VisualElement { name = "fellowship-mordor-ring", pickingMode = PickingMode.Ignore };
            status.Add(ring);
            status.Add(corruption);
            status.Add(idle);
            status.style.display = DisplayStyle.None;
            Pin(status);
            map.Add(status);
            tile.Root.name = "fellowship-mordor-tile";
            tile.Root.style.position = Position.Absolute;
            tile.Root.style.display = DisplayStyle.None;
            Pin(tile.Root);
            map.Add(tile.Root);
            motion = marker.schedule.Execute(Walk).Every(16);
            motion.Pause();
            pulse = new ChoicePulse(marker);
        }

        // The figure on a region at its design size: the Fellowship's illustration standing on its round Free Peoples base
        // over a contact shadow, and (UI-427) its Hidden or Revealed plaque on the base's front, drawn last so nothing of
        // the figure covers it; returns the plaque. The map's own figure needs no panel glow (UI-201); UI-404's flight
        // draws the same figure.
        internal static VisualElement Dress(Button figure, bool hidden)
        {
            figure.AddToClassList("fellowship-marker");
            figure.AddToClassList(MiniatureClass);
            figure.Add(Part("fellowship-contact-shadow", ShadowRect()));
            figure.Add(Part("fellowship-stand", BaseRect()));
            var image = FigureArtwork.Image(FigureAppearances.Fellowship);
            image.name = "figure-art";
            Place(image, FigureBox);
            figure.Add(image);
            var plaque = FellowshipStateBadge.Create(hidden);
            figure.Add(plaque);
            figure.style.width = RegionSize.x;
            figure.style.height = RegionSize.y;
            return plaque;
        }

        private static Rect BaseRect() => new Rect(RegionFeet.x - BaseSize.x / 2, RegionFeet.y - BaseSize.y * FeetOnTopFace,
            BaseSize.x, BaseSize.y);

        // The contact shadow lies under the base, a little down and right, as under a character's stand.
        private static Rect ShadowRect()
        {
            var stand = BaseRect();
            return new Rect(stand.x + 2.3f, stand.y + stand.height * .58f, stand.width, stand.height * .45f);
        }

        private static VisualElement Part(string name, Rect box)
        {
            var element = new VisualElement { name = name, pickingMode = PickingMode.Ignore };
            element.AddToClassList(name);
            Place(element, box);
            return element;
        }

        private static void Place(VisualElement element, Rect box)
        {
            element.style.position = Position.Absolute;
            element.style.left = box.x;
            element.style.top = box.y;
            element.style.width = box.width;
            element.style.height = box.height;
        }

        // The Hidden or Revealed symbol the figure shows now (UI-427: its plaque, State).
        public string Symbol => hidden ? "◇" : "◆";
        public bool Hidden => hidden;
        public VisualElement State => plaque;

        // The figure's click: the hide, the move or the declaration it offers, otherwise its inspection.
        public void Activate()
        {
            if (HideOffered)
                HideRequested?.Invoke();
            else if (MoveOffered && !mordor.Active)
                MoveRequested?.Invoke();
            else if (DeclareOffered && !mordor.Active)
                DeclareRequested?.Invoke();
            else
                Clicked?.Invoke();
        }

        public bool OnTrack => mordor.Active;
        // UI-522: the region it stands in, empty on the Mordor track or without a Fellowship.
        public string Region => regionId;
        public float TrackStep => shownStep;

        public void Apply(FellowshipView fellowship, HuntView hunt = null)
        {
            mordor = MordorTrackState.For(fellowship);
            regionId = fellowship?.MordorStep.HasValue == false ? fellowship.RegionId : "";
            hidden = fellowship?.Hidden == true;
            corruptionLevel = fellowship?.Corruption ?? 0;
            FellowshipStateBadge.Show(plaque, hidden);
            MoveOnTrack(mordor.Step);
            marker.userData = Token(StandingRegion());
            corruption.text = mordor.Corruption + "/12";
            idle.style.display = mordor.Attempted == false ? DisplayStyle.Flex : DisplayStyle.None;
            status.tooltip = "Corruption " + mordor.Corruption + "/12 · At 12 or more, the Shadow wins immediately." +
                (mordor.Attempted == false ? "\n" + MordorTrackState.ActivityHint : "");
            tileShown = mordor.Active && hunt?.Mordor == true && hunt.Face != null;
            if (tileShown)
                tile.Apply(hunt.Face);
            Stand();
            Layout();
            map.FitRows();
        }

        // UI-428 (TABLETOP_FIGURES_UX, "As built (UI-428)"): the region the figure stands in, where a figure carried onto it
        // is dropped: its region, or on the Mordor track (no region of the map's graph) the region under its step.
        private string StandingRegion()
        {
            if (!mordor.Active)
                return regionId;
            return map.Data.Contours.HitTest(map.Data.MordorTrack.PointAt(Mathf.Max(0, mordor.Step))) ?? "";
        }

        private static DraftToken Token(string region) => new DraftToken(DraftObject.Fellowship, region: region);

        // UI-401: where the figure stands, set when the Fellowship's position changes; the stand layer sorts it in then.
        private void Stand()
        {
            if (mordor.Active)
                map.Stands.Stand(stand, map.Data.MordorTrack.PointAt(Mathf.Max(0, shownStep)));
            else if (map.Data.Shapes.ContainsKey(regionId))
                map.Stands.Stand(stand, map.Stands.Stands(regionId).Fellowship);
            map.Stands.Sort();
        }

        // Positioned by translate only, from the map's corner.
        private static void Pin(VisualElement element)
        {
            element.style.left = 0;
            element.style.top = 0;
        }

        // UI-280: wherever the revealed Fellowship stands, on a region or on the Mordor track, its figure glows while the
        // chosen die can hide it and a click hides it; the Ring in the Fellowship panel stays the same control (UI-232).
        public void OfferHide(bool offered)
        {
            if (HideOffered == offered)
                return;
            HideOffered = offered;
            marker.EnableInClassList(HideClass, offered);
            Glow();
        }

        public void OfferDeclare(bool offered)
        {
            if (DeclareOffered == offered)
                return;
            DeclareOffered = offered;
            marker.EnableInClassList(DeclareClass, offered && !mordor.Active);
            Glow();
        }

        // UI-282: the hidden Fellowship's figure is the Character die's move on the map.
        public void OfferMove(bool offered)
        {
            if (MoveOffered == offered)
                return;
            MoveOffered = offered;
            marker.EnableInClassList(MoveClass, offered && !mordor.Active);
            Glow();
        }

        // UI-282: a figure that offers any of its clicks wears the shared gold ChoiceGlow and pulses, like the other
        // objects the player is asked to click.
        private void Glow()
        {
            var offers = HideOffered || MoveOffered && !mordor.Active || DeclareOffered && !mordor.Active;
            ChoiceGlow.Apply(marker, offers);
            pulse.Run(offers);
            map.Refresh();
        }

        public void Inspect(bool open)
        {
            if (inspected == open)
                return;
            inspected = open;
            map.Refresh();
        }

        public Rect? Bounds(string region) => region == regionId && marker.style.display == DisplayStyle.Flex ? stand.Bounds : null;

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
            // A figure that offers the declaration, the move or the hide is the control for it, so it shows at every zoom.
            // It has no token, so a tight region (UI-401) keeps it at the Detail level too.
            var visible = map.Data.Shapes.ContainsKey(regionId) && (DeclareOffered || HideOffered || MoveOffered || hovered ||
                inspected || map.MarkerVisibility.Detail || map.FiguresIn(regionId) || map.Figures?.Reveals(regionId) == true);
            var scale = map.Stands.Scale.Of(DesignUnit);
            visible = visible && Place(stand.Feet, scale);
            marker.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                return;
            map.ReserveMarker(stand.Bounds);
            RefreshRegionTooltip();
        }

        // Stands the figure on a board point at a scale (UI-401); UI-427: its plaque grows far out, and its visible part
        // reaches down to the grown plaque. Transforms only.
        private bool Place(Vector2 point, float scale)
        {
            var bottom = Mathf.Max(RegionSize.y, FellowshipStateBadge.Bottom(scale));
            stand.Design(RegionFeet, new Rect(0, 0, RegionSize.x, bottom));
            FellowshipStateBadge.Fit(plaque, scale);
            return map.Stands.Place(stand, point, scale);
        }

        private void RefreshRegionTooltip()
        {
            var state = (regionId, hidden, corruptionLevel, DeclareOffered, MoveOffered, HideOffered);
            if (shownTooltip == state)
                return;
            shownTooltip = state;
            marker.tooltip = TooltipText.Lines(RegionTooltip(hidden, corruptionLevel, map.Data.Regions[regionId].Name),
                DeclareOffered ? DeclareHint : "", MoveOffered ? MoveHint : "", HideOffered ? HideHint : "");
        }

        // UI-420 (owner): the figure's tooltip is the Fellowship's, titled as on the Mordor track (MordorTrackState), with
        // the region as its last known position: "Fellowship · Hidden · Rivendell (last known position)\nCorruption 3/12".
        public static string RegionTooltip(bool hidden, int corruption, string region) => TooltipText.Lines(
            TooltipText.Parts("Fellowship", hidden ? "Hidden" : "Revealed", region + " (last known position)"),
            "Corruption " + corruption + "/12");

        // The figure's feet stand on the step; under the step a row holds the drawn tile (zoomed in only) and the
        // Corruption badge, clear of the neighbouring steps along the path. UI-401: the figure keeps its design size and
        // is scaled to the track's size for the zoom level.
        private void LayoutOnTrack(bool reserve)
        {
            marker.style.opacity = 1;
            var detail = map.MarkerVisibility.Detail;
            var point = map.Data.MordorTrack.PointAt(shownStep);
            var visible = Place(point, (detail ? TrackDetail : TrackToken) / FigureHeight);
            var withTile = visible && tileShown && detail;
            marker.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            status.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            tile.Root.style.display = withTile ? DisplayStyle.Flex : DisplayStyle.None;
            if (!visible)
                return;
            var rect = stand.Bounds;
            if (reserve)
                map.ReserveMarker(rect);
            marker.tooltip = mordor.FigureTooltip + (HideOffered ? "\n" + HideHint : "");
            var anchor = map.Camera.ToScreen(point);
            var measured = status.resolvedStyle.width;
            var statusWidth = float.IsNaN(measured) || measured <= 0 ? StatusWidth : measured;
            var tileWidth = withTile ? TileWidth : 0;
            var left = anchor.x - (tileWidth + statusWidth) / 2;
            // Under the step's stone, which UI-283 made twice as tall.
            var top = anchor.y + (detail ? 48 : 28);
            // On the Crack of Doom the row would cover its caption and step 4, so it stands left of the crater.
            if (shownStep >= MordorTrackGeometry.LastStep)
            {
                left = rect.x - tileWidth - statusWidth - 4;
                top = anchor.y - 8;
            }
            tile.Root.style.translate = new Translate(left, top - 6);
            status.style.translate = new Translate(left + tileWidth, top);
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
