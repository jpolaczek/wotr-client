using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class ActionOpenRequest
    {
        public string ActionId { get; }
        public string Version { get; }
        public string StepId { get; }
        public ActionLogFilter Filter { get; }
        public bool IncludeConsequences { get; }
        // UI-166: the card shows the action's result (ActionResultText) instead of its steps.
        public bool Compact { get; }

        public ActionOpenRequest(string actionId, string version = "", string stepId = "", ActionLogFilter filter = ActionLogFilter.All,
            bool includeConsequences = false, bool compact = false)
        {
            ActionId = actionId;
            Version = version;
            StepId = stepId;
            Filter = filter;
            IncludeConsequences = includeConsequences;
            Compact = compact;
        }

        // The same action and version as its full record.
        public ActionOpenRequest Full() => new ActionOpenRequest(ActionId, Version, StepId, Filter, IncludeConsequences);
    }

    public sealed class ActionContextModel
    {
        public ActionContextKind Kind { get; private set; }
        public string BattleId { get; private set; } = "";
        public string HuntChainId { get; private set; } = "";
        public string NationId { get; private set; } = "";
        public string DefinitionId { get; private set; } = "";
        public IReadOnlyList<string> Regions { get; private set; }
        public IReadOnlyList<ActionMovement> Movements { get; private set; }
        public IReadOnlyList<int> MovementSteps { get; private set; }
        public IReadOnlyList<ActionHuntFact> Hunt { get; private set; }
        public InGameActionContext InGame { get; private set; }

        public static ActionContextModel For(ActionLogView view, string stepId = "")
        {
            var selected = string.IsNullOrEmpty(stepId) ? null : view.Steps.FirstOrDefault(s => s.Id == stepId);
            var context = selected?.Context ?? view.Action.Context;
            if (context.Kind == ActionContextKind.General && context.BattleId.Length == 0 && context.HuntChainId.Length == 0 && selected != null)
                context = view.Action.Context;
            var steps = selected == null ? view.Steps : context.BattleId.Length > 0
                ? view.Steps.Where(s => s.Context.BattleId == context.BattleId).ToList() : new List<ActionLogStep> { selected };
            var selectedIds = steps.Select(s => s.Id).ToHashSet();
            var recorded = view.Steps.SelectMany((step, index) => MovementsInOrder(new[] { step })
                .Select(move => (Move: move, Step: index + 1, Id: step.Id)))
                .Where(item => selectedIds.Contains(item.Id)).ToArray();
            var movements = recorded.Select(item => item.Move).ToArray();
            var chain = context.HuntChainId;
            var battleId = context.BattleId;
            var regions = context.Regions.Concat(movements.SelectMany(Route))
                .Concat(steps.SelectMany(s => s.Facts.Figures).SelectMany(f => new[] { f.From, f.To }))
                .Where(id => id.Length > 0).Distinct().ToArray();
            return new ActionContextModel
            {
                Kind = battleId.Length > 0 ? ActionContextKind.Battle : chain.Length > 0 ? ActionContextKind.Hunt : context.Kind,
                BattleId = battleId, HuntChainId = chain, NationId = context.NationId, DefinitionId = context.DefinitionId,
                Regions = regions, Movements = movements, MovementSteps = recorded.Select(item => item.Step).ToArray(),
                InGame = InGameActionContext.For(view, selected),
                Hunt = view.Steps.Select(s => s.Facts.Hunt).Where(h => h != null && h.ChainId == chain).ToArray()
            };
        }

        public static IEnumerable<string> Route(ActionMovement move) => move.Route.Count > 0
            ? move.Route : new[] { move.From, move.To };

        private static List<ActionMovement> MovementsInOrder(IEnumerable<ActionLogStep> steps)
        {
            var result = new List<ActionMovement>();
            foreach (var step in steps)
            {
                result.AddRange(step.Movements);
                if (step.Kind == "BattleStarted" && step.Facts.Battle is { } battle)
                    result.Add(Attack(battle));
                var route = step.Facts.FellowshipRoute;
                if (route.Count > 1)
                    result.Add(new ActionMovement { Kind = ActionMovementKind.Travel, From = route[0], To = route.Last(), Route = route });
            }
            return result;
        }

        private static ActionMovement Attack(BattleView battle) => new ActionMovement
        {
            Kind = ActionMovementKind.Attack, From = battle.Attacker.RegionId, To = battle.RegionId,
            Participants = battle.Attacker.Units.Select(u => new ActionParticipant
            {
                Id = u.Id, Side = u.Side, NationId = u.Nation, UnitKind = u.Kind, Garrison = u.Garrison
            }).Concat(battle.Attacker.Characters.Select(c => new ActionParticipant
            {
                Id = c.Id, DefinitionId = c.DefinitionId, Side = c.Side, Garrison = c.Garrison,
                NationId = c.NationIcon, Level = c.Level, Leadership = c.Leadership
            })).ToList()
        };
    }

    public static class ActionVersion
    {
        // The click captures a visible version. A GET arriving later may already contain newer steps.
        public static ActionLogView Through(ActionLogView view, string version, bool includeConsequences = false)
        {
            if (version.Length == 0 || view.Action.Version == version)
                return new ActionLogView { MatchId = view.MatchId, Action = view.Action, Steps = new List<ActionLogStep>(view.Steps) };
            var end = view.Steps.FindIndex(s => s.Id == version);
            if (end < 0) return null;
            if (includeConsequences)
                end = ConsequencesEnd(view.Steps, end);
            var action = view.Action;
            var steps = view.Steps.Take(end + 1).ToList();
            return new ActionLogView
            {
                MatchId = view.MatchId, Steps = steps,
                Action = new ActionLogSummary
                {
                    Id = action.Id, Version = steps.Last().Id, Initiator = action.Initiator, Command = action.Command,
                    Turn = action.Turn, PartialHistory = action.PartialHistory, Legacy = action.Legacy,
                    Context = action.Context, Costs = action.Costs, Status = steps.Last().Status, StepCount = steps.Count
                }
            };
        }

        private static int ConsequencesEnd(IReadOnlyList<ActionLogStep> steps, int end)
        {
            var source = steps[end];
            while (end + 1 < steps.Count)
            {
                var next = steps[end + 1];
                if (!next.Automatic || next.Actor.HasValue || next.Revision != source.Revision)
                    break;
                end++;
            }
            return end;
        }
    }
}
