using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-139: a roster row speaks in symbols: the portrait carries the level, the second line a status symbol and
    // the region or a short state; the row's tooltip reads the full state and statistics.
    // UI-391: a third line holds the character's ability icons, inside the row, so every row keeps its size.
    internal sealed class CharacterRosterRow
    {
        public Button Root { get; }
        public CharacterAbilityStrip Abilities { get; }
        // The version the row shows: the next one for a character not in play, the current one otherwise.
        public string Definition { get; private set; } = "";
        public const string RowClass = "character-roster-row";
        private readonly Image portrait = new Image { pickingMode = PickingMode.Ignore, scaleMode = UnityEngine.ScaleMode.ScaleToFit };
        private readonly Label level = new Label { name = "character-roster-level", pickingMode = PickingMode.Ignore, enableRichText = false };
        private readonly Label title = new Label { name = "character-roster-title" };
        private readonly VisualElement statusIcon = new VisualElement { name = "character-roster-status", pickingMode = PickingMode.Ignore };
        private readonly Label detail = new Label { name = "character-roster-detail" };
        private string status = "";
        private string description = "";
        private CharacterRosterView shown;
        private bool offered;
        private bool chosen;
        private string statusText = "";
        // UI-243: the chosen row while its entry waits for a region: solid gold and a short instruction.
        public const string ChosenClass = "entry-chosen";
        public const string ChosenHint = "Choose a highlighted region";

        public CharacterRosterRow(string id)
        {
            Root = BoardScreen.Button("", null);
            Root.name = "character-roster-" + id;
            Root.AddToClassList(RowClass);
            // UI-108: a row of the Characters list, distinct from a figure on the map; its double-click enters it.
            Root.userData = new DraftToken(DraftObject.Character, id);
            portrait.AddToClassList("character-roster-portrait");
            level.AddToClassList("character-roster-level");
            portrait.Add(level);
            title.AddToClassList("character-roster-title");
            statusIcon.AddToClassList("character-status-icon");
            detail.AddToClassList("character-roster-detail");
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.AddToClassList("character-roster-line");
            line.Add(statusIcon);
            line.Add(detail);
            var text = new VisualElement();
            text.AddToClassList("character-roster-text");
            text.Add(title);
            text.Add(line);
            Abilities = new CharacterAbilityStrip(id);
            text.Add(Abilities.Root);
            Root.Add(portrait);
            Root.Add(text);
        }

        public void Apply(CharacterRosterView entry, BoardPresentation data)
        {
            var candidate = CharacterRosterPresentation.Section(entry) == CharacterRosterSection.NotInPlay
                ? entry.FutureVersions.FirstOrDefault() : null;
            var definition = candidate?.DefinitionId ?? entry.CurrentDefinitionId;
            Definition = definition;
            Abilities.Apply(definition);
            title.text = CharacterLibrary.ShortName(definition);
            portrait.sprite = FigureArtwork.Sprite(FigureAppearances.Character(definition, entry.Side));
            portrait.EnableInClassList("free-peoples", entry.Side == Side.FreePeoples);
            portrait.EnableInClassList("shadow", entry.Side == Side.Shadow);
            var location = data.Regions.TryGetValue(entry.RegionId, out var region) ? region.Name : "";
            var state = CharacterCardFacts.Status(entry, location);
            statusText = state.Short;
            detail.text = chosen ? ChosenHint : statusText;
            statusIcon.RemoveFromClassList("character-status-" + status);
            statusIcon.AddToClassList("character-status-" + state.Key);
            status = state.Key;
            var facts = CharacterCardFacts.For(entry, definition, candidate == null);
            level.text = facts.First(f => f.Key == "level").Value;
            level.style.display = level.text == "?" ? DisplayStyle.None : DisplayStyle.Flex;
            var edition = CharacterLibrary.Edition(definition);
            var fullLocation = location.Length > 0
                ? location + (entry.Garrison ? " · Garrison" : "") : CharacterRosterPresentation.State(entry);
            // UI-187: the card's abilities follow in their short form.
            shown = entry;
            description = CharacterAbilitySummaries.Append(CharacterRosterPresentation.Title(entry) + (edition.Length > 0 ? " · " + edition : "") +
                "\n" + fullLocation + "\n" + CharacterCardFacts.Summary(facts) +
                (candidate == null ? "" : "\n" + CharacterRosterPresentation.Conditions(candidate.Entry)), definition);
            ShowOffer();
            Root.EnableInClassList("legal-target", entry.FutureVersions.Any(v => v.Entry.CanAct || v.CouncilEntry?.CanAct == true));
        }

        // UI-240: a row whose click enters a character, or changes one into its next version, glows and pulses
        // (ChoiceGlow), and its tooltip starts with what the click does.
        public void Offer(bool entry)
        {
            offered = entry;
            ShowOffer();
        }

        public void Choose(bool value)
        {
            chosen = value;
            Root.EnableInClassList(ChosenClass, value);
            detail.text = value ? ChosenHint : statusText;
            detail.EnableInClassList("entry-chosen-hint", value);
        }

        private void ShowOffer()
        {
            ChoiceGlow.Apply(Root, offered);
            Root.tooltip = offered && shown != null ? CharacterRosterPresentation.EntryOffer(shown) + "\n" + description : description;
        }
    }
}
