using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MapCanvas : VisualElement
    {
        public CameraState Camera { get; } = new CameraState();
        public MapMarkerVisibility MarkerVisibility { get; } = new MapMarkerVisibility();
        public Rect TerrainBounds => new Rect(0, 0, Data.Geometry.Width, Data.Geometry.Height);
        // UI-102: the camera may show the paper and sea around the map, so edge regions clear the HUD.
        public Rect CanvasBounds => Data.Canvas.Around(TerrainBounds);
        public MapRegionNames Names { get; }
        public float FitZoom => Mathf.Max(.01f, CameraState.CoverZoom(TerrainBounds, contentRect));
        public BoardPresentation Data
        {
            get; private set;
        }
        public string Selected { get; private set; } = "";
        public string Hovered { get; private set; } = "";
        public string AttentionRegion { get; private set; } = "";
        public event Action<string> RegionSelected;
        public event Action<DraftToken> ArmySelected;
        // UI-100: a right click without a drag reads a region without touching the planned action.
        public event Action<string> RegionInspected;
        public event Action CameraChanged;
        public MapDraftOverlay DraftOverlay { get; }
        public MapActionOverlay HistoryOverlay { get; }
        public MapRenderer Renderer { get; } = new MapRenderer();
        public MapLegend Legend { get; }
        // UI-218: the political view, kept on this device.
        public MapViewPreference ViewPreference { get; } = new MapViewPreference();
        public bool PoliticalView => Renderer.PoliticalActive;
        public event Action PoliticalViewChanged;
        public MapEdgePan EdgePan { get; }
        public MapFigureMotion Figures { get; }
        // UI-131: ghosts of reinforcements planned by a card or an ability.
        public MapPlannedRecruits PlannedRecruits { get; }
        // UI-212: a card's planned Nazgûl flights at their destinations.
        public MapPlannedRecruits PlannedFlights { get; }
        // UI-294: the companions a card's journey moves, drawn faintly at their destinations.
        public MapPlannedRecruits PlannedJourneys { get; }
        public bool Panning => panning;
        public MapFellowshipMarker FellowshipMarker { get; }
        public MapMordorTrack MordorTrack { get; }
        // The HUD panels over the map; region names hide under them (UI-199: forces do not avoid them).
        public Func<IEnumerable<Rect>> Occlusions { get; set; }
        // UI-305: the player moved the camera (wheel zoom or a right/middle-drag pan), never a programmatic move.
        public event Action Steered;
        private readonly HashSet<(string, Side, bool)> eligibleSources = new HashSet<(string, Side, bool)>();
        // UI-301: the armies a click may choose pulse together in ChoiceGlow's rhythm.
        private readonly ChoicePulse armyPulse;
        private readonly HashSet<string> sourceRegions = new HashSet<string>();
        private readonly HashSet<string> ownerRegions = new HashSet<string>();
        private readonly Dictionary<(string, Side, bool), Marker> markers = new Dictionary<(string, Side, bool), Marker>();
        private readonly List<(string, Side, bool)> obsolete = new List<(string, Side, bool)>();
        private readonly List<Rect> used = new List<Rect>();
        private readonly List<Rect> covered = new List<Rect>();
        // UI-126: the forces of a region in row order (garrison first, then field forces by side).
        private readonly Dictionary<string, List<Marker>> regionMarkers = new Dictionary<string, List<Marker>>();
        private readonly HashSet<(string, Side, bool)> marched = new HashSet<(string, Side, bool)>();
        private Vector2? attentionPoint;
        // UI-127: the planned army drawn faintly at a hovered legal target, with its caption; never interactive.
        private ArmyGhost ghost;
        private Button ghostMarker;
        private MapArmyArtwork ghostArtwork;
        private Label ghostCaption;
        private readonly VisualElement foreground;
        private bool panning;
        private int panPointer = -1;
        private int clickPointer = -1;
        private string pressedRegion = "";
        private Vector2 pressPosition;
        // The press of a right click picks the region; any drag past the tolerance makes it a pan.
        private Vector2 panStart;
        private bool panMoved;
        private string pressedMarker;
        private const float ClickTolerance = 6;
        private bool fitted;
        private Vector2 previous;
        private Vector2 lastPan;
        private float lastZoom;

        public MapCanvas(BoardPresentation data)
        {
            Data = data;
            name = "map";
            focusable = true;
            AddToClassList("map");
            armyPulse = new ChoicePulse(this);
            Renderer.Prepare(data);
            Renderer.SetPoliticalView(ViewPreference.Political);
            Add(Renderer.Root);
            // UI-186: the names lie on the terrain, under the settlements, the markers and every panel.
            Names = new MapRegionNames(data);
            Add(Names.Root);
            foreground = new VisualElement { name = "map-foreground", pickingMode = PickingMode.Ignore };
            foreground.StretchToParentSize();
            foreground.generateVisualContent += Draw;
            Add(foreground);
            RegisterCallback<DetachFromPanelEvent>(_ =>
            {
                CancelPress();
                CancelPan();
                Figures?.Dispose();
                Renderer.Dispose();
            });
            RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (!fitted && contentRect.height > 0)
                {
                    Fit();
                    fitted = true;
                }
                Refresh();
            });
            RegisterCallback<WheelEvent>(e =>
            {
                Camera.ZoomAt(e.localMousePosition, Mathf.Exp(-e.delta.y * .06f));
                Refresh();
                Steered?.Invoke();
                e.StopPropagation();
            });
            RegisterCallback<PointerDownEvent>(PointerDown);
            RegisterCallback<PointerMoveEvent>(PointerMove);
            RegisterCallback<PointerUpEvent>(PointerUp);
            RegisterCallback<PointerCancelEvent>(_ =>
            {
                CancelPress();
                CancelPan();
            });
            RegisterCallback<PointerCaptureOutEvent>(_ =>
            {
                clickPointer = -1;
                pressedRegion = "";
                panning = false;
                panPointer = -1;
                HistoryOverlay.Layout();
                Redraw();
            });
            // UI-154: the track's stones lie under every marker.
            MordorTrack = new MapMordorTrack(this);
            SyncMarkers();
            HistoryOverlay = new MapActionOverlay(this);
            DraftOverlay = new MapDraftOverlay(this);
            FellowshipMarker = new MapFellowshipMarker(this);
            Legend = new MapLegend(this);
            EdgePan = new MapEdgePan(this);
            Figures = new MapFigureMotion(this);
            PlannedRecruits = new MapPlannedRecruits(this);
            PlannedFlights = new MapPlannedRecruits(this, "planned-flight", removable: false);
            PlannedJourneys = new MapPlannedRecruits(this, "planned-journey", removable: false);
        }

        private void PointerDown(PointerDownEvent e)
        {
            CancelPress();
            if (e.button == 1 || e.button == 2)
            {
                panning = true;
                panPointer = e.pointerId;
                panStart = e.localPosition;
                panMoved = false;
                pressedMarker = MarkerRegion(e.target as VisualElement);
                HistoryOverlay?.Layout();
                previous = e.localPosition;
                this.CapturePointer(e.pointerId);
                e.StopPropagation();
            }
            else if (e.button == 0)
            {
                var id = HitTest(e.localPosition);
                if (id != null)
                {
                    clickPointer = e.pointerId;
                    pressedRegion = id;
                    pressPosition = e.position;
                    this.CapturePointer(e.pointerId);
                    Redraw();
                }
            }
        }

        private void PointerUp(PointerUpEvent e)
        {
            if (e.button == 0 && e.pointerId == clickPointer)
            {
                var region = pressedRegion;
                var local = this.WorldToLocal(e.position);
                var sameRegion = contentRect.Contains(local) && OnMap(panel?.Pick(e.position)) && HitTest(local) == region;
                CancelPress();
                if (sameRegion)
                    Select(region);
                e.StopPropagation();
                return;
            }
            if (panning && e.pointerId == panPointer)
            {
                var inspected = e.button == 1 && !panMoved ? pressedMarker ?? HitTest(panStart) : null;
                CancelPan();
                if (inspected != null)
                    RegionInspected?.Invoke(inspected);
                e.StopPropagation();
            }
        }

        // UI-105: a terrain press closes the open region panel, so the markers lay out again and one may come to
        // rest under the pointer. The press began on the terrain (markers stop their own presses), so a release
        // over the map's own marker still chooses the region beneath; a panel above the map does not.
        private bool OnMap(VisualElement picked) => picked == this || picked != null && Contains(picked);

        private void PointerMove(PointerMoveEvent e)
        {
            if (e.pointerId == clickPointer && Vector2.Distance(pressPosition, e.position) >= ClickTolerance)
                CancelPress();
            if (!panning || e.pointerId != panPointer)
                return;
            Vector2 now = e.localPosition;
            panMoved |= Vector2.Distance(panStart, now) >= ClickTolerance;
            Camera.Pan += now - previous;
            previous = now;
            Refresh();
            Steered?.Invoke();
        }

        public bool CancelPan()
        {
            if (!panning)
                return false;
            var captured = panPointer;
            panning = false;
            panPointer = -1;
            if (captured >= 0 && this.HasPointerCapture(captured))
                this.ReleasePointer(captured);
            HistoryOverlay?.Layout();
            Redraw();
            return true;
        }

        public void CancelPress()
        {
            var captured = clickPointer;
            var wasPressed = pressedRegion.Length > 0;
            clickPointer = -1;
            pressedRegion = "";
            if (captured >= 0 && this.HasPointerCapture(captured))
                this.ReleasePointer(captured);
            if (wasPressed)
                Redraw();
        }

        // UI-114: the terrain region held under the pointer, until it is released or the press is cancelled.
        public string PressedRegion => pressedRegion;

        public void Fit()
        {
            var bounds = TerrainBounds;
            Camera.Zoom = FitZoom;
            Camera.Pan = contentRect.center - bounds.center * Camera.Zoom;
            Refresh();
        }

        public void SetAttention(string region)
        {
            var next = region != null && Data.Shapes.ContainsKey(region) ? region : "";
            if (AttentionRegion == next)
                return;
            AttentionRegion = next;
            Redraw();
        }

        public void SetHovered(string region)
        {
            var next = region != null && Data.Shapes.ContainsKey(region) ? region : "";
            if (Hovered == next)
                return;
            Hovered = next;
            UpdateGhost();
            Redraw();
        }

        public void SetGhost(ArmyGhost next)
        {
            ghost = next;
            UpdateGhost();
        }

        private void UpdateGhost()
        {
            var caption = "";
            var shown = ghost != null && contentRect.width > 0 && Hovered.Length > 0 &&
                ghost.Captions.TryGetValue(Hovered, out caption) && Data.Shapes.ContainsKey(Hovered);
            if (!shown)
            {
                if (ghostMarker != null)
                    ghostMarker.style.display = ghostCaption.style.display = DisplayStyle.None;
                return;
            }
            EnsureGhost();
            ghostMarker.EnableInClassList("fp", ghost.Army.Side == Side.FreePeoples);
            ghostMarker.EnableInClassList("sh", ghost.Army.Side == Side.Shadow);
            ghostArtwork.Apply(ghost.Army);
            ghostArtwork.Layout(true);
            var rect = GhostRect(Hovered);
            ghostMarker.style.left = rect.x;
            ghostMarker.style.top = rect.y;
            ghostCaption.text = caption;
            ghostCaption.style.left = rect.x - 22;
            ghostCaption.style.top = rect.yMax + 2;
            ghostMarker.style.display = ghostCaption.style.display = DisplayStyle.Flex;
            ghostMarker.BringToFront();
            ghostCaption.BringToFront();
        }

        // Beside the target's own forces, or where its row would stand.
        internal Rect GhostRect(string region)
        {
            var size = MapArmyArtwork.Size(false, true);
            if (regionMarkers.TryGetValue(region, out var row) && row.Count > 0 && row[0].Visible)
            {
                var right = row[row.Count - 1].Bounds;
                return LocalMarkerRect(new Vector2(right.xMax + 4 + size.x / 2, right.yMax - size.y / 2), size);
            }
            var shape = Data.Shapes[region];
            var anchor = Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1]));
            return LocalMarkerRect(RowCentre(0, anchor, size, Data.Regions[region].Settlement != null), size);
        }

        private void EnsureGhost()
        {
            if (ghostMarker != null)
                return;
            ghostMarker = new Button { name = "army-ghost", focusable = false, pickingMode = PickingMode.Ignore };
            ghostMarker.AddToClassList("army");
            ghostMarker.AddToClassList("army-ghost");
            ghostArtwork = new MapArmyArtwork(ghostMarker);
            ghostCaption = new Label { name = "army-ghost-caption", pickingMode = PickingMode.Ignore };
            ghostCaption.AddToClassList("army-ghost-caption");
            Add(ghostMarker);
            Add(ghostCaption);
        }

        public void Focus(string id)
        {
            if (!Data.Shapes.TryGetValue(id, out var shape))
                return;
            Camera.Zoom = Mathf.Max(1, FitZoom);
            Camera.Pan = contentRect.center - new Vector2(shape.Anchor[0], shape.Anchor[1]) * Camera.Zoom;
            Selected = id;
            Refresh();
        }

        // UI-155: centres the map on a board point at least at the zoom where the track shows its numbers.
        public void FocusOn(Vector2 point, float zoom)
        {
            Camera.Zoom = Mathf.Max(zoom, FitZoom);
            Camera.Pan = contentRect.center - point * Camera.Zoom;
            Refresh();
        }

        public void FocusMordorTrack() =>
            FocusOn(Data.MordorTrack.Bounds.center, Mathf.Max(Camera.Zoom, FitZoom * MapMarkerVisibility.FiguresFrom * 1.1f));

        // UI-134: brings regions into view without selecting them. Nothing moves while every anchor is visible;
        // otherwise the camera centres them and zooms out only as far as needed to hold them all.
        public void Reveal(IEnumerable<string> regions)
        {
            const float margin = 72;
            var anchors = new List<Vector2>();
            foreach (var id in regions)
                if (Data.Shapes.TryGetValue(id, out var shape))
                    anchors.Add(new Vector2(shape.Anchor[0], shape.Anchor[1]));
            if (anchors.Count == 0 || contentRect.width <= 2 * margin || contentRect.height <= 2 * margin)
                return;
            var view = new Rect(margin, margin, contentRect.width - 2 * margin, contentRect.height - 2 * margin);
            if (anchors.TrueForAll(anchor => view.Contains(Camera.ToScreen(anchor))))
                return;
            var min = anchors[0];
            var max = anchors[0];
            foreach (var anchor in anchors)
            {
                min = Vector2.Min(min, anchor);
                max = Vector2.Max(max, anchor);
            }
            var span = Vector2.Max(max - min, Vector2.one);
            var holding = Mathf.Min(view.width / span.x, view.height / span.y);
            Camera.Zoom = Mathf.Max(FitZoom, Mathf.Min(Camera.Zoom, holding));
            Camera.Pan = contentRect.center - (min + max) / 2 * Camera.Zoom;
            Refresh();
        }

        public void Select(string id)
        {
            Selected = id;
            RegionSelected?.Invoke(id);
            Refresh();
        }
        public void SetPoliticalView(bool active)
        {
            if (PoliticalView == active)
                return;
            Renderer.SetPoliticalView(active);
            ViewPreference.SetPolitical(active);
            PoliticalViewChanged?.Invoke();
        }

        public void UpdateData(BoardPresentation data)
        {
            CancelPress();
            Data = data;
            Renderer.UpdateMetadata(data);
            SyncMarkers();
            Refresh();
        }
        public void HighlightSources(IEnumerable<ArmyOption> armies, Side side)
        {
            eligibleSources.Clear();
            eligibleSources.UnionWith(armies.Select(a => (a.RegionId, side, a.Garrison)));
            sourceRegions.Clear();
            foreach (var source in eligibleSources)
                sourceRegions.Add(source.Item1);
            foreach (var marker in markers.Values)
                marker.Button.EnableInClassList("eligible-army", eligibleSources.Contains((marker.Army.RegionId, marker.Army.Side, marker.Army.Garrison)));
            armyPulse.Run(eligibleSources.Count > 0);
            Redraw();
        }

        // UI-212: the source whose Nazgûl a destination click flies; null clears it.
        public void MarkArmed(ArmyOption source, Side side)
        {
            foreach (var marker in markers.Values)
                marker.Button.EnableInClassList("armed-force", source != null && marker.Army.RegionId == source.RegionId &&
                    marker.Army.Side == side && marker.Army.Garrison == source.Garrison);
        }

        public void InspectRegion(string region)
        {
            if (MarkerVisibility.InspectedRegion == region)
                return;
            MarkerVisibility.InspectedRegion = region ?? "";
            Refresh();
        }
        public void Refresh()
        {
            Camera.Constrain(CanvasBounds, contentRect);
            MarkerVisibility.UpdateZoom(Camera.Zoom / FitZoom);
            if (lastPan != Camera.Pan || lastZoom != Camera.Zoom)
            {
                lastPan = Camera.Pan;
                lastZoom = Camera.Zoom;
                CameraChanged?.Invoke();
            }
            BeginLayout();
            MordorTrack?.Layout();
            FellowshipMarker?.Layout();
            LayoutMarkers();
            UpdateGhost();
            PlannedRecruits?.Layout();
            PlannedFlights?.Layout();
            PlannedJourneys?.Layout();
            HistoryOverlay?.Layout();
            Figures?.Layout();
            covered.AddRange(used);
            Names.Layout(Camera, contentRect, covered);
            Redraw();
        }

        // UI-127: forces whose every figure already marched in this action.
        public void ShowMarched(IEnumerable<(string, Side, bool)> forces)
        {
            marched.Clear();
            marched.UnionWith(forces);
            foreach (var marker in markers.Values)
                marker.Button.EnableInClassList(MapArmyArtwork.MovedClass,
                    marched.Contains((marker.Army.RegionId, marker.Army.Side, marker.Army.Garrison)));
        }

        public void HighlightOwners(IEnumerable<string> regions)
        {
            ownerRegions.Clear();
            ownerRegions.UnionWith(regions);
            foreach (var marker in markers.Values)
                marker.Button.EnableInClassList("ability-owner", ownerRegions.Contains(marker.Army.RegionId));
            Redraw();
        }

        internal void ShowDraftSources(PlayerView view, TurnDraft draft)
        {
            var leaving = draft.Payload.Moves.SelectMany(m => m.UnitIds).ToHashSet();
            var characters = draft.Payload.Moves.SelectMany(m => m.CharacterIds).ToHashSet();
            var travellers = draft.Payload.CompanionMoves.SelectMany(m => m.Ids).ToHashSet();
            leaving.UnionWith(travellers);
            characters.UnionWith(travellers);
            var selected = view.Units.Where(u => leaving.Contains(u.Id)).Select(u => (u.Location, u.Side, u.Garrison))
                .Concat(view.Characters.Where(c => characters.Contains(c.Id)).Select(c => (c.Location, c.Side, c.Garrison))).ToHashSet();
            foreach (var marker in markers.Values)
            {
                marker.Selected = selected.Contains((marker.Army.RegionId, marker.Army.Side, marker.Army.Garrison));
                marker.Button.EnableInClassList("selected-force", marker.Selected);
            }
        }

        // A marker may sit off its region's contour; a press on it reads its own region.
        private string MarkerRegion(VisualElement target)
        {
            for (var element = target; element != null && element != this; element = element.parent)
                if (element.userData is DraftToken { Object: DraftObject.Army } token)
                    return token.Id;
            return null;
        }

        public string HitTest(Vector2 local)
        {
            var point = Camera.ToBoard(local);
            return Data.Contours.HitTest(point);
        }

        private void SyncMarkers()
        {
            obsolete.Clear();
            obsolete.AddRange(markers.Keys);
            foreach (var army in Data.Armies)
            {
                var key = (army.RegionId, army.Side, army.Garrison);
                obsolete.Remove(key);
                if (!markers.TryGetValue(key, out var marker))
                {
                    marker = CreateMarker(army);
                    markers.Add(key, marker);
                    Add(marker.Button);
                }
                marker.Army = army;
                marker.Artwork.Apply(army);
                marker.Button.EnableInClassList(MapArmyArtwork.MovedClass, marched.Contains(key));
                marker.Button.tooltip = ArmyMarkerText.Describe(army, Data.Regions[army.RegionId].Name);
            }
            foreach (var key in obsolete)
            {
                markers[key].Button.RemoveFromHierarchy();
                markers.Remove(key);
            }
            regionMarkers.Clear();
            foreach (var group in markers.Values.GroupBy(m => m.Army.RegionId))
                regionMarkers.Add(group.Key, group.OrderBy(m => m.Army.Garrison ? 0 : 1).ThenBy(m => m.Army.Side).ToList());
        }

        private Marker CreateMarker(ArmyMarker army)
        {
            var regionId = army.RegionId;
            var button = new Button(() => ArmySelected?.Invoke(MarkerToken(army)));
            button.name = "army-" + regionId + "-" + army.Side + (army.Garrison ? "-garrison" : "");
            button.userData = MarkerToken(army);
            button.AddToClassList("army");
            button.AddToClassList(RuntimeTooltip.RichHoverClass);
            button.AddToClassList(army.Side == Side.FreePeoples ? "fp" : "sh");
            if (army.Garrison)
                button.AddToClassList("garrison");
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
            return new Marker { Button = button, Army = army, Artwork = new MapArmyArtwork(button) };
        }

        private static DraftToken MarkerToken(ArmyMarker army) => new DraftToken(
            DraftObject.Army, army.RegionId, side: army.Side, garrison: army.Garrison);

        private void BeginLayout()
        {
            used.Clear();
            covered.Clear();
            if (Occlusions != null)
                covered.AddRange(Occlusions().Where(r => r.Overlaps(contentRect)));
        }

        public IEnumerable<Rect> MarkerBounds(string region)
        {
            foreach (var marker in markers.Values)
                if (marker.Army.RegionId == region && marker.Visible)
                    yield return marker.Bounds;
            if (FellowshipMarker.Bounds(region) is Rect fellowship)
                yield return fellowship;
        }

        // UI-126: every force is shown. A region's forces stand in one row just above its settlement symbol (or
        // centred on a region without one), as figures on a base when zoomed in and as tokens otherwise. A row that
        // would cover a placed marker or panel falls back to tokens, then tries the other sides of the anchor.
        private void LayoutMarkers()
        {
            attentionPoint = null;
            if (contentRect.width <= 0)
                return;
            foreach (var entry in regionMarkers)
                LayoutRegion(entry.Key, entry.Value);
        }

        private void LayoutRegion(string region, List<Marker> row)
        {
            var shape = Data.Shapes[region];
            var anchor = Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1]));
            var visible = NearView(anchor);
            foreach (var marker in row)
                ShowMarker(marker, visible);
            if (!visible)
                return;
            var settled = Data.Regions[region].Settlement != null;
            var focused = Focused(region, row);
            var figures = focused || MarkerVisibility.Detail;
            var bounds = PlaceRow(row, anchor, settled, figures);
            // Only the zoom gives way to crowding; an inspected or selected force keeps its figures.
            if (figures && !focused && OverlapsPlaced(bounds))
            {
                figures = false;
                bounds = PlaceRow(row, anchor, settled, false);
            }
            used.Add(bounds);
            ApplyRow(row, bounds, figures);
            if (region == AttentionRegion)
                attentionPoint = new Vector2(bounds.xMax + 12, bounds.center.y);
        }

        // The region shows figures for itself: inspected by a click, holding a selected force, or just arrived.
        private bool Focused(string region, List<Marker> row)
        {
            if (Figures?.Reveals(region) == true || region == MarkerVisibility.InspectedRegion)
                return true;
            foreach (var marker in row)
                if (marker.Selected)
                    return true;
            return false;
        }

        private void ShowMarker(Marker marker, bool visible)
        {
            marker.Visible = visible;
            marker.Button.style.opacity = Figures?.Arriving(marker.Army.RegionId, marker.Army.Side, marker.Army.Garrison) == true ? .25f : 1f;
            if (!visible && marker.Button.panel?.focusController.focusedElement == marker.Button)
                Focus();
            marker.Button.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // UI-199: a row gives way only to the forces placed before it, never to the HUD or the open region panel, so it
        // keeps its place on the map while the view scrolls.
        private Rect PlaceRow(List<Marker> row, Vector2 anchor, bool settled, bool figures)
        {
            var size = RowSize(row, figures);
            for (var place = 0; place < 4; place++)
            {
                var rect = LocalMarkerRect(RowCentre(place, anchor, size, settled), size);
                if (!OverlapsPlaced(rect))
                    return rect;
            }
            return LocalMarkerRect(RowCentre(0, anchor, size, settled), size);
        }

        // Above the settlement symbol first, then right of it, left of it and below the effects label. UI-195: the row
        // stands clear of the largest gem, a Stronghold in its gold setting.
        private const float SettlementClearance = 12;

        private static Vector2 RowCentre(int place, Vector2 anchor, Vector2 size, bool settled) => place switch
        {
            0 => settled ? anchor + new Vector2(0, -SettlementClearance - size.y / 2) : anchor,
            1 => anchor + new Vector2(14 + size.x / 2, 0),
            2 => anchor - new Vector2(14 + size.x / 2, 0),
            _ => anchor + new Vector2(0, 30 + size.y / 2)
        };

        private static Vector2 RowSize(List<Marker> row, bool figures)
        {
            var gap = figures ? 4 : 2;
            float width = -gap;
            var height = 0f;
            foreach (var marker in row)
            {
                var size = MapArmyArtwork.Size(marker.Army.Garrison, figures);
                width += size.x + gap;
                height = Mathf.Max(height, size.y);
            }
            return new Vector2(width, height);
        }

        private static void ApplyRow(List<Marker> row, Rect bounds, bool figures)
        {
            var gap = figures ? 4 : 2;
            var left = bounds.x;
            foreach (var marker in row)
            {
                var size = MapArmyArtwork.Size(marker.Army.Garrison, figures);
                var rect = new Rect(left, bounds.yMax - size.y, size.x, size.y);
                left += size.x + gap;
                marker.Artwork.Layout(figures);
                marker.Bounds = rect;
                marker.Button.style.left = rect.x;
                marker.Button.style.top = rect.y;
            }
        }

        private bool OverlapsPlaced(Rect rect)
        {
            foreach (var other in used)
                if (other.Overlaps(rect))
                    return true;
            return false;
        }

        // UI-199: a marker stands where it belongs and the map's edge cuts it (the map clips its overflow); it never
        // moves inward. It shows while its region's point is within reach of the view, so it slides in and out with
        // the map instead of appearing or vanishing whole.
        public const float MarkerReach = 160;

        internal Rect LocalMarkerRect(Vector2 centre, Vector2 size) => new Rect(centre - size / 2, size);

        internal bool NearView(Vector2 point) => new Rect(contentRect.x - MarkerReach, contentRect.y - MarkerReach,
            contentRect.width + 2 * MarkerReach, contentRect.height + 2 * MarkerReach).Contains(point);

        internal void ReserveMarker(Rect bounds) => used.Add(bounds);

        public void Redraw()
        {
            Renderer.Update(Data, Camera, Selected, sourceRegions, ownerRegions, DraftOverlay, Hovered, pressedRegion);
            foreground?.MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
            MapSettlementVisual.Draw(painter, Data, Camera, contentRect);
            if (attentionPoint is Vector2 beside)
                DrawAttention(painter, beside);
            else if (Data.Shapes.TryGetValue(AttentionRegion, out var attention))
                DrawAttention(painter, Camera.ToScreen(new Vector2(attention.Anchor[0], attention.Anchor[1])) + new Vector2(66, -15));
            HistoryOverlay?.Draw(painter);
        }

        // UI-126: beside the region's marker row when it has forces, otherwise at the old offset from the anchor.
        private static void DrawAttention(Painter2D painter, Vector2 anchor)
        {
            painter.strokeColor = new Color(1f, .83f, .42f);
            painter.lineWidth = 2;
            painter.BeginPath();
            painter.MoveTo(anchor + new Vector2(0, -10));
            painter.LineTo(anchor + new Vector2(10, 0));
            painter.LineTo(anchor + new Vector2(0, 10));
            painter.LineTo(anchor + new Vector2(-10, 0));
            painter.ClosePath();
            painter.Stroke();
            painter.BeginPath();
            painter.MoveTo(anchor + new Vector2(0, -5));
            painter.LineTo(anchor + new Vector2(0, 2));
            painter.MoveTo(anchor + new Vector2(0, 5));
            painter.LineTo(anchor + new Vector2(0, 6));
            painter.Stroke();
        }

        private sealed class Marker
        {
            public Button Button;
            public MapArmyArtwork Artwork;
            public ArmyMarker Army;
            public bool Selected;
            public bool Visible;
            public Rect Bounds;
        }
    }
}
