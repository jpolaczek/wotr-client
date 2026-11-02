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
        // UI-405: the region under a carried figure ("" over no region); null while nothing is carried.
        public string DragTarget { get; private set; }
        public string AttentionRegion { get; private set; } = "";
        public event Action<string> RegionSelected;
        public event Action<DraftToken> ArmySelected;
        // UI-402: a character's own miniature was clicked (DraftObject.Miniature: its id, side, garrison and region).
        public event Action<DraftToken> MiniatureSelected;
        // UI-402: which figures a click may choose, shared with the region panel's chips.
        public FigureChoices Choices { get; } = new FigureChoices();
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
        public MapSovereignMarkers SovereignMarkers { get; }
        // UI-212: a card's planned Nazgûl flights at their destinations.
        public MapPlannedRecruits PlannedFlights { get; }
        // UI-294: the companions a card's journey moves, drawn faintly at their destinations.
        public MapPlannedRecruits PlannedJourneys { get; }
        public bool Panning => panning;
        public MapFellowshipMarker FellowshipMarker { get; }
        public MapMordorTrack MordorTrack { get; }
        // UI-406: the settlements' gems, each painted once and moved with the map by a transform.
        public MapSettlementGems Settlements { get; }
        // The HUD panels over the map; region names hide under them (UI-199: forces do not avoid them).
        public Func<IEnumerable<Rect>> Occlusions { get; set; }
        // UI-305: the player moved the camera (wheel zoom or a right/middle-drag pan), never a programmatic move.
        public event Action Steered;
        // UI-392: the camera's glide to a chosen region.
        private IVisualElementScheduledItem glide;
        private readonly HashSet<(string, Side, bool)> eligibleSources = new HashSet<(string, Side, bool)>();
        public const string SovereignTargetClass = "sovereign-target";
        private Func<string, string> sovereignHint = _ => null;
        // UI-431: what a click on a lit force does now (a card's choice made on it), by region; its tooltip leads with it.
        private IReadOnlyDictionary<string, string> forceOffers = new Dictionary<string, string>();
        // UI-301: the armies a click may choose pulse together in ChoiceGlow's rhythm.
        private readonly ChoicePulse armyPulse;
        private readonly HashSet<string> sourceRegions = new HashSet<string>();
        private readonly HashSet<string> ownerRegions = new HashSet<string>();
        // UI-441: the characters with a skill usable now, and why; an Initial king among them glows on the force he leads.
        private IReadOnlyDictionary<string, string> skillLines = new Dictionary<string, string>();
        private readonly Dictionary<(string, Side, bool), MapArmyStand> markers = new Dictionary<(string, Side, bool), MapArmyStand>();
        private readonly List<(string, Side, bool)> obsolete = new List<(string, Side, bool)>();
        // UI-334: armies that appeared or grew in the last sync; they flare and emerge on the next frame unless a flight
        // brings them (it has its own arrival). The first sync with armies only sets the state; a quiet update too.
        private readonly List<(MapArmyStand Marker, int? Before)> emerging = new List<(MapArmyStand, int?)>();
        // UI-338: markers whose figures all left (they count as gone at once: no lookup, no pointer) and armies that
        // shrank; on the next frame a flight that carries their figures away spares them the fade.
        private readonly List<MapArmyStand> departing = new List<MapArmyStand>();
        private readonly List<MapArmyStand> shrinking = new List<MapArmyStand>();
        private readonly List<MapArmyStand> leaving = new List<MapArmyStand>();
        private bool changesScheduled;
        private bool armiesShown;
        public bool Quiet { get; set; }
        private readonly List<Rect> used = new List<Rect>();
        private readonly List<Rect> covered = new List<Rect>();
        // UI-126: the forces of each region.
        private readonly Dictionary<string, List<MapArmyStand>> regionMarkers = new Dictionary<string, List<MapArmyStand>>();
        private readonly HashSet<(string, Side, bool)> marched = new HashSet<(string, Side, bool)>();
        private Vector2? attentionPoint;
        // UI-127, UI-403: what a click on each legal target does with the unsent selection; on a hovered one the landing
        // mark lies on the prospective place and the caption hangs above the lifted figure. Never interactive.
        private ArmyGhost landingCaptions;
        // UI-401: the places, sizes, levels and depth of everything standing on the map.
        public MapStandLayout Stands { get; }
        // UI-403: the figures lifted off the table, and where they would land.
        internal MapFigureHand Hand { get; }
        internal MapLandingMark Landing { get; }
        // UI-402: every character's own miniature.
        internal MapCharacterFigures Characters { get; }
        // UI-404: the stand points flights leave from and land on.
        internal FlightPlaces FlightPlaces { get; }
        private readonly Func<string, bool> focusedRegion;
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
            focusedRegion = Focused;
            Renderer.Prepare(data);
            Renderer.SetPoliticalView(ViewPreference.Political);
            Add(Renderer.Root);
            // UI-186: the names lie on the terrain, under the settlements, the markers and every panel.
            Names = new MapRegionNames(data);
            Add(Names.Root);
            Settlements = new MapSettlementGems(this);
            Settlements.Apply(data);
            Add(Settlements.Layer);
            Names.Gem = Settlements.Bounds;
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
                glide?.Pause();
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
            // UI-401: one y-sorted layer for everything that stands on the map, under the overlays.
            Stands = new MapStandLayout(this);
            Hand = new MapFigureHand(this);
            Hand.Changed += ApplyLiftMask;
            Landing = new MapLandingMark(this, Stands.Layer, Hand.Layer);
            Characters = new MapCharacterFigures(this, figure => MiniatureSelected?.Invoke(figure));
            FlightPlaces = new FlightPlaces(this);
            Choices.Changed += ShowChoices;
            SyncMarkers();
            HistoryOverlay = new MapActionOverlay(this);
            DraftOverlay = new MapDraftOverlay(this);
            FellowshipMarker = new MapFellowshipMarker(this);
            Legend = new MapLegend(this);
            EdgePan = new MapEdgePan(this);
            Figures = new MapFigureMotion(this);
            Figures.Lapsed += ReturnLapsed;
            PlannedRecruits = new MapPlannedRecruits(this);
            PlannedFlights = new MapPlannedRecruits(this, "planned-flight", removable: false);
            SovereignMarkers = new MapSovereignMarkers(this);
            SovereignMarkers.Apply(Data);
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
            glide?.Pause();
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
            UpdateLanding();
            Redraw();
        }

        // UI-403: the captions of the unsent selection's legal targets (ArmyGhost); null when nothing is chosen.
        public void SetLanding(ArmyGhost captions)
        {
            landingCaptions = captions;
            UpdateLanding();
        }

        // UI-405 (TABLETOP_FIGURES_UX, "Drag"): the region under a carried figure lights (a legal target in its own colour,
        // more strongly), and on a legal target the landing mark and the caption show as on a hover; null ends the drag.
        public void SetDragTarget(string region)
        {
            var next = region == null ? null : Data.Shapes.ContainsKey(region) ? region : "";
            if (DragTarget == next)
                return;
            DragTarget = next;
            UpdateLanding();
            Redraw();
        }

        // Where the landing mark looks: the carried figure's region during a drag, otherwise the hovered one.
        private string LandingRegion => DragTarget ?? Hovered;

        // On a hovered legal target: the landing mark on the prospective place (an army's, or a lone character's next
        // place), green for a move and red for an attack, and the caption above the lifted figure.
        private void UpdateLanding()
        {
            var caption = "";
            var region = LandingRegion;
            var shown = landingCaptions != null && contentRect.width > 0 && region.Length > 0 &&
                landingCaptions.Captions.TryGetValue(region, out caption) && Data.Shapes.ContainsKey(region);
            if (!shown)
            {
                Landing.Hide();
                return;
            }
            var units = Hand.HoldsUnits || !Hand.Pieces.Any();
            var place = units ? LandingPlace(region) : Characters.NextPlace(region);
            var scale = Stands.FigureScale * (units ? 1 : MapLandingMark.CharacterShare);
            Landing.Show(Camera.ToScreen(place), scale, landingCaptions.Attacks.Contains(region), caption, Hand.RaisedBounds,
                contentRect);
        }

        // UI-405: the name of the stand a figure's press lifts from (an army's marker, a character's miniature); "" for
        // anything else.
        internal string LiftKey(DraftToken figure)
        {
            if (figure?.Object == DraftObject.Miniature)
                return MapCharacterFigures.Key(figure.Id);
            return figure?.Object == DraftObject.Army && figure.Side is Side side &&
                markers.TryGetValue((figure.Id, side, figure.Garrison), out var marker) ? marker.Button.name : "";
        }

        // UI-405: whether the figure's own raised piece holds figures of the unsent move.
        public bool Lifts(DraftToken figure) => Hand.Raises(LiftKey(figure));

        // UI-405: the pointer (map-local) takes up the raised figures of the unsent move; false when none is raised.
        public bool GrabFigures(Vector2 pointer) => Hand.Grab(pointer);

        // UI-405: the carried figures follow the pointer (map-local); the caption hangs above them.
        public void CarryFigures(Vector2 pointer)
        {
            Hand.Follow(pointer);
            UpdateLanding();
        }

        // UI-405: the carried figures rest where they were dropped; returns the figures they hold.
        public HashSet<string> DropFigures() => Hand.Release();

        // UI-405: after a drop, raised figures away from their place that no flight takes glide home over it.
        public void HomeFigures() => Hand.Home();

        // UI-401: an army arrives on the region's field when no field force stands there, otherwise on its planned place
        // beside them.
        internal Vector2 LandingPlace(string region)
        {
            var stands = Stands.Stands(region);
            var occupied = regionMarkers.TryGetValue(region, out var forces) && forces.Any(force => !force.Army.Garrison);
            return occupied ? stands.Planned : stands.Field;
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

        // UI-392 (owner): the camera glides to a region, centring it without a zoom change (CameraTravel, 450-750 ms, eased).
        // Under Reduced motion it moves at once; the player's own pan or zoom stops the glide.
        public void Glide(string id, bool reducedMotion)
        {
            if (!Data.Shapes.TryGetValue(id, out var shape))
                return;
            glide?.Pause();
            var target = contentRect.center - new Vector2(shape.Anchor[0], shape.Anchor[1]) * Camera.Zoom;
            if (reducedMotion)
            {
                Camera.Pan = target;
                Refresh();
                return;
            }
            var travel = new CameraTravel(Camera.Zoom, Camera.Pan, Camera.Zoom, target, contentRect.center);
            var started = Time.realtimeSinceStartupAsDouble;
            glide = schedule.Execute(() =>
            {
                var fraction = (Time.realtimeSinceStartupAsDouble - started) / travel.Duration;
                Camera.Pan = travel.At(fraction).Pan;
                Refresh();
                if (fraction >= 1)
                    glide.Pause();
            }).Every(16);
        }

        public bool Gliding => glide?.isActive == true;

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
        // otherwise the camera centres them and zooms out only as far as needed to hold them all. UI-434: visible means in
        // the free map — the map minus what covers it (the top bar, the console, open panels and windows), as a
        // presentation frames its cues (SafeMapArea) — so a lit object under the console counts as off view.
        public void Reveal(IEnumerable<string> regions)
        {
            const float margin = 72;
            var anchors = new List<Vector2>();
            foreach (var id in regions)
                if (Data.Shapes.TryGetValue(id, out var shape))
                    anchors.Add(new Vector2(shape.Anchor[0], shape.Anchor[1]));
            var free = FreeArea();
            if (anchors.Count == 0 || free.width <= 2 * margin || free.height <= 2 * margin)
                return;
            var view = new Rect(free.xMin + margin, free.yMin + margin, free.width - 2 * margin, free.height - 2 * margin);
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
            Camera.Pan = free.center - (min + max) / 2 * Camera.Zoom;
            Refresh();
        }

        // The largest part of the map that nothing covers, in map-local points (SafeMapArea).
        private Rect FreeArea() =>
            SafeMapArea.Largest(new Rect(Vector2.zero, contentRect.size), Occlusions?.Invoke() ?? Enumerable.Empty<Rect>());

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
            Settlements.Apply(data);
            SyncMarkers();
            SovereignMarkers.Apply(data);
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
            {
                marker.Button.EnableInClassList("eligible-army", eligibleSources.Contains((marker.Army.RegionId, marker.Army.Side, marker.Army.Garrison)));
                ApplySovereignHint(marker);
            }
            RunArmyPulse();
            Redraw();
        }

        // UI-374: the lit sources and the owners of a figure's ability pulse together; UI-402: so do the choosable
        // characters.
        private void RunArmyPulse() => armyPulse.Run(eligibleSources.Count > 0 || ownerRegions.Count > 0 || Characters.AnyLit ||
            markers.Values.Any(marker => SkillKing(marker) != null));

        // UI-402: a context that lights a figure's chip lights its miniature too.
        private void ShowChoices()
        {
            Characters.ShowChoices(Choices);
            RunArmyPulse();
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
            Stands.Measure();
            Settlements.Layout(Stands.Scale.Gem);
            MordorTrack?.Layout();
            FellowshipMarker?.Layout();
            LayoutMarkers();
            Hand.Layout();
            UpdateLanding();
            PlannedRecruits?.Layout();
            PlannedFlights?.Layout();
            SovereignMarkers?.Layout();
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

        // UI-374: the regions whose army holds a figure whose ability or order a click uses; UI-402: the characters among
        // those figures (by id) light on their own miniatures.
        public void HighlightOwners(IEnumerable<string> regions, IEnumerable<string> figures = null)
        {
            ownerRegions.Clear();
            ownerRegions.UnionWith(regions);
            foreach (var marker in markers.Values)
                ApplyOwner(marker);
            Characters.HighlightOwners(figures ?? Enumerable.Empty<string>());
            RunArmyPulse();
            Redraw();
        }

        // UI-441 (owner): "the character always lights up whenever its skill can be used now": its miniature glows (the
        // ability-owner glow) and its tooltip says why; an Initial king, drawn as a crown on the force he leads, lights that
        // force. lines: the characters' ids and why each glows.
        public void LightSkills(IReadOnlyDictionary<string, string> lines)
        {
            var next = lines ?? new Dictionary<string, string>();
            if (next.Count == skillLines.Count && next.All(pair => skillLines.TryGetValue(pair.Key, out var line) && line == pair.Value))
                return;
            skillLines = next;
            Characters.LightSkills(skillLines);
            foreach (var marker in markers.Values)
            {
                ApplyOwner(marker);
                ApplySovereignHint(marker);
            }
            RunArmyPulse();
        }

        // The Initial king with a usable skill this force holds, who has no miniature of his own; null.
        private string SkillKing(MapArmyStand marker) => skillLines.Count == 0 ? null
            : marker.Army.SovereignIds.FirstOrDefault(id => skillLines.ContainsKey(id) && Characters.Find(id) == null);

        private void ApplyOwner(MapArmyStand marker) =>
            marker.Button.EnableInClassList("ability-owner", ownerRegions.Contains(marker.Army.RegionId) || SkillKing(marker) != null);

        // The forces whose units the unsent moves take, and (UI-402) the characters they take, each on its own miniature.
        internal void ShowDraftSources(PlayerView view, TurnDraft draft)
        {
            var travellers = draft.Payload.CompanionMoves.SelectMany(m => m.Ids).ToList();
            var leaving = draft.Payload.Moves.SelectMany(m => m.UnitIds).Concat(travellers).ToHashSet();
            var characters = draft.Payload.Moves.SelectMany(m => m.CharacterIds).Concat(travellers).ToHashSet();
            var selected = view.Units.Where(u => leaving.Contains(u.Id)).Select(u => (u.Location, u.Side, u.Garrison)).ToHashSet();
            foreach (var marker in markers.Values)
            {
                marker.Selected = selected.Contains((marker.Army.RegionId, marker.Army.Side, marker.Army.Garrison));
                marker.Button.EnableInClassList("selected-force", marker.Selected);
            }
            Characters.ShowDrafted(characters);
            HoldLifted(view, draft);
        }

        // UI-403: the hand holds the draft's unsent move (and the figures on their way that no planned ghost shows).
        private void HoldLifted(PlayerView view, TurnDraft draft)
        {
            var planned = PlannedFigureIds();
            Hand.Hold(markers.Values, Characters.All, LiftedFigures.Selection(view, draft), LiftedFigures.OnTheirWay(view, draft),
                planned);
            ApplyLiftMask();
        }

        // UI-404: the step that sends these figures (by id) left. Those the player holds off the table — raised in the
        // hand, or drawn faintly at their destination by a planned flight or journey — fly from there when the view
        // takes them, never from their source.
        public void ExpectFlights(ICollection<string> sent)
        {
            if (sent.Count == 0)
                return;
            Hand.Expect(sent);
            PlannedFlights?.Expect(sent, Figures);
            PlannedJourneys?.Expect(sent, Figures);
        }

        // UI-404: expected figures the view did not fly come back down onto their base.
        private void ReturnLapsed(IReadOnlyCollection<string> keys)
        {
            if (Hand.Lapse(keys, markers.Values, Characters.All))
                ApplyLiftMask();
        }

        // The figures a card's planned flights and journeys show at their destinations.
        private HashSet<string> PlannedFigureIds()
        {
            var planned = new HashSet<string>(PlannedFlights?.FigureIds ?? Array.Empty<string>());
            planned.UnionWith(PlannedJourneys?.FigureIds ?? Array.Empty<string>());
            return planned;
        }

        private void ApplyLiftMask()
        {
            var lifted = PlannedFigureIds();
            lifted.UnionWith(Hand.Lifted);
            SetLiftMask(lifted);
        }

        // UI-403: the places of the lifted figures (in the hand or shown by a planned ghost) show only what stays behind;
        // a base none stays on disappears, its hit shape kept. A stand changes only when its own lifted figures do.
        internal void SetLiftMask(ICollection<string> lifted)
        {
            foreach (var marker in markers.Values)
                marker.Mask(lifted);
            Characters.Mask(lifted);
        }

        // A marker or a character may sit off its region's contour; a press on it reads its own region.
        private string MarkerRegion(VisualElement target)
        {
            for (var element = target; element != null && element != this; element = element.parent)
                if (element.userData is DraftToken { Object: DraftObject.Army or DraftObject.Miniature } token)
                    return token.RegionId;
            return null;
        }

        public string HitTest(Vector2 local)
        {
            var point = Camera.ToBoard(local);
            return Data.Contours.HitTest(point);
        }

        private void SyncMarkers()
        {
            // UI-404: where every figure stood, before the new view gives it a new place; its flight leaves from there.
            FlightPlaces.Record(markers.Values, Characters.All);
            obsolete.Clear();
            obsolete.AddRange(markers.Keys);
            foreach (var army in Data.Armies)
            {
                var key = (army.RegionId, army.Side, army.Garrison);
                obsolete.Remove(key);
                var fresh = !markers.TryGetValue(key, out var marker);
                var before = fresh ? (int?)null : marker.Army.Total;
                if (fresh)
                {
                    marker = CreateMarker(army);
                    markers.Add(key, marker);
                    Stands.Add(marker.Stand);
                }
                marker.Army = army;
                marker.ShowArmy();
                if (armiesShown && !Quiet && (fresh || army.Total > before))
                    Emerge(marker, before);
                else if (Animated() && army.Total < before)
                    Later(shrinking, marker);
                marker.Button.EnableInClassList(MapArmyArtwork.MovedClass, marched.Contains(key));
                ApplyOwner(marker);
                ApplySovereignHint(marker);
            }
            foreach (var key in obsolete)
            {
                var gone = markers[key];
                markers.Remove(key);
                if (Animated())
                    Depart(gone);
                else
                    gone.Stand.Remove();
            }
            Characters.Sync(Data.Characters, armiesShown && !Quiet, Reduced());
            armiesShown |= Data.Armies.Count > 0 || Data.Characters.Count > 0;
            AssignPlaces();
            Hand.Settle(markers.Values, Characters.All);
            ApplyLiftMask();
        }

        // UI-401, UI-402: every force and character on its region's place; the characters beside their side's army.
        private void AssignPlaces()
        {
            regionMarkers.Clear();
            var fieldSides = new Dictionary<string, Side?>();
            foreach (var group in markers.Values.GroupBy(m => m.Army.RegionId))
            {
                var forces = group.ToList();
                regionMarkers.Add(group.Key, forces);
                fieldSides[group.Key] = Stands.Assign(group.Key, forces);
            }
            Characters.Assign(region => fieldSides.TryGetValue(region, out var side) ? side : null);
            Stands.Sort();
        }

        // UI-274: a force holding a king that a decision or the selected die lights glows, and its click chooses the
        // king, unless the force is itself a source for the selected die (then the king's portrait chooses him).
        public void LightSovereigns(Func<string, string> hint)
        {
            sovereignHint = hint;
            foreach (var marker in markers.Values)
                ApplySovereignHint(marker);
            Characters.LightSovereigns(hint);
            SovereignMarkers.Light(hint);
        }

        // The lit king an army marker or (UI-402) an Awakened king's own miniature stands for; null when its click is
        // its own.
        public string LitSovereign(DraftToken figure)
        {
            if (figure.Object == DraftObject.Miniature)
                return Characters.LitSovereign(figure.Id);
            return figure.Side is Side side && markers.TryGetValue((figure.Id, side, figure.Garrison), out var marker)
                ? LitSovereign(marker) : null;
        }

        private string LitSovereign(MapArmyStand marker) => eligibleSources.Contains((marker.Army.RegionId, marker.Army.Side, marker.Army.Garrison))
            ? null : marker.Army.SovereignIds.FirstOrDefault(id => sovereignHint(id) != null);

        private void ApplySovereignHint(MapArmyStand marker)
        {
            var king = LitSovereign(marker);
            var description = ArmyMarkerText.Describe(marker.Army, Data.Regions[marker.Army.RegionId].Name);
            var offer = forceOffers.TryGetValue(marker.Army.RegionId, out var text) ? text : "";
            var skill = SkillKing(marker) is { } owner ? skillLines[owner] : "";
            marker.Button.tooltip = TooltipText.Lines(king != null ? sovereignHint(king) : "", skill, offer, description);
            marker.Button.EnableInClassList(SovereignTargetClass, king != null);
        }

        // UI-431 (TOOLTIP_AUDIT rule 1): the forces whose click makes a card's choice now (The Black Captain Commands' Nazgûl,
        // Monsters Roused's regions) say so first; an empty map withdraws the offers.
        public void OfferForces(IReadOnlyDictionary<string, string> offers)
        {
            var next = offers ?? new Dictionary<string, string>();
            if (SameOffers(next))
                return;
            forceOffers = next;
            foreach (var marker in markers.Values)
                ApplySovereignHint(marker);
        }

        // The offers each refresh brings are usually the last ones; only a change rewrites the markers' tooltips.
        private bool SameOffers(IReadOnlyDictionary<string, string> next) => next.Count == forceOffers.Count &&
            next.All(pair => forceOffers.TryGetValue(pair.Key, out var text) && text == pair.Value);

        // UI-274, UI-401: a corrupted king's marker stands beside his capital's forces. UI-425: on its own place (UI-422's
        // stand places), the nearest to the region's last character place on which it covers no force's count or plaque and
        // no settlement's gem at any zoom.
        internal Vector2 BesideForces(string region) => Stands.Stands(region).Sovereign;

        private bool Animated() => armiesShown && !Quiet && !Reduced();

        private void Emerge(MapArmyStand marker, int? before)
        {
            if (Reduced())
                return;
            emerging.Add((marker, before));
            ScheduleChanges();
        }

        private void Depart(MapArmyStand marker)
        {
            marker.Button.name = "leaving-" + marker.Button.name;
            marker.Button.pickingMode = PickingMode.Ignore;
            marker.Button.tooltip = "";
            Later(departing, marker);
        }

        private void Later(List<MapArmyStand> list, MapArmyStand marker)
        {
            list.Add(marker);
            ScheduleChanges();
        }

        internal void ScheduleChanges()
        {
            if (changesScheduled)
                return;
            changesScheduled = true;
            schedule.Execute(StartChanges);
        }

        // The frame after a sync: the view's flights are known, so a flight's own figures neither emerge nor fade.
        private void StartChanges()
        {
            changesScheduled = false;
            foreach (var (marker, before) in emerging)
                if (markers.ContainsValue(marker) && !Arriving(marker))
                    marker.Artwork.Emerge(before);
            emerging.Clear();
            foreach (var marker in shrinking)
                if (markers.ContainsValue(marker) && !Departing(marker))
                    marker.Artwork.Shrink();
            shrinking.Clear();
            foreach (var marker in departing)
                StartLeaving(marker);
            departing.Clear();
            Characters.StartChanges();
        }

        private bool Arriving(MapArmyStand marker) => Figures?.Arriving(marker.Army.RegionId, marker.Army.Side, marker.Army.Garrison) == true;

        private bool Departing(MapArmyStand marker) => Figures?.Departing(marker.Army.RegionId, marker.Army.Side, marker.Army.Garrison) == true;

        // UI-338, UI-401: a fading marker stays on its place while the view moves, until it is gone.
        private void StartLeaving(MapArmyStand marker)
        {
            if (marker.Stand.Root.parent == null)
                return;
            if (Departing(marker))
            {
                marker.Stand.Remove();
                return;
            }
            leaving.Add(marker);
            marker.Artwork.Leave(() => Gone(marker));
        }

        private void Gone(MapArmyStand marker)
        {
            marker.Stand.Remove();
            leaving.Remove(marker);
        }

        // Skip shows the latest state: every army stands clear with its number, and every departed one is gone.
        public void EndEmerging()
        {
            emerging.Clear();
            shrinking.Clear();
            foreach (var marker in markers.Values)
                marker.Artwork.EndEmerge();
            foreach (var marker in departing)
                marker.Stand.Remove();
            departing.Clear();
            foreach (var marker in leaving)
                marker.Stand.Remove();
            leaving.Clear();
            Characters.EndEmerging();
        }

        private bool Reduced()
        {
            for (VisualElement element = this; element != null; element = element.parent)
                if (element.ClassListContains("reduced-motion"))
                    return true;
            return false;
        }

        private MapArmyStand CreateMarker(ArmyMarker army)
        {
            var regionId = army.RegionId;
            var button = new MiniatureButton(() => ArmySelected?.Invoke(MarkerToken(army)));
            button.name = "army-" + regionId + "-" + army.Side + (army.Garrison ? "-garrison" : "");
            button.userData = MarkerToken(army);
            button.AddToClassList("army");
            button.AddToClassList(RuntimeTooltip.RichHoverClass);
            button.AddToClassList(army.Side == Side.FreePeoples ? "fp" : "sh");
            if (army.Garrison)
                button.AddToClassList("garrison");
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
            var artwork = new MapArmyArtwork(button);
            return new MapArmyStand
            {
                Button = button,
                Army = army,
                Artwork = artwork,
                Stand = new MapStand(button, MapArmyArtwork.HolderSize)
            };
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

        // UI-404: the board point one force stands on; null while the map has no marker for it.
        internal Vector2? ForceFeet(string region, Side side, bool garrison) =>
            markers.TryGetValue((region, side, garrison), out var marker) ? marker.Stand.Feet : (Vector2?)null;

        // UI-306: where one force's marker stands (map-local), for an observed trace; null while it is not shown.
        public Rect? MarkerBounds(string region, Side side, bool garrison) =>
            markers.TryGetValue((region, side, garrison), out var marker) && marker.Visible ? marker.Bounds : (Rect?)null;

        // UI-314: the artwork of one force's marker (its plinth, figures, emblems and count), for the observed glow; null
        // while it is not shown.
        public VisualElement MarkerArtwork(string region, Side side, bool garrison) =>
            markers.TryGetValue((region, side, garrison), out var marker) && marker.Visible ? marker.Artwork?.Root ?? marker.Button : null;

        public IEnumerable<Rect> MarkerBounds(string region)
        {
            foreach (var marker in markers.Values)
                if (marker.Army.RegionId == region && marker.Visible)
                    yield return marker.Bounds;
            if (FellowshipMarker.Bounds(region) is Rect fellowship)
                yield return fellowship;
        }

        // UI-402: everything shown standing in a region — its armies, the Fellowship and its characters' miniatures — so
        // the region's panel keeps clear of them.
        public IEnumerable<Rect> FigureBounds(string region) =>
            MarkerBounds(region).Concat(Characters.In(region).Where(c => c.Visible).Select(c => c.Bounds));

        // UI-402: where one force stands, its army and the characters beside it (map-local); null while none is shown.
        public Rect? ForceBounds(string region, Side side, bool garrison)
        {
            var shown = Characters.Of(region, side, garrison).Where(c => c.Visible).Select(c => c.Bounds).ToList();
            if (MarkerBounds(region, side, garrison) is Rect army)
                shown.Add(army);
            if (shown.Count == 0)
                return null;
            return Rect.MinMaxRect(shown.Min(r => r.xMin), shown.Min(r => r.yMin), shown.Max(r => r.xMax), shown.Max(r => r.yMax));
        }

        // UI-402: the artwork of one force as the observed light marks it: its army's base and figures and its characters'
        // miniatures beside it.
        public IEnumerable<VisualElement> ForceArtworks(string region, Side side, bool garrison)
        {
            if (MarkerArtwork(region, side, garrison) is { } army)
                yield return army;
            foreach (var character in Characters.Of(region, side, garrison))
                if (character.Visible)
                    yield return character.Artwork.Root;
        }

        // UI-402: one character's miniature (its button), while it is shown.
        public VisualElement CharacterMarker(string id) => Characters.Find(id) is { Visible: true } stand ? stand.Button : null;

        // UI-126, UI-401: every force is shown, standing on its region's place (MapStandLayout): figures on a base when the
        // region shows figures, a token otherwise. A fading force keeps its place until it is gone.
        private void LayoutMarkers()
        {
            attentionPoint = null;
            if (contentRect.width <= 0)
                return;
            Stands.LayoutArmies(markers.Values, focusedRegion, used);
            foreach (var marker in leaving)
                Stands.LayoutArmy(marker, false);
            Characters.Layout(focusedRegion, used);
            if (regionMarkers.TryGetValue(AttentionRegion, out var forces))
                attentionPoint = BesideShown(forces);
        }

        // The region shows figures for itself: inspected by a click, holding a selected force or character, or just
        // arrived.
        private bool Focused(string region)
        {
            if (Figures?.Reveals(region) == true || region == MarkerVisibility.InspectedRegion || Hand.Holds(region))
                return true;
            if (regionMarkers.TryGetValue(region, out var forces))
                foreach (var marker in forces)
                    if (marker.Selected)
                        return true;
            return Characters.HoldsSelected(region);
        }

        // UI-401: whether a region shows figures now (focused, or zoomed in with room for them).
        internal bool FiguresIn(string region) => Stands.Figures(region, Focused(region));

        // UI-126: the attention mark stands right of a region's shown forces.
        private static Vector2? BesideShown(List<MapArmyStand> forces)
        {
            Rect? union = null;
            foreach (var marker in forces)
                if (marker.Visible)
                    union = union is Rect rect ? Rect.MinMaxRect(Mathf.Min(rect.xMin, marker.Bounds.xMin),
                        Mathf.Min(rect.yMin, marker.Bounds.yMin), Mathf.Max(rect.xMax, marker.Bounds.xMax),
                        Mathf.Max(rect.yMax, marker.Bounds.yMax)) : marker.Bounds;
            return union is Rect bounds ? new Vector2(bounds.xMax + 12, bounds.center.y) : (Vector2?)null;
        }

        // UI-199: a marker stands where it belongs and the map's edge cuts it (the map clips its overflow); it never
        // moves inward. It shows while the point under its feet is within reach of the view, so it slides in and out
        // with the map instead of appearing or vanishing whole.
        public const float MarkerReach = 160;

        internal bool NearView(Vector2 point) => new Rect(contentRect.x - MarkerReach, contentRect.y - MarkerReach,
            contentRect.width + 2 * MarkerReach, contentRect.height + 2 * MarkerReach).Contains(point);

        internal void ReserveMarker(Rect bounds) => used.Add(bounds);

        public void Redraw()
        {
            Renderer.Update(Data, Camera, Selected, sourceRegions, ownerRegions, DraftOverlay, Hovered, pressedRegion,
                DragTarget ?? "");
            foreground?.MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            var painter = context.painter2D;
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
    }
}
