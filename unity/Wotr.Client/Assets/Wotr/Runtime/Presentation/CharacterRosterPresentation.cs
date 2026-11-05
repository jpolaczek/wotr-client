using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum CharacterRosterSection { OnBoard, NotInPlay, Removed }

    // Inspection only. Entry permission and dynamic values remain server-owned.
    public static class CharacterRosterPresentation
    {
        // UI-438 (owner): one row per character, as the server lists them — the Mouth of Sauron's and the Witch-king's
        // base and Lords cards included; like Strider/Aragorn, the row shows one version and its window switches between
        // them (ShownVersion, CharacterInspector's version tabs).
        public static IEnumerable<CharacterRosterView> Rows(PlayerView view) =>
            (view?.OwnCharacterRoster ?? new List<CharacterRosterView>()).Where(row => row.Side == view.You &&
                row.Identity != "gollum" && row.Identity != "smeagol" &&
                (row.Lifecycle != CharacterRosterLifecycle.InFellowship || row.FutureVersions.Count > 0));

        // UI-438: the version a row shows. In play, the version in play; otherwise the version the offered entry brings in,
        // or the first that may enter, or the first.
        public static string ShownVersion(CharacterRosterView row, string offered = "")
        {
            if (Section(row) != CharacterRosterSection.NotInPlay)
                return row.CurrentDefinitionId;
            var future = row.FutureVersions.FirstOrDefault(v => v.DefinitionId == offered) ?? Enterable(row) ?? row.FutureVersions.FirstOrDefault();
            return future?.DefinitionId ?? row.CurrentDefinitionId;
        }

        // UI-438: a character not in play with more than one version that may enter (both Mouths of Sauron).
        public static bool HasAlternatives(CharacterRosterView row) =>
            Section(row) == CharacterRosterSection.NotInPlay && row.FutureVersions.Count > 1;

        // UI-240: what a click on an enterable row does: a character in play changes into its next version (Strider
        // into Aragorn), one not in play comes into play. UI-438: version names the version the click brings in.
        public static string EntryOffer(CharacterRosterView row, string version = "")
        {
            var future = row.FutureVersions.FirstOrDefault(v => v.DefinitionId == version) ?? Enterable(row) ?? row.FutureVersions.FirstOrDefault();
            var target = CharacterLibrary.FullName(future?.DefinitionId ?? row.CurrentDefinitionId);
            var inPlay = row.Lifecycle is CharacterRosterLifecycle.OnBoard or CharacterRosterLifecycle.InFellowship;
            return inPlay && !string.IsNullOrEmpty(row.CurrentDefinitionId) && future != null &&
                future.DefinitionId != row.CurrentDefinitionId
                ? "Click to turn " + CharacterLibrary.FullName(row.CurrentDefinitionId) + " into " + target
                : "Click to bring " + target + " into play";
        }

        // UI-438: a row whose versions may each enter now opens its window, where the version is chosen.
        public const string ChooseVersionOffer = "Click to choose which version comes into play";

        private static CharacterEntryVersionView Enterable(CharacterRosterView row) =>
            row.FutureVersions.FirstOrDefault(v => v.Entry.CanAct || v.CouncilEntry?.CanAct == true);

        public static CharacterRosterSection Section(CharacterRosterView row) => row.Lifecycle switch
        {
            CharacterRosterLifecycle.OnBoard => CharacterRosterSection.OnBoard,
            CharacterRosterLifecycle.Eliminated => CharacterRosterSection.Removed,
            _ => CharacterRosterSection.NotInPlay
        };

        // UI-438: a row with versions not in play is named by the version it shows ("The Mouth of Sauron, Black Númenórean").
        public static string Title(CharacterRosterView row, string version = "")
        {
            if (HasAlternatives(row) && version.Length > 0)
                return CharacterLibrary.FullName(version);
            return (row.Lifecycle is CharacterRosterLifecycle.InFellowship or CharacterRosterLifecycle.EliminatedWithReplacement) &&
                row.FutureVersions.Count > 0 ? row.FutureVersions[0].Title : row.Title;
        }

        public static string State(CharacterRosterView row)
        {
            return row.Lifecycle switch
            {
                CharacterRosterLifecycle.OnBoard => row.Garrison ? "On board · Garrison" : "On board",
                CharacterRosterLifecycle.NotEntered => "Not entered",
                CharacterRosterLifecycle.InFellowship => "Current version in the Fellowship",
                CharacterRosterLifecycle.AwaitingPlacement => "Awaiting placement",
                CharacterRosterLifecycle.AwaitingReturn => "Awaiting return",
                CharacterRosterLifecycle.Eliminated => "Removed",
                CharacterRosterLifecycle.EliminatedWithReplacement => "New version may return",
                _ => "State not provided"
            };
        }

        public static CharacterRosterView Inspect(PlayerView view, string id)
        {
            var own = Rows(view).FirstOrDefault(row => row.FigureId == id);
            if (own != null)
                return own;
            var figure = view?.Characters.FirstOrDefault(c => c.Id == id);
            if (figure == null)
                return null;
            var card = CharacterLibrary.Find(figure.DefinitionId);
            return new CharacterRosterView
            {
                FigureId = figure.Id, Identity = card?.Identity ?? "", Side = figure.Side,
                CurrentDefinitionId = figure.DefinitionId, Title = CharacterLibrary.Name(figure.DefinitionId),
                Lifecycle = CharacterRosterLifecycle.OnBoard, RegionId = figure.Location, Garrison = figure.Garrison,
                Level = figure.Level, MovementLevel = card?.Abilities.Contains("movement-level-two") == true ? 2 : figure.Level,
                CombatLevel = card?.Abilities.Contains("combat-card-level-three") == true ? 3 : figure.Level, Leadership = figure.Leadership,
                NationIcon = figure.NationIcon, UnlimitedMovement = card?.UnlimitedMovement == true
            };
        }

        public static string Conditions(ActionAvailabilityView entry) => TooltipText.Lines(entry.Title, Cost(entry), Requirements(entry));

        // UI-420 (owner): "Cost: X" wherever an entry's cost is shown (the roster row, the inspector).
        public static string Cost(ActionAvailabilityView entry) => entry.Cost.Length > 0 ? "Cost: " + entry.Cost : "";

        // The entry's conditions, its timing while its window is closed, and its source: what the cost label leaves out.
        public static string Requirements(ActionAvailabilityView entry)
        {
            var lines = entry.Conditions.Select(c => (c.Met ? "✓ " : "○ ") + c.Description).ToList();
            if (!entry.WindowOpen)
                lines.Add(entry.Timing);
            lines.Add(entry.Source);
            return TooltipText.Lines(lines.ToArray());
        }
    }
}
