using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-459 (owner, 2026-11-03): "when we click the units a window with them opens where we mark who goes and who doesn't,
    // then the move is done as always, by click or drag". The Balrog die is its Use Balrog ability (ActionDieContext.Implied),
    // so the Balrog's force is one selection in his region's panel: with only the Balrog marked he moves alone (the
    // server's travel of the Balrog, LordsArmyCosts.ValidTravel); with units marked too he moves or attacks with them (an
    // army move whose Character cost includes him, LordsArmyCosts.ValidArmy). The Nazgûl never travels with him: a
    // selection without an army unit has no target (ArmyActionTargets.Problem), and one without him asks to include him.
    public static class BalrogSelection
    {
        // An army selection left with only the die's own character becomes his move alone, in the same gesture.
        public static bool Normalize(TurnDraft draft, PlayerView view, ActionDieContext context)
        {
            if (!context.Implied || draft.Kind is not (CommandKind.MoveArmies or CommandKind.AttackArmy))
                return false;
            var move = ArmyDraft.Current(draft);
            var character = Character(view, context);
            if (move == null || move.To.Length > 0 || character.Length == 0 || move.UnitIds.Count > 0 ||
                !move.CharacterIds.SequenceEqual(new[] { character }))
                return false;
            return TravelActions.Select(view, draft, context, character);
        }

        // The console's hint while the die waits for his force (DecisionTray's object hints); "" for any other die or step.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (draft.Kind.HasValue || view?.Decision?.Kind != DecisionKind.Action || view.DecisionOwner != view.You)
                return "";
            var context = ActionDieContext.Of(view, draft);
            return context.Implied ? Hint(view, context) : "";
        }

        // "Click the Balrog's army to select all of it · mark who goes in its panel; the Balrog alone moves by himself"
        public static string Hint(PlayerView view, ActionDieContext context)
        {
            var name = CharacterLibrary.Name(view, Character(view, context));
            return "Click " + name + "’s army to select all of it · mark who goes in its panel; " + name + " alone moves by himself";
        }

        // The character the die's ability moves (the Balrog), named by the server's movement cost.
        public static string Character(PlayerView view, ActionDieContext context) => view.Options.Where(o => o.Enabled)
            .SelectMany(o => o.MovementCosts)
            .FirstOrDefault(c => c.DieId == context.DieId && c.AbilityId == context.AbilityId)?.RequiredCharacterId ?? "";
    }
}
