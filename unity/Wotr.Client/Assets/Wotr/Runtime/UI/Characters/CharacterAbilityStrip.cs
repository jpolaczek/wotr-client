using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-391 (owner): a character's printed abilities as small icons on its roster row (UI-439: each ability's own icon,
    // AbilityArt), the third line of its text: up to four icons on a line of fixed height, so every row keeps one size. An
    // icon is a button inside the row's button: its press stops at the icon, so the row's own click (its window, an
    // entry) does not follow. An ability usable now glows, and its click starts it as the window's icon does; another
    // icon's click opens the character's window at that ability. UI-408: the four slots stand in two lines of two
    // (LineClass): side by side they make the text's third line, one above the other the 2x2 block beside the text when two
    // characters stand a line. The block never wraps, so its form does not depend on the UI scale. UI-440 (owner): an icon
    // is the bare skill icon (SkillIcon), without the small gilt frame, and its tooltip the skill's formatted description
    // (SkillTooltip), as in the window. UI-441: an icon whose one die can pay before any die is chosen glows and uses it,
    // as the window's does (UsableSkills).
    public sealed class CharacterAbilityStrip
    {
        public const string ClassName = "roster-abilities";
        public const string IconClass = "roster-ability";
        public const string LineClass = "roster-ability-line";
        private const int Slots = 4;
        private const int SlotsPerLine = 2;
        public VisualElement Root { get; } = new VisualElement();
        public event Action<ComponentAction> Used;
        public event Action<string> Opened;
        private readonly Button[] icons = new Button[Slots];
        private readonly string figure;
        private ILookup<string, ComponentAction> usable = Enumerable.Empty<ComponentAction>().ToLookup(action => "");
        private IReadOnlyList<CharacterAbilityText> abilities = Array.Empty<CharacterAbilityText>();
        private Func<string, string> dieName;
        private PlayerView view;
        private bool editable;
        private string version = "";

        public CharacterAbilityStrip(string figureId)
        {
            figure = figureId;
            Root.name = "roster-abilities-" + figureId;
            Root.AddToClassList(ClassName);
            // Between the icons the row takes the click.
            Root.pickingMode = PickingMode.Ignore;
            for (var index = 0; index < Slots; index++)
            {
                var slot = index;
                icons[index] = SkillIcon.Create(AbilityArt.Unknown, "Ability", () => Click(slot));
                icons[index].AddToClassList(IconClass);
                icons[index].AddToClassList(RuntimeTooltip.FormattedClass);
            }
            for (var first = 0; first < Slots; first += SlotsPerLine)
                Root.Add(Line(first));
        }

        private VisualElement Line(int first)
        {
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList(LineClass);
            for (var index = first; index < first + SlotsPerLine; index++)
                line.Add(icons[index]);
            return line;
        }

        public bool Offered => icons.Any(icon => icon.ClassListContains(ChoiceGlow.ClassName));

        public void Apply(string definition)
        {
            if (definition == version)
                return;
            version = definition;
            abilities = CharacterAbilityLibrary.For(definition);
            Layout();
            Refresh();
        }

        // The figure's actions that use one of its printed abilities light that ability's icon. latest: the view whose dice
        // may pay for an ability before any die is chosen.
        public void Offer(IEnumerable<ComponentAction> actions, bool enabled, Func<string, string> dieNames, PlayerView latest = null)
        {
            var printed = abilities.Select(ability => ability.Id).ToHashSet();
            usable = actions.Where(action => CharacterAbilitySlots.PrintedOf(action, printed).Length > 0)
                .ToLookup(action => CharacterAbilitySlots.PrintedOf(action, printed));
            editable = enabled;
            dieName = dieNames;
            view = latest;
            Refresh();
        }

        // The icons of this version's abilities; the other slots are not shown (the line keeps its height).
        private void Layout()
        {
            for (var index = 0; index < Slots; index++)
            {
                var icon = icons[index];
                var shown = index < abilities.Count;
                icon.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;
                icon.userData = shown ? abilities[index].Id : null;
                icon.name = shown ? "roster-ability-" + figure + "-" + abilities[index].Id : "";
                if (shown)
                    IconButton.SetPicture(icon, AbilityArt.For(version, abilities[index].Id));
            }
        }

        private void Refresh()
        {
            for (var index = 0; index < abilities.Count && index < Slots; index++)
            {
                var ability = abilities[index];
                var ready = Ready(ability.Id);
                icons[index].tooltip = Tooltip(ability.Id, ready);
                ChoiceGlow.Apply(icons[index], ready.Count > 0);
            }
            for (var index = abilities.Count; index < Slots; index++)
                ChoiceGlow.Apply(icons[index], false);
        }

        private string Tooltip(string ability, IReadOnlyList<ComponentAction> ready)
        {
            var parts = SkillTooltip.Of(version, ability);
            parts.State = UseText(ability, ready);
            parts.Ready = ready.Count > 0;
            return SkillTooltip.Format(parts);
        }

        // The actions a click can start now — offered with the chosen die, or else payable by their one die; several
        // (Saruman's Voice) are chosen in the window.
        private List<ComponentAction> Ready(string ability)
        {
            if (!editable)
                return new List<ComponentAction>();
            var ready = usable[ability].Where(action => action.Enabled).ToList();
            return ready.Count > 0 ? ready : usable[ability].Where(action => UsableSkills.UniformDie(view, action).Length > 0).ToList();
        }

        private string UseText(string ability, IReadOnlyList<ComponentAction> ready)
        {
            if (ready.Count > 1)
                return SkillUse.Several;
            if (ready.Count == 1)
                return SkillUse.Ready(ready[0], DieName(ready[0]));
            if (usable[ability].Any())
                return SkillUse.WithDie;
            var place = AbilityUsePlaces.For(ability);
            return place.Length > 0 ? place : SkillUse.OpenWindow;
        }

        // The chosen die's name, or the one die's that the click takes.
        private string DieName(ComponentAction action)
        {
            var die = action.Enabled ? action.DieId : UsableSkills.UniformDie(view, action);
            return dieName?.Invoke(die) ?? "";
        }

        private void Click(int slot) => Choose(icons[slot].userData as string ?? "");

        // An icon's click: its one usable action starts; otherwise the window opens at the ability.
        public void Choose(string ability)
        {
            if (abilities.All(item => item.Id != ability))
                return;
            var ready = Ready(ability);
            if (ready.Count == 1)
                Used?.Invoke(ready[0]);
            else
                Opened?.Invoke(ability);
        }
    }
}
