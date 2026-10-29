using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-391 (owner): a character's printed abilities as small thematic icons (AbilityIcons) on its roster row, the third
    // line of its text: up to four icons on a line of fixed height, so every row keeps one size. An icon is a button inside
    // the row's button: its press stops at the icon, so the row's own click (its window, an entry) does not follow. An
    // icon's tooltip names the ability and its text. An ability usable now glows, and its click starts it as the chip's
    // Click to use does (UI-369); another icon's click opens the character's window with that ability unrolled. The
    // window keeps its chips. UI-408: the four slots stand in two lines of two (LineClass): side by side they make the
    // text's third line, one above the other the 2x2 block beside the text when two characters stand a line. The block
    // never wraps, so its form does not depend on the UI scale.
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
                icons[index] = IconButton.Picture(AbilityPictures.Ability, "Ability", () => Click(slot));
                icons[index].AddToClassList(IconClass);
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

        // The figure's actions that use one of its printed abilities light that ability's icon.
        public void Offer(IEnumerable<ComponentAction> actions, bool enabled, Func<string, string> dieNames)
        {
            var printed = abilities.Select(ability => ability.Id).ToHashSet();
            usable = actions.Where(action => printed.Contains(CharacterAbilityChips.ChipOf(action, printed)))
                .ToLookup(action => CharacterAbilityChips.ChipOf(action, printed));
            editable = enabled;
            dieName = dieNames;
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
                    IconButton.SetPicture(icon, AbilityIcons.For(abilities[index].Id));
            }
        }

        private void Refresh()
        {
            for (var index = 0; index < abilities.Count && index < Slots; index++)
            {
                var ability = abilities[index];
                var ready = Ready(ability.Id);
                icons[index].tooltip = ability.Title + "\n" + ability.Text + "\n\n" + UseText(ability.Id, ready);
                ChoiceGlow.Apply(icons[index], ready.Count > 0);
            }
            for (var index = abilities.Count; index < Slots; index++)
                ChoiceGlow.Apply(icons[index], false);
        }

        // The actions a click can start now; several (Saruman's Voice) are chosen in the window.
        private List<ComponentAction> Ready(string ability) =>
            editable ? usable[ability].Where(action => action.Enabled).ToList() : new List<ComponentAction>();

        private string UseText(string ability, IReadOnlyList<ComponentAction> ready)
        {
            if (ready.Count > 1)
                return "Usable now: click it to choose how in the character's window.";
            if (ready.Count == 0)
                return usable[ability].Any() ? "Usable with an eligible die or token." : "Click to read it in the character's window.";
            var action = ready[0];
            var effect = action.Detail.Length > 0 ? action.Detail : "Use this ability.";
            return "Click to use · " + DieUse.Tooltip(dieName?.Invoke(action.DieId) ?? "", effect);
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
