using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class LordsCardSummary
    {
        public static string Describe(PlayerView view, TurnDraft draft)
        {
            var choices = view.Options.First(o => o.Kind == CommandKind.ChooseEffect).EffectChoices;
            var selected = choices.Where(c => draft.Payload.Ids.Contains(c.Id)).ToArray();
            if (view.Decision.Presentation == "SmeagolCorruption" || view.Decision.SourceId == "lords.sh.strategy.26")
                return selected.Single().Detail;
            if (selected.Any(c => c.Exclusive))
                return "Keep the rolled results.";
            var die = selected.Single(c => c.SelectionGroup == "Die");
            var result = selected.Single(c => c.SelectionGroup == "Result");
            return die.Title + " → " + result.Title + " · No die spent";
        }
    }
}
