using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // A selected historical chain is independent of the current Fellowship and Hunt report.
    public sealed class HuntArchivePresentation
    {
        public string ChainId { get; private set; }
        public string SourceId { get; private set; }
        public HuntView Roll { get; private set; }
        public HuntView Tile { get; private set; }
        public HuntOutcomeView Outcome { get; private set; }
        public bool Completed { get; private set; }
        public bool Mordor { get; private set; }

        public static HuntArchivePresentation For(ActionContextModel context)
        {
            if (context.HuntChainId.Length == 0 || context.Hunt.Count == 0)
                return null;
            var facts = context.Hunt;
            var entries = facts.Where(f => !f.Completed).Select(f => f.Entry).ToList();
            if (entries.Count == 0)
                return null;
            var last = entries.Last();
            var current = entries.Where(e => e.HuntId == last.HuntId).ToArray();
            var hunt = current.LastOrDefault(e => e.Hunt != null)?.Hunt;
            return new HuntArchivePresentation
            {
                ChainId = context.HuntChainId,
                SourceId = current.FirstOrDefault(e => e.Kind == HuntReportKind.Started)?.SourceId ?? hunt?.SourceId ?? "",
                Roll = current.LastOrDefault(e => e.Kind is HuntReportKind.Rolled or HuntReportKind.Rerolled)?.Hunt,
                Tile = hunt?.TileId.Length > 0 ? hunt : null,
                Outcome = last.Outcome,
                Completed = facts.Any(f => f.Completed),
                Mordor = hunt?.Mordor == true || last.Outcome.MordorStep.HasValue
            };
        }

        public string RollText => Mordor ? "Mordor · No Hunt roll" : Roll == null ? "No recorded roll" :
            Roll.Dice.Count == 0 ? "No Hunt dice · No hits" :
            Count(Roll.Dice.Count, "die", "dice") + " · " + Count(Roll.Dice.Count(d => d.Success), "hit", "hits");

        private static string Count(int count, string singular, string plural) =>
            count + " " + (count == 1 ? singular : plural);

        public string Result => (Completed ? "Resolved" : "Recorded progress") + " · Corruption " + Outcome.Corruption + "/12 · " +
            (Outcome.MordorStep.HasValue ? "Mordor " + Outcome.MordorStep + "/5" : "Progress " + Outcome.Progress) +
            " · " + (Outcome.Hidden ? "Hidden" : "Revealed");

        public string TileText(BoardPresentation data)
        {
            if (Tile == null)
                return Completed ? "No tile drawn" : "No tile drawn yet";
            var definition = data.Board.HuntTiles.FirstOrDefault(t => t.Id == Tile.TileId);
            var face = definition == null ? "Hunt tile" : FigureGrid.Name(definition.Value);
            var effect = Tile.SeparatesCompanions ? (Tile.Mordor ? "Remove " : "Separate ") :
                Tile.CorruptionOnly ? "Corruption " : "Damage ";
            return face + " · " + (Tile.DamagePending ? effect + "?" : Tile.Healing > 0 ? "Heal " + Tile.Healing : effect + Tile.Damage) +
                (Tile.Reveal ? " · Reveal" : "") + (Tile.Stop ? " · Stop" : "");
        }
    }
}
