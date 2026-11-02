using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // One instance serves the roster, public map figures and awakened sovereigns. UI-139: the card speaks in
    // symbols (statistics, ability icons) with tooltips; UI-440: an ability's description is its icon's formatted tooltip,
    // and a lit icon's click uses the ability (CharacterAbilityChips). UI-436 (owner): where
    // the figure is is a picture of the place captioned with its name (clicked, a region shows on the map, as Locate
    // did), beside a line saying what being there means and the statistics; the window's actions read in words
    // (AvailabilityRows) instead of a bare "Limits". UI-441 (owner): while a skill of the character can be used now
    // (UsableSkills) its header glows and breathes with the lit icons, in one pulse.
    public sealed class CharacterInspector
    {
        public const string HeaderGlowClass = "character-header-glow";
        public VisualElement Root { get; } = new VisualElement { name = "character-inspector" };
        public VisualElement Actions { get; } = new VisualElement { name = "character-actions" };
        public VisualElement DecisionHost { get; } = new VisualElement { name = "character-decision-host" };
        // UI-369: the chips light the abilities usable now; UI-440: a lit icon's click uses its ability.
        public CharacterAbilityChips Abilities => abilities;
        public string FigureId { get; private set; } = "";
        public string DefinitionId => version;
        public event Action Changed;
        public PlacePicture Place { get; }
        public AvailabilityRows Limits { get; } = new AvailabilityRows("character-limits");
        private readonly BoardPresentation data;
        private readonly Label title = BoardScreen.Title("");
        private readonly Label edition = Text("character-edition");
        private readonly Label state = Text("character-state");
        private readonly CharacterFactStrip stats = new CharacterFactStrip();
        private readonly CharacterAbilityChips abilities = new CharacterAbilityChips(ownPulse: false);
        private readonly VisualElement header = BoardScreen.Row("character-header");
        private readonly ChoicePulse pulse;
        private readonly Label entry = Text("character-entry");
        private readonly VisualElement versions = BoardScreen.Row();
        private CharacterRosterView row;
        private PlayerView view;
        private string version = "";

        public CharacterInspector(BoardPresentation data, Action<string> focus, Action close, Action pin)
        {
            this.data = data;
            Root.AddToClassList("character-window");
            Root.AddToClassList("character-inspector");
            Root.style.display = DisplayStyle.None;
            Place = new PlacePicture("locate-character", focus);
            pulse = new ChoicePulse(Root);
            FloatingWindow.MarkHandle(header);
            header.Add(HeaderGlow());
            header.Add(title);
            edition.AddToClassList("character-edition");
            header.Add(edition);
            var pinButton = IconButton.Create("pin", "Pin", pin);
            pinButton.name = "pin-character";
            pinButton.tooltip = "Pin this character";
            header.Add(pinButton);
            var dismiss = IconButton.Create("close", "Close", close);
            dismiss.name = "close-character";
            header.Add(dismiss);
            Root.Add(header);
            // UI-387 (owner): the versions are tabs right under the title, switching the card the window shows.
            versions.AddToClassList("character-version-tabs");
            Root.Add(versions);
            Root.Add(PlaceRow());
            Limits.Root.AddToClassList("character-window-actions");
            Root.Add(Limits.Root);
            Root.Add(abilities.Root);
            Root.Add(entry);
            Root.Add(Actions);
            Root.Add(DecisionHost);
        }

        // UI-441: the light behind the header's plate and title; only its opacity breathes.
        private static VisualElement HeaderGlow()
        {
            var glow = new VisualElement { name = HeaderGlowClass, pickingMode = PickingMode.Ignore };
            glow.AddToClassList(HeaderGlowClass);
            return glow;
        }

        // UI-441: the header glows while a skill of the shown version can be used now (the lit icons below say what their
        // click does); the window's one pulse runs while the header or an icon glows.
        public void ShowSkill(UsableSkills.Skill skill)
        {
            ChoiceGlow.Apply(header, skill != null);
            pulse.Run(skill != null || abilities.Lit);
        }

        public bool HeaderLit => header.ClassListContains(ChoiceGlow.ClassName);

        // UI-436: the place's picture, and beside it what being there means and the card's statistics.
        private VisualElement PlaceRow()
        {
            var place = BoardScreen.Row("character-place-row");
            place.Add(Place.Root);
            var facts = new VisualElement { name = "character-place-facts" };
            facts.AddToClassList("character-place-facts");
            state.AddToClassList("character-place-meaning");
            facts.Add(state);
            facts.Add(stats.Root);
            place.Add(facts);
            return place;
        }

        public bool Apply(PlayerView latest, string id)
        {
            var current = CharacterRosterPresentation.Inspect(latest, id);
            if (current == null)
                return false;
            view = latest;
            row = current;
            if (FigureId != id)
                version = CharacterRosterPresentation.Section(row) == CharacterRosterSection.NotInPlay
                    ? row.FutureVersions.FirstOrDefault()?.DefinitionId ?? "" : "";
            FigureId = id;
            if (version != row.CurrentDefinitionId && row.FutureVersions.All(v => v.DefinitionId != version))
                version = row.CurrentDefinitionId;
            if (version.Length == 0)
                version = row.FutureVersions.FirstOrDefault()?.DefinitionId ?? "";
            Refresh();
            return true;
        }

        private void Refresh()
        {
            title.text = version.Length > 0 ? CharacterLibrary.Name(version) : row.Title;
            edition.text = version.Length > 0 ? CharacterLibrary.Edition(version) : "";
            edition.tooltip = edition.text.Length > 0 ? "Card version · " + edition.text : "";
            edition.style.display = edition.text.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            var place = PlacePresentation.For(row, data.Regions);
            Place.Apply(place);
            state.text = place.Meaning;
            var future = row.FutureVersions.FirstOrDefault(v => v.DefinitionId == version);
            stats.Apply(CharacterCardFacts.For(row, version, future == null));
            abilities.Apply(version);
            RefreshLimits();
            RefreshVersions();
            entry.text = future == null ? "" : EntryText(future);
            entry.tooltip = future == null ? "" : EntryTooltip(future);
            entry.EnableInClassList("legal-target", future?.Entry.CanAct == true || future?.CouncilEntry?.CanAct == true);
            entry.style.display = future == null ? DisplayStyle.None : DisplayStyle.Flex;
            Changed?.Invoke();
        }

        // UI-420 (owner): the label reads "Cost: X", as the roster row does; its tooltip adds what the label leaves out,
        // without repeating the cost, then the entry with a Council token under its own heading (no blank line).
        private static string EntryText(CharacterEntryVersionView future) => TooltipText.Lines(
            CharacterRosterPresentation.Cost(future.Entry),
            future.ExtraDiceNextRecovery > 0 ? "+" + future.ExtraDiceNextRecovery + " action die at Recover" : "");

        private static string EntryTooltip(CharacterEntryVersionView future) => TooltipText.Lines(
            future.Entry.Title,
            CharacterRosterPresentation.Requirements(future.Entry),
            CouncilTerms(future.CouncilEntry));

        private static string CouncilTerms(ActionAvailabilityView council) => council == null ? "" : TooltipText.Lines(
            TooltipText.Parts("Or with a Council token", CharacterRosterPresentation.Cost(council)),
            CharacterRosterPresentation.Requirements(council));

        public void SelectVersion(string id)
        {
            if (row == null || id == version || id != row.CurrentDefinitionId && row.FutureVersions.All(item => item.DefinitionId != id))
                return;
            version = id;
            Refresh();
        }

        public void SetDecisionContext(bool active)
        {
            Root.EnableInClassList("character-decision-active", active);
            ShowLimits(!active && Limits.Count > 0);
        }

        private void ShowLimits(bool shown) => Limits.Root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;

        // UI-436: the window shows its own side's actions in words, only when it has some.
        private void RefreshLimits()
        {
            Limits.Apply(row.Side == view.You ? AvailabilityLines.For(view, row.FigureId) : new List<AvailabilityLine>());
            ShowLimits(Limits.Count > 0);
        }

        private void RefreshVersions()
        {
            var ids = row.FutureVersions.Select(v => v.DefinitionId).ToList();
            if (row.CurrentDefinitionId.Length > 0)
                ids.Insert(0, row.CurrentDefinitionId);
            var buttons = versions.Children().OfType<Button>().ToList();
            var focusedVersion = buttons.FirstOrDefault(b => b == Root.panel?.focusController.focusedElement)?.userData as string;
            if (!buttons.Select(b => b.userData as string).SequenceEqual(ids))
            {
                versions.Clear();
                var names = ids.Select(CharacterLibrary.ShortName).ToList();
                var alike = names.Distinct().Count() < names.Count;
                for (var index = 0; index < ids.Count; index++)
                {
                    var id = ids[index];
                    // A tab reads the version's name; the edition is added only where two names are alike.
                    var button = BoardScreen.Button(alike ? CharacterLibrary.Edition(id) + " · " + names[index] : names[index], () =>
                    {
                        version = id;
                        Refresh();
                    });
                    button.name = "character-version-" + id;
                    button.userData = id;
                    button.tooltip = CharacterLibrary.Name(id) + " · " + CharacterLibrary.Edition(id);
                    button.AddToClassList("character-version-tab");
                    versions.Add(button);
                }
            }
            versions.style.display = ids.Count > 1 ? DisplayStyle.Flex : DisplayStyle.None;
            foreach (var button in versions.Children().OfType<Button>())
                button.EnableInClassList("selected", (string)button.userData == version);
            if (focusedVersion != null)
            {
                var replacement = ids.Count > 1 ? versions.Children().OfType<Button>()
                    .FirstOrDefault(b => (string)b.userData == focusedVersion) : null;
                (replacement ?? (Place.Root.enabledSelf ? Place.Root : Root.Q<Button>("pin-character"))).Focus();
            }
        }

        private static Label Text(string name) => new Label { name = name, enableRichText = false };
    }
}
