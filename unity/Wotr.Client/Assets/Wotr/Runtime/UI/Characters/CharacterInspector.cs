using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // One instance serves the roster, public map figures and awakened sovereigns. UI-139: the card speaks in
    // symbols (statistics, ability chips) with tooltips; the full ability text unrolls on a click. UI-436 (owner): where
    // the figure is is a picture of the place captioned with its name (clicked, a region shows on the map, as Locate
    // did), beside a line saying what being there means and the statistics; the window's actions read in words
    // (AvailabilityRows) instead of a bare "Limits". An awakened ruler's window has the size of every ruler's window.
    public sealed class CharacterInspector
    {
        public VisualElement Root { get; } = new VisualElement { name = "character-inspector" };
        public VisualElement Actions { get; } = new VisualElement { name = "character-actions" };
        public VisualElement DecisionHost { get; } = new VisualElement { name = "character-decision-host" };
        // UI-369: the chips light the abilities usable now and offer Click to use.
        public CharacterAbilityChips Abilities => abilities;
        public string FigureId { get; private set; } = "";
        public string DefinitionId => version;
        public event Action Changed;
        // UI-371: the player is about to change the window's content (an ability's text, a page). A version the decision
        // selects is not such a change: the window still moves clear of the panels the decision opens.
        public event Action ContentChanging;
        public PlacePicture Place { get; }
        public AvailabilityRows Limits { get; } = new AvailabilityRows("character-limits");
        private readonly BoardPresentation data;
        private readonly Label title = BoardScreen.Title("");
        private readonly Label edition = Text("character-edition");
        private readonly Label state = Text("character-state");
        private readonly Label abilityHeading = Text("character-ability-heading");
        private readonly CharacterFactStrip stats = new CharacterFactStrip();
        private readonly CharacterAbilityChips abilities = new CharacterAbilityChips();
        private readonly Label entry = Text("character-entry");
        private readonly VisualElement versions = BoardScreen.Row();
        private CharacterRosterView row;
        private PlayerView view;
        private string version = "";
        private HashSet<string> offeredVersions = new HashSet<string>();

        public CharacterInspector(BoardPresentation data, Action<string> focus, Action close, Action pin)
        {
            this.data = data;
            Root.AddToClassList("character-window");
            Root.AddToClassList("character-inspector");
            Root.style.display = DisplayStyle.None;
            Place = new PlacePicture("locate-character", focus);
            var header = BoardScreen.Row("character-header");
            FloatingWindow.MarkHandle(header);
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
            abilityHeading.AddToClassList("ability-lines-heading");
            Root.Add(abilityHeading);
            abilities.Changing += () => ContentChanging?.Invoke();
            Root.Add(abilities.Root);
            Root.Add(abilities.Pages);
            Root.Add(entry);
            Root.Add(Actions);
            Root.Add(DecisionHost);
        }

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
            // UI-438: a window opened on another character starts on the version its row shows.
            if (FigureId != id)
                version = CharacterRosterPresentation.Section(row) == CharacterRosterSection.NotInPlay
                    ? CharacterRosterPresentation.ShownVersion(row) : "";
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
            RefreshRuler(view.Sovereigns.FirstOrDefault(s => s.Id == row.FigureId));
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

        // UI-438: while more than one version of the shown character may enter now, the other versions' tabs glow; the
        // shown version's entry is its Enter button.
        public void OfferVersions(IReadOnlyCollection<string> enterable)
        {
            var next = enterable.Count > 1 ? new HashSet<string>(enterable) : new HashSet<string>();
            if (next.SetEquals(offeredVersions))
                return;
            offeredVersions = next;
            if (row != null)
                RefreshVersions();
        }

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
            abilities.Pages.style.display = !active && abilities.Paged ? DisplayStyle.Flex : DisplayStyle.None;
        }

        private void ShowLimits(bool shown) => Limits.Root.style.display = shown ? DisplayStyle.Flex : DisplayStyle.None;

        // UI-436: an awakened ruler's window has the rows of every ruler's window — his action in words (or what being
        // awakened means) and his abilities under their heading — and the same size; another character's window shows
        // its own actions only when it has some.
        private void RefreshRuler(SovereignView ruler)
        {
            Root.EnableInClassList(SovereignInspector.RulerWindowClass, ruler != null);
            var lines = ruler != null ? SovereignPresentation.Actions(view, ruler)
                : row.Side == view.You ? AvailabilityLines.For(view, row.FigureId) : new List<AvailabilityLine>();
            Limits.Apply(lines);
            ShowLimits(Limits.Count > 0);
            abilityHeading.text = ruler != null ? SovereignPresentation.AbilityHeading(ruler.Status) : "";
            abilityHeading.style.display = ruler != null ? DisplayStyle.Flex : DisplayStyle.None;
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
                var names = ids.Select(CharacterLibrary.VersionName).ToList();
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
            {
                button.EnableInClassList("selected", (string)button.userData == version);
                ChoiceGlow.Apply(button, offeredVersions.Contains((string)button.userData) && (string)button.userData != version);
            }
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
