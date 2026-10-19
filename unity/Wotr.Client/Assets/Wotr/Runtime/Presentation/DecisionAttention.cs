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
            // UI-274: a king's decision is made in the Characters panel, on his portrait or tiles.
            if (SovereignPresentation.OwnsDecision(view, null))
            {
                var sovereign = SovereignPresentation.DecisionTarget(view, null);
                return At(DecisionContext.Sovereign, view.Sovereigns.FirstOrDefault(s => s.Id == sovereign)?.RegionId, sovereign);
            }
            if (view.Battle != null || view.EventLoss != null)
                return At(DecisionContext.Battle, view.EventLoss?.RegionId ?? view.Battle.RegionId);
            if (HuntActions.UsesPanel(view))
                return At(DecisionContext.Hunt, "");
            if (FellowshipActions.UsesPanel(view, null))
                return At(DecisionContext.Fellowship, view.Fellowship.MordorStep.HasValue ? "" : view.Fellowship.RegionId);
            var figures = FigureRegion(view);
            if (figures.Length > 0)
                return At(DecisionContext.Region, figures);
            var character = view.Characters.FirstOrDefault(c => c.Id == view.Decision.SourceId);
            return character != null ? At(DecisionContext.Region, character.Location, character.Id) : At(DecisionContext.Resources, "");
        }

        // UI-254: excess units and a card's choice of figures are made in the region panel of the region holding the
        // offered figures (UI-215); the first one names it.
        private static string FigureRegion(PlayerView view)
        {
            if (DecisionRoutes.Of(view) != DecisionHost.RegionPanel)
                return "";
            return view.Options.Where(o => o.Kind is CommandKind.RemoveExcess or CommandKind.ChooseEffect)
                .SelectMany(o => o.Kind == CommandKind.RemoveExcess ? o.EligibleIds : o.EffectChoices.Select(c => c.Id))
                .Select(id => Location(view, id))
                .FirstOrDefault(region => region.Length > 0) ?? "";
        }

        private static string Location(PlayerView view, string id) =>
            view.Units.FirstOrDefault(u => u.Id == id)?.Location ?? view.Characters.FirstOrDefault(c => c.Id == id)?.Location ?? "";

        private static DecisionAttention At(DecisionContext context, string region, string subject = "") =>
            new DecisionAttention { Context = context, Region = region ?? "", Subject = subject };
    }
}
