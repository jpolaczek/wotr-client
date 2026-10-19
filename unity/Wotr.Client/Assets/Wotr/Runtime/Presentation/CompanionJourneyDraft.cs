using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-294 (owner's decision): a card's journey of companions outside the Fellowship — Book of Mazarbul, Fear! Fire!
    // Foes!, Kings' Aid, Gwaihir the Windlord, We Prove the Swifter — is made on the map: a lit companion is chosen at
    // its region (its marker, or the region panel when several stand there), a lit destination is clicked and its
    // figure stands there faintly, and the console's ✓ finishes once everything the player wanted has moved. The
    // decision window lists nothing: no companions, groups, Stay all or Stay here.
    public static class CompanionJourneyDraft
    {
        public static bool Active(PlayerView view, TurnDraft draft) =>
            OwnJourney(view) && draft?.Kind == CommandKind.MoveCompanions;

        // A journey whose only offer is to move companions outside the Fellowship: the map is its host.
        public static bool OnMap(PlayerView view) => OwnJourney(view) &&
            view.Options.Any(o => o.Kind == CommandKind.MoveCompanions && o.Enabled) &&
            !view.Options.Any(o => o.Kind == CommandKind.SeparateCompanions && o.Enabled);

        public static string Hint(PlayerView view, TurnDraft draft)
        {
            var option = DraftOptions.For(view, draft);
            if (option == null)
                return "";
            if (TravelDraft.Group(draft) != null)
                return "Click a highlighted destination for this companion · its own region keeps it there.";
            if (option.MaximumGroups == 1)
                return "Click a lit companion, then its destination; it moves at once.";
            return "Click a lit companion, then its destination · ✓ finishes" +
                (option.AllowEmptySelection ? "; moving none is allowed." : ".");
        }

        // The companions planned to move, drawn faintly at their destinations.
        public static IReadOnlyDictionary<string, ArmyMarker> Planned(PlayerView view, TurnDraft draft)
        {
            var planned = new Dictionary<string, ArmyMarker>();
            foreach (var group in draft.Payload.CompanionMoves.Where(g => g.Route.Count > 1).GroupBy(g => g.Route.Last()))
            {
                var ids = group.SelectMany(g => g.Ids).ToHashSet();
                var characters = view.Characters.Where(c => ids.Contains(c.Id)).ToList();
                if (characters.Count == 0)
                    continue;
                var marker = ArmyMarker.From(group.Key, view.You, false, Enumerable.Empty<(string, UnitKind)>());
                marker.Characters = characters.Count;
                marker.CharacterAppearance = FigureAppearances.Art(characters[0]);
                marker.CharacterNames = characters.Select(c => CharacterLibrary.Name(c.DefinitionId)).ToList();
                planned.Add(group.Key, marker);
            }
            return planned;
        }

        private static bool OwnJourney(PlayerView view) => view?.Decision?.Kind == DecisionKind.CompanionSeparation &&
            view.DecisionOwner == view.You && !view.Winner.HasValue && view.Battle == null && view.Hunt == null &&
            view.EventLoss == null;
    }
}
