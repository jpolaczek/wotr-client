using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class WillFaceSlot
    {
        public DieFace Face { get; set; }
        public bool Enabled { get; set; }
        public bool Chosen { get; set; }
        public string Tooltip { get; set; } = "";
    }

    // UI-461 (owner, 2026-11-03): "We need the previous small menu at the bottom (console) that let us choose the new face
    // WotW turns into". While a chosen Will of the West die waits for its use, the console offers the four results it can
    // become in fixed slots (a result with no use now is disabled, not hidden); a click turns the die into that result and
    // the board lights what such a die does; a second click on the chosen result, or Back, makes it Will of the West
    // itself again. A use the die takes as itself (WillOfTheWestUses) needs no result: its lit object spends the die.
    public static class WillFaceChoices
    {
        private static readonly IReadOnlyList<WillFaceSlot> None = new WillFaceSlot[0];

        public const string Hint = "Choose the result Will of the West becomes · or click a glowing object to use it as itself.";

        public static bool Shown(PlayerView view, TurnDraft draft, ActionDieContext die) =>
            die.Will && view?.Decision?.Kind == DecisionKind.Action && view.DecisionOwner == view.You &&
            !view.Winner.HasValue && !draft.Kind.HasValue;

        public static IReadOnlyList<WillFaceSlot> For(PlayerView view, TurnDraft draft, ActionDieContext die)
        {
            if (!Shown(view, draft, die))
                return None;
            return WillOfTheWestUses.Faces.Select(face => Slot(view, draft, die, face)).ToList();
        }

        // A second click on the chosen result turns the die back into Will of the West itself.
        public static void Choose(TurnDraft draft, DieFace face) => draft.TurnWill(draft.WillFace == face ? (DieFace?)null : face);

        private static WillFaceSlot Slot(PlayerView view, TurnDraft draft, ActionDieContext die, DieFace face)
        {
            var chosen = die.WillFace == face;
            var enabled = chosen || Uses(view, draft, die, face);
            return new WillFaceSlot { Face = face, Enabled = enabled, Chosen = chosen, Tooltip = Tooltip(face, chosen, enabled) };
        }

        // What a die of that result would light now (UI-136), judged with the same die turned.
        private static bool Uses(PlayerView view, TurnDraft draft, ActionDieContext die, DieFace face)
        {
            var turned = new ActionDieContext();
            turned.Sync(view, die.DieId, "", face);
            return DieOptions.For(view, draft, turned).Any;
        }

        public static string Tooltip(DieFace face, bool chosen, bool enabled)
        {
            var name = DieDescriptions.Face(face);
            if (chosen)
                return name + " · Will of the West is used as " + name + " · Click again to use it as itself";
            return enabled ? name + " · Click to use Will of the West as " + name : name + " · No " + name + " use now";
        }
    }
}
