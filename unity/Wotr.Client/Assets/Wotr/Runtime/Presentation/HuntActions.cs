using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class HuntActions
    {
        // UI-162: the Hunt window hosts the decisions of a Hunt under way; the allocation belongs to the console.
        public static bool UsesPanel(PlayerView view, TurnDraft draft = null) => view != null &&
            view.Battle == null && view.EventLoss == null && view.Hunt != null &&
            view.Decision?.Kind is not (DecisionKind.FellowshipRoute or DecisionKind.CompanionSeparation) &&
            !FellowshipDraft.UsesRoute(draft?.Kind);

        public static ActionOption Allocation(PlayerView view) => view?.DecisionOwner == view?.You &&
            view?.Decision?.Kind == DecisionKind.AllocateHunt
                ? view.Options.FirstOrDefault(o => o.Kind == CommandKind.AllocateHunt && o.Enabled) : null;

        // A click adds the die to the local allocation or returns it; Allocate & roll submits the whole set once.
        public static bool ToggleDie(TurnDraft draft, ActionOption option, string id)
        {
            if (option == null || !option.EligibleIds.Contains(id))
                return false;
            if (!draft.Payload.Ids.Contains(id))
                return Allocate(draft, option, id);
            draft.Change(p => p.Ids.Remove(id));
            return true;
        }

        public static bool Allocate(TurnDraft draft, ActionOption option, string id)
        {
            if (option == null || !option.EligibleIds.Contains(id) || draft.Payload.Ids.Contains(id) || AtLimit(draft, option))
                return false;
            draft.Change(p => p.Ids.Add(id));
            return true;
        }

        public static bool AtLimit(TurnDraft draft, ActionOption option) => draft.Payload.Ids.Count >= option.Maximum;

        public static bool CanAdd(TurnDraft draft, ActionOption option, string id) =>
            option.EligibleIds.Contains(id) && !draft.Payload.Ids.Contains(id) && !AtLimit(draft, option);

        public static string PoolHint(TurnDraft draft, ActionOption option, string id) =>
            !option.EligibleIds.Contains(id) ? ""
            : draft.Payload.Ids.Contains(id) ? "In your Hunt allocation · Click to return it"
            : AtLimit(draft, option) ? "Allocation limit reached · Return a die first"
            : "Click to allocate to the Hunt · The Hunt plaque allocates and rolls the whole set";

        public static string Hint(TurnDraft draft, ActionOption option) => draft.Payload.Ids.Count < option.Minimum
            ? $"Click dice in your pool to allocate at least {option.Minimum} · The Hunt plaque allocates and rolls them together."
            : "";

        public static HuntView Displayed(PlayerView view) => view.Hunt ??
            view.HuntReport?.Entries.LastOrDefault(e => e.Hunt != null)?.Hunt;

        public static string DecisionText(PlayerView view)
        {
            if (view.Hunt == null && view.Phase != GamePhase.HuntAllocation)
                return view.HuntReport == null ? "No Hunt report yet" : view.HuntReport.Completed
                    ? "Last Hunt · Complete" : "Hunt · Awaiting continuation";
            if (view.DecisionOwner != view.You || view.Decision == null)
                return view.DecisionOwner.HasValue ? "Waiting for " + BoardScreen.SideName(view.DecisionOwner.Value) : "Resolving the Hunt";
            return view.Decision.Kind switch
            {
                DecisionKind.AllocateHunt => "Allocate dice",
                DecisionKind.HuntReroll => "Choose rerolls",
                DecisionKind.HuntDamage => "Resolve damage",
                DecisionKind.Guide => "Choose the next Guide",
                DecisionKind.FellowshipRoute => "Reveal the route on the map",
                DecisionKind.CompanionSeparation => view.Fellowship.MordorStep.HasValue
                    ? "Remove companions from play" : "Place companions on the map",
                DecisionKind.EffectChoice when view.Decision.Presentation == "FellowshipCompanions" =>
                    view.Fellowship.MordorStep.HasValue ? "Remove companions from play" : "Separate companions",
                DecisionKind.Reaction => view.Decision.Window switch
                {
                    "hunt.replace" or "hunt.shadow-replace" => "Replace the tile",
                    "hunt.table" => "Protect the Fellowship",
                    "hunt.guide" => "Guide ability",
                    "hunt.draw" => "Before drawing a tile",
                    "hunt.roll" => "Before the Hunt roll",
                    "hunt.casualty" => "After the casualty",
                    _ => "Your reaction"
                },
                _ => "Your response"
            };
        }
    }
}
