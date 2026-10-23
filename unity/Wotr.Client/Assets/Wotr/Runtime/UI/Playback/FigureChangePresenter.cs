using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-307 (OPPONENT_ACTIONS_UX §8 recruitment/upgrade and characters; storyboard B4–B5): the recorded figure changes of
    // one region — recruits, an upgrade, a removal, a character entering, transforming or leaving the Fellowship. The
    // region's force is traced; the staged board takes the recorded changes, so the stack settles to its true count and an
    // upgrade replaces its Regular by the Elite (never an extra figure); new figures land with the board's arrival accent;
    // a delta badge beside the stack shows unit art, the nation's crest and a signed count. Several regions of one source
    // are successive beats (the camera visits each), and a battle's own figure changes stay in its window.
    public sealed class FigureChangePresenter : ICuePresenter
    {
        public const double Seconds = 1;
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly MapObservedTrace trace;

        public FigureChangePresenter(BoardScreen board, StagedBoard staged, MapObservedTrace trace)
        {
            this.board = board;
            this.staged = staged;
            this.trace = trace;
        }

        public static bool Shows(PresentationCue cue) => cue.BattleId.Length == 0 && cue.Figures.Count > 0 &&
            cue.Kind is PresentationCueKind.Recruit or PresentationCueKind.Upgrade or PresentationCueKind.Removal or
                PresentationCueKind.Entry or PresentationCueKind.Transformation or PresentationCueKind.Relocation;

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            var region = cue.Regions.FirstOrDefault();
            if (!Shows(cue) || region == null || !board.Map.Data.Shapes.ContainsKey(region))
                return null;
            var figure = cue.Figures[0].After ?? cue.Figures[0].Before;
            var garrison = cue.Figures.Select(change => change.After ?? change.Before).Any(f => f?.Garrison == true);
            var before = new BoardFigureSnapshot(staged.View, board.Map.Data.Regions.ContainsKey);
            foreach (var change in cue.Figures)
                staged.Apply(change);
            staged.Show();
            Land(before, cue.Figures);
            var set = trace.Begin().Force(region, figure.Side, garrison).Delta(region, figure.Side, garrison, Deltas(cue));
            return new SettledBeat(set, staged, now, Seconds);
        }

        // New figures on the board land in place with the arrival accent; removed ones simply leave the stack.
        private void Land(BoardFigureSnapshot before, IEnumerable<ActionFigureChange> changes)
        {
            var after = new BoardFigureSnapshot(staged.View, board.Map.Data.Regions.ContainsKey);
            var arrivals = changes.Select(change => change.After ?? change.Before).Where(f => f != null).Select(Key)
                .Where(key => after.Figures.ContainsKey(key) && !before.Figures.ContainsKey(key))
                .Select(key => new BoardFigureMove(after.Figures[key], after.Figures[key])).ToList();
            if (arrivals.Count > 0)
                board.Map.Figures.Fly(arrivals);
        }

        public static IEnumerable<FigureDelta> Deltas(PresentationCue cue)
        {
            if (cue.Kind is PresentationCueKind.Upgrade or PresentationCueKind.Transformation)
                return Replacements(cue);
            var sign = cue.Kind == PresentationCueKind.Removal ? -1 : 1;
            return cue.Figures.Select(change => change.After ?? change.Before).Where(f => f != null)
                .GroupBy(f => (Art: Art(f), Nation: f.NationId))
                .Select(group => new FigureDelta { Art = group.Key.Art, Nation = group.Key.Nation, Count = sign * group.Count() });
        }

        // An upgrade pairs its Regular and Elite halves; a transformation its old and new versions.
        private static IEnumerable<FigureDelta> Replacements(PresentationCue cue)
        {
            if (cue.Kind == PresentationCueKind.Transformation)
                return cue.Figures.Where(change => change.Before != null && change.After != null)
                    .Select(change => new FigureDelta { From = Art(change.Before), Art = Art(change.After), Count = 1 });
            var regulars = cue.Figures.Where(change => change.Before?.UnitKind == UnitKind.Regular && change.To != cue.Regions[0]).ToList();
            var elites = cue.Figures.Where(change => change.After?.UnitKind == UnitKind.Elite && change.To == cue.Regions[0]).ToList();
            var nation = elites.FirstOrDefault()?.After.NationId ?? "";
            if (regulars.Count == 0 || elites.Count == 0)
                return Enumerable.Empty<FigureDelta>();
            return new[]
            {
                new FigureDelta { From = Art(regulars[0].Before), Art = Art(elites[0].After), Nation = nation, Count = elites.Count }
            };
        }

        private static FigureArt Art(ActionParticipant figure) => figure.UnitKind.HasValue
            ? FigureAppearances.Unit(figure.NationId, figure.UnitKind.Value, figure.Side)
            : FigureAppearances.Character(figure.DefinitionId, figure.Side);

        private static string Key(ActionParticipant figure) => (figure.UnitKind.HasValue ? "unit:" : "character:") + figure.Id;
    }
}
