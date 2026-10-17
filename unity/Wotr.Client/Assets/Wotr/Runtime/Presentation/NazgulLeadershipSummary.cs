using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class NazgulLeadershipSummary
    {
        public static string Title(PlayerView view)
        {
            var option = view.Options.First(o => o.Kind == CommandKind.ChooseEffect);
            return option.Minimum == option.Maximum ? "Forfeit " + option.Minimum + " Leadership" : "Forfeit Leadership";
        }

        public static string Describe(TurnDraft draft, PlayerView view)
        {
            var title = CardLibrary.Cards[view.Decision.SourceId].CombatTitle;
            var benefit = title == "The Battle Is Vain" ? "Reduce opposing Leadership by " + draft.Payload.Ids.Count
                : title == "Dread and Despair" ? "Fewer opposing Combat dice · Minimum one"
                : title == "They Are Terrible" ? "+1 Leader re-roll" : "+1 Combat roll";
            return "Forfeit " + draft.Payload.Ids.Count + " Leadership · " + benefit + ".";
        }
    }
}
