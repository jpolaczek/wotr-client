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
        // UI-269: a candidate's crown chooses the starting Guide while the versions are chosen.
        public event Action<string> GuideChosen;
        // False when the Guide has no portrait here (e.g. Gollum); the status row then names it.
        public bool ShowsGuide { get; private set; }
        private readonly Dictionary<string, Button> portraits = new Dictionary<string, Button>();
        private readonly Dictionary<string, Button> crowns = new Dictionary<string, Button>();
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
                crowns.Clear();
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
                    button.Add(VersionsBadge());
                    button.userData = new DraftToken(DraftObject.FellowshipPortrait, id);
                    portraits.Add(id, button);
                    // UI-269: the crown stands over the portrait's corner, beside it rather than inside its button.
                    var cell = new VisualElement { name = "fellowship-cell-" + id };
                    cell.AddToClassList("fellowship-portrait-cell");
                    cell.Add(button);
                    var crown = Crown(id);
                    crowns.Add(id, crown);
                    cell.Add(crown);
                    Root.Add(cell);
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

        // UI-269: two faces at the corner of a companion with two versions to choose from.
        private static VisualElement VersionsBadge()
        {
            var badge = new VisualElement { name = "versions-badge", pickingMode = PickingMode.Ignore };
            badge.AddToClassList("versions-badge");
            badge.style.display = DisplayStyle.None;
            return badge;
        }

        // UI-269: a starting Guide candidate's crown; lit on the chosen Guide.
        private Button Crown(string id)
        {
            var crown = new Button(() => GuideChosen?.Invoke(id)) { name = "starting-guide-" + id };
            crown.AddToClassList("guide-crown");
            crown.style.display = DisplayStyle.None;
            return crown;
        }

        // UI-187: every ability of the companion in its short form, the Guide's among them; a card without short forms
        // keeps its Guide ability text.
        private static string Abilities(string version)
        {
            var lines = CharacterAbilitySummaries.Lines(version);
            return lines.Count > 0 ? string.Join("\n", lines) : FellowshipGuideText.Ability(version);
        }

        // Some portrait's click makes a choice now, so the panel's plaque invites a look.
        public bool Choosing { get; private set; }

        public void Context(PlayerView view, TurnDraft draft, ActionDieContext context, bool enabled, string compared = "")
        {
            if (CompanionSetupPresentation.VersionOption(view) is { } versions)
            {
                Versions(view, draft, versions, enabled, compared);
                return;
            }
            HideVersions();
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
                    : draft.Kind is CommandKind.SeparateCompanions or CommandKind.ChooseEffect && draft.Payload.Ids.Contains(item.Key));
            }
            Choosing = choosing;
            pulse.Run(choosing);
        }

        // UI-269: the private versions on the portraits — a two-faces badge on each companion with two versions (its
        // click compares them), the drafted version's art and name, and a crown on each starting Guide candidate.
        private void Versions(PlayerView view, TurnDraft draft, ActionOption option, bool enabled, string compared)
        {
            var choosing = false;
            foreach (var item in portraits)
            {
                var group = option.CompanionVersions.FirstOrDefault(candidate => candidate.FigureId == item.Key);
                var button = item.Value;
                var badge = button.Q("versions-badge");
                badge.style.display = group == null ? DisplayStyle.None : DisplayStyle.Flex;
                var version = group == null ? "" : CouncilDraft.Version(draft.Payload, group);
                var shown = version.Length > 0 ? version : group?.BaseId ?? item.Key;
                FigureArtwork.Apply(button, FigureAppearances.Character(shown, Side.FreePeoples), Side.FreePeoples);
                button.Q<Label>("portrait-name").text = CharacterLibrary.ShortName(shown);
                var required = enabled && group != null && version.Length == 0;
                choosing |= required;
                ChoiceGlow.Apply(button, required);
                button.SetEnabled(true);
                button.EnableInClassList("legal-target", enabled && group != null);
                button.EnableInClassList("selected", group != null && item.Key == compared);
                var description = descriptions.TryGetValue(item.Key, out var text) ? text : "";
                button.tooltip = group == null ? description : CompanionSetupPresentation.PortraitOffer(draft, group);
                var crown = crowns[item.Key];
                var candidate = option.Targets.Contains(item.Key);
                crown.style.display = candidate ? DisplayStyle.Flex : DisplayStyle.None;
                crown.EnableInClassList("selected", draft.Payload.GuideId == item.Key);
                crown.SetEnabled(enabled && candidate);
                crown.tooltip = candidate ? CompanionSetupPresentation.CrownOffer(draft, option, item.Key) : "";
                button.EnableInClassList("current-guide", draft.Payload.GuideId == item.Key);
                button.Q<Label>("guide-badge").style.display = DisplayStyle.None;
            }
            choosing |= enabled && !option.Targets.Contains(draft.Payload.GuideId);
            Choosing = choosing;
            pulse.Run(choosing);
        }

        private void HideVersions()
        {
            foreach (var button in portraits.Values)
                button.Q("versions-badge").style.display = DisplayStyle.None;
            foreach (var crown in crowns.Values)
                crown.style.display = DisplayStyle.None;
        }

        // UI-269: a click on a portrait compares its versions now, before the next view arrives.
        public void Compare(string figure)
        {
            foreach (var item in portraits)
                item.Value.EnableInClassList("selected", item.Key == figure);
        }

        public static string Offer(PlayerView view, string id, CommandKind? kind) => kind switch
        {
            CommandKind.SelectGuide => "Click to make " + CharacterLibrary.ShortName(view, id) + " the Guide" +
                (view.Decision?.Kind == DecisionKind.FellowshipPhase ? " · This ends the Fellowship phase" : ""),
            CommandKind.SeparateCompanions => "Click to separate " + CharacterLibrary.ShortName(view, id) + " from the Fellowship",
            // UI-247: the Guide's reaction (Gandalf the Grey's draw).
            CommandKind.RespondEffect => FellowshipActions.RosterReaction(view, id) is { } reaction ? BattleReactions.Offer(reaction) : "",
            // UI-249: The Breaking of the Fellowship places the Companion in the Fellowship's region, or removes it in Mordor.
            CommandKind.ChooseEffect when view.Fellowship.MordorStep.HasValue =>
                "Click to remove " + CharacterLibrary.ShortName(view, id) + " from play",
            CommandKind.ChooseEffect => "Click to separate " + CharacterLibrary.ShortName(view, id) + " into the Fellowship's region",
            _ => ""
        };
    }
}
