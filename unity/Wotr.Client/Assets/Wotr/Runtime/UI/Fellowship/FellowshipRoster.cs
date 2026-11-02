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
        // The offer each portrait's click makes now (the last Context), and (UI-441) the companions with a usable skill.
        private readonly Dictionary<string, string> offers = new Dictionary<string, string>();
        private readonly HashSet<string> lured = new HashSet<string>();
        private IReadOnlyDictionary<string, string> skills = new Dictionary<string, string>();
        // Some portrait's click makes a turn's choice now (the last Context).
        private bool offering;
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
                item.Value.tooltip = descriptions[item.Key] = TooltipText.Lines(
                    Heading(CharacterLibrary.Name(version.Length > 0 ? version : item.Key), character),
                    version.Length > 0 ? Abilities(version) : view.Configuration.Lords ? "Version unrevealed" : "");
            }
        }

        // UI-187: every ability of the companion in its short form, the Guide's among them; a card without short forms
        // keeps its Guide ability text.
        // UI-420 (owner): "Strider · Level 3 · Leadership 1 · North"; a companion without a nation ends after its
        // Leadership, with no dangling separator, and the opponent sees the name alone.
        private static string Heading(string name, CharacterView character)
        {
            if (character == null)
                return name;
            var nation = string.IsNullOrEmpty(character.NationIcon) ? "" : TooltipText.Nation(character.NationIcon);
            return TooltipText.Parts(name, "Level " + character.Level, "Leadership " + character.Leadership, nation);
        }

        private static string Abilities(string version)
        {
            var lines = CharacterAbilitySummaries.Lines(version);
            return lines.Count > 0 ? string.Join("\n", lines) : FellowshipGuideText.Ability(version);
        }

        // Some portrait's click makes a choice now, so the panel's plaque invites a look.
        public bool Choosing { get; private set; }

        public void Context(PlayerView view, TurnDraft draft, ActionDieContext context, bool enabled)
        {
            offering = false;
            offers.Clear();
            lured.Clear();
            foreach (var item in portraits)
            {
                var kind = FellowshipActions.PortraitKind(view, context, item.Key, draft);
                // UI-431: the companion Lure of the Ring may eliminate glows red and its click eliminates it.
                var lure = enabled && LureChoice.Eliminates(view, item.Key);
                // Inspection and keyboard focus remain available for non-candidates.
                item.Value.SetEnabled(true);
                item.Value.EnableInClassList("legal-target", enabled && kind.HasValue);
                item.Value.EnableInClassList(LureChoice.EliminateClass, lure);
                // UI-229/UI-240: a portrait whose click chooses the next Guide or separates the companion glows and
                // pulses, and its tooltip starts with what the click does.
                var offer = lure ? LureChoice.EliminateTooltip(view) : enabled ? Offer(view, item.Key, kind) : "";
                offering |= offer.Length > 0;
                offers[item.Key] = offer;
                if (lure)
                    lured.Add(item.Key);
                ShowLight(item.Key, item.Value);
                item.Value.EnableInClassList("selected", draft.Kind == CommandKind.SelectGuide ? draft.Payload.GuideId == item.Key
                    : draft.Kind is CommandKind.SeparateCompanions or CommandKind.ChooseEffect && draft.Payload.Ids.Contains(item.Key));
            }
            Choosing = offering;
            RunPulse();
        }

        // UI-441 (owner): a companion whose skill can be used now glows on its portrait and pulses with the roster, and its
        // tooltip says why — unless its click makes another choice now, which keeps its own words.
        public void LightSkills(IReadOnlyDictionary<string, string> lines)
        {
            skills = lines ?? new Dictionary<string, string>();
            foreach (var item in portraits)
                ShowLight(item.Key, item.Value);
            RunPulse();
        }

        // This companion's skill can be used now (UI-441).
        public bool SkillUsable(string id) => skills.ContainsKey(id);

        private void ShowLight(string id, Button button)
        {
            var offer = offers.TryGetValue(id, out var text) ? text : "";
            var skill = offer.Length == 0 && skills.TryGetValue(id, out var line) ? line : "";
            ChoiceGlow.Apply(button, offer.Length > 0 && !lured.Contains(id) || skill.Length > 0);
            var lead = offer.Length > 0 ? offer : skill;
            if (descriptions.TryGetValue(id, out var description))
                button.tooltip = lead.Length > 0 ? lead + "\n" + description : description;
        }

        private void RunPulse() => pulse.Run(offering || portraits.Keys.Any(skills.ContainsKey));

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
