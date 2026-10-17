using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-162: the Hunt window. Like the battle window (UI-149) a new Hunt opens it for both players and a window the
    // player closed stays closed until the next Hunt; the result stays until it is closed. It hosts the decision tray
    // while the Hunt owns the decision; the Shadow's allocation happens in the turn console, not here.
    public sealed class HuntPanel
    {
        public VisualElement Root { get; } = new VisualElement { name = "hunt-panel", focusable = true };
        public Button Tab { get; }
        public HuntTileView Tile { get; }
        public HuntDiceStrip Dice { get; } = new HuntDiceStrip();
        public FellowshipRoster Guides { get; } = new FellowshipRoster();
        public HuntResponsesView Responses { get; } = new HuntResponsesView();
        public HuntBoxView Box { get; } = new HuntBoxView();
        public HuntPoolList Pool { get; } = new HuntPoolList();
        // UI-141: the header icon that shows or hides the pool list; the choice lasts for the session.
        public Button PoolToggle { get; }
        public VisualElement DecisionHost { get; } = new VisualElement { name = "hunt-choices" };
        public event Action Opening;
        public event Action VisibilityChanged;
        public event Action ContextChanged;
        // UI-172: the plaque clicked during the Shadow's own allocation submits it, once it is ready.
        public event Action AllocationConfirmed;
        public Func<bool> HasInspection { get; set; }
        // Whether the Hunt owns the current decision, so the tray stands in this window.
        public bool Held { get; private set; }
        public bool IsOpen => view != null && Pinned;
        public bool Pinned { get; private set; }
        public FloatingWindow Window { get; set; }
        private readonly MapCanvas map;
        private readonly BoardPresentation data;
        private readonly HuntReportStrip report;
        private readonly VisualElement main = new VisualElement { name = "hunt-main" };
        private readonly VisualElement stages = new VisualElement { name = "hunt-stage-row" };
        private readonly Label title;
        private readonly Label step = new Label { name = "hunt-step" };
        private readonly Label owner = new Label { name = "hunt-owner" };
        private readonly Label source = new Label { name = "hunt-source" };
        // UI-90: the docked header carries the idle count; Styles/RightColumn.uss hides the empty body then.
        private readonly Label idleCount = new Label { name = "hunt-idle-count", pickingMode = PickingMode.Ignore };
        private PlayerView view;
        private VisualElement tray;
        private VisualElement dock;
        private string observedDecision = "";
        // The Hunt shown now, the one the player closed and the report whose result stays in the open window.
        private string scope = "";
        private string closedFor = "";
        private string result = "";
        private string shownStages = "";
        private bool allocating;
        private bool allocationReady;
        // UI-205: during the allocation the plaque reads ALLOCATE beside a badge with the count of dice in it; only the
        // badge lights up (gilt from the first die) and swells briefly when the count changes. The plaque itself has no
        // glow, and with no die in the allocation it stays plain, so the dice draw the eye first.
        public const string CountFilledClass = "hunt-count-filled";
        public const string CountBumpClass = "hunt-count-bump";
        public const string EmptyClass = "hunt-allocation-empty";
        private const long BumpMilliseconds = 260;
        public Label AllocationCount { get; } = new Label { name = "hunt-allocation-count", pickingMode = PickingMode.Ignore };
        // What the plaque reads: "Allocate 1/3" during the allocation, "Hunt" otherwise.
        public string Caption => allocating ? "Allocate " + AllocationCount.text : Tab.text;
        private readonly VisualElement allocationLine = new VisualElement { name = "hunt-allocation-line", pickingMode = PickingMode.Ignore };
        private IVisualElementScheduledItem bump;
        private string shownCount = "";
        private int allocated;
        private bool wasVisible;

        public HuntPanel(MapCanvas map, BoardPresentation data)
        {
            this.map = map;
            this.data = data;
            Root.AddToClassList("hunt-panel");
            Root.AddToClassList("hunt-window");
            Root.style.display = DisplayStyle.None;
            // UI-98: the top bar's plaque — the Eyes in the Hunt Box and any Free Peoples dice beside them.
            Tab = BoardScreen.Button("Hunt", PlaqueClicked);
            Tab.name = "hunt-tab";
            Tab.AddToClassList("hunt-tab");
            Tab.AddToClassList(EdgePlaque.ClassName);
            Tab.userData = new DraftToken(DraftObject.HuntEye);
            Tab.style.display = DisplayStyle.None;
            var emblem = new VisualElement { name = "hunt-tab-emblem", pickingMode = PickingMode.Ignore };
            emblem.AddToClassList(EdgePlaque.EmblemClass);
            Tab.Add(emblem);
            Tab.Add(Box.Counts);
            Tab.Add(AllocationLine());
            var header = FloatingWindow.Header("Hunt");
            title = header.Q<Label>();
            idleCount.AddToClassList("hunt-idle-count");
            header.Add(idleCount);
            PoolToggle = IconButton.Create("hunt-pool", "Hunt pool · Show the tiles in the pool", TogglePool);
            PoolToggle.name = "hunt-pool-toggle";
            header.Add(PoolToggle);
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-hunt";
            close.tooltip = "Close Hunt panel · Keep your selection";
            header.Add(close);
            Root.Add(header);
            Root.Add(stages);
            var context = BoardScreen.Row("hunt-heading");
            context.Add(step);
            context.Add(owner);
            Root.Add(context);
            Root.Add(Box.Root);
            Pool.Root.style.display = DisplayStyle.None;
            Root.Add(Pool.Root);
            report = new HuntReportStrip(data);
            Root.Add(report.Root);
            main.Add(source);
            main.Add(Dice.Root);
            Tile = new HuntTileView(data);
            main.Add(Tile.Root);
            main.Add(Guides.Root);
            Root.Add(main);
            DecisionHost.Add(Responses.Root);
            Root.Add(DecisionHost);
            Root.RegisterCallback<GeometryChangedEvent>(e => { if (PopoverPlacement.SizeChanged(e.oldRect, e.newRect)) Position(); });
        }

        public void Attach(VisualElement decisionTray, VisualElement decisionDock)
        {
            tray = decisionTray;
            dock = decisionDock;
        }

        public void Apply(PlayerView latest)
        {
            if (view != null && view.MatchId != latest.MatchId)
            {
                Close();
                observedDecision = scope = closedFor = result = "";
            }
            view = latest;
            Tab.style.display = DisplayStyle.Flex;
            OpenNewHunt();
            Render();
        }

        // UI-109: with no Hunt under way the window is only the Hunt Box; UI-162: the result of the Hunt it opened
        // for stays until the player closes it, so opening and closing render the window again.
        private void Render()
        {
            var underway = Underway(view);
            var showResult = !underway && IsOpen && view.HuntReport?.Completed == true && view.HuntReport.Id == result;
            var hunt = underway || showResult ? HuntActions.Displayed(view) : null;
            underway |= showResult;
            Stages(hunt, underway);
            Root.EnableInClassList("hunt-idle", !underway);
            Root.EnableInClassList("hunt-underway", underway);
            PlacePool(underway);
            Root.EnableInClassList("hunt-mordor-panel", hunt?.Mordor == true);
            title.text = (underway ? HuntPresentation.Title(view) : "Hunt").ToUpperInvariant();
            step.text = HuntPresentation.Activity(view);
            owner.text = HuntPresentation.Owner(view);
            owner.style.display = owner.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            idleCount.text = "Dice in Hunt · " + view.Dice.Count(d => d.Location == DieLocation.Hunt);
            Box.Apply(view);
            Pool.Apply(view.HuntPool);
            Box.Root.style.display = underway ? DisplayStyle.None : DisplayStyle.Flex;
            source.text = hunt == null ? idleCount.text
                : HuntReportText.Source(hunt.SourceId, data) + " · Pool " + hunt.PoolCount + " · Corruption " + view.Fellowship.Corruption + "/12";
            if (hunt?.Mordor == true)
                source.text += "\nMordor · No Hunt roll · Eye " + hunt.EyeDamage;
            source.tooltip = hunt?.PoolCount == 0 ? "Eligible standard discards refill an empty pool on the next draw." : source.text;
            Dice.Apply(hunt);
            Tile.Apply(hunt);
            Guides.Apply(view);
            report.Apply(underway ? view.HuntReport : null);
            ApplyFocus();
            RefreshVisibility();
        }

        private static bool Underway(PlayerView view) => view.Hunt != null || view.HuntReport?.Completed == false;

        // A new Hunt opens the window for both players unless the player closed it during this very Hunt.
        private void OpenNewHunt()
        {
            var key = HuntWindowState.Key(view);
            if (key.Length == 0 || key == scope)
                return;
            scope = key;
            result = view.HuntReport?.Id ?? "";
            if (closedFor != key)
                Open();
        }

        private void Stages(HuntView hunt, bool underway)
        {
            var key = underway ? (hunt?.Mordor == true) + "/" + HuntWindowState.Index(view, hunt) : "";
            if (key == shownStages)
                return;
            shownStages = key;
            stages.Clear();
            if (underway)
                stages.Add(HuntStages.Create(view, hunt));
        }

        public void Context(PlayerView latest, TurnDraft draft, ActionDieContext context, bool enabled)
        {
            var owns = HuntActions.UsesPanel(latest, draft);
            Held = owns;
            // Esc passes a held panel by, as it did the permanent column, and reaches the current choice.
            Root.EnableInClassList("edge-permanent-panel", Held);
            Root.EnableInClassList("hunt-reaction-panel", owns && draft.Kind == CommandKind.RespondEffect &&
                DraftOptions.For(latest, draft)?.EffectChoices.Count > 1);
            if (owns && tray.parent != DecisionHost)
                DecisionHost.Add(tray);
            else if (!owns && tray.parent == DecisionHost)
                dock.Add(tray);
            Allocate(latest, draft);
            Dice.Context(latest, draft, enabled);
            Responses.Context(latest, draft, enabled);
            var tileLegal = enabled && HuntResponseActions.Option(latest) is { } damage && draft.Ready(damage);
            Tile.Root.EnableInClassList("legal-target", tileLegal);
            ReadOnlyControl.Apply(Tile.Root, tileLegal);
            var guide = owns && latest.Decision?.Kind == DecisionKind.Guide && latest.DecisionOwner == latest.You;
            Guides.Root.style.display = guide ? DisplayStyle.Flex : DisplayStyle.None;
            Guides.Context(latest, draft, context, enabled && guide);
            Tab.EnableInClassList("legal-target", owns && latest.DecisionOwner == latest.You && latest.Decision != null);
            var decision = latest.Decision?.Id ?? "";
            if (decision != observedDecision)
            {
                observedDecision = decision;
                if (owns && latest.DecisionOwner == latest.You && latest.Decision != null && closedFor != scope)
                    Open();
            }
            RefreshVisibility();
            ContextChanged?.Invoke();
        }

        private void ApplyFocus()
        {
            var focus = HuntPresentation.Focus(view);
            Tile.Root.style.display = focus == HuntFocus.Tile ? DisplayStyle.Flex : DisplayStyle.None;
            Dice.Root.style.display = focus == HuntFocus.Roll ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // UI-162: the Shadow's own allocation shows its count on the plaque; UI-172: the plaque also submits it.
        private void Allocate(PlayerView latest, TurnDraft draft)
        {
            var option = HuntActions.Allocation(latest);
            allocating = option != null;
            Tab.text = allocating ? "" : "Hunt";
            allocationLine.style.display = allocating ? DisplayStyle.Flex : DisplayStyle.None;
            allocated = allocating ? draft.Payload.Ids.Count : 0;
            ShowCount(allocating ? HuntAllocationText.Count(draft, option) : "");
            if (!allocating)
            {
                Tab.tooltip = "Hunt Box · Eyes and Free Peoples movement dice · Click to open the Hunt";
                SetAllocationReady(false, "");
            }
            Tab.EnableInClassList(EmptyClass, allocating && allocated == 0);
        }

        private VisualElement AllocationLine()
        {
            allocationLine.AddToClassList("hunt-allocation-line");
            allocationLine.style.display = DisplayStyle.None;
            var title = new Label("ALLOCATE") { name = "hunt-allocation-title", pickingMode = PickingMode.Ignore };
            title.AddToClassList("hunt-allocation-title");
            AllocationCount.AddToClassList("hunt-allocation-count");
            allocationLine.Add(title);
            allocationLine.Add(AllocationCount);
            return allocationLine;
        }

        // The badge turns gilt with the first die and swells for a moment whenever the count changes.
        private void ShowCount(string count)
        {
            AllocationCount.text = count;
            AllocationCount.EnableInClassList(CountFilledClass, allocated > 0);
            if (count.Length > 0 && shownCount.Length > 0 && count != shownCount)
                Bump();
            shownCount = count;
        }

        private void Bump()
        {
            bump?.Pause();
            AllocationCount.AddToClassList(CountBumpClass);
            bump = AllocationCount.schedule.Execute(() => AllocationCount.RemoveFromClassList(CountBumpClass)).StartingIn(BumpMilliseconds);
        }

        // The console decides readiness (the draft's limits and the server preview); a plaque that is not ready
        // stays in place, dimmed, and says what is missing.
        public void SetAllocationReady(bool ready, string tooltip)
        {
            allocationReady = allocating && ready;
            Tab.EnableInClassList("hunt-confirm-ready", allocating && ready);
            Tab.EnableInClassList("hunt-confirm-waiting", allocating && !ready);
            if (allocating)
                Tab.tooltip = tooltip;
        }

        private void PlaqueClicked()
        {
            if (!allocating)
            {
                Open();
                // UI-209: opened from its plaque, the window shows the tiles left in the pool at once.
                ShowPool(true);
            }
            else if (allocationReady)
                AllocationConfirmed?.Invoke();
        }

        public void Open()
        {
            if (view == null)
                return;
            var opening = !Pinned;
            Pinned = true;
            if (opening)
                Opening?.Invoke();
            Render();
            Root.BringToFront();
        }

        // UI-184: under way the pool list drops over the window's content, so it is drawn after the decision; at rest it
        // follows the Hunt Box in the window. The keyboard focus ring may still come after it.
        private void PlacePool(bool underway)
        {
            var anchor = underway ? DecisionHost : Box.Root;
            if (Root.IndexOf(Pool.Root) != Root.IndexOf(anchor) + 1)
                Pool.Root.PlaceInFront(anchor);
        }

        private void TogglePool() => ShowPool(Pool.Root.style.display.value == DisplayStyle.None);

        private void ShowPool(bool shown)
        {
            Pool.Root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
            PoolToggle.EnableInClassList("selected", shown);
            PoolToggle.tooltip = shown ? "Hunt pool · Hide the tiles in the pool" : "Hunt pool · Show the tiles in the pool";
            Window?.Refresh();
        }

        public void Close()
        {
            Pinned = false;
            closedFor = scope;
            result = "";
            if (view != null)
                Render();
        }

        // The window is opened on purpose or by a new Hunt, so transient closing leaves it alone.
        public void CloseTransient() { }

        private void RefreshVisibility()
        {
            Root.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            // A local choice must keep its hit target under the pointer. Geometry
            // callbacks handle resizing; only visibility changes need a new anchor.
            if (wasVisible != IsOpen)
                Position();
            Window?.Refresh();
            if (wasVisible == IsOpen)
                return;
            wasVisible = IsOpen;
            VisibilityChanged?.Invoke();
            map.Refresh();
        }

        // Beside the Fellowship on the map, as the battle window stands beside its region; never moves the camera.
        public void Position()
        {
            if (Window?.Restore() == true || !IsOpen || Root.layout.width <= 0 || Root.layout.height <= 0 ||
                float.IsNaN(Root.layout.width) || float.IsNaN(Root.layout.height))
                return;
            var anchor = map.Data.Shapes.TryGetValue(view.Fellowship?.RegionId ?? "", out var shape)
                ? map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1]))
                : map.contentRect.center;
            var bounds = MapPanelPlacement.Near(map, Root, anchor);
            Root.style.left = bounds.x;
            Root.style.top = bounds.y;
        }
    }
}
