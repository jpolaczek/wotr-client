using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class FellowshipRoster
    {
        public VisualElement Root { get; } = BoardScreen.Row("fellowship-members");
        public event Action<string> Selected;
        // False when the Guide has no portrait here (e.g. Gollum); the status row then names it.
        public bool ShowsGuide { get; private set; }
        private readonly Dictionary<string, Button> portraits = new Dictionary<string, Button>();
        private readonly Dictionary<string, string> descriptions = new Dictionary<string, string>();
        private readonly ChoicePulse pulse;
        private string roster = "";

        public FellowshipRoster()
        {
            pulse = new ChoicePulse(Root);
        }

        public void Apply(PlayerView view)
        {
            var ids = view.You == Side.FreePeoples ? view.OwnCompanions.Select(c => c.Id) : view.Fellowship.CompanionIdentities;
            var key = view.You + ":" + string.Join("|", ids);
            if (roster != key)
            {
                roster = key;
                Root.Clear();
                portraits.Clear();
                foreach (var id in ids)
                {
                    var button = BoardScreen.Button("", () => Selected?.Invoke(id));
                    button.name = "fellowship-portrait-" + id;
                    button.displayTooltipWhenElided = false;
                    button.AddToClassList("companion-token");
                    button.AddToClassList("illustrated-companion");
                    button.Add(new Label { name = "portrait-name", pickingMode = PickingMode.Ignore });
                    var badge = new Label("Guide") { name = "guide-badge", pickingMode = PickingMode.Ignore };
                    button.Add(badge);
                    button.userData = new DraftToken(DraftObject.FellowshipPortrait, id);
                    portraits.Add(id, button);
                    Root.Add(button);
                }
            }
            var privateStats = view.OwnCompanions.ToDictionary(c => c.Id);
            ShowsGuide = portraits.Keys.Any(id => FellowshipGuideText.IsGuide(view.Fellowship, id));
            foreach (var item in portraits)
            {
                var version = privateStats.TryGetValue(item.Key, out var character)
                    ? CharacterLibrary.Version(view, item.Key) : FellowshipGuideText.PublicVersion(view.Fellowship, item.Key);
                var guide = FellowshipGuideText.IsGuide(view.Fellowship, item.Key);
                var shownDefinition = version.Length > 0 ? version : item.Key;
                FigureArtwork.Apply(item.Value, FigureAppearances.Character(shownDefinition, Side.FreePeoples), Side.FreePeoples);
                item.Value.Q<Label>("portrait-name").text = CharacterLibrary.ShortName(version.Length > 0 ? version : item.Key);
                item.Value.EnableInClassList("current-guide", guide);
                item.Value.Q<Label>("guide-badge").style.display = guide ? DisplayStyle.Flex : DisplayStyle.None;
                item.Value.tooltip = descriptions[item.Key] = CharacterLibrary.Name(version.Length > 0 ? version : item.Key) +
                    (character == null ? "" : $" · Level {character.Level} · Leadership {character.Leadership} · {FigureGrid.Name(character.NationIcon)}") +
                    (version.Length > 0 ? "\n" + Abilities(version) : view.Configuration.Lords ? "\nVersion unrevealed" : "");
            }
        }

        // UI-187: every ability of the companion in its short form, the Guide's among them; a card without short forms
        // keeps its Guide ability text.
        private static string Abilities(string version)
        {
            var lines = CharacterAbilitySummaries.Lines(version);
            return lines.Count > 0 ? string.Join("\n", lines) : FellowshipGuideText.Ability(version);
        }

        public void Context(PlayerView view, TurnDraft draft, ActionDieContext context, bool enabled)
        {
            var choosing = false;
            foreach (var item in portraits)
            {
                var kind = FellowshipActions.PortraitKind(view, context, item.Key, draft);
                // Inspection and keyboard focus remain available for non-candidates.
                item.Value.SetEnabled(true);
                item.Value.EnableInClassList("legal-target", enabled && kind.HasValue);
                // UI-229/UI-240: a portrait whose click chooses the next Guide or separates the companion glows and
                // pulses, and its tooltip starts with what the click does.
                var offer = enabled ? Offer(view, item.Key, kind) : "";
                choosing |= offer.Length > 0;
                ChoiceGlow.Apply(item.Value, offer.Length > 0);
                if (descriptions.TryGetValue(item.Key, out var description))
                    item.Value.tooltip = offer.Length > 0 ? offer + "\n" + description : description;
                item.Value.EnableInClassList("selected", draft.Kind == CommandKind.SelectGuide ? draft.Payload.GuideId == item.Key
                    : draft.Kind == CommandKind.SeparateCompanions && draft.Payload.Ids.Contains(item.Key));
            }
            pulse.Run(choosing);
        }

        public static string Offer(PlayerView view, string id, CommandKind? kind) => kind switch
        {
            CommandKind.SelectGuide => "Click to make " + CharacterLibrary.ShortName(view, id) + " the Guide",
            CommandKind.SeparateCompanions => "Click to separate " + CharacterLibrary.ShortName(view, id) + " from the Fellowship",
            _ => ""
        };
    }
}
