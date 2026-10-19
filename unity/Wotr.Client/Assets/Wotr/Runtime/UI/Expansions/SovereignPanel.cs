using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-101: the portraits live in the Characters panel (Root is its Sovereigns section); the inspector
    // opens beside the portrait, or beside the Characters tab while the panel is rolled up. UI-274: every sovereign keeps
    // a fixed slot (SovereignPortrait) in every state, and the decisions about the kings are made on the portraits, their
    // attempt buttons and tiles; the inspector only informs and hosts no decision.
    public sealed class SovereignPanel
    {
        public VisualElement Root { get; } = BoardScreen.Row("sovereign-strip");
        public VisualElement Inspector => inspector.Root;
        public string Selected => selected;
        public string DecisionTarget { get; private set; } = "";
        public bool IsOpen => Inspector.style.display.value != DisplayStyle.None;
        public bool Pinned { get; private set; }
        public FloatingWindow Window { get; set; }
        public Func<string, bool> Activate { get; set; }
        public Func<IEnumerable<Rect>> Occlusions { get; set; }
        public Func<IEnumerable<Rect>> FixedOcclusions { get; set; }
        public Func<VisualElement> FallbackAnchor { get; set; }
        public bool HasShownPortraits => portraits.Count > 0;
        // UI-274: a portrait's choice in the current decision (a card's king, the cleanse) and the objects' clicks.
        public Func<string, SovereignChoice> Choice { get; set; } = _ => null;
        public event Action<string> AttemptChosen;
        public event Action<string, string> TileChosen;
        public event Action Opening;
        public event Action<bool> Highlighted;
        // UI-274: what a click on each king does now (null when it only informs), for his objects on the map.
        public event Action<Func<string, string>> Lit;
        public event Action Applied;
        public Func<string, bool, VisualElement, bool> InspectCharacter { get; set; }
        private readonly SovereignInspector inspector;
        private readonly Dictionary<string, SovereignPortrait> portraits = new Dictionary<string, SovereignPortrait>();
        private readonly BoardPresentation data;
        private readonly IVisualElementScheduledItem positionUpdate;
        private bool positionScheduled;
        private PlayerView view;
        private TurnDraft draft;
        private Func<string, bool> lit = _ => false;
        private string selected = "";

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

        public void Apply(PlayerView latest)
        {
            if (view != null && view.MatchId != latest.MatchId)
            {
                DecisionTarget = "";
                draft = null;
                Close();
            }
            view = latest;
            Root.style.display = view.Sovereigns.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var id in portraits.Keys.Where(id => view.Sovereigns.All(s => s.Id != id)).ToArray())
            {
                portraits[id].Root.RemoveFromHierarchy();
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
            if (!portraits.TryGetValue(sovereign.Id, out var portrait))
            {
                var id = sovereign.Id;
                portrait = new SovereignPortrait(id, () => Click(id), () => AttemptChosen?.Invoke(id), (king, tile) => TileChosen?.Invoke(king, tile));
                portraits.Add(id, portrait);
                Root.Add(portrait.Root);
            }
            portrait.Apply(view, sovereign, draft, data, ClickHint(sovereign.Id));
        }

        public string ClickHint(string id)
        {
            var choice = Choice(id);
            if (choice != null)
                return choice.Tooltip;
            return lit(id) && view != null ? SovereignPresentation.LitHint(view, draft, id) : null;
        }

        // A king's object (portrait or map figure) whose decision uses it acts; any other opens his information.
        public void Click(string id)
        {
            if (Activate?.Invoke(id) != true)
                Show(id);
        }

        public void Context(PlayerView latest, TurnDraft selection)
        {
            view = latest;
            draft = selection;
            var previousTarget = DecisionTarget;
            DecisionTarget = SovereignPresentation.DecisionTarget(view, draft);
            // The portraits follow in Highlight, which the same refresh calls next.
            if (selected.Length > 0 && previousTarget == DecisionTarget)
                Render();
        }

        public void Show(string id)
        {
            if (view?.Sovereigns.Any(s => s.Id == id) != true)
                return;
            Pinned = true;
            Open(id);
        }

        public void Pin() => Show(selected);
        public void CloseTransient() { if (!Pinned) Close(); }

        // The portraits a die or a decision lights; the Characters tab glows while one does.
        public void Highlight(Func<string, bool> legal)
        {
            lit = legal;
            var any = false;
            foreach (var sovereign in view?.Sovereigns ?? new List<SovereignView>())
            {
                Portrait(sovereign);
                any |= legal(sovereign.Id) || Choice(sovereign.Id) != null || SovereignAttempt.Enabled(view, draft, sovereign.Id) ||
                    SovereignAttempt.Tiles(view, sovereign).Any(t => t.Selectable);
            }
            Highlighted?.Invoke(any);
            Lit?.Invoke(ClickHint);
        }

        public void Close()
        {
            Pinned = false;
            positionUpdate.Pause();
            positionScheduled = false;
            selected = "";
            foreach (var portrait in portraits.Values)
                portrait.Portrait.EnableInClassList("inspected-sovereign", false);
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
                portrait.Value.Portrait.EnableInClassList("inspected-sovereign", portrait.Key == selected);
            Position();
            Window?.Refresh();
        }

        // The portrait while the Characters panel shows it, otherwise the element that stands for the panel.
        private VisualElement Source(string id)
        {
            if (portraits.TryGetValue(id, out var portrait) && Shown(portrait.Portrait))
                return portrait.Portrait;
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
