using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Adapts current server options to nation gestures; does not implement political rules.
    public static class PoliticalActions
    {
        public static bool UsesPanel(TurnDraft draft) => draft.Kind == CommandKind.AdvancePolitics ||
            draft.Kind == CommandKind.UseCouncilToken && draft.Payload.AbilityId == ExpansionDraft.CouncilPolitics;

        public static string Hint(PlayerView view, TurnDraft draft, ActionDieContext die)
        {
            if (AdvanceOption(view, draft, die) != null)
                return "Politics → click a nation’s arrow or token to move it one step toward war.";
            if (draft.Kind == CommandKind.UseCouncilToken)
                return "The Council politics token is not available in this decision.";
            return "Select an available Muster result from your dice, then continue in Politics.";
        }

        public static ActionOption AdvanceOption(PlayerView view, TurnDraft draft, ActionDieContext die)
        {
            if (view?.DecisionOwner != view?.You || view?.Decision?.Kind != DecisionKind.Action)
                return null;
            if (draft.Kind == CommandKind.UseCouncilToken && draft.Payload.AbilityId == ExpansionDraft.CouncilPolitics)
                return view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.UseCouncilToken &&
                    o.EligibleIds.Contains(ExpansionDraft.CouncilPolitics));
            return MusterActions.Active(view, draft, die) ? MusterActions.Option(view, die, CommandKind.AdvancePolitics) : null;
        }

        public static bool Advance(TurnDraft draft, PlayerView view, ActionDieContext die, string nation)
        {
            var option = AdvanceOption(view, draft, die);
            if (option?.Targets.Contains(nation) != true)
                return false;
            if (option.Kind == CommandKind.AdvancePolitics)
                return MusterActions.Politics(draft, view, die, nation);
            draft.Choose(CommandKind.UseCouncilToken, "");
            draft.Change(p =>
            {
                p.AbilityId = ExpansionDraft.CouncilPolitics;
                p.NationId = nation;
            });
            return true;
        }

        public static ActionOption NaryaOption(PlayerView view, TurnDraft draft)
        {
            if (view?.DecisionOwner != view?.You || view?.Decision?.Kind != DecisionKind.Action || draft.Payload.RulerTokenId.Length > 0)
                return null;
            return view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.UseNarya);
        }

        public static bool CanActivate(PlayerView view, TurnDraft draft, string nation)
        {
            var option = NaryaOption(view, draft);
            return option?.Targets.Contains(nation) == true && option.EligibleDice.Contains(draft.Payload.DieId) &&
                view.Nations.Any(n => n.Id == nation && !n.Active);
        }

        public static bool Activate(TurnDraft draft, PlayerView view, string nation)
        {
            if (!CanActivate(view, draft, nation))
                return false;
            var option = NaryaOption(view, draft);
            var die = draft.Payload.DieId;
            draft.Choose(CommandKind.UseNarya, die);
            ExpansionDraft.Initialize(draft, view, option, "");
            draft.Change(p => p.NationId = nation);
            return true;
        }

        // UI-144: the Activate button stands beside each passive nation the server lets activate.
        public static bool ActivationOffered(PlayerView view, TurnDraft draft, string nation) =>
            NaryaOption(view, draft)?.Targets.Contains(nation) == true && view.Nations.Any(n => n.Id == nation && !n.Active);

        public static string ActivationHint(PlayerView view, TurnDraft draft, string nation)
        {
            if (!ActivationOffered(view, draft, nation))
                return "";
            return (CanActivate(view, draft, nation) ? "Use Narya with the selected die." : "Select an available action die to use Narya.") +
                "\nSpend the die and transfer Narya to the Shadow. Activate this nation and place it At War.";
        }

        public const string ActivationSources =
            "Activation follows an eligible Fellowship declaration, companion arrival, enemy incursion or attack, or a card effect." +
            "\nMuster advances the track; it does not activate a nation. Card restrictions still apply.";

        public static string Describe(NationView nation) => FigureGrid.Name(nation.Id) + " · " +
            (nation.Active ? "Active" : "Passive") + " · " +
            (nation.StepsToWar == 0 ? "At war" : nation.StepsToWar + (nation.StepsToWar == 1 ? " step" : " steps") + " to war");
    }
}
