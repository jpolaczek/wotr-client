using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class CharacterRosterPanel
    {
        public Button Tab { get; }
        public VisualElement Root { get; } = new VisualElement { name = "characters-panel" };
        public CharacterInspector Inspector { get; }
        public VisualElement DecisionHost => Inspector.DecisionHost;
        public CharacterRosterContext Actions { get; }
        // UI-108: the chosen cost can bring this figure into play; its row enters on a double-click or Enter.
        public const string EntryClass = "entry-target";
        public Func<string, bool> Enterable { get; set; } = _ => false;
        public event Action<string> EntryRequested;
        public FloatingWindow Window { get; set; }
        public FloatingWindow InspectorWindow { get; set; }
        public bool Pinned => hover.Pinned;
        public bool InspectorPinned { get; private set; }
        public event Action Opening;
        public event Action Inspecting;
        public Func<VisualElement, bool> IsRegionSource { get; set; }
        public CharacterWindowPlace Place { get; set; }
        public Func<IEnumerable<Rect>> Occlusions { get; set; }
        public Func<IEnumerable<Rect>> FixedOcclusions { get; set; }
        private readonly DrawerHover hover;
        private readonly BoardPresentation data;
        // UI-449: brings the camera to a region (the map's focus).
        private readonly Action<string> locate;
        private readonly Dictionary<string, CharacterRosterRow> rows = new Dictionary<string, CharacterRosterRow>();
        private readonly Dictionary<CharacterRosterSection, VisualElement> sections = new Dictionary<CharacterRosterSection, VisualElement>();
        private readonly Foldout removed = new Foldout { text = "Removed", value = false, name = "characters-removed" };
        private readonly Label empty = new Label("No named characters in this view") { name = "characters-empty" };
        private VisualElement sovereigns;
        private readonly ChoicePulse pulse;
        private bool rosterOffered;
        private bool sovereignsOffered;
        // UI-391: a row's ability icon glows.
        private bool abilitiesOffered;
        // UI-441: a character's skill can be used now.
        private bool skillsOffered;
        private bool rosterAvailable;
        // UI-408: two rows a line unless the roster is too tall for the board in that form; decided per roster size and room.
        // UI-445: a roster too tall even compact takes one more column (DenseClass), the panel one row wider.
        public const string WideClass = "roster-wide";
        public const string CompactClass = "roster-compact";
        public const string DenseClass = "roster-dense";
        private enum RosterForm
        {
            Wide,
            Compact,
            Dense
        }
        private RosterForm form = RosterForm.Wide;
        private string fitted = "";
        private PlayerView view;
        private VisualElement source;
        private bool visible;

        public CharacterRosterPanel(VisualElement surface, BoardPresentation data, Action<string> focus)
        {
            this.data = data;
            locate = focus;
            Tab = BoardScreen.Button("Characters", Open);
            Tab.name = "characters-tab";
            Tab.AddToClassList("characters-tab");
            Tab.AddToClassList(RuntimeTooltip.RichHoverClass);
            Root.AddToClassList("characters-panel");
            Root.AddToClassList(WideClass);
            Root.RegisterCallback<GeometryChangedEvent>(_ => FitRows());
            Root.style.display = DisplayStyle.None;
            pulse = new ChoicePulse(Root);
            Inspector = new CharacterInspector(data, focus, CloseInspection, PinInspection);
            Actions = new CharacterRosterContext(this);
            var header = BoardScreen.Row("character-header");
            FloatingWindow.MarkHandle(header);
            header.Add(BoardScreen.Title("CHARACTERS"));
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-characters";
            header.Add(close);
            Root.Add(header);
            AddSection(CharacterRosterSection.OnBoard, "On board", Root);
            AddSection(CharacterRosterSection.NotInPlay, "Not in play", Root);
            Root.Add(removed);
            AddSection(CharacterRosterSection.Removed, "", removed);
            Root.Add(empty);
            hover = new DrawerHover(surface, Tab, Root, element => Contains(Inspector.Root, element), RefreshVisibility);
            BindInspection(surface);
        }

        public bool Lists(string figureId) => rows.ContainsKey(figureId);

        public void ShowSovereigns(bool shown)
        {
            if (sovereigns == null)
                return;
            sovereigns.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshEmpty();
        }

        private void RefreshEmpty()
        {
            var sovereignsShown = sovereigns != null && sovereigns.style.display.value != DisplayStyle.None;
            empty.style.display = rows.Count == 0 && !sovereignsShown ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // UI-101: the sovereign portraits (SovereignPanel.Root) as a section under the characters.
        public void AddSovereigns(VisualElement portraits)
        {
            sovereigns = new VisualElement { name = "characters-section-Sovereigns" };
            sovereigns.Add(new Label("Sovereigns"));
            portraits.AddToClassList("character-roster-grid");
            sovereigns.Add(portraits);
            sovereigns.style.display = DisplayStyle.None;
            Root.Insert(Root.IndexOf(removed), sovereigns);
        }

        private void AddSection(CharacterRosterSection kind, string title, VisualElement parent)
        {
            var section = new VisualElement { name = "characters-section-" + kind };
            if (title.Length > 0)
                section.Add(new Label(title));
            var grid = BoardScreen.Row("character-roster-grid");
            section.Add(grid);
            sections.Add(kind, grid);
            parent.Add(section);
        }

        public void Apply(PlayerView latest)
        {
            if (view != null && (view.MatchId != latest.MatchId || view.You != latest.You))
                Close();
            view = latest;
            var entries = CharacterRosterPresentation.Rows(view).ToArray();
            var ids = entries.Select(row => row.FigureId).ToHashSet();
            foreach (var id in rows.Keys.Where(id => !ids.Contains(id)).ToArray())
            {
                rows[id].Root.RemoveFromHierarchy();
                rows.Remove(id);
            }
            foreach (var entry in entries)
                UpdateRow(entry);
            foreach (var section in sections)
                section.Value.parent.style.display = section.Value.childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            removed.style.display = sections[CharacterRosterSection.Removed].childCount > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            RefreshEmpty();
            if (Inspector.FigureId.Length > 0 && !Inspector.Apply(view, Inspector.FigureId))
                CloseInspection();
        }

        private void UpdateRow(CharacterRosterView entry)
        {
            if (!rows.TryGetValue(entry.FigureId, out var row))
            {
                var id = entry.FigureId;
                // UI-105: a row only inspects; figures are selected in their region's panel on the map.
                row = new CharacterRosterRow(id);
                row.Wide(Wide);
                rows.Add(id, row);
                ObserveRow(row.Root, id);
                ObserveAbilities(row, id);
            }
            var grid = sections[CharacterRosterPresentation.Section(entry)];
            if (row.Root.parent != grid)
            {
                var focused = row.Root.panel?.focusController.focusedElement == row.Root;
                grid.Add(row.Root);
                if (focused)
                    row.Root.Focus();
            }
            row.Apply(entry, data);
        }

        public void RefreshSelection()
        {
            var offered = false;
            var available = false;
            var abilities = false;
            var skills = false;
            foreach (var row in rows)
            {
                var chosen = row.Key == Actions.EnteringFigure;
                var entry = Enterable(row.Key) && !chosen;
                // UI-438: several versions that may enter now are chosen in the character's window.
                var versions = !entry && !chosen && Actions.EntryVersions(row.Key).Count > 1;
                offered |= entry || versions;
                available |= Actions.Available(row.Key);
                row.Value.ShowVersion(OfferedVersion(row.Key, chosen, entry));
                row.Value.Root.EnableInClassList("legal-target", Actions.Available(row.Key));
                row.Value.Abilities.Offer(Actions.RowActions(row.Key, row.Value.Definition), Actions.Editable, die => DieUse.Name(view, die));
                abilities |= row.Value.Abilities.Offered;
                row.Value.ShowSkill(Actions.Skill(row.Key));
                skills |= row.Value.SkillLit;
                row.Value.Root.EnableInClassList(EntryClass, entry);
                row.Value.Offer(entry, versions);
                row.Value.Choose(chosen);
            }
            rosterOffered = offered;
            rosterAvailable = available;
            abilitiesOffered = abilities;
            skillsOffered = skills;
            RunPulse();
            RefreshTab();
        }

        private void RunPulse() => pulse.Run(rosterOffered || sovereignsOffered || abilitiesOffered || skillsOffered);

        // UI-438: the version a row shows while its entry is chosen or offered; empty for the row's own choice.
        private string OfferedVersion(string figure, bool chosen, bool entry)
        {
            if (chosen)
                return Actions.EnteringVersion;
            return entry ? Actions.Entry(figure)?.Character?.CharacterId ?? "" : "";
        }

        // UI-380 (owner): the Characters tab glows while the roster offers something — an entry, an action on a row, a
        // king's decision — so the way to it is lit before the panel opens; UI-441: a character's skill usable now too.
        private void RefreshTab() => Tab.EnableInClassList("legal-target", rosterOffered || rosterAvailable || sovereignsOffered || skillsOffered);

        // UI-345: a lit sovereign's portrait, attempt or tile pulses with the roster, in its one rhythm.
        public void OfferSovereigns(bool any)
        {
            sovereignsOffered = any;
            RunPulse();
            RefreshTab();
        }

        public void Attach(VisualElement tray, VisualElement dock) => Actions.Attach(tray, dock);
        public void OpenDecision() => Actions.OpenDecision();

        public void Open() => hover.Open();
        public void Close()
        {
            hover.Close();
            CloseInspection();
        }
        public void CloseTransient()
        {
            hover.CloseTransient();
            if (!InspectorPinned)
                CloseInspection();
        }
        public void PinInspection() => InspectorPinned = true;

        public void Show(string id, bool pin = false, VisualElement anchor = null)
        {
            if (InspectorPinned && !pin && Inspector.FigureId != id)
                return;
            if (view == null || !Inspector.Apply(view, id))
                return;
            source = anchor ?? (rows.TryGetValue(id, out var row) ? row.Root : Tab);
            InspectorPinned |= pin;
            Inspecting?.Invoke();
            Inspector.Root.style.display = DisplayStyle.Flex;
            Inspector.Root.BringToFront();
            PositionInspector();
        }

        public bool InspectingRegion => Inspector.Root.style.display.value != DisplayStyle.None && IsRegionSource?.Invoke(source) == true;

        private void CloseInspection()
        {
            InspectorPinned = false;
            source = null;
            Inspector.Root.style.display = DisplayStyle.None;
        }

        private void RefreshVisibility()
        {
            if (visible != hover.Visible)
            {
                visible = hover.Visible;
                if (visible)
                {
                    Opening?.Invoke();
                    Root.BringToFront();
                }
                Appearance.Display(Root, visible);
                Tab.EnableInClassList("selected", visible);
            }
            Window?.Refresh();
        }

        // UI-436 (owner): a window opened from the Characters tab, its panel or a ruler stands at the one place beside the
        // tab (CharacterWindowPlace); one opened from a region's window stands beside that window, its own panel. UI-440:
        // a click on an ability no longer changes the window's content (UI-371's kept place is gone with the unrolled text).
        public void PositionInspector()
        {
            if (Inspector.Root.style.display.value == DisplayStyle.None || Inspector.Root.parent == null || InspectorWindow?.Restore() == true || source == null)
                return;
            if (Place != null && IsRegionSource?.Invoke(source) != true && Place.Apply(Inspector.Root, SovereignPanel.WindowSize))
                return;
            var viewport = Inspector.Root.parent;
            var size = Inspector.Root.layout.size;
            var measured = size.x > 0 && !float.IsNaN(size.x);
            var point = viewport.WorldToLocal(source.worldBound.center);
            if (!measured)
                size = new Vector2(420, 280);
            var obstacles = (Occlusions?.Invoke() ?? new[] { source.worldBound })
                .Select(r => new Rect(viewport.WorldToLocal(r.position), r.size));
            var fixedControls = (FixedOcclusions?.Invoke() ?? Array.Empty<Rect>())
                .Select(r => new Rect(viewport.WorldToLocal(r.position), r.size)).ToList();
            // The roster's layout rect, not its world bound: while it unrolls, the entering scale would make it
            // look narrower and the inspector would later overlap its edge.
            if (!Appearance.Closed(Root) && Root.resolvedStyle.display != DisplayStyle.None && Root.layout.width > 0)
                fixedControls.Add(Root.parent.ChangeCoordinatesTo(viewport, Root.layout));
            var rectangle = SovereignPlacement.Near(point, size, viewport.contentRect, obstacles, fixedControls);
            if (Mathf.Abs(Inspector.Root.style.left.value.value - rectangle.x) > .25f)
                Inspector.Root.style.left = rectangle.x;
            if (Mathf.Abs(Inspector.Root.style.top.value.value - rectangle.y) > .25f)
                Inspector.Root.style.top = rectangle.y;
        }

        // UI-408 (owner): two characters a line give each its larger icons; a roster that would then reach past the board
        // area keeps three a line (four on a short viewport). UI-445: one that still reaches past it takes one more column.
        // A new roster size or a new room tries two a line again, so the form never flips back and forth while both stay
        // the same.
        private void FitRows()
        {
            if (Root.panel == null || Root.parent == null || Root.resolvedStyle.display == DisplayStyle.None)
                return;
            var key = rows.Count + "/" + Mathf.RoundToInt(Root.parent.worldBound.height);
            if (key != fitted)
            {
                fitted = key;
                if (form != RosterForm.Wide)
                {
                    SetForm(RosterForm.Wide);
                    return;
                }
            }
            if (form != RosterForm.Dense && Root.worldBound.yMax > Root.parent.worldBound.yMax + 0.5f)
                SetForm(form + 1);
        }

        private void SetForm(RosterForm next)
        {
            form = next;
            Root.EnableInClassList(WideClass, next == RosterForm.Wide);
            Root.EnableInClassList(CompactClass, next != RosterForm.Wide);
            Root.EnableInClassList(DenseClass, next == RosterForm.Dense);
            // UI-436: the window stands beside the panel, so its slot takes the panel's width.
            Place?.Widen(next == RosterForm.Dense);
            foreach (var row in rows.Values)
                row.Wide(next == RosterForm.Wide);
        }

        public bool Wide => form == RosterForm.Wide;

        // UI-445: one more column than compact.
        public bool Dense => form == RosterForm.Dense;

        // UI-391: a usable ability's icon starts it, as the window's icon does; another opens the window at that ability (UI-440:
        // its icon takes the focus, so its formatted description shows).
        private void ObserveAbilities(CharacterRosterRow row, string id)
        {
            row.Abilities.Used += item => Actions.Use(item);
            row.Abilities.Opened += ability =>
            {
                Show(id, true, row.Root);
                Inspector.Abilities.Present(ability);
            };
        }

        // A single click inspects, except when the row can be entered: its double-click must not flash the window.
        private void ObserveRow(Button row, string id)
        {
            row.AddToClassList(RuntimeTooltip.RichHoverClass);
            // UI-142: a row the chosen cost can bring into play enters at once; any other row opens its card.
            row.clicked += () =>
            {
                if (row.panel == null)
                    return;
                if (Enterable(id))
                    EntryRequested?.Invoke(id);
                else
                {
                    Show(id, true, row);
                    LocateOnBoard(id);
                }
            };
            row.RegisterCallback<NavigationSubmitEvent>(e =>
            {
                if (!Enterable(id))
                    return;
                EntryRequested?.Invoke(id);
                e.StopImmediatePropagation();
            }, TrickleDown.TrickleDown);
        }

        // UI-449 (owner): a row's click on a character in play also brings the camera to its figure; one off the board
        // moves nothing (an inspection opened any other way never moves the camera).
        private void LocateOnBoard(string id)
        {
            var entry = view?.OwnCharacterRoster.FirstOrDefault(row => row.FigureId == id);
            var region = entry == null ? "" : PlacePresentation.CameraRegion(view, entry, data.Regions);
            if (region.Length > 0)
                locate(region);
        }

        public void Observe(VisualElement element, string id)
        {
            element.AddToClassList(RuntimeTooltip.RichHoverClass);
            if (element is Button button)
                button.clicked += () =>
                {
                    // The existing action callback runs first; inspection cannot consume it.
                    if (element.panel != null)
                        Show(id, true, element);
                };
        }

        private void BindInspection(VisualElement surface)
        {
            Root.RegisterCallback<GeometryChangedEvent>(_ => PlaceInspectorAgain());
            Root.RegisterCallback<TransitionEndEvent>(evt =>
            {
                if (evt.target == Root)
                    PlaceInspectorAgain();
            });
            Inspector.Root.RegisterCallback<GeometryChangedEvent>(e =>
            {
                // Fractional scaling can round the height differently after a move.
                // Reposition for changed content, not that subpixel feedback.
                if (PopoverPlacement.SizeChanged(e.oldRect, e.newRect))
                    PositionInspector();
            });
            // UI-371: a new board size or interface scale changes what the window must avoid, so it is placed again.
            Inspector.Root.RegisterCallback<AttachToPanelEvent>(_ =>
                Inspector.Root.parent?.RegisterCallback<GeometryChangedEvent>(BoardResized));
            Root.RegisterCallback<AttachToPanelEvent>(_ => Application.focusChanged += FocusChanged);
            Root.RegisterCallback<DetachFromPanelEvent>(_ => { Application.focusChanged -= FocusChanged; Close(); });
        }

        // A panel beside the window grew or finished appearing over it (a Ruler's Reserve filling its rows after the
        // window was placed): the window moves clear of it again, as when it opened.
        public void ObstacleChanged()
        {
            if (Inspector.Root.style.display.value == DisplayStyle.None || Occlusions == null)
                return;
            var bounds = Inspector.Root.worldBound;
            if (Occlusions().Any(obstacle => obstacle.Overlaps(bounds)))
                PlaceInspectorAgain();
        }

        // The roster's own move (unrolling, closing) changes what the window must avoid, so it is placed again.
        private void PlaceInspectorAgain() => PositionInspector();

        private void BoardResized(GeometryChangedEvent change)
        {
            if (PopoverPlacement.SizeChanged(change.oldRect, change.newRect))
                PlaceInspectorAgain();
        }

        private void FocusChanged(bool focused) { if (!focused) CloseTransient(); }
        private static bool Contains(VisualElement parent, VisualElement child) => parent != null && child != null && (child == parent || parent.Contains(child));

        public static bool ContainsInput(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.name is "characters-panel" or "character-inspector" or "characters-tab")
                    return true;
            return false;
        }
    }
}
