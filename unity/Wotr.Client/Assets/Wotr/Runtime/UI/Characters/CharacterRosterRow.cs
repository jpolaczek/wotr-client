using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-139: a roster row speaks in symbols: the portrait carries the level, the second line a status symbol and
    // the region or a short state; the row's tooltip reads the full state and statistics.
    internal sealed class CharacterRosterRow
    {
        public Button Root { get; }
        private readonly Image portrait = new Image { pickingMode = PickingMode.Ignore, scaleMode = UnityEngine.ScaleMode.ScaleToFit };
        private readonly Label level = new Label { name = "character-roster-level", pickingMode = PickingMode.Ignore, enableRichText = false };
        private readonly Label title = new Label { name = "character-roster-title" };
        private readonly VisualElement statusIcon = new VisualElement { name = "character-roster-status", pickingMode = PickingMode.Ignore };
        private readonly Label detail = new Label { name = "character-roster-detail" };
        private string status = "";

        public CharacterRosterRow(string id)
        {
            Root = BoardScreen.Button("", null);
            Root.name = "character-roster-" + id;
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
            Root.Add(portrait);
            Root.Add(text);
        }

        public void Apply(CharacterRosterView entry, BoardPresentation data)
        {
            var candidate = CharacterRosterPresentation.Section(entry) == CharacterRosterSection.NotInPlay
                ? entry.FutureVersions.FirstOrDefault() : null;
            var definition = candidate?.DefinitionId ?? entry.CurrentDefinitionId;
            title.text = CharacterLibrary.ShortName(definition);
            portrait.sprite = FigureArtwork.Sprite(FigureAppearances.Character(definition, entry.Side));
            portrait.EnableInClassList("free-peoples", entry.Side == Side.FreePeoples);
            portrait.EnableInClassList("shadow", entry.Side == Side.Shadow);
            var location = data.Regions.TryGetValue(entry.RegionId, out var region) ? region.Name : "";
            var state = CharacterCardFacts.Status(entry, location);
            detail.text = state.Short;
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
            Root.tooltip = CharacterAbilitySummaries.Append(CharacterRosterPresentation.Title(entry) + (edition.Length > 0 ? " · " + edition : "") +
                "\n" + fullLocation + "\n" + CharacterCardFacts.Summary(facts) +
                (candidate == null ? "" : "\n" + CharacterRosterPresentation.Conditions(candidate.Entry)), definition);
            Root.EnableInClassList("legal-target", entry.FutureVersions.Any(v => v.Entry.CanAct || v.CouncilEntry?.CanAct == true));
        }
    }
}
