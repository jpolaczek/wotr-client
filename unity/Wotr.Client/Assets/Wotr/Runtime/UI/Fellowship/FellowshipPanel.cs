using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class FellowshipPanel
    {
        private BoardPresentation data;
        public VisualElement Root { get; } = new VisualElement { name = "fellowship-panel", focusable = true };
        public Button Status => status.Summary;
        public Button Ring => status.Ring;
        public Button PanelRing => status.PanelRing;
        public bool HideOffered => controls.HideOffered;
        public event Action<CommandKind> ActionSelected;
        public event Action<string> PortraitSelected;
        // UI-269: a starting Guide candidate's crown while the private versions are chosen.
        public event Action<string> GuideChosen;
        public VersionComparison Versions { get; } = new VersionComparison();
        public event Action Opening;
        public event Action VisibilityChanged;
        public bool Docked { get; set; }
        // UI-98: a docked panel unrolls under its top-bar plaque and is held open while it hosts a decision.
        public bool Held { get; private set; }
        public bool IsOpen => view != null && (Held || hover.Visible.Length > 0);
        public bool Pinned { get; private set; }
        public FloatingWindow Window { get; set; }
        private readonly MapCanvas map;
        private readonly HoverState hover = new HoverState();
        private readonly FellowshipStatus status;
        private readonly FellowshipControls controls = new FellowshipControls();
        private readonly FellowshipRoster roster = new FellowshipRoster();
        private readonly Button focusButton;
        private string observedDecision = "";
        private bool choosingVersions;
        private bool wasVisible;
        private VisualElement tray;
        private VisualElement dock;
        private PlayerView view;
        private double Now => Time.realtimeSinceStartupAsDouble;

        public FellowshipPanel(Action focus, MapCanvas map)
        {
            this.map = map;
            Root.AddToClassList("fellowship-panel");
            Root.style.display = DisplayStyle.None;
            status = new FellowshipStatus(Open);
            status.HideRequested += () => RequestHide();
            status.LocateRequested += focus;
            status.TrackFocusRequested += focus;
            Observe(Status);
            Observe(map.FellowshipMarker.Root);
            map.FellowshipMarker.Clicked += Open;
            var heading = FloatingWindow.Header("FELLOWSHIP");
            // UI-90: the condition and position row is the locate control, saving a row and a button.
            focusButton = BoardScreen.Button("", focus);
            focusButton.name = "focus-fellowship";
            focusButton.AddToClassList("fellowship-locate");
            focusButton.Add(status.StateRow);
            heading.Add(focusButton);
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-fellowship";
            close.tooltip = "Close Fellowship panel · Keep your selection";
            heading.Add(close);
            Root.Add(heading);
            Root.Add(status.Root);
            Root.Add(controls.Root);
            Root.Add(roster.Root);
            controls.Selected += kind => ActionSelected?.Invoke(kind);
            status.MoveRequested += () => ActionSelected?.Invoke(CommandKind.MoveFellowship);
            roster.Selected += Portrait;
            Versions.Changed += roster.Compare;
            roster.GuideChosen += id => GuideChosen?.Invoke(id);
            RegisterHover();
        }

        // UI-269: while the versions are chosen a portrait's click compares its two versions; otherwise it acts.
        private void Portrait(string id)
        {
            if (!choosingVersions)
            {
                PortraitSelected?.Invoke(id);
                return;
            }
            Versions.Select(id);
        }

        // UI-232: a click on the Ring icon hides a revealed Fellowship when the chosen die allows it.
        public bool RequestHide()
        {
            if (!controls.HideOffered)
                return false;
            ActionSelected?.Invoke(CommandKind.HideFellowship);
            return true;
        }

        public void Attach(VisualElement decisionTray, VisualElement decisionDock)
        {
            tray = decisionTray;
            dock = decisionDock;
        }

        public void Apply(PlayerView latest, BoardPresentation data)
        {
            if (view != null && view.MatchId != latest.MatchId)
            {
                Close();
                observedDecision = "";
            }
            view = latest;
            this.data = data;
            status.Apply(view.Fellowship, data, view.Winner.HasValue);
            focusButton.tooltip = status.Description + (view.Fellowship.MordorStep.HasValue ? "\nClick to focus the Mordor track"
                : "\nClick to center the map on the last known position");
            roster.Apply(view);
            RefreshVisibility();
        }

        // AUDIO-06: the tracks as one stage of a two-track change shows them (TrackSequence).
        public void ShowTracks(FellowshipView fellowship)
        {
            if (view != null && data != null)
                status.Apply(fellowship, data, view.Winner.HasValue);
        }

        public void Context(PlayerView latest, TurnDraft draft, ActionDieContext context, bool enabled)
        {
            var route = draft.Kind is CommandKind.DeclareFellowship or CommandKind.RevealFellowship;
            var separation = draft.Kind == CommandKind.SeparateCompanions ||
                latest.Decision?.Kind == DecisionKind.CompanionSeparation;
            Root.EnableInClassList("fellowship-route", route);
            var hunt = HuntActions.UsesPanel(latest, draft);
            Root.EnableInClassList("fellowship-hunt-summary", hunt && latest.Hunt != null);
            controls.Refresh(latest, draft, context, enabled && !hunt, route || separation || hunt, Docked);
            status.OfferMove(controls.MoveOffered, controls.MoveDescription);
            status.OfferHide(controls.HideOffered, controls.HideTooltip);
            var versions = CompanionSetupPresentation.Versions(latest);
            if (!versions)
                Versions.Reset();
            choosingVersions = versions;
            Root.EnableInClassList("fellowship-versions", versions);
            roster.Context(latest, draft, context, enabled && !hunt, Versions.Figure);
            var rosterVisible = !route && latest.Decision?.Presentation != "FellowshipCompanions";
            roster.Root.style.display = rosterVisible ? DisplayStyle.Flex : DisplayStyle.None;
            status.Compact(route || separation || draft.Kind == CommandKind.SelectGuide, latest.Hunt != null, rosterVisible && roster.ShowsGuide);
            var owns = FellowshipActions.UsesPanel(latest, draft);
            Held = Docked && owns;
            // Esc passes a held panel by, as it did the permanent column, and reaches the current choice.
            Root.EnableInClassList("edge-permanent-panel", Held);
            if (owns && tray.parent != Root)
                Root.Add(tray);
            else if (!owns && tray.parent == Root)
                dock.Add(tray);
            Status.EnableInClassList("legal-target", owns && latest.Decision != null || controls.Offers || roster.Choosing);
            OpenNewDecision(latest, owns);
            RefreshVisibility();
        }

        private void OpenNewDecision(PlayerView latest, bool owns)
        {
            var decision = latest.Decision?.Id ?? "";
            if (decision == observedDecision)
                return;
            observedDecision = decision;
            if (owns && latest.DecisionOwner == latest.You && latest.Decision != null &&
                latest.Decision.Kind is not (DecisionKind.Action or DecisionKind.ActionSequence))
                OpenDecision();
        }

        // A held panel is already shown; the others still close for the new decision, as Open does.
        private void OpenDecision()
        {
            if (Docked)
                Opening?.Invoke();
            else
                Open();
        }

        public void Open()
        {
            if (view == null)
                return;
            Pinned = true;
            hover.Focus("fellowship");
            Opening?.Invoke();
            RefreshVisibility();
            if (!Docked)
                Root.BringToFront();
        }

        public void Close()
        {
            Pinned = false;
            hover.Clear();
            RefreshVisibility();
        }

        public void CloseTransient() { if (!Pinned) Close(); }

        private void Observe(VisualElement element)
        {
            element.AddToClassList(RuntimeTooltip.RichHoverClass);
        }

        private void RegisterHover()
        {
            Root.RegisterCallback<GeometryChangedEvent>(e => { if (PopoverPlacement.SizeChanged(e.oldRect, e.newRect)) Position(); });
            map.CameraChanged += Position;
            map.RegisterCallback<GeometryChangedEvent>(_ => Position());
            Root.RegisterCallback<AttachToPanelEvent>(e => { if (e.target == Root) Application.focusChanged += FocusChanged; });
            Root.RegisterCallback<DetachFromPanelEvent>(e => { if (e.target == Root) Application.focusChanged -= FocusChanged; });

        }

        private void FocusChanged(bool focused)
        {
            if (!focused && !Pinned)
                Close();
        }

        private void RefreshVisibility()
        {
            Root.style.display = IsOpen ? DisplayStyle.Flex : DisplayStyle.None;
            map.FellowshipMarker.Inspect(IsOpen);
            Position();
            Window?.Refresh();
            if (wasVisible != IsOpen)
            {
                wasVisible = IsOpen;
                if (IsOpen && !Docked)
                    Root.BringToFront();
                VisibilityChanged?.Invoke();
                map.Refresh();
            }
        }

        public void Position()
        {
            if (Docked)
                return;
            if (Window?.Restore() == true)
                return;
            if (!IsOpen || view == null || Root.layout.width <= 0 || Root.layout.height <= 0 || float.IsNaN(Root.layout.width) || float.IsNaN(Root.layout.height))
                return;
            var anchor = map.Data.Shapes.TryGetValue(view.Fellowship.RegionId, out var shape)
                ? map.Camera.ToScreen(new Vector2(shape.Anchor[0], shape.Anchor[1]))
                : map.WorldToLocal(Status.worldBound.center);
            var bounds = MapPanelPlacement.Near(map, Root, anchor);
            Root.style.left = bounds.x;
            Root.style.top = bounds.y;
        }
    }
}
