using System;
using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-454: what changed among the placed recruits since the last look: the figures placed now (they fly from their
    // Reserve slot) and the ones taken back (their flight ends), each by unit id, in the order given.
    public readonly struct RecruitPlacementChange
    {
        public static readonly RecruitPlacementChange None =
            new RecruitPlacementChange(Array.Empty<string>(), Array.Empty<string>());

        public readonly IReadOnlyList<string> Added;
        public readonly IReadOnlyList<string> Removed;

        public RecruitPlacementChange(IReadOnlyList<string> added, IReadOnlyList<string> removed)
        {
            Added = added;
            Removed = removed;
        }
    }

    // UI-454 (owner, 2026-11-03): which recruits the player has just placed. Each refresh hands over the units the draft's
    // recruitment places (a card's or an ability's plan, or the Muster die's step) and the scope they belong to (the match
    // and the decision); a unit not placed at the last look is new, one no longer placed was taken back. A new scope only
    // sets the baseline, so a draft restored on start, a new decision or a reconnection flies nothing.
    public sealed class RecruitPlacementWatch
    {
        private readonly HashSet<string> placed = new HashSet<string>();
        private string scope;

        public RecruitPlacementChange Observe(string current, IEnumerable<string> units)
        {
            var now = new List<string>();
            var seen = new HashSet<string>();
            foreach (var unit in units)
                if (!string.IsNullOrEmpty(unit) && seen.Add(unit))
                    now.Add(unit);
            if (current != scope)
            {
                scope = current;
                Keep(now);
                return RecruitPlacementChange.None;
            }
            var added = now.FindAll(unit => !placed.Contains(unit));
            var removed = new List<string>();
            foreach (var unit in placed)
                if (!seen.Contains(unit))
                    removed.Add(unit);
            removed.Sort(string.CompareOrdinal);
            Keep(now);
            return added.Count == 0 && removed.Count == 0 ? RecruitPlacementChange.None : new RecruitPlacementChange(added, removed);
        }

        private void Keep(IEnumerable<string> units)
        {
            placed.Clear();
            placed.UnionWith(units);
        }
    }
}
