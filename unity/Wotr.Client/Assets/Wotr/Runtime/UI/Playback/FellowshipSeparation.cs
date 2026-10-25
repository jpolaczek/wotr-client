using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-309 (FELLOWSHIP_UX F04): a recorded Travel of Companions that are not on the presented board but in its Fellowship
    // is their separation. It leaves from the Fellowship's region only while that position is known (no hidden progress
    // since it was last declared, BoardFigureSnapshot); otherwise the Companions arrive at their destination and no route
    // from the last known region is suggested.
    public static class FellowshipSeparation
    {
        public static List<ActionParticipant> Leaving(ActionMovement movement, PlayerView view)
        {
            if (movement?.Kind != ActionMovementKind.Travel || view.Fellowship == null)
                return new List<ActionParticipant>();
            return movement.Participants.Where(figure => CharacterMarks.Named(figure) && view.Characters.All(c => c.Id != figure.Id) &&
                view.Fellowship.CompanionIdentities.Contains(CharacterLibrary.Identity(figure.DefinitionId))).ToList();
        }

        public static bool HiddenOrigin(ActionMovement movement, PlayerView view, Func<string, bool> isRegion) =>
            Leaving(movement, view).Count > 0 && !BoardFigureSnapshot.PositionKnown(view.Fellowship, isRegion);
    }
}
