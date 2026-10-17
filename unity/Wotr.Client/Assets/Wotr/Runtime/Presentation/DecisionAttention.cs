using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public enum DecisionContext { Resources, Battle, Fellowship, Sovereign, Region, Hunt }

    // Navigation only. Candidates are never treated as a player's chosen destination.
    public sealed class DecisionAttention
    {
        public DecisionContext Context { get; private set; }
        public string Region { get; private set; } = "";
        public string Subject { get; private set; } = "";

        public static DecisionAttention For(PlayerView view)
        {
            if (view?.Decision == null || view.DecisionOwner != view.You || view.Winner.HasValue)
                return null;
            var sovereign = SovereignPresentation.DecisionTarget(view, null);
            if (sovereign.Length > 0)
            {
                return At(DecisionContext.Sovereign, view.Sovereigns.FirstOrDefault(s => s.Id == sovereign)?.RegionId, sovereign);
            }
            if (view.Battle != null || view.EventLoss != null)
                return At(DecisionContext.Battle, view.EventLoss?.RegionId ?? view.Battle.RegionId);
            if (HuntActions.UsesPanel(view))
                return At(DecisionContext.Hunt, "");
            if (FellowshipActions.UsesPanel(view, null))
                return At(DecisionContext.Fellowship, view.Fellowship.MordorStep.HasValue ? "" : view.Fellowship.RegionId);
            var character = view.Characters.FirstOrDefault(c => c.Id == view.Decision.SourceId);
            return character != null ? At(DecisionContext.Region, character.Location, character.Id) : At(DecisionContext.Resources, "");
        }

        private static DecisionAttention At(DecisionContext context, string region, string subject = "") =>
            new DecisionAttention { Context = context, Region = region ?? "", Subject = subject };
    }
}
