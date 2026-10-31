using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-432 (owner's rule, UI-430): the tray keeps only the buttons no object draws. Stay here is the click on the group's
    // lit origin region (TravelDraft.RouteTo), Stay all the console's ✓ with an empty plan, and Far-sighted Thranduil's
    // component (component-far-sighted-<kind>) — the standard move its plain gesture — so none of them is a button. Undo
    // step and Cancel of a drawn route stay: the console's ↶ is off while the tray is in the Fellowship panel, and a step
    // back is not always an Undo step (the first step adds the origin too), so no object does what they do.
    public sealed class FellowshipChoices
    {
        private readonly List<Action> refresh = new List<Action>();
        private readonly TurnDraft draft;
        private readonly PlayerView view;
        private readonly ActionOption option;
        private readonly Action<Action> edit;
        private readonly BoardPresentation data;
        private readonly bool contextual;

        public FellowshipChoices(VisualElement root, PlayerView view, TurnDraft draft, Action<Action> edit, BoardPresentation data, VisualElement header = null, bool contextual = false)
        {
            this.draft = draft;
            this.view = view;
            this.edit = edit;
            this.data = data;
            this.contextual = contextual;
            option = DraftOptions.For(view, draft);
            if (header == null && (FarSighted() || TravelDraft.IsGrouped(draft.Kind) && option.MaximumGroups != 1))
            {
                header = BoardScreen.Row();
                root.Add(header);
            }
            if (FarSighted())
                header.Add(new Label(FarSightedLabel) { name = "travel-far-sighted" });
            if (TravelDraft.IsGrouped(draft.Kind) && option.MaximumGroups != 1)
                Groups(header);
            if (draft.Kind == CommandKind.SeparateCompanions)
                root.Add(new Label("Separated companions cannot rejoin") { name = "fellowship-separation-hint" });
            if (draft.Kind is CommandKind.SelectGuide or CommandKind.SeparateCompanions || TravelDraft.IsGrouped(draft.Kind))
                Portraits(root, PortraitIds());
            if (FellowshipDraft.UsesRoute(draft.Kind) && option.Travel.Count > 0)
                Route(root);
        }

        // UI-432: a move that uses Far-sighted (Thranduil's component or his Ruler token) names it; the standard one shows
        // nothing.
        private bool FarSighted() => option.RulerTravel != null &&
            (draft.Payload.RulerTokenId.Length > 0 || draft.Payload.AbilityId == option.RulerTravel.AbilityId);

        private string FarSightedLabel => option.RulerTravel.HuntDiceReduction > 0 ? "Far-sighted · Hunt −1" : "Far-sighted · Range +1";

        // UI-298: in the Fellowship panel its roster already offers the Fellowship's own companions; the tray repeats none of
        // them and shows only the rest (a Guide outside the roster, such as Gollum).
        private List<string> PortraitIds() => contextual && !TravelDraft.IsGrouped(draft.Kind)
            ? option.EligibleIds.Where(id => view.OwnCompanions.All(c => c.Id != id)).ToList()
            : option.EligibleIds.ToList();

        private void Portraits(VisualElement root, List<string> ids)
        {
            if (ids.Count == 0)
                return;
            var row = BoardScreen.Row("companion-portraits");
            row.EnableInClassList("dense-travel", ids.Count > 7);
            root.Add(row);
            var pulse = new ChoicePulse(row);
            foreach (var id in ids)
            {
                var guide = draft.Kind == CommandKind.SelectGuide;
                var button = Choice("", "companion-" + id,
                    () => guide ? draft.Payload.GuideId == id : (TravelDraft.IsGrouped(draft.Kind) ? TravelDraft.Group(draft)?.Ids : draft.Payload.Ids)?.Contains(id) == true,
                    () =>
                    {
                        if (guide)
                            draft.Change(p => p.GuideId = id);
                        else
                            TravelDraft.ToggleCompanion(draft, view, option, id);
                    });
                button.AddToClassList("companion-portrait");
                Illustrate(button, id);
                if (id == view.Fellowship.Guide)
                {
                    var badge = new Label("★") { pickingMode = PickingMode.Ignore };
                    badge.AddToClassList("portrait-guide");
                    button.Add(badge);
                }
                var character = view.OwnCompanions.Concat(view.Characters).FirstOrDefault(c => c.Id == id);
                button.tooltip = CharacterLibrary.Name(view, id) + (character == null ? "" : $" · Leadership {character.Leadership}");
                var travel = option.Travel.FirstOrDefault(t => t.Id == id);
                if (travel != null)
                    button.tooltip += travel.Flight ? " · Flying" : " · Range " + travel.Range;
                var origin = travel?.Origin ?? character?.Location;
                if (origin != null && data.Regions.TryGetValue(origin, out var region))
                    button.tooltip += " · " + region.Name;
                // UI-250: a member of another planned group stays shown; its click selects that group. UI-284: a portrait
                // that can still be clicked glows; a chosen one shows its selection instead.
                refresh.Add(() =>
                {
                    var selectable = guide || TravelDraft.Selectable(draft, view, option, id);
                    button.SetEnabled(selectable);
                    ChoiceGlow.Apply(button, selectable && !button.ClassListContains("selected"));
                });
                if (TravelDraft.IsGrouped(draft.Kind))
                    refresh.Add(() => button.EnableInClassList("planned-group", TravelDraft.PlannedGroup(draft, id) >= 0));
                row.Add(button);
            }
            refresh.Add(() => pulse.Run(row.Query(className: ChoiceGlow.ClassName).ToList().Count > 0));
        }

        // UI-284: the figure's painted portrait with its short name under it, as on the Fellowship panel, instead of the
        // name alone; a Nazgûl shows its figure.
        private void Illustrate(Button button, string id)
        {
            var unit = view.Units.FirstOrDefault(u => u.Id == id);
            var character = view.OwnCompanions.Concat(view.Characters).FirstOrDefault(c => c.Id == id);
            if (unit != null)
                FigureArtwork.Apply(button, unit);
            else if (character != null)
                FigureArtwork.Apply(button, character);
            else
                FigureArtwork.Apply(button, FigureAppearances.Character(id, view.You), view.You);
            button.AddToClassList("illustrated-companion");
            button.Add(new Label(CharacterLibrary.ShortName(view, id)) { name = "portrait-name", pickingMode = PickingMode.Ignore });
        }

        // UI-250: the plan's progress, read only: the groups are chosen by clicking their members.
        private void Groups(VisualElement root)
        {
            var progress = new Label { name = "companion-group-progress" };
            refresh.Add(() =>
            {
                var count = draft.Payload.CompanionMoves.Count;
                progress.text = count == 0 ? "No group" : "Group " + (Math.Max(0, draft.CompanionFocus) + 1) + " / " + count;
                progress.tooltip = string.Join("\n", draft.Payload.CompanionMoves.Select((group, index) => index + 1 + ". " +
                    string.Join(", ", group.Ids.Select(id => CharacterLibrary.Name(view, id))) +
                    (group.Route.Count > 0 ? " → " + Region(group.Route[group.Route.Count - 1]) : "")));
            });
            root.Add(progress);
        }

        private void Route(VisualElement root)
        {
            if (FellowshipDraft.LocationRoute(draft.Kind))
                root.Add(new Label(draft.Kind == CommandKind.DeclareFellowship
                    ? "Declare position · Remain hidden" : "Reveal position · Become revealed") { name = "fellowship-route-effect" });
            var row = BoardScreen.Row("travel-summary");
            root.Add(row);
            var summary = new Label { name = "travel-summary" };
            row.Add(summary);
            refresh.Add(() =>
            {
                var route = TravelDraft.Route(draft);
                var travel = TravelDraft.Travel(draft, option);
                summary.text = travel == null ? "Choose a group" : route.Count == 0 ? Region(travel.Origin) + " · " + Prompt
                    : "→ " + Region(route[route.Count - 1]) + (travel.Flight ? " · Flying" : $" · {route.Count - 1} / {travel.Range}");
                summary.tooltip = route == null ? "" : string.Join(" → ", route.Select(Region));
                if (draft.Kind == CommandKind.AwakenSovereign)
                    summary.tooltip = "Click a highlighted destination, or this region to stay." +
                        (summary.tooltip.Length > 0 ? "\n" + summary.tooltip : "");
            });
            if (FellowshipDraft.SingleRoute(draft.Kind))
            {
                var actions = BoardScreen.Row();
                // UI-105: only a drawn Fellowship route has steps to undo; a destination click moves at once.
                if (FellowshipDraft.LocationRoute(draft.Kind))
                {
                    var undo = Choice("Undo step", "route-undo", () => false, () => FellowshipDraft.UndoRoute(draft));
                    undo.tooltip = "Undo step · Remove the last region from the route";
                    refresh.Add(() => undo.SetEnabled(draft.Payload.Route.Count > 0));
                    actions.Add(undo);
                }
                var cancel = Choice("Cancel", "route-cancel", () => false, () => FellowshipDraft.CancelRoute(draft, view));
                cancel.tooltip = draft.Kind == CommandKind.SeparateCompanions
                    ? "Cancel · Clear the chosen companions and their route" : "Cancel · Clear the drawn route";
                // UI-298: a forced separation starts with its companions chosen; Cancel waits until there is something to clear.
                refresh.Add(() => cancel.SetEnabled(FellowshipDraft.CancelChanges(draft, view)));
                actions.Add(cancel);
                root.Add(actions);
            }
        }

        private string Prompt => TravelDraft.TargetRoute(draft.Kind) ? "Choose a destination" : "Choose a route";

        private string Region(string id) => data.Regions.TryGetValue(id, out var region) ? region.Name : id;
        private Button Choice(string label, string name, Func<bool> selected, Action change)
        {
            var button = BoardScreen.Button(label, () => edit(change));
            button.name = name;
            refresh.Add(() => button.EnableInClassList("selected", selected()));
            return button;
        }

        public void Refresh()
        {
            foreach (var action in refresh)
                action();
        }
    }
}
