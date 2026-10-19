using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum CharacterRosterSection { OnBoard, NotInPlay, Removed }

    // Inspection only. Entry permission and dynamic values remain server-owned.
    public static class CharacterRosterPresentation
    {
        public static IEnumerable<CharacterRosterView> Rows(PlayerView view) =>
            (view?.OwnCharacterRoster ?? new List<CharacterRosterView>()).Where(row => row.Side == view.You &&
                row.Identity != "gollum" && row.Identity != "smeagol" &&
                (row.Lifecycle != CharacterRosterLifecycle.InFellowship || row.FutureVersions.Count > 0))
                .SelectMany(CharacterRosterVersions.Split);

        // UI-240: what a click on an enterable row does: a character in play changes into its next version (Strider
        // into Aragorn), one not in play comes into play.
        public static string EntryOffer(CharacterRosterView row)
        {
            var future = row.FutureVersions.FirstOrDefault(v => v.Entry.CanAct || v.CouncilEntry?.CanAct == true) ?? row.FutureVersions.FirstOrDefault();
            var target = CharacterLibrary.FullName(future?.DefinitionId ?? row.CurrentDefinitionId);
            var inPlay = row.Lifecycle is CharacterRosterLifecycle.OnBoard or CharacterRosterLifecycle.InFellowship ||
                CharacterRosterVersions.Switches(row);
            return inPlay && !string.IsNullOrEmpty(row.CurrentDefinitionId) && future != null &&
                future.DefinitionId != row.CurrentDefinitionId
                ? "Click to turn " + CharacterLibrary.FullName(row.CurrentDefinitionId) + " into " + target
                : "Click to bring " + target + " into play";
        }

        public static CharacterRosterSection Section(CharacterRosterView row) => row.Lifecycle switch
        {
            CharacterRosterLifecycle.OnBoard => CharacterRosterSection.OnBoard,
            CharacterRosterLifecycle.Eliminated => CharacterRosterSection.Removed,
            _ => CharacterRosterSection.NotInPlay
        };

        public static string Title(CharacterRosterView row)
        {
            var version = CharacterRosterVersions.Version(row.FigureId);
            if (version.Length > 0)
                return CharacterLibrary.FullName(version);
            return (row.Lifecycle is CharacterRosterLifecycle.InFellowship or CharacterRosterLifecycle.EliminatedWithReplacement) &&
                row.FutureVersions.Count > 0 ? row.FutureVersions[0].Title : row.Title;
        }

        public static string State(CharacterRosterView row)
        {
            if (CharacterRosterVersions.Switches(row))
                return "May replace the version in play";
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

        public static string Conditions(ActionAvailabilityView entry)
        {
            var lines = new List<string> { entry.Title, "Cost · " + entry.Cost };
            lines.AddRange(entry.Conditions.Select(c => (c.Met ? "✓ " : "○ ") + c.Description));
            if (!entry.WindowOpen)
                lines.Add(entry.Timing);
            lines.Add(entry.Source);
            return string.Join("\n", lines.Where(line => line.Length > 0));
        }
    }
}
