using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class RegionHoverPanel
    {
        public VisualElement Root { get; } = new VisualElement { name = "region-hover" };
        public string Region { get; private set; } = "";
        public ComponentActionView Components { get; } = new ComponentActionView();
        public Func<string, bool> Eligible { get; set; } = _ => false;
        public Func<string, bool> Selected { get; set; } = _ => false;
        // UI-127: figures that already marched in this action; they cannot be chosen again.
        public Func<string, bool> Moved { get; set; } = _ => false;
        public event Action<string> FigureChosen;
        public event Action<string> EffectInspected;
        public event Action<string> CharacterInspected;
        // UI-130/UI-135: the nation of an inspected settlement, or "" for any other region; marks its Reinforcements row.
        public event Action<string> SettlementInspected;
        // UI-140: select or leave behind every figure of a force that can move (region, garrison).
        public event Action<string, bool> SelectAllRequested;
        // UI-251: the card choice a force section makes now ("" when none), and its click.
        public Func<Side, bool, string> ForceChoice { get; set; } = (_, _) => "";
        public event Action<string> ForceChosen;
        private readonly List<(VisualElement Force, VisualElement Header, Side Side, bool Garrison)> forceSections =
            new List<(VisualElement, VisualElement, Side, bool)>();
        private readonly Dictionary<string, Button> figures = new Dictionary<string, Button>();
        private readonly Dictionary<string, string> descriptions = new Dictionary<string, string>();
        // UI-187: a character's ability lines, after the line that names it and its state.
        private readonly Dictionary<string, string> abilities = new Dictionary<string, string>();
        private readonly List<(Button Toggle, string[] Ids)> forceToggles = new List<(Button, string[])>();
        private string signature;
        private List<ComponentAction> actions = new List<ComponentAction>();
        private bool actionsEnabled;
        private float contentsWidth = 240;
        private bool quiet;

        public const string NameOnlyClass = "name-only";

        public float PreferredWidth(bool compact) => Math.Min(compact ? 520 : 440, contentsWidth);

        // UI-188: a region with no forces, Fellowship, effects or actions shows only its name and state, in a small
        // window sized to that text.
        public bool NameOnly => quiet && !actions.Any(a => a.Region == Region);

        public string ActionRegion(string key) => actions.FirstOrDefault(a => a.Key == key)?.Region ?? "";

        public void Context(IEnumerable<ComponentAction> latest, bool enabled)
        {
            actions = latest.ToList();
            actionsEnabled = enabled;
            RefreshActions();
        }

        private void RefreshActions()
        {
            Components.Apply(actions.Where(a => a.Region == Region), actionsEnabled);
            Root.EnableInClassList(NameOnlyClass, NameOnly);
            RefreshForces();
        }

        // UI-251: a force the current card may choose glows and its header chooses it.
        public void RefreshForces()
        {
            foreach (var force in forceSections)
            {
                var choice = ForceChoice(force.Side, force.Garrison);
                ChoiceGlow.Apply(force.Force, choice.Length > 0);
                force.Header.tooltip = choice.Length > 0 ? "Click to choose this force" : "";
            }
        }

        public RegionHoverPanel()
        {
            Root.AddToClassList("region-hover");
            Root.style.display = DisplayStyle.None;
            Root.RegisterCallback<PointerDownEvent>(e => e.StopPropagation());
            Root.RegisterCallback<PointerUpEvent>(e => e.StopPropagation());
            Root.RegisterCallback<ClickEvent>(e => e.StopPropagation());
        }

        public void Show(PlayerView view, RegionDefinition region, RegionContents contents)
        {
            contents ??= new RegionContents();
            view.Control.TryGetValue(region.Id, out var control);
            var key = region.Id + "/" + control + "/" + region.Settlement + "/" + region.Nation + "/" +
                string.Join("|", contents.Units.Select(u => u.Id + ":" + u.Kind + ":" + u.Garrison)) + "/" +
                string.Join("|", contents.Characters.Select(c => c.Id + ":" + c.DefinitionId + ":" + c.Garrison + ":" + c.Level + ":" + c.Leadership)) + "/" +
                string.Join("|", contents.Effects.Select(e => e.Id + ":" + e.Description + ":" + e.ExpiryDescription)) + "/" +
                (contents.Fellowship == null ? "" : contents.Fellowship.Hidden + ":" + contents.Fellowship.Progress);
            Region = region.Id;
            if (signature != key)
            {
                signature = key;
                Build(region, control, contents);
            }
            RefreshSelection();
            SettlementInspected?.Invoke(region.Settlement != null ? region.Nation ?? "" : "");
            RefreshActions();
            Root.style.display = DisplayStyle.Flex;
        }

        private void Build(RegionDefinition region, Side? control, RegionContents contents)
        {
            contentsWidth = 240;
            Root.Clear();
            figures.Clear();
            descriptions.Clear();
            abilities.Clear();
            forceToggles.Clear();
            forceSections.Clear();
            Root.Add(BoardScreen.Title(region.Name));
            Root.Add(new Label((region.Settlement ?? "Wilderness") + " · " +
                (region.Nation == null ? "Neutral" : FigureGrid.Name(region.Nation)) + " · " +
                (control.HasValue ? BoardScreen.SideName(control.Value) : "Uncontrolled")) { name = "hover-region-state" });
            var forces = contents.Units.Select(u => (u.Side, u.Garrison))
                .Concat(contents.Characters.Select(c => (c.Side, c.Garrison))).Distinct().OrderBy(f => f.Side).ThenBy(f => f.Garrison);
            foreach (var force in forces)
                Force(contents, force.Side, force.Garrison);
            if (contents.Fellowship != null)
                Root.Add(new Label("Fellowship · " + (contents.Fellowship.Hidden ? "Hidden" : "Revealed") +
                    " · Progress " + contents.Fellowship.Progress)
                {
                    name = "hover-fellowship",
                    tooltip = "Last known position · The Fellowship’s progress does not reveal a route."
                });
            if (figures.Count == 0 && contents.Fellowship == null)
                Root.Add(new Label("No forces") { name = "hover-empty" });
            var effects = BoardScreen.Row("hover-effects");
            foreach (var group in contents.Effects.GroupBy(e => EffectsPresentation.Key(e.SourceId, e.Owner)))
            {
                var effect = group.First();
                var button = BoardScreen.Button("◇ " + (effect.Title.Length > 0 ? effect.Title : CardLibrary.Title(effect.SourceId)),
                    () => EffectInspected?.Invoke(effect.SourceId));
                button.name = "hover-effect-" + effect.SourceId;
                button.tooltip = string.Join("\n", group.Select(EffectsPresentation.Expiry).Distinct());
                effects.Add(button);
            }
            Root.Add(effects);
            Root.Add(Components.Root);
            quiet = figures.Count == 0 && contents.Fellowship == null && !contents.Effects.Any();
        }

        private void Force(RegionContents contents, Side side, bool garrison)
        {
            var units = contents.Units.Where(u => u.Side == side && u.Garrison == garrison).ToArray();
            var characters = contents.Characters.Where(c => c.Side == side && c.Garrison == garrison).ToArray();
            var rowWidth = 24 + units.Length * 36 + characters.Length * 42 +
                units.Select(u => u.Nation).Distinct().Count() * 24;
            contentsWidth = Math.Max(contentsWidth, Math.Max(280, rowWidth));
            var force = new VisualElement();
            force.AddToClassList("hover-force");
            force.AddToClassList(side == Side.FreePeoples ? "fp" : "sh");
            var header = BoardScreen.Row("hover-force-header");
            header.Add(new Label((side == Side.FreePeoples ? "FP" : "SH") + (garrison ? " · Garrison" : " · Field")));
            header.Add(SelectAllToggle(side, garrison, units.Select(u => u.Id).Concat(characters.Select(c => c.Id)).ToArray()));
            header.AddManipulator(new Clickable(() =>
            {
                var choice = ForceChoice(side, garrison);
                if (choice.Length > 0)
                    ForceChosen?.Invoke(choice);
            }));
            force.Add(header);
            forceSections.Add((force, header, side, garrison));
            var row = BoardScreen.Row("hover-unit-row");
            foreach (var nation in units.GroupBy(u => u.Nation))
            {
                var name = FigureGrid.Name(nation.Key);
                row.Add(new Label(name.Substring(0, Math.Min(2, name.Length))) { tooltip = name });
                foreach (var unit in nation.OrderBy(u => u.Kind))
                {
                    AddFigure(row, unit.Id, FigureGrid.Symbol(unit.Kind), FigureGrid.Name(unit.Nation) + " · " + unit.Kind, false);
                    FigureArtwork.Apply(figures[unit.Id], unit);
                }
            }
            foreach (var character in characters)
            {
                AddFigure(row, character.Id, CharacterLibrary.Symbol(character.DefinitionId),
                    CharacterLibrary.Name(character.DefinitionId) + " · Level " + character.Level + " · Leadership " + character.Leadership, true);
                abilities[character.Id] = CharacterAbilitySummaries.Append("", character.DefinitionId);
                FigureArtwork.Apply(figures[character.Id], character);
            }
            force.Add(row);
            Root.Add(force);
        }

        private Button SelectAllToggle(Side side, bool garrison, string[] ids)
        {
            var region = Region;
            var toggle = IconButton.Create("select-all", "Select all · Every figure of this force that can move",
                () => SelectAllRequested?.Invoke(region, garrison));
            toggle.name = "hover-select-all-" + side + (garrison ? "-garrison" : "-field");
            toggle.AddToClassList("hover-select-all");
            forceToggles.Add((toggle, ids));
            return toggle;
        }

        private void AddFigure(VisualElement row, string id, string symbol, string description, bool character)
        {
            var button = BoardScreen.Button(symbol, () =>
            {
                if (Eligible(id))
                    FigureChosen?.Invoke(id);
                else if (character)
                    CharacterInspected?.Invoke(id);
            });
            button.name = "hover-figure-" + id;
            button.AddToClassList(character ? "hover-portrait" : "hover-figure");
            if (character)
                button.AddToClassList(RuntimeTooltip.RichHoverClass);
            button.userData = new DraftToken(DraftObject.Figure, id);
            figures.Add(id, button);
            descriptions.Add(id, description);
            row.Add(button);
        }

        public void RefreshSelection()
        {
            foreach (var (toggle, ids) in forceToggles)
                RefreshToggle(toggle, ids);
            foreach (var entry in figures)
            {
                var eligible = Eligible(entry.Key);
                var selected = Selected(entry.Key);
                var moved = Moved(entry.Key);
                entry.Value.EnableInClassList("eligible-figure", eligible);
                entry.Value.EnableInClassList("selected", selected);
                entry.Value.EnableInClassList(MapArmyArtwork.MovedClass, moved);
                entry.Value.tooltip = descriptions[entry.Key] + (selected ? " · Selected · Click to leave behind" : eligible
                    ? " · Click to select" : moved ? " · Already marched in this action" : "") +
                    (abilities.TryGetValue(entry.Key, out var lines) ? lines : "");
            }
        }

        // Shown only for a force with a figure that can move; it selects them all, or leaves all behind once they are.
        private void RefreshToggle(Button toggle, string[] ids)
        {
            var eligible = ids.Where(Eligible).ToList();
            var all = eligible.Count > 0 && eligible.All(Selected);
            toggle.style.display = eligible.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            IconButton.SetIcon(toggle, all ? "deselect-all" : "select-all");
            toggle.EnableInClassList("all-selected", all);
            toggle.tooltip = all ? "Deselect all · Leave every figure of this force behind"
                : "Select all · Every figure of this force that can move";
        }

        public void Hide() => Root.style.display = DisplayStyle.None;
    }
}
