using System;

namespace Wotr.Client
{
    // The tray's current parent is authoritative when a reaction interrupts another context.
    public static class DecisionHostNavigation
    {
        public static bool TryOpen(BoardScreen board)
        {
            var open = Opener(board);
            if (open == null)
                return false;
            open();
            return true;
        }

        // UI-292: the tray sits in a panel that the decision's click would open.
        public static bool Hosted(BoardScreen board) => Opener(board) != null;

        private static Action Opener(BoardScreen board)
        {
            var parent = board.Tray.parent;
            if (parent == board.Hunt.DecisionHost)
                return board.Hunt.Open;
            if (parent == board.TableEffects.DecisionHost)
                return board.TableEffects.Open;
            if (parent == board.ResourceDecision.DecisionHost)
                return board.ResourceDecision.Open;
            if (parent == board.Characters.DecisionHost)
                return board.Characters.OpenDecision;
            if (parent == board.Fellowship.Root)
                return board.Fellowship.Open;
            if (parent == board.Battles.Root)
                return board.Battles.Open;
            return null;
        }
    }
}
