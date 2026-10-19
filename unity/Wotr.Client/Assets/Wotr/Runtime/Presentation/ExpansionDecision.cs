using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class ExpansionDecision
    {
        public static bool IsImmediate(CommandKind kind) => kind is CommandKind.DeclineVilya
            or CommandKind.DeclineDismay or CommandKind.UseLoreBeforeActions or CommandKind.DrawCorruptionTile;

        public static string Title(DecisionKind kind) => kind switch
        {
            DecisionKind.CouncilVersions => "Choose your companions · Private",
            DecisionKind.CouncilHomelands => "Starting positions · Private until confirmed",
            DecisionKind.CouncilToken => "Choose a Council token",
            DecisionKind.SpecialDieChoice => "Special dice",
            DecisionKind.RulerDiscard => "Choose a die to discard",
            DecisionKind.Dismay => "Dismay · Re-roll a Ruler die?",
            DecisionKind.KeepActionDie => "Vilya · Keep a die?",
            _ => ""
        };

        public static string Label(CommandKind kind) => kind switch
        {
            CommandKind.ChooseCompanionVersions => "Confirm companions",
            CommandKind.ChooseCouncilHomelands => "Confirm starting positions",
            CommandKind.ChooseCouncilToken => "Take token",
            CommandKind.UseCouncilToken => "Use Council token",
            CommandKind.ChooseSpecialDie => "Keep this die",
            CommandKind.DiscardRulerDie => "Discard this die",
            CommandKind.UseDismay => "Use Dismay",
            CommandKind.DeclineDismay => "Keep tokens",
            CommandKind.UseLoreBeforeActions => "Use Lore first",
            CommandKind.UseNarya => "Use Narya",
            CommandKind.UseVilya => "Keep die · Use Vilya",
            CommandKind.DeclineVilya => "Spend die · Keep Vilya",
            CommandKind.AwakenSovereign => "Awaken sovereign",
            CommandKind.RemoveCorruptedSovereign => "Remove corrupted sovereign",
            CommandKind.AttemptCorruption => "Attempt corruption",
            CommandKind.UseEvilKnowledge => "Evil Knowledge · Extend siege",
            _ => ""
        };

        public static IEnumerable<ActionOption> Alternatives(PlayerView view, TurnDraft draft) =>
            view.Decision.Kind is DecisionKind.Dismay or DecisionKind.KeepActionDie or DecisionKind.CorruptionTile
                ? view.Options.Where(o => o.Kind != draft.Kind) : Enumerable.Empty<ActionOption>();
    }
}
