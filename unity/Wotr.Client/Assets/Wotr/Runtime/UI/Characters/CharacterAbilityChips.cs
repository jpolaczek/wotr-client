using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-139: the abilities of a character card as chips: a scroll symbol and the ability's title, its full text
    // in the tooltip. A click unrolls the full text under the chips; nothing is unrolled by default. Three chips per
    // page, as in UI-66. UI-369: a chip whose ability can be used now glows; selected, its unrolled text carries
    // Click to use, whose click uses the ability. A second click on the selected chip changes nothing (owner, so the
    // window never shrinks under the pointer); another chip or another page changes the selection.
    public sealed class CharacterAbilityChips
    {
        public const string ChipClass = "character-ability";
        public const string TitleClass = "character-ability-title";
        public const string UseClass = "character-ability-use-action";
        private const int PageSize = 3;
        // A command's ability id where it differs from the printed ability it uses.
        private static readonly IReadOnlyDictionary<string, string> CommandChips = new Dictionary<string, string>
        {
            ["voice-recruit"] = "muster-each-isengard-settlement-or-upgrade-two",
            ["voice-upgrade"] = "muster-each-isengard-settlement-or-upgrade-two"
        };
        public event Action<ComponentAction> Use;
        // UI-371: a click changes the unrolled text or the page; the window keeps its place for it.
        public event Action Changing;
        public VisualElement Root { get; } = new VisualElement { name = "character-abilities" };
        public VisualElement Pages { get; } = BoardScreen.Row("character-ability-pages");
        public Label Text { get; } = new Label { name = "character-ability-text", enableRichText = false };
        private readonly Button[] chips = new Button[PageSize];
        private readonly Button previous, next;
        // The parchment under the chips: the text and, for a usable ability, Click to use below it. The label has no
        // children, so it measures its text.
        private readonly VisualElement sheet = new VisualElement { name = "character-ability-sheet" };
        private readonly VisualElement use = new VisualElement { name = "character-ability-use" };
        private readonly ChoicePulse pulse;
        private ILookup<string, ComponentAction> usable = Enumerable.Empty<ComponentAction>().ToLookup(ChipOf);
        private bool editable;
        private Func<string, string> dieName;
        private string shownUse = "";
        private string version = "";
        private string opened = "";
        private int page;

        public CharacterAbilityChips()
        {
            Root.AddToClassList("character-abilities");
            for (var index = 0; index < chips.Length; index++)
            {
                var slot = index;
                chips[index] = BoardScreen.Button("", () => Open(slot));
                chips[index].AddToClassList(ChipClass);
                // Symbol and title are children: a button with children is not sized by its own text.
                var icon = new VisualElement { pickingMode = PickingMode.Ignore };
                icon.AddToClassList("character-ability-icon");
                var title = new Label { pickingMode = PickingMode.Ignore, enableRichText = false };
                title.AddToClassList(TitleClass);
                chips[index].Add(icon);
                chips[index].Add(title);
                Root.Add(chips[index]);
            }
            previous = IconButton.Create("back", "More abilities", () => Turn(-1));
            next = IconButton.Create("next", "More abilities", () => Turn(1));
            previous.name = "character-abilities-previous";
            next.name = "character-abilities-next";
            Pages.Add(previous);
            Pages.Add(next);
            // Inside the chips' row, on a line of its own: a decision context hides both together.
            Text.AddToClassList("character-ability-text");
            Text.style.display = DisplayStyle.None;
            sheet.AddToClassList("character-ability-sheet");
            sheet.style.display = DisplayStyle.None;
            use.AddToClassList("character-ability-use");
            sheet.Add(Text);
            sheet.Add(use);
            Root.Add(sheet);
            pulse = new ChoicePulse(Root);
        }

        public bool Paged => CharacterAbilityLibrary.For(version).Count > PageSize;

        public void Apply(string definition)
        {
            if (definition != version)
            {
                version = definition;
                page = 0;
                opened = "";
            }
            Refresh();
        }

        // UI-369: the figure's actions that use a chip's ability light that chip; the others are returned, for their
        // own buttons.
        public IReadOnlyList<ComponentAction> Usable(IEnumerable<ComponentAction> actions, bool enabled, Func<string, string> dieNames = null)
        {
            dieName = dieNames;
            var printed = CharacterAbilityLibrary.For(version).Select(ability => ability.Id).ToHashSet();
            var all = actions.ToList();
            usable = all.Where(action => printed.Contains(ChipOf(action, printed))).ToLookup(action => ChipOf(action, printed));
            editable = enabled;
            Refresh();
            return all.Where(action => !printed.Contains(ChipOf(action, printed))).ToList();
        }

        // UI-380: an expansion's command names the ability with its prefix ("kings.call-to-arms"); its chip has the
        // printed id ("call-to-arms").
        private static string ChipOf(ComponentAction action, ISet<string> printed)
        {
            var chip = ChipOf(action);
            var bare = chip.Substring(chip.IndexOf('.') + 1);
            return printed.Contains(chip) || !printed.Contains(bare) ? chip : bare;
        }

        // The chip a character action uses; entries and other actions have none.
        public static string ChipOf(ComponentAction action)
        {
            var ability = action.Character?.AbilityId ?? "";
            return CommandChips.TryGetValue(ability, out var chip) ? chip : ability;
        }

        // Unrolls one chip's text, as its click does.
        public void Select(string abilityId)
        {
            opened = abilityId ?? "";
            Refresh();
        }

        private void Turn(int step)
        {
            Changing?.Invoke();
            page += step;
            opened = "";
            Refresh();
        }

        private void Open(int slot)
        {
            var id = chips[slot].userData as string ?? "";
            if (id == opened)
                return;
            Changing?.Invoke();
            opened = id;
            Refresh();
        }

        private void Refresh()
        {
            var entries = CharacterAbilityLibrary.For(version);
            page = Math.Max(0, Math.Min(page, Math.Max(0, (entries.Count - 1) / PageSize)));
            for (var index = 0; index < chips.Length; index++)
            {
                var chip = chips[index];
                var entry = page * PageSize + index;
                chip.style.display = entry < entries.Count ? DisplayStyle.Flex : DisplayStyle.None;
                if (entry >= entries.Count)
                    continue;
                var ability = entries[entry];
                chip.name = "character-ability-" + ability.Id;
                chip.userData = ability.Id;
                chip.Q<Label>(className: TitleClass).text = ability.Title;
                var ready = editable && usable[ability.Id].Any(action => action.Enabled);
                chip.tooltip = ability.Title + "\n" + ability.Text + "\n\n" + ability.Source +
                    (ready ? "\n\nUsable now: click it, then Click to use." : "");
                chip.EnableInClassList("selected", opened == ability.Id);
                ChoiceGlow.Apply(chip, ready);
            }
            var open = entries.FirstOrDefault(a => a.Id == opened);
            Text.text = open == null ? "" : open.Text + "\n" + open.Source;
            Text.style.display = open == null ? DisplayStyle.None : DisplayStyle.Flex;
            sheet.style.display = Text.style.display;
            ShowUse(open == null ? new List<ComponentAction>() : usable[open.Id].ToList());
            pulse.Run(chips.Any(chip => chip.ClassListContains(ChoiceGlow.ClassName)) ||
                use.Children().Any(button => button.ClassListContains(ChoiceGlow.ClassName)));
            Pages.style.display = entries.Count > PageSize ? DisplayStyle.Flex : DisplayStyle.None;
            previous.SetEnabled(page > 0);
            next.SetEnabled((page + 1) * PageSize < entries.Count);
        }

        // One Click to use per action of the unrolled ability (Saruman's Voice has two); rebuilt only when they change.
        private void ShowUse(IReadOnlyList<ComponentAction> actions)
        {
            use.style.display = actions.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            var key = string.Join("|", actions.Select(action => action.Key + ":" + action.Enabled)) + "/" + editable;
            if (key == shownUse)
                return;
            shownUse = key;
            use.Clear();
            foreach (var action in actions)
                use.Add(UseButton(action, actions.Count > 1));
        }

        private Button UseButton(ComponentAction action, bool named)
        {
            var button = BoardScreen.Button("", () => Use?.Invoke(action));
            button.name = "character-ability-use-" + (action.Character?.AbilityId ?? action.Key);
            button.AddToClassList(UseClass);
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList("character-ability-use-icon");
            button.Add(icon);
            var label = named ? "Click to use · " + (action.Character?.Title ?? action.Owner) : "Click to use";
            var title = new Label(label) { pickingMode = PickingMode.Ignore };
            title.AddToClassList("character-ability-use-title");
            button.Add(title);
            button.SetEnabled(editable && action.Enabled);
            // UI-372: a usable Click to use glows like every other choice.
            ChoiceGlow.Apply(button, editable && action.Enabled);
            button.tooltip = UseTooltip(action);
            return button;
        }

        // UI-373: "Use your Event die: <what the ability does>".
        private string UseTooltip(ComponentAction action)
        {
            if (!action.Enabled)
                return "Select an eligible die to use it.";
            var effect = action.Detail.Length > 0 ? action.Detail : "Use this ability.";
            return DieUse.Tooltip(dieName?.Invoke(action.DieId) ?? "", effect);
        }
    }
}
