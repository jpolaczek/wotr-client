using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-212 (CARD_CHOICES_UX): a card's Nazgûl flights, chosen on the map. A click arms the unmoved Nazgûl of a region,
    // or one figure clicked in the region panel; every click on a destination flies one armed Nazgûl there, and a region
    // stays armed while it has an unmoved Nazgûl. The Witch-king flies only when he is armed himself (his figure, or a
    // region where he is the only unmoved Nazgûl). Each flight is one undo step. UI-271: every Shadow character a card
    // moves (the Balrog alone, the Lords Minions) uses the same gesture; a walker goes along its shortest path in range.
    public static class NazgulFlightDraft
    {
        public static bool Active(PlayerView view, TurnDraft draft) =>
            view?.Decision?.Kind == DecisionKind.CharacterEvent && view.DecisionOwner == view.You &&
            draft.Kind == CommandKind.MoveShadowCharacters;

        public static IEnumerable<TravelOption> Unmoved(PlayerView view, TurnDraft draft) =>
            DraftOptions.For(view, draft)?.Travel.Where(t => !Flown(draft, t.Id)) ?? Enumerable.Empty<TravelOption>();

        // Where an unmoved Nazgûl stands, for the map's source highlight.
        public static IEnumerable<ArmyOption> Sources(PlayerView view, TurnDraft draft) => Unmoved(view, draft)
            .Select(t => (t.Origin, Garrison: Garrison(view, t.Id)))
            .Distinct()
            .Select(source => new ArmyOption { RegionId = source.Origin, Garrison = source.Garrison });

        public static IEnumerable<string> Movable(PlayerView view, TurnDraft draft) => Unmoved(view, draft).Select(t => t.Id);

        // The armed region, or the region of the armed figure.
        public static string ArmedRegion(PlayerView view, TurnDraft draft)
        {
            var armed = draft.ArmedSource;
            return Unmoved(view, draft).FirstOrDefault(t => t.Id == armed)?.Origin ?? (IsSource(view, draft, armed) ? armed : "");
        }

        public static bool IsSource(PlayerView view, TurnDraft draft, string region) =>
            region.Length > 0 && Unmoved(view, draft).Any(t => t.Origin == region);

        // Arms a region or a figure; arming what is armed releases it.
        public static bool Arm(PlayerView view, TurnDraft draft, string source)
        {
            var unmoved = Unmoved(view, draft).ToList();
            if (!unmoved.Any(t => t.Origin == source || t.Id == source))
                return false;
            var armed = draft.ArmedSource == source ? "" : ArmedFor(view, unmoved, source);
            draft.Gesture(() => draft.ArmedSource = armed);
            return true;
        }

        // A region where the Witch-king is the only unmoved Nazgûl arms him.
        private static string ArmedFor(PlayerView view, List<TravelOption> unmoved, string source)
        {
            if (unmoved.Any(t => t.Id == source) || unmoved.Any(t => t.Origin == source && !Character(view, t.Id)))
                return source;
            return unmoved.First(t => t.Origin == source).Id;
        }

        public static bool Release(TurnDraft draft)
        {
            if (draft.ArmedSource.Length == 0)
                return false;
            draft.Gesture(() => draft.ArmedSource = "");
            return true;
        }

        // The Nazgûl the next destination click flies.
        public static TravelOption Next(PlayerView view, TurnDraft draft)
        {
            var armed = draft.ArmedSource;
            if (armed.Length == 0)
                return null;
            var unmoved = Unmoved(view, draft).ToList();
            return unmoved.FirstOrDefault(t => t.Id == armed) ?? unmoved
                .Where(t => t.Origin == armed && !Character(view, t.Id))
                .OrderBy(t => t.Id, StringComparer.Ordinal)
                .FirstOrDefault();
        }

        public static IEnumerable<string> Targets(PlayerView view, TurnDraft draft) =>
            Next(view, draft) is { } next ? next.Targets.Where(t => t != next.Origin) : Enumerable.Empty<string>();

        public static bool Fly(PlayerView view, TurnDraft draft, string destination, BoardPresentation data = null)
        {
            var next = Next(view, draft);
            if (next == null || destination == next.Origin || !next.Targets.Contains(destination))
                return false;
            var route = TravelDraft.Path(next, data, destination);
            if (route == null)
                return false;
            draft.Gesture(() =>
            {
                var flying = DraftOptions.For(view, draft).Travel.Where(t => t.Flight).Select(t => t.Id).ToHashSet();
                draft.Change(payload => Add(payload, view, next, route, flying));
                if (draft.ArmedSource == next.Id || Next(view, draft) == null)
                    draft.ArmedSource = "";
            });
            return true;
        }

        public static int Moved(TurnDraft draft) => draft.Payload.CompanionMoves.Sum(g => g.Ids.Count);

        public static string Hint(PlayerView view, TurnDraft draft)
        {
            if (Next(view, draft) is { } next)
                return next.Flight ? "Click a lit region to fly the armed " + Name(view, next.Id) + " there · ✓ confirms."
                    : "Click a lit region to move the armed " + Name(view, next.Id) + " there · ✓ confirms.";
            var unmoved = Unmoved(view, draft).ToList();
            var nazgul = OnlyNazgul(view, draft);
            if (unmoved.Count > 0)
                return nazgul ? "Click a lit Nazgûl, then where it flies · ✓ confirms; moving none is allowed."
                    : "Click a lit figure, then where it moves · ✓ confirms; moving none is allowed.";
            return nazgul ? "Every Nazgûl has flown · ✓ confirms." : "Every figure has moved · ✓ confirms.";
        }

        private static string Name(PlayerView view, string id) => Character(view, id) ? CharacterLibrary.ShortName(view, id) : "Nazgûl";

        // A card that moves only Nazgûl (the Witch-king among them) speaks of Nazgûl; one that moves other Minions, of figures.
        public static bool OnlyNazgul(PlayerView view, TurnDraft draft) =>
            (DraftOptions.For(view, draft)?.Travel ?? new List<TravelOption>()).All(t => Nazgul(view, t.Id));

        private static bool Nazgul(PlayerView view, string id) =>
            view.Characters.FirstOrDefault(c => c.Id == id) is not { } character || CharacterLibrary.Identity(character.DefinitionId) == "witch-king";

        // The marker of the Nazgûl the next destination click flies, for the map's armed highlight.
        public static ArmyOption Armed(PlayerView view, TurnDraft draft) =>
            Next(view, draft) is { } next ? new ArmyOption { RegionId = next.Origin, Garrison = Garrison(view, next.Id) } : null;

        // Every moved figure at its destination, for the map's ghosts; a character keeps its own figure (UI-271).
        public static List<PlannedReinforcement> Planned(PlayerView view, TurnDraft draft) => draft.Payload.CompanionMoves
            .SelectMany(group => group.Ids.Select(id => new PlannedReinforcement
            {
                UnitId = id,
                Nation = view.Units.FirstOrDefault(u => u.Id == id)?.Nation ?? "sauron",
                Kind = UnitKind.Nazgul,
                CharacterId = view.Characters.FirstOrDefault(c => c.Id == id)?.DefinitionId ?? "",
                RegionId = group.Route.LastOrDefault() ?? ""
            }))
            .ToList();

        // One group per origin, garrison and destination, as the server groups a flight; a walker moves alone.
        internal static void Add(CommandPayload payload, PlayerView view, TravelOption travel, List<string> route, ISet<string> flying)
        {
            var garrison = Garrison(view, travel.Id);
            var group = travel.Flight ? payload.CompanionMoves.FirstOrDefault(g => g.Route.SequenceEqual(route) &&
                g.Ids.All(id => Garrison(view, id) == garrison && flying.Contains(id))) : null;
            if (group == null)
            {
                group = new CompanionMoveChoice();
                group.Route.AddRange(route);
                payload.CompanionMoves.Add(group);
            }
            group.Ids.Add(travel.Id);
        }

        private static bool Flown(TurnDraft draft, string id) => draft.Payload.CompanionMoves.Any(g => g.Ids.Contains(id));

        private static bool Character(PlayerView view, string id) => view.Characters.Any(c => c.Id == id);

        private static bool Garrison(PlayerView view, string id) =>
            view.Characters.FirstOrDefault(c => c.Id == id)?.Garrison ?? view.Units.FirstOrDefault(u => u.Id == id)?.Garrison ?? false;
    }
}
