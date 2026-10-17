using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class MapDraftOverlay
    {
        private readonly MapCanvas map;
        public HashSet<string> Targets { get; } = new HashSet<string>();
        public HashSet<string> AttackTargets { get; } = new HashSet<string>();
        public HashSet<string> PlannedRegions { get; } = new HashSet<string>();
        private string endpoint = "";

        public MapDraftOverlay(MapCanvas map) => this.map = map;

        public bool IsRouteEnd(string region) => endpoint.Length > 0 && endpoint == region;

        public void Apply(PlayerView view, TurnDraft draft, ActionDieContext context = null)
        {
            Targets.Clear();
            AttackTargets.Clear();
            endpoint = "";
            if (context != null)
            {
                if (MusterActions.Active(view, draft, context) && draft.Kind is null or CommandKind.Recruit)
                    Targets.UnionWith(MusterActions.Targets(view, draft, context));
                if (ContextArmySelection.Active(view, draft, context) && draft.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy)
                {
                    Targets.UnionWith(ArmyActionTargets.For(draft, view, context));
                    AttackTargets.UnionWith(ArmyActionTargets.For(draft, view, context, CommandKind.AttackArmy));
                }
            }
            if (context == null && draft.Kind == CommandKind.AttackArmy)
                Targets.UnionWith(AttackDraft.Targets(draft, view));
            if (draft.Kind == CommandKind.RetreatBattle)
                Targets.UnionWith(view.Options.First(o => o.Kind == draft.Kind).Targets);
            if (draft.Kind == CommandKind.AdvanceBattle && view.Battle != null && draft.Payload.Ids.Count > 0)
                Targets.Add(view.Battle.RegionId);
            if (context == null && draft.Kind == CommandKind.MoveArmies && draft.MoveFocus >= 0)
                Targets.UnionWith(ArmyDraft.Targets(draft, view));
            if (context == null && draft.Kind == CommandKind.Recruit)
                Targets.UnionWith(RecruitDraft.Targets(draft, view));
            if (draft.Kind == CommandKind.ChooseEffect)
                Targets.UnionWith(EventRecruitDraft.Targets(draft, EventRecruitDraft.Option(view)));
            if (draft.Kind == CommandKind.ChooseEffect)
                Targets.UnionWith(view.Options.Where(o => o.Kind == draft.Kind)
                    .SelectMany(o => o.EligibleIds.Concat(view.Decision?.Presentation is "Region" or "ArmyFigure" or "ArmyFigures"
                        ? o.EffectChoices.Select(c => view.Decision.Presentation == "Region" ? c.TargetId : EffectChoiceDraft.FigureRegion(c)) : Enumerable.Empty<string>())).Where(map.Data.Regions.ContainsKey));
            if (NazgulFlightDraft.Active(view, draft))
                Targets.UnionWith(NazgulFlightDraft.Targets(view, draft));
            else if (TravelDraft.TargetRoute(draft.Kind))
                Targets.UnionWith(TravelDraft.Reachable(draft, view, map.Data));
            else if (FellowshipDraft.UsesRoute(draft.Kind))
            {
                Targets.UnionWith(TravelDraft.Next(draft, view, map.Data));
                var travel = DraftOptions.For(view, draft) is { } option ? TravelDraft.Travel(draft, option) : null;
                var route = TravelDraft.Route(draft);
                if (travel != null && route != null)
                {
                    var end = route.Count > 0 ? route.Last() : travel.Origin;
                    if (travel.Targets.Contains(end))
                        Targets.Add(end);
                    if (route.Count > 0 && TravelDraft.EndAllowed(route, travel))
                        endpoint = end;
                }
            }
            if (CharacterDraft.UsesCharacters(draft.Kind))
            {
                var character = CharacterDraft.Selected(draft, DraftOptions.For(view, draft));
                Targets.UnionWith(character?.RulerRecruitment != null ? CharacterRecruitDraft.Targets(draft, character)
                    : character?.Targets ?? new List<string>());
            }
            if (draft.Kind == CommandKind.ChooseCouncilHomelands)
                Targets.UnionWith(DraftOptions.For(view, draft).CharacterActions
                    .Where(c => c.CharacterId == draft.Payload.CharacterId).SelectMany(c => c.Targets));
            UpdatePlannedRegions(view, draft);
            map.ShowDraftSources(view, draft);
            map.Refresh();
        }

        private void UpdatePlannedRegions(PlayerView view, TurnDraft draft)
        {
            PlannedRegions.Clear();
            PlannedRegions.UnionWith(draft.Payload.Moves.Where(_ => draft.Kind != CommandKind.AttackArmy)
                .Select(move => move.To).Where(map.Data.Regions.ContainsKey));
            PlannedRegions.UnionWith(draft.Payload.Placements.Concat(EffectChoiceDraft.Placements(view, draft))
                .Select(placement => placement.RegionId));
            PlannedRegions.UnionWith(draft.Payload.CompanionMoves.Where(move => move.Route.Count > 0)
                .Select(move => move.Route.Last()));
            if (draft.Kind == CommandKind.EnterCharacter && map.Data.Regions.ContainsKey(draft.Payload.RegionId))
                PlannedRegions.Add(draft.Payload.RegionId);
            if (FellowshipDraft.UsesRoute(draft.Kind) && draft.Payload.Route.Count > 0)
                PlannedRegions.Add(draft.Payload.Route.Last());
            var fellowship = EffectChoiceDraft.FellowshipDestination(view, draft);
            if (map.Data.Regions.ContainsKey(fellowship))
                PlannedRegions.Add(fellowship);
        }
    }
}
