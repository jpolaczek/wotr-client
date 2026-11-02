using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-139/UI-369/UI-436 gave a card's abilities chips, then lines that unrolled their text and offered Click to use.
    // UI-440 (owner: "the click simply uses the skill; the descriptions in nicely formatted tooltips"): each ability is its
    // bare icon (SkillIcon, AbilityArt) in one row, in printed order; an ability whose commands differ shows one badged icon
    // per variant (AbilityVariants), side by side in one group, its slots fixed. A lit icon (its ability usable now) uses it
    // at once with one click, through the component path (Use → ComponentActions.Select): a one-click skill sends, a
    // click-then-choose skill starts its choice on the board. Every other icon is read only — a passive, triggered,
    // reaction or entry ability, or one waiting for its die (UI-443: also when one die alone could pay) — and only explains
    // itself: the formatted tooltip (SkillTooltip) names the ability, gives its gist, printed text and source, and says what
    // a click does now, what it waits for or where the ability is used (AbilityUsePlaces). UI-441: an icon glows exactly
    // while its click uses the ability now, the condition UsableSkills shares, so its character lights with it.
    public sealed class CharacterAbilityChips
    {
        public const string ChipClass = "character-ability";
        public const string GroupClass = "character-ability-group";
        public const string VariantsClass = "character-ability-variants";
        // A variant that cannot be used now is dimmed in its fixed slot.
        public const string IdleClass = "skill-icon-idle";
        public event Action<ComponentAction> Use;
        public VisualElement Root { get; } = new VisualElement { name = "character-abilities" };
        private readonly List<Icon> icons = new List<Icon>();
        // The window's own pulse when it hosts more lights than the icons (UI-441: the header); otherwise the row's.
        private readonly ChoicePulse pulse;
        private ILookup<string, ComponentAction> offered = Enumerable.Empty<ComponentAction>().ToLookup(action => "");
        private bool editable;
        private PlayerView view;
        private string version;

        // One icon: its slot, its button, and the action its click uses now (none while it is read only).
        private sealed class Icon
        {
            public Icon(CharacterAbilitySlots.Slot slot) => Slot = slot;

            public CharacterAbilitySlots.Slot Slot { get; }
            public Button Button { get; set; }
            public ComponentAction Action { get; set; }
        }

        // ownPulse: false when the host runs one pulse for the icons and its other lights (CharacterInspector).
        public CharacterAbilityChips(bool ownPulse = true)
        {
            Root.AddToClassList("character-abilities");
            pulse = ownPulse ? new ChoicePulse(Root) : null;
        }

        // UI-441: some icon's click uses its ability now.
        public bool Lit => icons.Any(icon => icon.Button.ClassListContains(ChoiceGlow.ClassName));

        // The card's icons, built once per card.
        public void Apply(string definition)
        {
            if (definition != version)
            {
                version = definition;
                Build();
            }
            Refresh();
        }

        // UI-369: the figure's actions that use one of the card's printed abilities light their icons (UI-440: an action of
        // the window's former Actions row too — Narya, a Muster die's move or attack, Far-sighted); the others are returned,
        // for their own buttons.
        public IReadOnlyList<ComponentAction> Usable(IEnumerable<ComponentAction> actions, bool enabled, PlayerView latest = null)
        {
            view = latest;
            var printed = CharacterAbilityLibrary.For(version ?? "").Select(ability => ability.Id).ToHashSet();
            var all = actions.ToList();
            offered = all.Where(action => CharacterAbilitySlots.KeyOf(action, printed).Length > 0)
                .ToLookup(action => CharacterAbilitySlots.KeyOf(action, printed));
            editable = enabled;
            Refresh();
            return all.Where(action => CharacterAbilitySlots.KeyOf(action, printed).Length == 0).ToList();
        }

        // Shows one ability's description: its first icon takes the focus, so its tooltip opens at once.
        public void Present(string abilityId)
        {
            var icon = icons.FirstOrDefault(item => item.Slot.Ability.Id == abilityId)?.Button;
            icon?.schedule.Execute(() => icon.Focus());
        }

        public IEnumerable<Button> Icons => icons.Select(icon => icon.Button);

        private void Build()
        {
            Root.Clear();
            icons.Clear();
            foreach (var group in CharacterAbilitySlots.For(version).GroupBy(slot => slot.Ability.Id))
                Root.Add(Group(group.Key, group));
        }

        // UI-440 (owner): an ability's icons stand together, one group that never splits across rows.
        private VisualElement Group(string ability, IEnumerable<CharacterAbilitySlots.Slot> slots)
        {
            var group = new VisualElement { name = "character-ability-group-" + ability, pickingMode = PickingMode.Ignore };
            group.AddToClassList(GroupClass);
            group.EnableInClassList(VariantsClass, AbilityVariants.Grouped(ability));
            foreach (var slot in slots)
                group.Add(Create(slot));
            return group;
        }

        private Button Create(CharacterAbilitySlots.Slot slot)
        {
            var icon = new Icon(slot);
            icon.Button = SkillIcon.Create(AbilityArt.For(version, slot.Ability.Id), slot.Ability.Title, () => Click(icon));
            icon.Button.name = slot.Name;
            icon.Button.userData = slot.Ability.Id;
            icon.Button.AddToClassList(ChipClass);
            icon.Button.AddToClassList(RuntimeTooltip.FormattedClass);
            if (slot.Variant.Badge.Length > 0)
                IconButton.AddBadge(icon.Button, slot.Variant.Badge);
            icons.Add(icon);
            return icon.Button;
        }

        // An icon's click, by the icon's name: uses its action when it has one; false for a read-only icon.
        public bool Choose(string iconName)
        {
            var icon = icons.FirstOrDefault(item => item.Button.name == iconName);
            return icon != null && Click(icon);
        }

        private bool Click(Icon icon)
        {
            if (icon.Action == null)
                return false;
            Use?.Invoke(icon.Action);
            return true;
        }

        private void Refresh()
        {
            foreach (var icon in icons)
                RefreshIcon(icon);
            pulse?.Run(Lit);
        }

        // A usable icon glows and takes clicks; any other is read only, a variant among them dimmed in its slot.
        private void RefreshIcon(Icon icon)
        {
            var actions = editable ? offered[icon.Slot.Key].ToList() : new List<ComponentAction>();
            icon.Action = actions.FirstOrDefault(UsableSkills.Usable);
            var button = icon.Button;
            ReadOnlyControl.Apply(button, icon.Action != null);
            button.EnableInClassList(IdleClass, icon.Action == null && AbilityVariants.Grouped(icon.Slot.Ability.Id));
            ChoiceGlow.Apply(button, icon.Action != null);
            var tooltip = Tooltip(icon.Slot, State(icon.Slot, actions, icon.Action), icon.Action != null);
            if (button.tooltip != tooltip)
                button.tooltip = tooltip;
        }

        // UI-443: an ability its die has not been chosen for waits for it, even when one die alone could pay.
        private string State(CharacterAbilitySlots.Slot slot, IReadOnlyList<ComponentAction> actions, ComponentAction ready)
        {
            if (ready != null)
                return SkillUse.Ready(ready, DieName(ready.DieId));
            return actions.Count > 0 ? SkillUse.ChooseDie : AbilityUsePlaces.For(slot.Ability.Id);
        }

        private string Tooltip(CharacterAbilitySlots.Slot slot, string state, bool usable)
        {
            var parts = SkillTooltip.Of(version, slot.Ability.Id);
            parts.Variant = slot.Variant.Name;
            parts.State = state;
            parts.Ready = usable;
            return SkillTooltip.Format(parts);
        }

        private string DieName(string die) => DieUse.Name(view, die);
    }
}
