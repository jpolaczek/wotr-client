using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-333 (owner 2026-10-25: "once the battle is resolved and the next animations start, close its window, unless the
    // user moved it"): registered first, so the window goes as the next scene begins. When a cue is not part of what the
    // battle window shows, the battle is over in the latest view and nothing more of it is queued, the window closes —
    // unless the player dragged it to a place of their own, which keeps it through the rest of the presentation.
    public sealed class BattleWindowRetirement : ICuePresenter
    {
        private readonly BoardScreen board;
        private readonly PresentationPlayback playback;

        public BattleWindowRetirement(BoardScreen board, PresentationPlayback playback)
        {
            this.board = board;
            this.playback = playback;
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            var window = board.Battles;
            var battle = window.BattleId;
            if (window.IsOpen && battle.Length > 0 && !Shows(cue, battle) && Over(battle) && !Moved())
                window.Close();
            return null;
        }

        // What the battle window itself shows of its battle: the opening, its rounds, the cards, the casualties and a
        // siege's figure changes.
        private static bool Shows(PresentationCue cue, string battle) => cue.BattleId == battle && cue.Kind is
            PresentationCueKind.Attack or PresentationCueKind.Battle or PresentationCueKind.Card or PresentationCueKind.Losses or
            PresentationCueKind.Removal or PresentationCueKind.Recruit or PresentationCueKind.Upgrade;

        // Over in the latest view, and nothing more of it queued but its state after the decision (an advance, the end),
        // which the window would show unchanged.
        private bool Over(string battle) =>
            (playback.Latest?.Battle is not { } latest || latest.Id != battle || latest.Stage == "Complete") &&
            !playback.Upcoming.Any(next => Shows(next, battle) && !Decided(next));

        private static bool Decided(PresentationCue cue) =>
            cue.Kind == PresentationCueKind.Battle && cue.Battle?.Stage is "Advance" or "Finish" or "Complete";

        private bool Moved() => board.Windows.TryGet(board.Battles.Root.name, out _);
    }
}
