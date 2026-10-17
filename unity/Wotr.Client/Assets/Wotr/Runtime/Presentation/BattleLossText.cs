using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class BattleLossText
    {
        public static string Budget(PlayerView view, TurnDraft draft, ArmyLossPreview preview = null)
        {
            var chosen = preview?.AppliedHits ?? BattleDraft.Damage(draft, view);
            var hits = preview?.RequiredHits ?? view.EventLoss?.Hits ?? view.Battle.PendingHits;
            if (preview?.Voluntary == true || view.Battle?.VoluntaryLosses == true)
                return "Chosen " + chosen + " / " + hits;
            var remaining = hits - chosen;
            return "Assigned " + chosen + " / " + hits + " · " + (remaining < 0 ? "Over by " + -remaining :
                preview != null && remaining > 0 ? remaining + " excess hits" : "Remaining " + remaining);
        }

        public static string Operation(ActionLossFact loss)
        {
            var outcome = loss.Operation == BattleLossKind.Remove ? "Remove" : loss.ReplacementId.Length == 0
                ? "No regular available · Elite removed" : "Elite → Regular from " + loss.ReplacementFrom;
            return outcome + " · " + loss.Hits + (loss.Hits == 1 ? " hit" : " hits");
        }

        public static string Details(ArmyLossPreview preview) => preview == null ? "" : string.Join("\n",
            preview.Operations.Select((loss, index) => (index + 1) + ". " + FigureGrid.Name(loss.Figure.NationId) + " · " + Operation(loss)));
    }
}
