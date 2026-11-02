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
        // UI-430 (owner): a table card an effect lets the player discard wears the hand's red discard parchment (UI-222).
        public const string DiscardClass = "discard-target";
        public VisualElement Root { get; } = new VisualElement { name = "effects-panel" };
        public Button Tab { get; }
        public VisualElement DecisionHost { get; } = new VisualElement { name = "effects-decision-host" };
        public FloatingWindow Window { get; set; }
        public bool Docked { get; set; }
        // UI-424: the left rail, whose top is the highest place a docked panel may take (BoardEdgeLayout.PlaceBesideTab).
        public VisualElement Rail { get; set; }
        // A source's details: since UI-446 only while its removal is paid, inside the panel above the payment.
        public VisualElement Details => details.Root;
        public EffectsGridArrangement Arrangement { get; private set; } = EffectsGridArrangement.Regular;
        public bool Pinned => hover.Pinned;
        public bool IsOpen => hover.Visible;
        // The source whose removal is being prepared (UI-446: a click selects nothing else).
        public string SelectedSource => selected?.SourceId ?? "";
        // UI-221: the source whose text is unrolled now, or "".
        public string UnrolledSource => unroll.Entry?.Source.SourceId ?? "";
        public event Action Opening;
        public event Action<ComponentAction> RemovalSelected;
        // UI-214: the source a card's effect offers was chosen with its × (UI-430: or with its red tile).
        public event Action<string> OfferChosen;
        // UI-272: a card the decision uses itself was clicked (The Lore of the Elven Rings before Dismay).
        public event Action<string> UseChosen;
        private readonly DrawerHover hover;
        private readonly EffectSourceDetails details;
        private readonly VisualElement list = new VisualElement { name = "effects-sources" };
        private readonly Label empty = new Label("No active effects") { name = "effects-empty" };
        private readonly Label cost = new Label { name = "effect-removal-cost" };
        private readonly Dictionary<string, EffectEntry> entries = new Dictionary<string, EffectEntry>();
        private Dictionary<string, ComponentAction> choices = new Dictionary<string, ComponentAction>();
        private IReadOnlyDictionary<string, string> offers = new Dictionary<string, string>();
        private IReadOnlyDictionary<string, string> uses = new Dictionary<string, string>();
        private bool choicesEnabled, offersEnabled;
        private IReadOnlyList<EffectSource> sources = Array.Empty<EffectSource>();
        // UI-221: the hovered source's text, over the other sources.
        private readonly EffectUnroll unroll = new EffectUnroll();
        private PlayerView view;
        private EffectSource selected;
        private VisualElement tray, dock, obstruction, viewport;
        private bool shown, ended, preparing;
        // UI-424: a new stage size rearranges the grid after layout.
        private readonly IVisualElementScheduledItem rearrange;
        // UI-446: the source Inspect unrolls once the panel is laid out, and how often it looks before giving up.
        private readonly IVisualElementScheduledItem unrollInspected;
        private EffectEntry inspecting;
        private int inspectTries;
        private const int InspectTries = 30;
        private const long InspectRetryMilliseconds = 16;
        // The rail's top below the stage's top (LeftRail.uss .board-left-tabs) until the rail is laid out.
        private const float DefaultCeiling = 58;

        public TableEffectsView(VisualElement surface, MapCanvas map)
        {
            Root.AddToClassList("effects-panel");
            Root.style.display = DisplayStyle.None;
            Tab = BoardScreen.Button("Effects · 0", Open);
            Tab.name = "effects-tab";
            Tab.AddToClassList("effects-tab");
            Tab.AddToClassList(RuntimeTooltip.RichHoverClass);
            Tab.tooltip = "Active sources on both sides";
            // UI-221: the header holds only the close button, on the right; every source is in view at once.
            var header = FloatingWindow.Header("EFFECTS");
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-effects";
            header.Add(close);
            Root.Add(header);
            Root.Add(empty);
            Root.Add(list);
            details = new EffectSourceDetails(map.Focus);
            Root.Add(details.Root);
            Root.Add(cost);
            Root.Add(DecisionHost);
            Root.Add(unroll.Root);
            hover = new DrawerHover(surface, Tab, Root, _ => false, RefreshVisibility);
            rearrange = Root.schedule.Execute(Arrange);
            rearrange.Pause();
            unrollInspected = Root.schedule.Execute(UnrollInspected);
            unrollInspected.Pause();
            Root.RegisterCallback<AttachToPanelEvent>(e =>
            {
                if (e.target == Root)
                    Root.parent.RegisterCallback<GeometryChangedEvent>(StageChanged);
            });
            Root.RegisterCallback<DetachFromPanelEvent>(e =>
            {
                if (e.target == Root)
                    Root.parent?.UnregisterCallback<GeometryChangedEvent>(StageChanged);
            });
            Root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            Root.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
            Root.RegisterCallback<GeometryChangedEvent>(OwnGeometryChanged);
            ApplyArrangement();
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
                Close();
            }
            view = latest;
            sources = EffectsPresentation.Sources(latest);
            var current = sources.ToDictionary(s => s.Key);
            foreach (var key in entries.Keys.Where(k => !current.ContainsKey(k)).ToList())
            {
                if (unroll.Entry == entries[key])
                    unroll.Hide();
                entries[key].Root.RemoveFromHierarchy();
                entries.Remove(key);
            }
            foreach (var source in sources)
            {
                if (!entries.TryGetValue(source.Key, out var entry))
                {
                    entry = new EffectEntry(source, Remove, Hover, Use);
                    entry.Arrange(Arrangement);
                    entries.Add(source.Key, entry);
                    list.Add(entry.Root);
                }
                entry.Apply(source);
            }
            Arrange();
            if (selected != null && current.TryGetValue(selected.Key, out var updated))
            {
                selected = updated;
                ended = false;
            }
            else if (selected != null)
                ended = true;
            ValuePulse.Set(Tab, "Effects · " + sources.Count);
            empty.style.display = sources.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            details.Show(preparing ? selected : null, ended, view.Configuration);
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
        }

        // UI-272: the cards the decision uses with a click on the card itself (source id → what the click does).
        public void Uses(IReadOnlyDictionary<string, string> latest)
        {
            if (uses.Count == 0 && latest.Count == 0)
                return;
            uses = latest;
            ApplyContext();
            RefreshChoices();
        }

        private void Use(string id)
        {
            if (uses.ContainsKey(id))
                UseChosen?.Invoke(id);
        }

        private void ApplyContext()
        {
            foreach (var entry in entries.Values)
            {
                if (offers.TryGetValue(entry.Source.SourceId, out var offer))
                    entry.Offer(offer, offersEnabled);
                else
                {
                    choices.TryGetValue(entry.Source.SourceId, out var choice);
                    entry.Context(choice, choicesEnabled, view);
                }
                entry.Use(uses.TryGetValue(entry.Source.SourceId, out var use) ? use : "");
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
            if (preparing)
            {
                if (tray.parent != DecisionHost)
                    DecisionHost.Add(tray);
                if (!wasPreparing || selected?.Key != source.Key)
                    Select(source);
            }
            else if (tray?.parent == DecisionHost)
                dock.Add(tray);
            if (!preparing && wasPreparing)
                selected = null;
            details.Show(preparing ? selected : null, false, view?.Configuration);
            list.style.display = preparing ? DisplayStyle.None : DisplayStyle.Flex;
            if (preparing)
                unroll.Hide();
            RefreshChoices();
        }

        // Opens the panel with the source's text unrolled under its tile, as a hover shows it (UI-446: no details card). A
        // panel that opens now unrolls it once it and the tile are laid out, so the text never stands where the closed
        // panel's stale geometry would put it.
        public void Inspect(string sourceId)
        {
            Open();
            inspecting = entries.Values.FirstOrDefault(item => item.Source.SourceId == sourceId);
            inspectTries = 0;
            if (inspecting != null)
                unrollInspected.ExecuteLater(0);
        }

        private void UnrollInspected()
        {
            if (inspecting == null || !IsOpen)
            {
                inspecting = null;
                return;
            }
            if (!LaidOut(inspecting) && ++inspectTries < InspectTries)
            {
                // An item rescheduled from its own callback is dropped once the callback returns, so each retry is new.
                Root.schedule.Execute(UnrollInspected).ExecuteLater(InspectRetryMilliseconds);
                return;
            }
            var entry = inspecting;
            inspecting = null;
            Hover(entry, true);
        }

        private bool LaidOut(EffectEntry entry) => entry.Root.panel != null && Root.resolvedStyle.display != DisplayStyle.None &&
            entry.Root.layout.height > 0 && !float.IsNaN(entry.Root.layout.height) && list.layout.width > 0;

        // The source whose removal is being prepared; the panel opens for its payment.
        private void Select(EffectSource source)
        {
            selected = source;
            ended = false;
            Open();
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
            Select(source);
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
                offersEnabled && sources.Any(s => offers.ContainsKey(s.SourceId)) || sources.Any(s => uses.ContainsKey(s.SourceId)));
        }

        // UI-221: hovering a tile unrolls its text like a hand card; leaving it rolls it back. Nothing is sent.
        private void Hover(EffectEntry entry, bool over)
        {
            if (over && !preparing && IsOpen)
                unroll.Show(entry, list, Root, view?.Configuration);
            else if (!over && unroll.Entry == entry)
                unroll.Hide();
        }

        // UI-424: a new stage size (another window size or interface scale) rearranges the grid once layout is done; the
        // panel's own geometry never does.
        private void StageChanged(GeometryChangedEvent e)
        {
            if (e.oldRect.size != e.newRect.size)
                rearrange.ExecuteLater(0);
        }

        private void OwnGeometryChanged(GeometryChangedEvent e) => Position();

        // UI-424: the grid for this many sources in the height a docked panel has between the rail's top and the bottom
        // row, read outside layout callbacks; nothing is written while the arrangement stays.
        private void Arrange()
        {
            var next = EffectsGridArrangement.Choose(sources.Count, AvailableHeight());
            if (next == Arrangement)
                return;
            Arrangement = next;
            ApplyArrangement();
        }

        private void ApplyArrangement()
        {
            foreach (var arrangement in EffectsGridArrangement.All)
                Root.EnableInClassList(arrangement.ClassName, arrangement == Arrangement);
            Root.style.width = Arrangement.PanelWidth;
            Root.style.maxWidth = StyleKeyword.None;
            foreach (var entry in entries.Values)
                entry.Arrange(Arrangement);
        }

        // A docked panel stands between the rail's top and the bottom margin; a floating one between both margins.
        private float AvailableHeight()
        {
            var stage = Root.parent;
            if (stage == null || float.IsNaN(stage.contentRect.height) || stage.contentRect.height <= 0)
                return float.NaN;
            var top = Docked ? Ceiling(stage) : EffectsGridArrangement.BottomMargin;
            return stage.contentRect.height - EffectsGridArrangement.BottomMargin - top;
        }

        private float Ceiling(VisualElement stage) => Rail?.panel != null && Rail.worldBound.height > 0
            ? stage.WorldToLocal(Rail.worldBound.position).y
            : DefaultCeiling;

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
                    Arrange();
                }
                Appearance.Display(Root, shown);
                Tab.EnableInClassList("selected", shown);
                if (!shown)
                    Rollback();
            }
            Position();
            Window?.Refresh();
        }

        // UI-424: a closing panel's hovered text goes at once, so while the panel fades out (UI-322) it covers nothing.
        private void Rollback() => unroll.Hide();
    }
}
