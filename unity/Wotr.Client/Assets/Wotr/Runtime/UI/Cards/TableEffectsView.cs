using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class TableEffectsView
    {
        private const int PageSize = 4;
        public VisualElement Root { get; } = new VisualElement { name = "effects-panel" };
        public Button Tab { get; }
        public VisualElement DecisionHost { get; } = new VisualElement { name = "effects-decision-host" };
        public FloatingWindow Window { get; set; }
        public bool Docked { get; set; }
        public bool Pinned => hover.Pinned;
        public bool IsOpen => hover.Visible;
        public string SelectedSource => selected?.SourceId ?? "";
        public event Action Opening;
        public event Action<ComponentAction> RemovalSelected;
        // UI-214: the source a card's effect offers was chosen with its ×.
        public event Action<string> OfferChosen;
        private readonly DrawerHover hover;
        private readonly EffectSourceDetails details;
        private readonly EffectsReadingWidth reading;
        private readonly VisualElement list = new VisualElement { name = "effects-sources" };
        private readonly VisualElement pagination = BoardScreen.Row("effects-pagination");
        private readonly Label empty = new Label("No active effects") { name = "effects-empty" };
        private readonly Label cost = new Label { name = "effect-removal-cost" };
        private readonly Dictionary<string, EffectEntry> entries = new Dictionary<string, EffectEntry>();
        private Dictionary<string, ComponentAction> choices = new Dictionary<string, ComponentAction>();
        private IReadOnlyDictionary<string, string> offers = new Dictionary<string, string>();
        private bool choicesEnabled, offersEnabled;
        private IReadOnlyList<EffectSource> sources = Array.Empty<EffectSource>();
        private readonly Button previous, next;
        private readonly Label pageLabel = new Label();
        private PlayerView view;
        private EffectSource selected;
        private VisualElement tray, dock, obstruction, viewport;
        private bool shown, ended, sourcePinned, preparing;
        private int page;

        public TableEffectsView(VisualElement surface, MapCanvas map)
        {
            Root.AddToClassList("effects-panel");
            Root.style.display = DisplayStyle.None;
            Tab = BoardScreen.Button("Effects · 0", Open);
            Tab.name = "effects-tab";
            Tab.AddToClassList("effects-tab");
            Tab.AddToClassList(RuntimeTooltip.RichHoverClass);
            Tab.tooltip = "Active sources on both sides";
            var header = FloatingWindow.Header("EFFECTS");
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-effects";
            header.Add(close);
            Root.Add(header);
            Root.Add(empty);
            Root.Add(list);
            previous = IconButton.Create("back", "Previous", () => Page(-1));
            next = IconButton.Create("next", "Next", () => Page(1));
            previous.name = "effects-previous";
            next.name = "effects-next";
            pagination.Add(previous);
            pagination.Add(pageLabel);
            pagination.Add(next);
            header.Insert(header.childCount - 1, pagination);
            details = new EffectSourceDetails(map.Focus);
            reading = new EffectsReadingWidth(Root);
            details.Rebuilt += ReadingChanged;
            Root.Add(details.Root);
            Root.Add(cost);
            Root.Add(DecisionHost);
            hover = new DrawerHover(surface, Tab, Root, _ => false, RefreshVisibility);
            Root.RegisterCallback<AttachToPanelEvent>(e =>
            {
                if (e.target == Root)
                    Root.parent.RegisterCallback<GeometryChangedEvent>(UpdateCompactLayout);
            });
            Root.RegisterCallback<DetachFromPanelEvent>(e =>
            {
                if (e.target == Root)
                    Root.parent?.UnregisterCallback<GeometryChangedEvent>(UpdateCompactLayout);
            });
            Root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            Root.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
            Root.RegisterCallback<GeometryChangedEvent>(OwnGeometryChanged);
        }

        public void Attach(VisualElement decisionTray, VisualElement decisionDock)
        {
            tray = decisionTray;
            dock = decisionDock;
        }

        public void KeepClearOf(VisualElement panel)
        {
            if (obstruction == panel)
                return;
            obstruction?.UnregisterCallback<GeometryChangedEvent>(NeighborChanged);
            obstruction = panel;
            obstruction?.RegisterCallback<GeometryChangedEvent>(NeighborChanged);
            if (viewport == Root.parent)
                return;
            viewport?.UnregisterCallback<GeometryChangedEvent>(NeighborChanged);
            viewport = Root.parent;
            viewport?.RegisterCallback<GeometryChangedEvent>(NeighborChanged);
        }

        public void Position()
        {
            if (Docked)
                return;
            if (!IsOpen || Root.parent == null || Root.layout.width <= 0 || Root.layout.height <= 0)
                return;
            if (Window?.Restore() == true)
                return;
            Rect? blocked = obstruction != null && Visible(obstruction) && obstruction.layout.width > 0
                && obstruction.layout.height > 0 ? Root.parent.WorldToLocal(obstruction.worldBound) : (Rect?)null;
            var placement = EffectsPanelPlacement.Place(Root.parent.contentRect.size, Root.layout, blocked);
            if (!Mathf.Approximately(Root.style.left.value.value, placement.x))
                Root.style.left = placement.x;
            if (!Mathf.Approximately(Root.style.top.value.value, placement.y))
                Root.style.top = placement.y;
        }

        private void NeighborChanged(GeometryChangedEvent e) => Position();

        private static bool Visible(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.style.display.value == DisplayStyle.None)
                    return false;
            return true;
        }

        public void Apply(PlayerView latest)
        {
            if (view != null && (view.MatchId != latest.MatchId || view.You != latest.You))
            {
                selected = null;
                sourcePinned = false;
                page = 0;
                Close();
            }
            view = latest;
            sources = EffectsPresentation.Sources(latest);
            var current = sources.ToDictionary(s => s.Key);
            foreach (var key in entries.Keys.Where(k => !current.ContainsKey(k)).ToList())
            {
                entries[key].Root.RemoveFromHierarchy();
                entries.Remove(key);
            }
            foreach (var source in sources)
            {
                if (!entries.TryGetValue(source.Key, out var entry))
                {
                    entry = new EffectEntry(source, Select, Remove);
                    entries.Add(source.Key, entry);
                    list.Add(entry.Root);
                }
                entry.Apply(source);
            }
            if (selected != null && current.TryGetValue(selected.Key, out var updated))
            {
                selected = updated;
                ended = false;
            }
            else if (selected != null)
            {
                ended = true;
                if (!sourcePinned)
                    selected = null;
            }
            Tab.text = "Effects · " + sources.Count;
            empty.style.display = sources.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            details.Show(selected, ended, view.Configuration);
            Page(0);
            RefreshChoices();
        }

        public void Context(IEnumerable<ComponentAction> actions, bool enabled)
        {
            choices = actions.GroupBy(a => a.CardId).ToDictionary(g => g.Key, g => g.First());
            choicesEnabled = enabled;
            ApplyContext();
            RefreshChoices();
        }

        // UI-214: sources a card's effect lets the player choose (source id → tooltip); none clears them.
        public void Offer(IReadOnlyDictionary<string, string> latest, bool enabled)
        {
            if (offers.Count == 0 && latest.Count == 0)
                return;
            var appearing = offers.Count == 0;
            offers = latest;
            offersEnabled = enabled;
            ApplyContext();
            RefreshChoices();
            if (appearing)
                ShowFirstOffer();
        }

        // A new offer turns to the page of its first source, so a lit source is in view when the panel opens.
        private void ShowFirstOffer()
        {
            var index = sources.ToList().FindIndex(s => offers.ContainsKey(s.SourceId));
            if (index < 0)
                return;
            page = index / PageSize;
            Page(0);
        }

        private void ApplyContext()
        {
            foreach (var entry in entries.Values)
            {
                if (offers.TryGetValue(entry.Source.SourceId, out var offer))
                {
                    entry.Offer(offer, offersEnabled);
                    continue;
                }
                choices.TryGetValue(entry.Source.SourceId, out var choice);
                entry.Context(choice, choicesEnabled, view);
            }
        }

        public void Draft(PlayerView latest, TurnDraft draft)
        {
            var action = choices.Values.FirstOrDefault(a => a.Kind == draft.Kind &&
                (a.Kind != CommandKind.DiscardProtection || a.CardId == draft.Payload.CardId));
            var source = action == null ? null : sources.FirstOrDefault(s => s.SourceId == action.CardId);
            var wasPreparing = preparing;
            preparing = source != null;
            Root.EnableInClassList("preparing-effect-removal", preparing);
            if (wasPreparing != preparing)
                ReadingChanged();
            if (preparing)
            {
                if (tray.parent != DecisionHost)
                    DecisionHost.Add(tray);
                if (!wasPreparing || selected?.Key != source.Key)
                    Select(source, true);
            }
            else if (tray?.parent == DecisionHost)
                dock.Add(tray);
            list.style.display = preparing ? DisplayStyle.None : DisplayStyle.Flex;
            Page(0);
        }

        public void Inspect(string sourceId)
        {
            var source = sources.FirstOrDefault(s => s.SourceId == sourceId);
            if (source != null)
            {
                Select(source, true);
                page = sources.ToList().FindIndex(s => s.Key == source.Key) / PageSize;
                Page(0);
            }
        }

        private void Select(EffectSource source, bool pin)
        {
            if (!pin && sourcePinned)
                return;
            selected = source;
            ended = false;
            sourcePinned = pin;
            if (pin)
                Open();
            details.Show(source, false, view?.Configuration);
            RefreshChoices();
        }

        private void Remove(string id)
        {
            if (offers.ContainsKey(id))
            {
                if (offersEnabled)
                    OfferChosen?.Invoke(id);
                return;
            }
            var source = sources.FirstOrDefault(s => s.SourceId == id);
            if (source == null || !choices.TryGetValue(id, out var choice) || !choice.Enabled)
                return;
            Select(source, true);
            RemovalSelected?.Invoke(choice);
        }

        private void RefreshChoices()
        {
            foreach (var entry in entries.Values)
                entry.Root.EnableInClassList("selected-effect", entry.Source.Key == selected?.Key);
            var choice = selected == null || ended ? null : choices.GetValueOrDefault(selected.SourceId);
            cost.text = choice == null ? "" : EffectsPresentation.RemovalCost(view, choice);
            cost.style.display = cost.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            Tab.EnableInClassList("legal-target", choices.Values.Any(c => c.Enabled && sources.Any(s => s.SourceId == c.CardId)) ||
                offersEnabled && sources.Any(s => offers.ContainsKey(s.SourceId)));
        }

        private void Page(int delta)
        {
            var pages = Math.Max(1, (sources.Count + PageSize - 1) / PageSize);
            page = Math.Max(0, Math.Min(pages - 1, page + delta));
            for (var i = 0; i < sources.Count; i++)
                entries[sources[i].Key].Root.style.display = i / PageSize == page ? DisplayStyle.Flex : DisplayStyle.None;
            pagination.style.display = pages > 1 && !preparing ? DisplayStyle.Flex : DisplayStyle.None;
            previous.SetEnabled(page > 0);
            next.SetEnabled(page + 1 < pages);
            pageLabel.text = (page + 1) + " / " + pages;
        }

        private const float CompactBelow = 400;

        private void UpdateCompactLayout(GeometryChangedEvent e)
        {
            RefreshCompact();
            if (e.oldRect.size != e.newRect.size)
                ReadingChanged();
        }

        // QUAL-19: the docked panel's own place is known only after its layout, so its geometry also decides.
        private void OwnGeometryChanged(GeometryChangedEvent e)
        {
            Position();
            RefreshCompact();
        }

        private void RefreshCompact() =>
            Root.EnableInClassList("compact-effects", Root.parent != null && AvailableHeight() < CompactBelow);

        // QUAL-19: a panel docked at the left rail starts below the rail's top, so its space is shorter than the stage;
        // a floating panel moves up when compact, so it is measured against the whole stage.
        private float AvailableHeight() => Root.parent.contentRect.height - (Docked ? Root.layout.y : 0);

        // A card's text is hidden while a removal is being paid, so only reading may widen the panel.
        private void ReadingChanged() => reading.Read(details.ShowsCard && !preparing);

        public void Open() => hover.Open();
        public void Close() => hover.Close();
        public void CloseTransient() => hover.CloseTransient();

        private void RefreshVisibility()
        {
            if (shown != IsOpen)
            {
                shown = IsOpen;
                if (shown)
                {
                    Opening?.Invoke();
                    Root.BringToFront();
                }
                Root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                Tab.EnableInClassList("selected", shown);
            }
            Position();
            Window?.Refresh();
        }
    }
}
