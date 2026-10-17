using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Public counters of one side, read from the projection for the top bar.
    public readonly struct SideResourceCounts
    {
        public int Cards { get; }
        public int Rings { get; }
        public int Spent { get; }
        public int Hunt { get; }
        public int Out { get; }

        private SideResourceCounts(int cards, int rings, int spent, int hunt, int removed)
        {
            Cards = cards;
            Rings = rings;
            Spent = spent;
            Hunt = hunt;
            Out = removed;
        }

        public static SideResourceCounts From(PlayerView view, Side side) => new SideResourceCounts(
            view.Players.FirstOrDefault(p => p.Side == side)?.HandCount ?? 0,
            view.Rings.Count(r => r.Owner == side),
            // A die spent through another side's effect counts for that side, as in the own pool.
            view.Dice.Count(d => d.Location == DieLocation.Spent && (d.SpentWith ?? d.Owner) == side),
            view.Dice.Count(d => d.Location == DieLocation.Hunt && d.Owner == side),
            view.Dice.Count(d => d.Location == DieLocation.Discarded && d.Owner == side));
    }
}
