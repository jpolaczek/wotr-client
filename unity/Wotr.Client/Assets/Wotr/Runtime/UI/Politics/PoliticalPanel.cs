using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class PoliticalPanel
    {
        public Button Root { get; }
        public VisualElement Drawer { get; } = new VisualElement { name = "politics-panel" };
        public bool Pinned => hover.Pinned;
        public bool IsOpen => hover.Visible;
        public FloatingWindow Window { get; set; }
        // UI-197: a short caption keeps the narrower header clear of the title; the tooltip says it in full.
        public Toggle ShowOpponent { get; } = new Toggle("Opponent") { name = "politics-show-opponent", value = true,
            tooltip = "Show the opponent's nations" };
        public Func<string, bool> SelectTarget { get; set; }
        // UI-366: region names for the abilities' explanations.
        public Func<string, string> RegionName { get; set; }
        // UI-366: the political abilities the selected die pays for, by nation; set before RefreshActions.
        public IReadOnlyDictionary<string, CharacterPolitics.Offer> Abilities { get; set; } =
            new Dictionary<string, CharacterPolitics.Offer>();
        public event Action<string> Advanced;
        public event Action<string> Activated;
        public event Action Opening;
        private readonly Dictionary<string, NationTrackRow> rows = new Dictionary<string, NationTrackRow>();
        private readonly VisualElement tracks = new VisualElement { name = "politics-tracks" };
        private readonly PoliticalHover hover;
        private readonly Dictionary<string, Side> sides;
        // A card's nation targets and what each click does ("" for the general hint).
        private IReadOnlyDictionary<string, string> cardTargets = new Dictionary<string, string>();
        private PlayerView view;
        private TurnDraft draft = new TurnDraft();
        private ActionOption advancement;
        private string selected = "";
        private string dragged;
        private bool interactive;
        // The time an own step's token takes to travel, the server's answer included.
        private const long OwnStepMilliseconds = 2000;
        private IVisualElementScheduledItem ownStep;
        private bool visible;

        public PoliticalPanel(VisualElement surface, BoardData board)
        {
            sides = board.Nations.ToDictionary(n => n.Id, n => n.Side);
            Root = BoardScreen.Button("Politics", Open);
            Root.name = "politics-tab";
            Root.AddToClassList("politics-tab");
            Root.AddToClassList(RuntimeTooltip.RichHoverClass);
            Root.tooltip = "Political tracks · Inspect nations and advance toward war";
            Root.style.display = DisplayStyle.None;
            Drawer.AddToClassList("politics-panel");
            Drawer.style.display = DisplayStyle.None;
            var header = BoardScreen.Row("politics-header");
            FloatingWindow.MarkHandle(header);
            header.Add(new Label("POLITICS"));
            header.Add(ShowOpponent);
            var close = IconButton.Create("close", "Close", Close);
            close.name = "close-politics";
            header.Add(close);
            Drawer.Add(header);
            AddAxis();
            tracks.AddToClassList("politics-tracks");
            Drawer.Add(tracks);
            ShowOpponent.RegisterValueChangedCallback(_ => Filter());
            hover = new PoliticalHover(surface, Drawer);
            hover.Changed += RefreshVisibility;
        }

        private void AddAxis()
        {
            var axis = BoardScreen.Row("politics-axis");
            axis.Add(Legend());
            foreach (var text in new[] { "Peace", "·", "·", "War" })
            {
                var label = new Label(text);
                label.AddToClassList("politics-axis-step");
                axis.Add(label);
            }
            // UI-178: the axis keeps the step arrows' column so its headings stand over the stations.
            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("politics-axis-spacer");
            axis.Add(spacer);
            Drawer.Add(axis);
        }

        // UI-197: a dimmed and a lit medallion, as the nations' crests look on the track. A label, like the nation names,
        // so the axis column lays out exactly as theirs.
        private static VisualElement Legend()
        {
            var legend = new Label { name = "politics-legend", tooltip = "Passive / Active · Only active nations may enter war" };
            legend.AddToClassList("nation-name");
            legend.AddToClassList("politics-legend");
            legend.Add(LegendToken("passive-nation"));
            legend.Add(LegendToken("active-nation"));
            return legend;
        }

        private static VisualElement LegendToken(string state)
        {
            var token = new VisualElement { name = "politics-legend-" + state, pickingMode = PickingMode.Ignore };
            token.AddToClassList("politics-legend-token");
            token.AddToClassList(state);
            var crest = new VisualElement { pickingMode = PickingMode.Ignore };
            crest.AddToClassList("nation-emblem");
            crest.AddToClassList("nation-emblem-free-peoples");
            token.Add(crest);
            return token;
        }

        public void Apply(PlayerView latest)
        {
            if (view != null && view.MatchId != latest.MatchId)
                Close();
            view = latest;
            Root.style.display = view.Nations.Count == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            var ids = view.Nations.Select(n => n.Id).ToHashSet();
            foreach (var id in rows.Keys.Where(id => !ids.Contains(id)).ToList())
            {
                rows[id].Root.RemoveFromHierarchy();
                rows.Remove(id);
            }
            var index = 0;
            foreach (var nation in view.Nations.OrderBy(n => sides[n.Id] == view.You ? 0 : 1))
            {
                if (!rows.TryGetValue(nation.Id, out var row))
                {
                    row = new NationTrackRow(nation.Id, Select, id => Advanced?.Invoke(id), id => Activated?.Invoke(id));
                    rows.Add(nation.Id, row);
                }
                if (row.Root.parent != tracks || tracks.IndexOf(row.Root) != index)
                    tracks.Insert(index, row.Root);
                row.Apply(nation, sides[nation.Id], view.You);
                index++;
            }
            Filter();
            RefreshRows();
        }

        public void RefreshActions(ActionOption option, string nation, bool enabled, TurnDraft selection)
        {
            advancement = option;
            selected = nation;
            interactive = enabled;
            draft = selection;
            RefreshRows();
        }

        public void Highlight(IEnumerable<string> targets) => Highlight(targets.Distinct().ToDictionary(id => id, _ => ""));

        // UI-278: the Kings cards' nation choices say what their token's click does.
        public void Highlight(IReadOnlyDictionary<string, string> targets)
        {
            cardTargets = targets;
            RefreshRows();
        }

        public void PreviewDrag(DraftToken token)
        {
            var nation = token?.Object == DraftObject.Nation ? token.Id : null;
            if (dragged == nation)
                return;
            dragged = nation;
            RefreshRows();
        }

        private void RefreshRows()
        {
            if (view == null)
                return;
            var activationAvailable = false;
            foreach (var row in rows.Values)
            {
                var actions = Actions(row);
                activationAvailable |= actions.CanActivate;
                row.Refresh(actions);
            }
            Root.EnableInClassList("legal-target", cardTargets.Count > 0 || advancement?.Targets.Count > 0 || activationAvailable ||
                interactive && Abilities.Count > 0);
        }

        // UI-148: while a card waits for its nation, the tokens choose that nation; the die's own steps and
        // activations step aside so a click cannot turn the card into another action.
        private NationRowActions Actions(NationTrackRow row)
        {
            var id = row.Nation.Id;
            var own = row.Own && cardTargets.Count == 0;
            var step = advancement?.Targets.Contains(id) == true;
            var ability = Abilities.TryGetValue(id, out var offer) ? offer : null;
            return new NationRowActions
            {
                CanAdvance = interactive && own && (step || ability != null),
                AdvanceHint = ability == null || step && !ability.Activates ? "" : CharacterPolitics.Hint(view, ability, RegionName),
                Highlighted = (dragged ?? selected) == id,
                CardTarget = cardTargets.ContainsKey(id),
                CardHint = cardTargets.TryGetValue(id, out var hint) ? hint : "",
                ActivationOffered = own && PoliticalActions.ActivationOffered(view, draft, id),
                CanActivate = interactive && own && PoliticalActions.CanActivate(view, draft, id),
                ActivationHint = PoliticalActions.ActivationHint(view, draft, id)
            };
        }

        private void Filter()
        {
            var count = 0;
            foreach (var row in rows.Values)
            {
                var show = row.Own || ShowOpponent.value;
                row.Root.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
                if (show)
                    count++;
            }
            Drawer.style.maxHeight = count * 34 + 50;
        }

        // UI-144: a card's choice comes first; otherwise the click is the step itself when the die can move it.
        private void Select(string nation)
        {
            if (SelectTarget?.Invoke(nation) != true)
                Advanced?.Invoke(nation);
        }

        // UI-144: opening an already open panel raises it above windows a Muster die opened.
        public void Open()
        {
            if (view?.Nations.Count > 0)
            {
                hover.Open();
                Drawer.BringToFront();
            }
        }

        // UI-366: an own political step opens the panel and its token travels as in the opponent's political beat.
        public void PresentOwnStep()
        {
            Open();
            Drawer.AddToClassList(PoliticsPresenter.MovingClass);
            ownStep?.Pause();
            ownStep = Drawer.schedule.Execute(() => Drawer.RemoveFromClassList(PoliticsPresenter.MovingClass)).StartingIn(OwnStepMilliseconds);
        }

        public void Close() => hover.Close();
        public void CloseTransient() { if (!Pinned) Close(); }

        private void RefreshVisibility()
        {
            if (visible != hover.Visible)
            {
                visible = hover.Visible;
                if (visible)
                {
                    Opening?.Invoke();
                    Drawer.BringToFront();
                }
                Appearance.Display(Drawer, visible);
                Root.EnableInClassList("selected", visible);
            }
            Window?.Refresh();
        }
    }
}
