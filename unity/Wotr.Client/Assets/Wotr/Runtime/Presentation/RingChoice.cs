using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum RingStep { None, Die, Face }

    public readonly struct RingFace
    {
        public RingFace(DieFace face, bool enabled)
        {
            Face = face;
            Enabled = enabled;
        }

        public DieFace Face { get; }
        public bool Enabled { get; }
    }

    // UI-161: the Elven Ring gesture ring → die → result, read only from the server's UseRing option (BASE p. 21,
    // LOGIC B05). The current result of the chosen die is offered but disabled, so a click always changes the die.
    public static class RingChoice
    {
        public static bool Armed(TurnDraft draft) => draft.Kind == CommandKind.UseRing && draft.Payload.RingId.Length > 0;

        public static RingStep Step(TurnDraft draft) => !Armed(draft) ? RingStep.None
            : draft.Payload.DieId.Length == 0 ? RingStep.Die : RingStep.Face;

        public static IReadOnlyCollection<string> Dice(PlayerView view, TurnDraft draft) =>
            Armed(draft) ? (IReadOnlyCollection<string>)(Option(view, draft)?.EligibleDice ?? new List<string>()) : Array.Empty<string>();

        public static IReadOnlyList<RingFace> Faces(PlayerView view, TurnDraft draft)
        {
            var option = Option(view, draft);
            if (Step(draft) != RingStep.Face || option == null)
                return Array.Empty<RingFace>();
            var current = view.Dice.FirstOrDefault(d => d.Id == draft.Payload.DieId)?.Face;
            return option.Faces.Select(face => new RingFace(face, face != current)).ToList();
        }

        public static string Hint(TurnDraft draft) => Step(draft) switch
        {
            RingStep.Die => "Click a highlighted die to change its result · Esc cancels",
            RingStep.Face => "Choose the new result above the die · Esc cancels",
            _ => ""
        };

        private static ActionOption Option(PlayerView view, TurnDraft draft) => DraftOptions.For(view, draft, CommandKind.UseRing);
    }
}
