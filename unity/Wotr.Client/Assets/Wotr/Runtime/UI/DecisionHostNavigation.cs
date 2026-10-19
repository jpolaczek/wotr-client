namespace Wotr.Client
{
    // The tray's current parent is authoritative when a reaction interrupts another context.
    public static class DecisionHostNavigation
    {
        public static bool TryOpen(BoardScreen board)
        {
            var parent = board.Tray.parent;
            if (parent == board.Hunt.DecisionHost)
                board.Hunt.Open();
            else if (parent == board.TableEffects.DecisionHost)
                board.TableEffects.Open();
            else if (parent == board.Sovereigns.DecisionHost)
                board.Sovereigns.OpenDecision();
            else if (parent == board.ResourceDecision.DecisionHost)
                board.ResourceDecision.Open();
            else if (parent == board.Characters.DecisionHost)
                board.Characters.OpenDecision();
            else if (parent == board.Fellowship.Root)
                board.Fellowship.Open();
            else if (parent == board.Battles.Root)
                board.Battles.Open();
            else
                return false;
            return true;
        }
    }
}
