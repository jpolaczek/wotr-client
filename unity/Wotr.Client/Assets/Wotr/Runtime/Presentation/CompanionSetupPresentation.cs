using System;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CompanionSetupPresentation
    {
        // UI-270: the Council's setup (versions and Guide, then homelands) is made on the Fellowship panel.
        public static bool Setup(PlayerView view) => Versions(view) || Homelands(view);

        // UI-270: the Free Peoples' homelands, chosen on the Fellowship panel's portraits and the map.
        public static bool Homelands(PlayerView view) => Own(view) && view.Decision?.Kind == DecisionKind.CouncilHomelands;

        public static ActionOption HomelandOption(PlayerView view) => Homelands(view)
            ? view.Options.FirstOrDefault(o => o.Kind == CommandKind.ChooseCouncilHomelands) : null;

        // What a click on a companion who may start in its homeland does, and where the draft sends it.
        // The companion's own version decides its nation icon: leaving activates that nation, a version without one none.
        public static string HomelandOffer(TurnDraft draft, CharacterActionOption choice, Func<string, string> regionName,
            string nationIcon)
        {
            var name = CharacterLibrary.ShortName(choice.CharacterId);
            var homes = string.Join(" or ", choice.Targets.Select(regionName));
            var placement = draft.Payload.Placements.FirstOrDefault(p => p.UnitId == choice.CharacterId);
            var state = placement == null ? "stays with the Fellowship" : "starts in " + regionName(placement.RegionId);
            var nation = string.IsNullOrEmpty(nationIcon) ? "No nation icon: leaving does not activate a nation."
                : "Leaving activates the " + FigureGrid.Name(nationIcon) + ".";
            // UI-347 (D14): the selected, placed companion's second click keeps it with the Fellowship.
            var click = placement != null && draft.Payload.CharacterId == choice.CharacterId
                ? "Click again to keep " + name + " with the Fellowship, or a lit homeland to start in " + homes
                : "Click, then a lit homeland on the map, to start " + name + " in " + homes;
            return click + " · Now " + state + "\n" + nation;
        }

        // UI-269: the Free Peoples' private versions and starting Guide, chosen on the Fellowship panel's portraits.
        public static bool Versions(PlayerView view) => Own(view) && view.Decision?.Kind == DecisionKind.CouncilVersions;

        public static ActionOption VersionOption(PlayerView view) => Versions(view)
            ? view.Options.FirstOrDefault(o => o.Kind == CommandKind.ChooseCompanionVersions) : null;

        // UI-270: a companion in its alternate version (Lords and the Council of Rivendell, Fate's Dwarven Lord). Its art
        // stays the base portrait; only its owner, who knows the version, sees the mark.
        public static bool Alternate(string figure, string version) =>
            !string.IsNullOrEmpty(version) && version != figure &&
            (version.StartsWith("lords.character.") || version == "fate.character.gimli-dwarven-lord");

        public const string AlternateTooltip = "Alternate version · only you see this mark";

        // UI-269: the tray's and the console's hint — what is still missing, or that the ✓ sends the choice.
        public static string Hint(TurnDraft draft, ActionOption option)
        {
            var missing = Missing(draft, option);
            return missing.Length > 0 ? missing + " on the portraits." : "Confirm the versions and the starting Guide.";
        }

        // What a click on a portrait with two versions does, and which version the draft holds.
        public static string PortraitOffer(TurnDraft draft, CompanionVersionOption group)
        {
            var name = CharacterLibrary.ShortName(group.FigureId);
            var version = CouncilDraft.Version(draft.Payload, group);
            var state = version.Length == 0 ? "a choice is required"
                : "chosen: " + (version == group.BaseId ? "Original" : "Alternate") + " · " + CharacterLibrary.Name(version);
            return "Click to compare " + name + "'s two versions · " + state;
        }

        // The crown of a starting Guide candidate: what its click does and the Guide ability it brings.
        public static string CrownOffer(TurnDraft draft, ActionOption option, string figure)
        {
            var chosen = draft.Payload.GuideId == figure;
            var text = (chosen ? "Starting Guide · " : "Click to make " + CharacterLibrary.ShortName(figure) + " the starting Guide · ") +
                CharacterLibrary.Name(Definition(draft, option, figure));
            var ability = CharacterAbilityLibrary.For(Definition(draft, option, figure)).FirstOrDefault(candidate => candidate.Id.Contains("guide"));
            return ability == null ? text : text + "\n" + ability.Text + "\n" + ability.Source;
        }

        private static bool Own(PlayerView view) => view != null && !view.Winner.HasValue &&
            view.You == Side.FreePeoples && view.DecisionOwner == view.You;

        public static string Missing(TurnDraft draft, ActionOption option)
        {
            if (option == null)
                return "Waiting for the current choices";
            var required = option.CompanionVersions.FirstOrDefault(group =>
                group.Required && string.IsNullOrEmpty(CouncilDraft.Version(draft.Payload, group)));
            if (required != null)
                return "Choose a version for " + CharacterLibrary.Name(required.FigureId);
            return option.Targets.Contains(draft.Payload.GuideId) ? "" : "Choose the starting Guide";
        }

        public static string Definition(TurnDraft draft, ActionOption option, string figure)
        {
            var group = option.CompanionVersions.FirstOrDefault(candidate => candidate.FigureId == figure);
            return group == null ? figure : CouncilDraft.Version(draft.Payload, group);
        }
    }
}
