using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class TravelDraft
    {
        public static CompanionMoveChoice Group(TurnDraft draft) => draft.CompanionFocus >= 0 && draft.CompanionFocus < draft.Payload.CompanionMoves.Count
            ? draft.Payload.CompanionMoves[draft.CompanionFocus] : null;
        public static bool IsGrouped(CommandKind? kind) => kind is CommandKind.MoveCompanions or CommandKind.MoveShadowCharacters or CommandKind.UseCouncilToken;
        public static List<string> Route(TurnDraft draft) => IsGrouped(draft.Kind) ? Group(draft)?.Route : draft.Payload.Route;
        public static TravelOption GroupTravel(IEnumerable<string> ids, ActionOption option)
        {
            var group = option.Travel.Where(t => ids.Contains(t.Id)).ToList();
            var travel = group.OrderByDescending(t => t.Range).FirstOrDefault();
            if (travel == null)
                return null;
            var bonus = group.Count <= 2 ? group.FirstOrDefault(t => t.SmallGroupRange > t.Range &&
                group.All(other => other.Id == t.Id || t.SmallGroupPartners.Contains(other.Id))) : null;
            var source = bonus ?? travel;
            return new TravelOption
            {
                Id = source.Id,
                Origin = source.Origin,
                Range = bonus?.SmallGroupRange ?? source.Range,
                Flight = source.Flight,
                Targets = source.Targets,
                StopRegions = source.StopRegions,
                BlockedRegions = source.BlockedRegions,
                RequiredGroup = source.RequiredGroup
            };
        }
        public static TravelOption Travel(TurnDraft draft, ActionOption option) => draft.Kind == CommandKind.SeparateCompanions
            ? GroupTravel(draft.Payload.Ids, option) : IsGrouped(draft.Kind)
            ? GroupTravel(Group(draft)?.Ids ?? new List<string>(), option) : option.Travel.FirstOrDefault();

        public static bool EndAllowed(IReadOnlyList<string> route, TravelOption travel) => travel != null && route.Count > 0 &&
            route[0] == travel.Origin && (travel.Flight ? route.Count == 2 : route.Count - 1 <= travel.Range) && travel.Targets.Contains(route[route.Count - 1]);

        public static bool PrefixAllowed(IReadOnlyList<string> route, TravelOption travel)
        {
            if (route.Count == 0)
                return true;
            if (travel == null || route[0] != travel.Origin)
                return false;
            if (travel.Flight)
                return route.Count == 1 || route.Count == 2 && travel.Targets.Contains(route[1]);
            return route.Count - 1 <= travel.Range && !route.Any(travel.BlockedRegions.Contains) &&
                !route.Skip(1).Take(route.Count - 2).Any(travel.StopRegions.Contains) &&
                (travel.Targets.Contains(route[route.Count - 1]) ||
                    route.Count - 1 < travel.Range && !travel.StopRegions.Contains(route[route.Count - 1]));
        }

        public static IEnumerable<string> Next(TurnDraft draft, PlayerView view, BoardPresentation data)
        {
            var option = DraftOptions.For(view, draft);
            if (option == null || !FellowshipDraft.UsesRoute(draft.Kind))
                return Enumerable.Empty<string>();
            var travel = Travel(draft, option);
            var route = Route(draft);
            if (travel == null || route == null)
                return Enumerable.Empty<string>();
            if (travel.Flight)
                return travel.Targets;
            var last = route.Count == 0 ? travel.Origin : route[route.Count - 1];
            if (System.Math.Max(0, route.Count - 1) >= travel.Range || travel.BlockedRegions.Contains(last) || route.Count > 1 && travel.StopRegions.Contains(last))
                return Enumerable.Empty<string>();
            return data.Regions.TryGetValue(last, out var region)
                ? region.Neighbors.Where(id => !travel.BlockedRegions.Contains(id)) : Enumerable.Empty<string>();
        }

        public static bool Step(TurnDraft draft, PlayerView view, BoardPresentation data, string region)
        {
            var option = DraftOptions.For(view, draft);
            if (FellowshipDraft.UsesRoute(draft.Kind) && Route(draft)?.Count == 0 && option != null && Travel(draft, option) is { } location &&
                location.Origin == region && location.Targets.Contains(region))
            {
                Stay(draft, option);
                return true;
            }
            if (!Next(draft, view, data).Contains(region))
                return false;
            draft.Change(_ =>
            {
                var route = Route(draft);
                if (Travel(draft, option).Flight)
                    route.Clear();
                if (route.Count == 0)
                    route.Add(Travel(draft, option).Origin);
                route.Add(region);
            });
            return true;
        }

        // UI-105: character groups move by a click on their destination. Only the Fellowship's declaration and
        // revelation keep a route the player draws step by step.
        public static bool TargetRoute(CommandKind? kind) => FellowshipDraft.UsesRoute(kind) && !FellowshipDraft.LocationRoute(kind);

        // The destinations the current group can reach with its range, stops and blocked regions.
        public static HashSet<string> Reachable(TurnDraft draft, PlayerView view, BoardPresentation data)
        {
            var option = DraftOptions.For(view, draft);
            var travel = option == null ? null : Travel(draft, option);
            if (travel == null || Route(draft) == null)
                return new HashSet<string>();
            if (travel.Flight)
                return new HashSet<string>(travel.Targets);
            return new HashSet<string>(Routes(travel, data).Keys.Where(travel.Targets.Contains));
        }

        // Sets the shortest legal route to the clicked destination; ties go to the first neighbour by id,
        // since only the destination is the player's choice.
        public static bool RouteTo(TurnDraft draft, PlayerView view, BoardPresentation data, string target)
        {
            var option = DraftOptions.For(view, draft);
            var travel = option == null ? null : Travel(draft, option);
            if (travel == null || Route(draft) == null || !travel.Targets.Contains(target))
                return false;
            if (target == travel.Origin)
            {
                Stay(draft, option);
                return true;
            }
            var route = Path(travel, data, target);
            if (route == null)
                return false;
            draft.Change(_ =>
            {
                Route(draft).Clear();
                Route(draft).AddRange(route);
            });
            return true;
        }

        // The route of one figure to a target: a flight goes straight there, a walker along the shortest path within its
        // range (UI-271: also a Shadow character moved by a card); the server checks every step.
        public static List<string> Path(TravelOption travel, BoardPresentation data, string target)
        {
            if (travel.Flight)
                return new List<string> { travel.Origin, target };
            return data != null && Routes(travel, data).TryGetValue(target, out var route) ? route : null;
        }

        private static Dictionary<string, List<string>> Routes(TravelOption travel, BoardPresentation data)
        {
            var routes = new Dictionary<string, List<string>> { [travel.Origin] = new List<string> { travel.Origin } };
            var frontier = new Queue<string>();
            frontier.Enqueue(travel.Origin);
            while (frontier.Count > 0)
            {
                var region = frontier.Dequeue();
                var route = routes[region];
                var passing = route.Count == 1 || !travel.StopRegions.Contains(region);
                if (route.Count - 1 >= travel.Range || !passing || !data.Regions.TryGetValue(region, out var definition))
                    continue;
                foreach (var next in definition.Neighbors.OrderBy(id => id, System.StringComparer.Ordinal))
                {
                    if (routes.ContainsKey(next) || travel.BlockedRegions.Contains(next))
                        continue;
                    routes[next] = new List<string>(route) { next };
                    frontier.Enqueue(next);
                }
            }
            return routes;
        }

        public static void Stay(TurnDraft draft, ActionOption option)
        {
            var travel = Travel(draft, option);
            if (travel != null && Route(draft) != null)
                draft.Change(_ =>
                {
                    Route(draft).Clear();
                    Route(draft).Add(travel.Origin);
                    if (travel.Flight)
                        Route(draft).Add(travel.Origin);
                });
        }

        // UI-250: groups are built by clicks alone. A member of another planned group selects that group for editing;
        // a companion outside the current group starts the next group once the current one has its route (or when it
        // cannot join it); a group left without members is dropped. No group buttons are needed.
        public static void ToggleCompanion(TurnDraft draft, PlayerView view, ActionOption option, string id)
        {
            if (IsGrouped(draft.Kind))
            {
                var planned = PlannedGroup(draft, id);
                if (planned >= 0)
                {
                    SelectGroup(draft, planned);
                    return;
                }
                if (StartsNextGroup(draft, view, option, id))
                    NewGroup(draft);
            }
            if (!CanToggle(draft, view, option, id))
                return;
            draft.Change(p =>
            {
                if (IsGrouped(draft.Kind) && Group(draft) == null)
                {
                    p.CompanionMoves.Add(new CompanionMoveChoice());
                    draft.CompanionFocus = p.CompanionMoves.Count - 1;
                }
                var ids = IsGrouped(draft.Kind) ? Group(draft).Ids : p.Ids;
                if (!ids.Remove(id))
                    ids.Add(id);
                if (IsGrouped(draft.Kind) && Group(draft).Ids.Count == 0)
                {
                    p.CompanionMoves.RemoveAt(draft.CompanionFocus);
                    draft.CompanionFocus = p.CompanionMoves.Count - 1;
                }
            });
        }

        // UI-250: a companion's click does something now: it joins or leaves the current group, starts the next group or
        // selects its planned group.
        public static bool Selectable(TurnDraft draft, PlayerView view, ActionOption option, string id) =>
            CanToggle(draft, view, option, id) || IsGrouped(draft.Kind) &&
            (PlannedGroup(draft, id) >= 0 || StartsNextGroup(draft, view, option, id));

        // The index of another planned group that holds this figure, or -1.
        public static int PlannedGroup(TurnDraft draft, string id)
        {
            for (var index = 0; index < draft.Payload.CompanionMoves.Count; index++)
                if (index != draft.CompanionFocus && draft.Payload.CompanionMoves[index].Ids.Contains(id))
                    return index;
            return -1;
        }

        private static bool StartsNextGroup(TurnDraft draft, PlayerView view, ActionOption option, string id)
        {
            var current = Group(draft);
            if (current == null || current.Ids.Contains(id) || !option.EligibleIds.Contains(id) ||
                option.Travel.Any(t => t.RequiredGroup.Contains(id)))
                return false;
            if (option.MaximumGroups > 0 && draft.Payload.CompanionMoves.Count >= option.MaximumGroups)
                return false;
            return current.Route.Count > 0 || !CanToggle(draft, view, option, id);
        }

        public static bool CanToggle(TurnDraft draft, PlayerView view, ActionOption option, string id)
        {
            if (!option.EligibleIds.Contains(id))
                return false;
            if (option.Travel.Any(t => t.RequiredGroup.Contains(id)))
                return false;
            if (!IsGrouped(draft.Kind))
                return true;
            if (Group(draft) == null && option.MaximumGroups > 0 && draft.Payload.CompanionMoves.Count >= option.MaximumGroups)
                return false;
            if (draft.Payload.CompanionMoves.Where((_, i) => i != draft.CompanionFocus).Any(g => g.Ids.Contains(id)))
                return false;
            var first = Group(draft)?.Ids.FirstOrDefault();
            if (first == null)
                return true;
            var current = option.Travel.First(c => c.Id == first);
            var candidate = option.Travel.First(c => c.Id == id);
            var currentGarrison = view.Characters.FirstOrDefault(c => c.Id == first)?.Garrison ?? view.Units.FirstOrDefault(u => u.Id == first)?.Garrison;
            var candidateGarrison = view.Characters.FirstOrDefault(c => c.Id == id)?.Garrison ?? view.Units.FirstOrDefault(u => u.Id == id)?.Garrison;
            return current.Origin == candidate.Origin && currentGarrison == candidateGarrison && current.Flight == candidate.Flight;
        }

        public static void NewGroup(TurnDraft draft) => draft.Change(_ => draft.CompanionFocus = -1);
        public static void SelectGroup(TurnDraft draft, int index) => draft.Change(_ => draft.CompanionFocus = index);
        public static void RemoveGroup(TurnDraft draft)
        {
            if (Group(draft) != null)
                draft.Change(p =>
                {
                    p.CompanionMoves.RemoveAt(draft.CompanionFocus);
                    draft.CompanionFocus = p.CompanionMoves.Count - 1;
                });
        }
    }
}
