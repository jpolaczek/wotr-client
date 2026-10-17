using Wotr.Contracts;

namespace Wotr.Client
{
    public static class DraftContinuity
    {
        public static bool CanCarry(PlayerView previous, PlayerView next)
        {
            if (previous?.Decision == null || next?.Decision == null || next.Winner.HasValue ||
                previous.MatchId != next.MatchId || previous.You != next.You ||
                previous.Turn != next.Turn || previous.Phase != next.Phase ||
                previous.DecisionOwner != next.DecisionOwner ||
                previous.Decision.Kind != next.Decision.Kind ||
                previous.Decision.Window != next.Decision.Window ||
                previous.Decision.SourceId != next.Decision.SourceId)
                return false;
            if (previous.Decision.Id == next.Decision.Id)
                return true;
            return previous.Decision.Kind == DecisionKind.Action &&
                previous.Battle == null && next.Battle == null &&
                previous.Hunt == null && next.Hunt == null &&
                previous.EventLoss == null && next.EventLoss == null;
        }
    }
}
