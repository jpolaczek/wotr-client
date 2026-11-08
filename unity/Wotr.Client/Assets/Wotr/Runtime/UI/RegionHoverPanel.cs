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
        // UI-402: the figures' choices, shared with the map's character miniatures.
        public FigureChoices Choices { get; }
        public Func<string, bool> Eligible { get => Choices.Eligible; set => Choices.Eligible = value; }
        public Func<string, bool> Selected { get => Choices.Selected; set => Choices.Selected = value; }
        // UI-127: figures that already marched in this action; they cannot be chosen again.
        public Func<string, bool> Moved { get => Choices.Moved; set => Choices.Moved = value; }
        // UI-271: what a lit figure's click does when it is not a selection (an ability used on the figure).
        public Func<string, string> Offer { get; set; } = _ => "";
        public event Action<string> FigureChosen;
        public event Action<string> EffectInspected;
        public event Action<string> CharacterInspected;
        // UI-130/UI-135: the nation of an inspected settlement, or "" for any other region; marks its Reinforcements row.
        // The shown settlement's region and nation, or two empty strings for a region without a settlement (UI-412).
        public event Action<string, string> SettlementInspected;
        // UI-140: select or leave behind every figure of a force that can move (region, garrison).
        public event Action<string, bool> SelectAllRequested;
        // UI-251: the card choice a force section makes now ("" when none), and its click. UI-431: what its tooltip says.
        public Func<Side, bool, string> ForceChoice { get; set; } = (_, _) => "";
        public Func<string, string> ForceTooltip { get; set; } = _ => "";
        public event Action<string> ForceChosen;
        private readonly List<(VisualElement Force, VisualElement Header, Side Side, bool Garrison)> forceSections =
            new List<(VisualElement, VisualElement, Side, bool)>();
        private readonly Dictionary<string, Button> figures = new Dictionary<string, Button>();
        // UI-301: the figures a click may choose pulse in ChoiceGlow's rhythm; UI-431: so do the forces a card may choose.
        private readonly ChoicePulse pulse;
        private bool figuresGlowing;
        private bool forcesGlowing;
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
            forcesGlowing = false;
            foreach (var force in forceSections)
            {
                var choice = ForceChoice(force.Side, force.Garrison);
                ChoiceGlow.Apply(force.Force, choice.Length > 0);
                var tooltip = choice.Length > 0 ? ForceTooltip(choice) : "";
                force.Header.tooltip = choice.Length == 0 ? "" : tooltip.Length > 0 ? tooltip : "Click to choose this force";
                // UI-113: a header that chooses signals it and depresses while pressed.
                force.Header.EnableInClassList(PressFeedback.PressableClass, choice.Length > 0);
                forcesGlowing |= choice.Length > 0;
            }
            pulse.Run(figuresGlowing || forcesGlowing);
        }

        public RegionHoverPanel(FigureChoices choices = null)
        {
            Choices = choices ?? new FigureChoices();
            Choices.Changed += ShowChoices;
            Root.AddToClassList("region-hover");
            pulse = new ChoicePulse(Root);
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
            var settlement = region.Settlement != null;
            SettlementInspected?.Invoke(settlement ? region.Id : "", settlement ? region.Nation ?? "" : "");
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
            // UI-532: the place's picture beside the name and a line of symbols (RegionHeader).
            Root.Add(RegionHeader.Create(region, control));
            var forces = contents.Units.Select(u => (u.Side, u.Garrison))
                .Concat(contents.Characters.Select(c => (c.Side, c.Garrison))).Distinct().OrderBy(f => f.Side).ThenBy(f => f.Garrison);
            foreach (var force in forces)
                Force(contents, force.Side, force.Garrison);
            if (contents.Fellowship != null)
                Root.Add(FellowshipLine(contents.Fellowship.Hidden, contents.Fellowship.Progress));
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

        // UI-532: the Fellowship's icon, its state and its Progress, in one line.
        private static VisualElement FellowshipLine(bool hidden, int progress)
        {
            var line = BoardScreen.Row("hover-fellowship-row");
            line.name = "hover-fellowship-row";
            line.tooltip = "The Fellowship · Last known position · The Fellowship’s progress does not reveal a route.";
            line.Add(RegionHeader.Icon("hover-fellowship-icon"));
            line.Add(new Label((hidden ? "Hidden" : "Revealed") + " · Progress " + progress)
            {
                name = "hover-fellowship",
                pickingMode = PickingMode.Ignore
            });
            return line;
        }

        // UI-532: a force's combat strength (its Regulars and Elites, a die each up to five) and its leadership (its
        // Leaders, Nazgûl and characters' Leadership, a re-roll each up to five), each as an icon and a number.
        private static VisualElement Stat(string kind, int value, string tooltip)
        {
            var stat = BoardScreen.Row("hover-stat");
            stat.name = "hover-stat-" + kind;
            stat.tooltip = tooltip;
            stat.Add(RegionHeader.Icon("hover-" + kind + "-icon"));
            stat.Add(new Label(value.ToString()) { pickingMode = PickingMode.Ignore });
            return stat;
        }

        private void Force(RegionContents contents, Side side, bool garrison)
        {
            var units = contents.Units.Where(u => u.Side == side && u.Garrison == garrison).ToArray();
            var characters = contents.Characters.Where(c => c.Side == side && c.Garrison == garrison).ToArray();
            var rowWidth = 24 + units.Length * 36 + characters.Length * 42 +
                units.Select(u => u.Nation).Distinct().Count() * 24;
            contentsWidth = Math.Max(contentsWidth, Math.Max(280, rowWidth));
            // UI-431: named by side and place, as its Select all toggle is.
            var force = new VisualElement { name = "hover-force-" + side + (garrison ? "-garrison" : "-field") };
            force.AddToClassList("hover-force");
            force.AddToClassList(side == Side.FreePeoples ? "fp" : "sh");
            var header = BoardScreen.Row("hover-force-header");
            // UI-532: the side by the section's colour, the place as a word (a garrison with its icon), then the force's
            // strength and leadership.
            if (garrison)
                header.Add(RegionHeader.Icon("hover-garrison-icon"));
            header.Add(new Label(garrison ? "Garrison" : "Field")
            {
                name = "hover-force-place",
                tooltip = BoardScreen.SideName(side) + (garrison ? " · Garrison" : " · Field")
            });
            var strength = units.Count(u => u.Kind == UnitKind.Regular || u.Kind == UnitKind.Elite);
            var leadership = units.Count(u => u.Kind == UnitKind.Leader || u.Kind == UnitKind.Nazgul) + characters.Sum(c => c.Leadership);
            if (strength > 0)
                header.Add(Stat("strength", strength, "Combat strength " + strength + " · rolls " + Math.Min(5, strength) +
                    (Math.Min(5, strength) == 1 ? " die" : " dice")));
            if (leadership > 0)
                header.Add(Stat("leadership", leadership, "Leadership " + leadership + " · re-rolls up to " + Math.Min(5, leadership) +
                    (Math.Min(5, leadership) == 1 ? " miss" : " misses")));
            var spacer = new VisualElement { pickingMode = PickingMode.Ignore };
            spacer.AddToClassList("hover-spacer");
            header.Add(spacer);
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
                // UI-420 (owner): the nation's figures as on its army marker, each figure as printed ("Rohan Elite", "Nazgûl").
                var composition = ArmyMarkerText.Composition(nation.Key, nation.Select(u => u.Kind).ToList());
                // UI-532: the nation's emblem in place of its first two letters.
                var emblem = RegionHeader.Emblem(nation.Key);
                emblem.pickingMode = PickingMode.Position;
                emblem.tooltip = composition;
                emblem.name = "hover-nation-" + nation.Key;
                row.Add(emblem);
                foreach (var unit in nation.OrderBy(u => u.Kind))
                {
                    AddFigure(row, unit.Id, FigureGrid.Symbol(unit.Kind), TooltipText.Unit(unit.Nation, unit.Kind), false);
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

        // Tells every reader of the choices, this panel's chips and the map's figures.
        public void RefreshSelection() => Choices.Refresh();

        private void ShowChoices()
        {
            foreach (var (toggle, ids) in forceToggles)
                RefreshToggle(toggle, ids);
            var glowing = false;
            foreach (var entry in figures)
            {
                var eligible = Eligible(entry.Key);
                var selected = Selected(entry.Key);
                glowing |= eligible && !selected;
                var moved = Moved(entry.Key);
                entry.Value.EnableInClassList("eligible-figure", eligible);
                entry.Value.EnableInClassList("selected", selected);
                entry.Value.EnableInClassList(MapArmyArtwork.MovedClass, moved);
                var offer = eligible ? Offer(entry.Key) : "";
                entry.Value.tooltip = descriptions[entry.Key] + (selected ? " · Selected · Click to leave behind" : offer.Length > 0
                    ? " · " + offer : eligible ? " · Click to select" : moved ? " · Already marched in this action" : "") +
                    (abilities.TryGetValue(entry.Key, out var lines) ? lines : "");
            }
            figuresGlowing = glowing;
            pulse.Run(figuresGlowing || forcesGlowing);
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
