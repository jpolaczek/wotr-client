using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-101: the portraits live in the Characters panel (Root is its Sovereigns section); the inspector
    // opens beside the portrait, or beside the Characters tab while the panel is rolled up. An awakened
    // sovereign the player's roster already lists as a character keeps only that roster entry.
    public sealed class SovereignPanel
    {
        public VisualElement Root { get; } = BoardScreen.Row("sovereign-strip");
        public VisualElement Inspector => inspector.Root;
        public VisualElement DecisionHost => inspector.DecisionHost;
        public string Selected => selected;
        public string DecisionTarget { get; private set; } = "";
        public bool IsOpen => Inspector.style.display.value != DisplayStyle.None;
        public bool Pinned { get; private set; }
        public FloatingWindow Window { get; set; }
        public Func<string, bool> Activate { get; set; }
        public Func<IEnumerable<Rect>> Occlusions { get; set; }
        public Func<IEnumerable<Rect>> FixedOcclusions { get; set; }
        public Func<VisualElement> FallbackAnchor { get; set; }
        public Func<string, bool> ListedAsCharacter { get; set; }
        public bool HasShownPortraits => portraits.Values.Any(portrait => portrait.style.display.value != DisplayStyle.None);
        public event Action Opening;
        public event Action<bool> Highlighted;
        public event Action Applied;
        public Func<string, bool, VisualElement, bool> InspectCharacter { get; set; }
        private readonly SovereignInspector inspector;
        private readonly Dictionary<string, Button> portraits = new Dictionary<string, Button>();
        private readonly BoardPresentation data;
        private readonly IVisualElementScheduledItem positionUpdate;
        private bool positionScheduled;
        private PlayerView view;
        private TurnDraft draft;
        private VisualElement tray, dock;
        private string selected = "";
        private string observedDecision = "";

        public SovereignPanel(BoardPresentation data, Action<string> focus)
        {
            this.data = data;
            inspector = new SovereignInspector(data, focus, Close);
            positionUpdate = Inspector.schedule.Execute(() => { positionScheduled = false; Position(); });
            positionUpdate.Pause();
            Root.name = "sovereigns";
            Inspector.RegisterCallback<KeyDownEvent>(e => { if (e.keyCode == KeyCode.Escape) Close(); });
            Inspector.RegisterCallback<GeometryChangedEvent>(e => { if (PopoverPlacement.SizeChanged(e.oldRect, e.newRect)) RequestPosition(); });
            Root.RegisterCallback<AttachToPanelEvent>(e => { if (e.target == Root) Application.focusChanged += FocusChanged; });
            Root.RegisterCallback<DetachFromPanelEvent>(e => { if (e.target == Root) { Application.focusChanged -= FocusChanged; Close(); } });
            Close();
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
                observedDecision = DecisionTarget = "";
                draft = null;
                Close();
            }
            view = latest;
            Root.style.display = view.Sovereigns.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var id in portraits.Keys.Where(id => view.Sovereigns.All(s => s.Id != id)).ToArray())
            {
                portraits[id].RemoveFromHierarchy();
                portraits.Remove(id);
            }
            foreach (var sovereign in view.Sovereigns)
                Portrait(sovereign);
            if (selected.Length > 0)
                Render();
            Applied?.Invoke();
        }

        private void Portrait(SovereignView sovereign)
        {
            if (!portraits.TryGetValue(sovereign.Id, out var button))
            {
                var id = sovereign.Id;
                button = BoardScreen.Button("", () =>
                {
                    if (Activate?.Invoke(id) != true)
                        Show(id);
                });
                button.name = "sovereign-" + id;
                button.AddToClassList(RuntimeTooltip.RichHoverClass);
                button.userData = new DraftToken(DraftObject.Sovereign, id);
                portraits.Add(id, button);
                Root.Add(button);
            }
            button.text = sovereign.Title.Split(' ')[0] + "\n" + SovereignPresentation.Status(sovereign.Status);
            var region = data.Regions.TryGetValue(sovereign.RegionId, out var definition) ? definition.Name : "Out of play";
            button.tooltip = sovereign.Title + " · " + sovereign.Status + " · " + region;
            button.EnableInClassList("corrupted-sovereign", sovereign.Status == SovereignStatus.Corrupted);
            var listed = sovereign.Status == SovereignStatus.Awakened && ListedAsCharacter?.Invoke(sovereign.Id) == true;
            button.style.display = listed ? DisplayStyle.None : DisplayStyle.Flex;
        }

        public void Context(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            var previousTarget = DecisionTarget;
            DecisionTarget = SovereignPresentation.DecisionTarget(view, draft);
            if (DecisionTarget.Length > 0 && tray != null && tray.parent != DecisionHost)
                DecisionHost.Add(tray);
            else if (DecisionTarget.Length == 0 && tray?.parent == DecisionHost)
                dock.Add(tray);
            var decision = view.Decision?.Id ?? "";
            var changed = decision != observedDecision;
            observedDecision = decision;
            if (DecisionTarget.Length > 0 && (changed || previousTarget != DecisionTarget))
                Show(DecisionTarget);
            else if (selected.Length > 0)
                Render();
        }

        public void Show(string id)
        {
            if (view?.Sovereigns.Any(s => s.Id == id) != true)
                return;
            Pinned = true;
            Open(id);
        }

        public void OpenDecision() => Show(DecisionTarget);
        public void Pin() => Show(selected);
        public void CloseTransient() { if (!Pinned) Close(); }

        public void Highlight(Func<string, bool> legal)
        {
            var any = false;
            foreach (var portrait in portraits)
            {
                var target = legal(portrait.Key);
                portrait.Value.EnableInClassList("legal-target", target);
                any |= target;
            }
            Highlighted?.Invoke(any);
        }

        public void Close()
        {
            Pinned = false;
            positionUpdate.Pause();
            positionScheduled = false;
            selected = "";
            foreach (var portrait in portraits.Values)
                portrait.EnableInClassList("inspected-sovereign", false);
            Inspector.style.display = DisplayStyle.None;
        }

        private void FocusChanged(bool focused) { if (!focused) CloseTransient(); }

        private void Open(string id)
        {
            if (view?.Sovereigns.FirstOrDefault(s => s.Id == id)?.Status == SovereignStatus.Awakened &&
                InspectCharacter?.Invoke(id, Pinned, Source(id)) == true)
            {
                Close();
                return;
            }
            if (!IsOpen || selected != id)
                Opening?.Invoke();
            selected = id;
            Inspector.style.display = DisplayStyle.Flex;
            Inspector.BringToFront();
            Render();
        }

        private void Render()
        {
            var sovereign = view.Sovereigns.FirstOrDefault(s => s.Id == selected);
            if (sovereign == null)
            {
                Close();
                return;
            }
            if (sovereign.Status == SovereignStatus.Awakened &&
                InspectCharacter?.Invoke(selected, Pinned, Source(selected)) == true)
            {
                Close();
                return;
            }
            inspector.Apply(view, sovereign, draft, selected == DecisionTarget && DecisionTarget.Length > 0);
            foreach (var portrait in portraits)
                portrait.Value.EnableInClassList("inspected-sovereign", portrait.Key == selected);
            Position();
            Window?.Refresh();
        }

        // The portrait while the Characters panel shows it, otherwise the element that stands for the panel.
        private VisualElement Source(string id)
        {
            if (portraits.TryGetValue(id, out var portrait) && Shown(portrait))
                return portrait;
            return FallbackAnchor?.Invoke() ?? Root;
        }

        private static bool Shown(VisualElement element)
        {
            if (element.panel == null || element.worldBound.width <= 0)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None)
                    return false;
            return true;
        }

        public void RequestPosition()
        {
            if (!IsOpen || positionScheduled)
                return;
            positionScheduled = true;
            positionUpdate.ExecuteLater(0);
        }

        public void Position()
        {
            if (!IsOpen || Inspector.parent == null || Window?.Restore() == true)
                return;
            var bounds = Inspector.parent.contentRect;
            if (bounds.width <= 0 || bounds.height <= 0)
                return;
            var width = Inspector.layout.width > 0 ? Inspector.layout.width : 440;
            var height = Inspector.layout.height > 0 ? Inspector.layout.height : 330;
            var anchor = Source(selected).worldBound;
            var point = Inspector.parent.WorldToLocal(anchor.center);
            var occupied = (Occlusions?.Invoke() ?? new[] { Root.worldBound })
                .Select(rect => new Rect(Inspector.parent.WorldToLocal(rect.position), rect.size));
            var fixedOccupied = (FixedOcclusions?.Invoke() ?? new[] { Root.worldBound })
                .Select(rect => new Rect(Inspector.parent.WorldToLocal(rect.position), rect.size));
            var placement = SovereignPlacement.Near(point, new Vector2(width, height), bounds, occupied, fixedOccupied);
            if (Inspector.style.left.keyword != StyleKeyword.Undefined || Mathf.Abs(Inspector.style.left.value.value - placement.x) > .25f)
                Inspector.style.left = placement.x;
            if (Inspector.style.top.keyword != StyleKeyword.Undefined || Mathf.Abs(Inspector.style.top.value.value - placement.y) > .25f)
                Inspector.style.top = placement.y;
        }
    }
}
