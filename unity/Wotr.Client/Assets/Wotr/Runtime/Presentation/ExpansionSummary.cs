using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ExpansionSummary
    {
        public static string Preview(PlayerView view, TurnDraft draft, ActionPreview preview)
        {
            if (draft.Kind == CommandKind.ChooseCompanionVersions)
                return ""; // The setup panel already marks every version and the explicitly selected Guide.
            if (draft.Kind == CommandKind.ChooseSpecialDie)
            {
                var die = view.Dice.Single(d => d.Id == draft.Payload.DieId);
                return "Keep " + ExpansionLabels.DieSource(die) + (die.Face == DieFace.Eye ? " in the Hunt" : " available") +
                    " · Discard the other special dice.";
            }
            if (draft.Kind == CommandKind.DiscardRulerDie)
                return "Discard " + ExpansionLabels.DieSource(view.Dice.Single(d => d.Id == draft.Payload.DieId)) + " until recovery.";
            return preview.Summary.Length > 0 ? preview.Summary : "Confirm this choice.";
        }
    }
}
