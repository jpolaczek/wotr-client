using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // A single unsent force. A saved step is represented only by ActionSequenceView.
    public static class ContextArmySelection
    {
        public static bool Active(PlayerView view, TurnDraft draft, ActionDieContext context) =>
            view.Decision?.Kind is DecisionKind.Action or DecisionKind.ActionSequence &&
            (context.Supports(DieFace.Army) || context.Supports(DieFace.Character)) &&
            draft.Payload.RulerTokenId.Length == 0 && draft.Payload.AbilityId == context.AbilityId &&
            ActionDieContext.DirectIntent(draft) &&
            (view.ActionSequence == null || view.ActionSequence.Kind == CommandKind.MoveArmies);

        public static IEnumerable<ActionOption> Options(PlayerView view, ActionDieContext context) => view.Options
            .Where(o => o.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy && context.Allows(o));

        // UI-145: the server lists every army in one option. A Character cost commands only a force with a commander,
        // and an ability's cost only the force with its named character, so only those forces are sources.
        public static IEnumerable<(ActionOption Option, ArmyOption Army)> Forces(PlayerView view, ActionDieContext context) =>
            Options(view, context).SelectMany(o => o.Armies.Where(a => Commands(context.ArmyCost(o), a)).Select(a => (o, a)));

        public static IEnumerable<ArmyOption> Sources(PlayerView view, ActionDieContext context) =>
            Forces(view, context).Select(force => force.Army);

        public static bool Commands(MovementCostOption cost, ArmyOption army) =>
            cost?.RequiresCommander != true || army.CommanderIds.Count > 0 &&
            (cost.RequiredCharacterId.Length == 0 || army.CharacterIds.Contains(cost.RequiredCharacterId));

        public static bool RequiresCommander(PlayerView view, ActionDieContext context) =>
            Options(view, context).Any(o => context.ArmyCost(o)?.RequiresCommander == true);

        public static HashSet<string> Eligible(PlayerView view, ActionDieContext context) => Sources(view, context)
            .SelectMany(a => a.UnitIds.Concat(a.CharacterIds)).ToHashSet();

        public static bool Select(TurnDraft draft, PlayerView view, ActionDieContext context, string id)
        {
            var (option, army) = Forces(view, context).FirstOrDefault(force => Contains(force.Army, id));
            if (army == null)
                return false;
            var move = ArmyDraft.Current(draft);
            if (move == null || move.From != army.RegionId || move.To.Length > 0 ||
                move.UnitIds.Count + move.CharacterIds.Count == 0 ||
                move.UnitIds.Any(existing => view.Units.First(u => u.Id == existing).Garrison != army.Garrison) ||
                move.CharacterIds.Any(existing => view.Characters.First(c => c.Id == existing).Garrison != army.Garrison))
                Prepare(draft, view, context, option.Kind, army.RegionId);
            ArmyDraft.Toggle(draft, new[] { id }, army.CharacterIds.Contains(id));
            return true;
        }

        public static bool SelectAll(TurnDraft draft, PlayerView view, ActionDieContext context, string region, bool garrison)
        {
            var forces = Forces(view, context).Where(force => force.Army.RegionId == region && force.Army.Garrison == garrison)
                .ToList();
            if (forces.Count == 0)
                return false;
            Prepare(draft, view, context, forces[0].Option.Kind, region);
            draft.Change(p =>
            {
                p.Moves[0].UnitIds = forces.SelectMany(force => force.Army.UnitIds).Distinct().ToList();
                p.Moves[0].CharacterIds = forces.SelectMany(force => force.Army.CharacterIds).Distinct().ToList();
            });
            return true;
        }

        private static void Prepare(TurnDraft draft, PlayerView view, ActionDieContext context, CommandKind kind, string region)
        {
            draft.Choose(kind, context.DieId);
            draft.Change(p =>
            {
                p.Stepwise = kind == CommandKind.MoveArmies;
                p.MovementCost = context.ArmyFace(view.Options.First(o => o.Kind == kind)).Value;
                p.AbilityId = context.AbilityId;
                p.Moves.Add(new ArmyMoveChoice { From = region });
                draft.MoveFocus = 0;
            });
        }

        public static bool Target(TurnDraft draft, PlayerView view, ActionDieContext context, string region)
        {
            var kind = ArmyActionTargets.Kind(draft, view, context, region);
            if (!kind.HasValue)
                return false;
            var move = ArmyDraft.Current(draft);
            var units = new List<string>(move.UnitIds);
            var characters = new List<string>(move.CharacterIds);
            Prepare(draft, view, context, kind.Value, move.From);
            draft.Change(p =>
            {
                p.Moves[0].UnitIds = units;
                p.Moves[0].CharacterIds = characters;
                p.Moves[0].To = region;
                draft.MoveFocus = -1;
            });
            return true;
        }

        public static bool Contains(ArmyOption army, string id) => army.UnitIds.Contains(id) || army.CharacterIds.Contains(id);
    }
}
