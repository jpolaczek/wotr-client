using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-303: the figure changes of one revision of an action, as recruitment, upgrades, removals, character entries,
    // transformations and relocations (OPPONENT_ACTIONS_UX §8). Changes a movement or a battle loss already describes are
    // left out, so the primary step's board difference and the producer steps never show the same figure twice.
    internal static class FigureCues
    {
        public const string Fellowship = "fellowship";

        public static IEnumerable<(PresentationCueKind Kind, string Region, List<ActionFigureChange> Changes)> Classify(
            IEnumerable<ActionFigureChange> changes, BoardPresentation data)
        {
            var list = changes.ToList();
            var upgrades = Upgrades(list, data).ToList();
            var paired = upgrades.SelectMany(upgrade => upgrade.Changes).ToHashSet();
            foreach (var upgrade in upgrades)
                yield return upgrade;
            var rest = list.Where(change => !paired.Contains(change));
            foreach (var group in LeavingFirst(rest.GroupBy(change => (Kind: Kind(change, data), Region: Region(change, data)))))
                if (group.Key.Kind.HasValue)
                    yield return (group.Key.Kind.Value, group.Key.Region, group.ToList());
        }

        // UI-486 (owner, 2026-11-04): a downgrade outside a battle reads as the Elite leaving, then its Regular arriving,
        // whichever order the board's difference lists them in. Regions keep their recorded order.
        private static IEnumerable<IGrouping<(PresentationCueKind? Kind, string Region), ActionFigureChange>> LeavingFirst(
            IEnumerable<IGrouping<(PresentationCueKind? Kind, string Region), ActionFigureChange>> groups) => groups
            .GroupBy(group => group.Key.Region)
            .SelectMany(region => region.OrderBy(group => group.Key.Kind == PresentationCueKind.Removal ? 0 : 1));

        // UI-485 (owner, 2026-11-04): one figure's arrival, once. The engine records a recruit twice with different origins
        // (its producer fact from nowhere, the board's difference from `reserve`), and recorded histories keep that shape.
        // A change that lands a figure where its last change already put it says nothing new; the record that knows the
        // origin stands. A figure that really left and came back has another change between the two, so both stay.
        public static List<ActionFigureChange> Once(IEnumerable<ActionFigureChange> changes) =>
            Once(changes.Select(change => (change, true))).Select(record => record.Change).ToList();

        // The same for records that carry where they came from: a kept record keeps the source of the first one.
        public static List<(ActionFigureChange Change, T Source)> Once<T>(IEnumerable<(ActionFigureChange Change, T Source)> records)
        {
            var kept = new List<(ActionFigureChange Change, T Source)>();
            var last = new Dictionary<string, int>();
            foreach (var record in records)
            {
                var id = FigureId(record.Change);
                if (id.Length > 0 && last.TryGetValue(id, out var index) && kept[index].Change.To == record.Change.To)
                {
                    if (kept[index].Change.From.Length == 0)
                        kept[index] = (record.Change, kept[index].Source);
                    continue;
                }
                last[id] = kept.Count;
                kept.Add(record);
            }
            return kept;
        }

        public static string FigureId(ActionFigureChange change) => change.After?.Id ?? change.Before?.Id ?? "";

        // A Regular leaving a region and an Elite of the same nation arriving there from the reserve: one replacement.
        private static IEnumerable<(PresentationCueKind Kind, string Region, List<ActionFigureChange> Changes)> Upgrades(
            List<ActionFigureChange> changes, BoardPresentation data)
        {
            var removed = changes.Where(change => change.Before?.UnitKind == UnitKind.Regular && OnBoard(change.From, data) &&
                !OnBoard(change.To, data)).ToList();
            foreach (var elite in changes.Where(change => change.After?.UnitKind == UnitKind.Elite && Arrives(change, data)))
            {
                var regular = removed.FirstOrDefault(change => change.From == elite.To && change.Before.NationId == elite.After.NationId);
                if (regular == null)
                    continue;
                removed.Remove(regular);
                yield return (PresentationCueKind.Upgrade, elite.To, new List<ActionFigureChange> { regular, elite });
            }
        }

        private static PresentationCueKind? Kind(ActionFigureChange change, BoardPresentation data)
        {
            var character = change.After?.UnitKind == null && (change.After?.DefinitionId.Length ?? 0) > 0 ||
                change.Before?.UnitKind == null && (change.Before?.DefinitionId.Length ?? 0) > 0;
            if (Changed(change))
                return PresentationCueKind.Transformation;
            if (Arrives(change, data))
                return character ? PresentationCueKind.Entry : PresentationCueKind.Recruit;
            if (Leaves(change, data))
                return PresentationCueKind.Removal;
            // UI-344: a Companion leaving the Fellowship out of play (Sméagol) leaves its roster.
            if (FromFellowship(change) && change.To != Fellowship && !OnBoard(change.To, data))
                return PresentationCueKind.Removal;
            // A separated Companion steps out of the Fellowship onto its region.
            if ((OnBoard(change.From, data) || FromFellowship(change)) && OnBoard(change.To, data) && change.From != change.To)
                return PresentationCueKind.Relocation;
            return null;
        }

        private static string Region(ActionFigureChange change, BoardPresentation data) =>
            OnBoard(change.To, data) ? change.To : change.From;

        // A new version of a character already in play (Gandalf the White): it replaces the figure, never adds one. UI-344:
        // so does an awakening sovereign — his Leader becomes his character, with the same id — and the Balrog's activation,
        // which changes his level in place.
        private static bool Changed(ActionFigureChange change) =>
            (change.Before?.DefinitionId.Length ?? 0) > 0 && (change.After?.DefinitionId.Length ?? 0) > 0 &&
            change.Before.DefinitionId != change.After.DefinitionId || Awakened(change) || Leveled(change);

        public static bool Awakened(ActionFigureChange change) => change.Before?.UnitKind != null && change.After != null &&
            change.After.UnitKind == null && change.After.DefinitionId.Length > 0 && change.Before.Id == change.After.Id;

        private static bool Leveled(ActionFigureChange change) => change.Before?.UnitKind == null && change.After?.UnitKind == null &&
            change.Before?.Level.HasValue == true && change.After?.Level.HasValue == true && change.Before.Level != change.After.Level &&
            change.From == change.To;

        // Onto the board from the reserve or off-board; a Companion leaving the Fellowship is a relocation, not an entry.
        private static bool Arrives(ActionFigureChange change, BoardPresentation data) =>
            change.After != null && OnBoard(change.To, data) && !OnBoard(change.From, data) && !FromFellowship(change);

        // UI-309: the engine records a character new to the game (a Minion entering) from "fellowship", its default origin;
        // the Shadow has no Fellowship, so only a Free Peoples character recorded from there leaves it.
        private static bool FromFellowship(ActionFigureChange change) =>
            change.From == Fellowship && (change.After ?? change.Before)?.Side != Side.Shadow;

        private static bool Leaves(ActionFigureChange change, BoardPresentation data) =>
            change.Before != null && OnBoard(change.From, data) && !OnBoard(change.To, data) && change.To != Fellowship;

        public static bool OnBoard(string location, BoardPresentation data) => data.Regions.ContainsKey(location);

        public static string Key(ActionFigureChange change) =>
            (change.After?.Id ?? change.Before?.Id ?? "") + ":" + change.From + ">" + change.To;
    }
}
