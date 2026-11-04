using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-491 (owner, 2026-11-04): the battle's quiet decisions, which step only its ribbon, each with its one short caption.
    // Everything here is public: whether a side played a card shows at any table, and a committed card has no caption at all.
    public static class BattleChoiceCaption
    {
        public static bool Quiet(ActionLogStep step) => step.Command == CommandKind.CommitCombatCard
            ? step.Facts.Cards.Any(card => card.Use == ActionCardUse.CombatCommit && card.Count == 0)
            : Of(step.Command) != null;

        public static string Of(CommandKind? command) => command switch
        {
            CommandKind.ContinueBattle => "Continues",
            CommandKind.StopBattle => "Stops",
            CommandKind.HoldBattle => "Holds",
            CommandKind.FightFieldBattle => "Fights in the field",
            CommandKind.UseEvilKnowledge => "Evil Knowledge",
            CommandKind.CommitCombatCard => "No card",
            _ => null
        };
    }
}
