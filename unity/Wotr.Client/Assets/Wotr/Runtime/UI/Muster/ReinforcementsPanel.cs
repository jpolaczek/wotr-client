using System;
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-179: the Reserve tab of the left rail, under Politics (UI-181 renamed it from Reinforcements); it replaced the
    // window of UI-130/UI-131. The rows list each own nation's reserves. During a recruitment (a Muster die, or a card
    // or ability plan) a nation that can recruit has a lit row whose tokens can be chosen, the others stay plain and
    // inert. UI-181: a Muster die lights the tab like the other tabs and unrolls nothing by itself; once the player has
    // chosen a reserve the panel stays while the figures are placed, also for the next figure of the same Muster. A card
    // or ability plan is a decision only this panel completes (its ✓), so it unrolls and holds the tab as a hosted
    // decision holds the other tabs. Either way the player's close keeps it closed until the next use, and the end of
    // the use rolls it back.
    public sealed class ReinforcementsPanel
    {
        public Button Tab { get; }
        public VisualElement Root { get; } = new VisualElement { name = "reinforcements-panel" };
        public bool IsOpen => Root.style.display.value == DisplayStyle.Flex;
        // A reserve chosen during the current use keeps the unrolled tab until the use ends or the player closes it.
        public bool Held => active && engaged && !dismissed && IsOpen;
        public event Action Opening;
        public event Action<string> FigureChosen;
        public event Action<string> PlacementRemoved;
        private readonly Label title;
        private readonly Label optional = new Label("Optional") { name = "reinforcements-optional" };
        private readonly VisualElement budget = new VisualElement { name = "reinforcement-budget" };
        private readonly VisualElement rows = new VisualElement { name = "reinforcement-rows" };
        private readonly PlannedReinforcementsView planned;
        private readonly Dictionary<string, ReinforcementRowView> views = new Dictionary<string, ReinforcementRowView>();
        private string focused = "";
        private bool confirmable;
        // The tray's server preview found the plan legal; the console's ✓ commits only then (UI-381).
        private bool legal;
        private bool active;
        private bool engaged;
        private bool dismissed;

        public ReinforcementsPanel()
        {
            Tab = BoardScreen.Button("Reserve", Open);
            Tab.name = "reinforcements-tab";
            Tab.AddToClassList("reinforcements-tab");
            Root.AddToClassList("reinforcements-panel");
            var header = FloatingWindow.Header("Reserve");
            title = header.Q<Label>(className: "title");
            optional.AddToClassList("reinforcements-optional");
            optional.tooltip = "You may confirm without recruiting";
            header.Add(optional);
            budget.AddToClassList("reinforcement-budget");
            budget.tooltip = "Recruitment points · An Elite takes both";
            header.Add(budget);
            // UI-381 (owner): the console's ✓ recruits the plan and finishes a sequence; the panel has no ✓ of its own.
            header.Add(HeaderButton("close", "reinforcements-close", "Close · Keep the chosen die; reopen from the tab or the turn console", Close));
            Root.Add(header);
            rows.AddToClassList("reinforcement-rows");
            Root.Add(rows);
            planned = new PlannedReinforcementsView(id => PlacementRemoved?.Invoke(id));
            FigureChosen += _ => engaged = active;
            Root.Add(planned.Root);
            Root.style.display = DisplayStyle.None;
        }

        // UI-181: a use lights the tab; a use that ends rolls the panel back. Outside a use the rows show the reserves.
        public void Apply(ReinforcementsState state)
        {
            // A card or ability plan, and the next figure of a Muster the player has begun, keep the tab open. UI-392: a plan
            // that starts on the map (an ability's recruitment) lights the tab and opens with its settlement's click.
            Follow(state.Open, state.Planning && !state.RegionFirst || state.Finishable);
            Tab.EnableInClassList("legal-target", state.Open && state.Interactive);
            title.text = state.Title;
            optional.style.display = state.Optional ? DisplayStyle.Flex : DisplayStyle.None;
            budget.style.display = state.Open ? DisplayStyle.Flex : DisplayStyle.None;
            ShowBudget(state.Points, state.Planning);
            confirmable = state.Planning && state.Interactive && state.Confirmable;
            Sync(state.Rows);
            foreach (var row in state.Rows)
                views[row.Id].Apply(row, state);
            planned.Apply(state);
            ShowFocus();
        }

        private void Follow(bool use, bool hold)
        {
            if (use && !active)
            {
                active = true;
                engaged = hold;
                dismissed = false;
                if (hold)
                    Open();
            }
            else if (use && hold && !engaged)
            {
                // UI-271: a plan begun while the panel already followed a Muster die (the Balrog's entry with its
                // optional reserve figure) unrolls it, unless the player closed it for this use.
                engaged = true;
                if (!dismissed)
                    Open();
            }
            else if (!use && active)
            {
                active = false;
                engaged = false;
                dismissed = false;
                focused = "";
                Collapse();
            }
        }

        // The tab, a hover or the context unrolls the panel; it never takes the player's decision away.
        public void Open()
        {
            if (active)
                dismissed = false;
            if (!IsOpen)
            {
                Appearance.Display(Root, true);
                Opening?.Invoke();
            }
            Root.BringToFront();
            Tab.EnableInClassList("selected", true);
        }

        // The tray's or the console's way back to a use the player closed.
        public void Show()
        {
            if (active)
                Open();
        }

        // UI-135: a settlement click brings a closed panel back; an open one keeps its place.
        public void Reopen()
        {
            if (active && !IsOpen)
                Open();
        }

        // The player's close: the panel stays closed until the next use.
        public void Close()
        {
            dismissed = active;
            Collapse();
        }

        // Another left tab or the end of a hover rolls the panel back without dismissing the use.
        public void Collapse()
        {
            Root.style.display = DisplayStyle.None;
            Tab.EnableInClassList("selected", false);
        }

        public void ConfirmLegal(bool value) => legal = value;

        // UI-381: the plan may be recruited with the console's ✓.
        public bool ConfirmReady => confirmable && legal;

        // A settlement inspected on the map marks the row of its nation; any other region clears the mark.
        public void Focus(string nation)
        {
            focused = nation ?? "";
            ShowFocus();
        }

        private static Button HeaderButton(string icon, string name, string tooltip, Action action)
        {
            var button = IconButton.Create(icon, tooltip, action);
            button.name = name;
            return button;
        }

        private void ShowFocus()
        {
            foreach (var pair in views)
                pair.Value.Root.EnableInClassList("reinforcement-focused", active && pair.Key == focused);
        }

        // Muster has two recruitment points; a card or ability shows one slot per figure it may recruit.
        private void ShowBudget((int Spent, int Total) points, bool planning)
        {
            while (budget.childCount < points.Total)
            {
                var slot = new VisualElement();
                slot.AddToClassList("budget-slot");
                budget.Add(slot);
            }
            while (budget.childCount > points.Total)
                budget.RemoveAt(budget.childCount - 1);
            for (var i = 0; i < budget.childCount; i++)
                budget[i].EnableInClassList("filled", i < points.Spent);
            budget.tooltip = planning ? "Planned figures · " + points.Spent + " of " + points.Total
                : "Recruitment points · An Elite takes both";
        }

        // Rows are kept per nation; only a changed set of nations rebuilds them.
        private void Sync(IReadOnlyList<ReinforcementRow> latest)
        {
            var same = latest.Count == views.Count;
            foreach (var row in latest)
                same &= views.ContainsKey(row.Id);
            if (same)
                return;
            rows.Clear();
            views.Clear();
            foreach (var row in latest)
            {
                var view = new ReinforcementRowView(row.Id, row.Nation, id => FigureChosen?.Invoke(id));
                views.Add(row.Id, view);
                rows.Add(view.Root);
            }
        }
    }
}
