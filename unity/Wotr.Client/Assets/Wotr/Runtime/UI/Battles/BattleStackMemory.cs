using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-322: the figure stacks the live battle window showed, for each side of its battle, so a stack whose count
    // changed pulses and a stack that left fades out where it stood instead of vanishing (BattleContext).
    public sealed class BattleStackMemory
    {
        private readonly ShownBefore<Side, IReadOnlyList<BattleStack>> shown =
            new ShownBefore<Side, IReadOnlyList<BattleStack>>(new BattleStack[0]);
        private string battle = "";

        // The stacks shown before this frame for this side of this battle (none for another battle).
        public IReadOnlyList<BattleStack> Exchange(string battleId, Side side, IReadOnlyList<BattleStack> now)
        {
            if (battleId != battle)
            {
                shown.Clear();
                battle = battleId;
            }
            return shown.Exchange(side, now);
        }
    }

    // One figure per nation and kind, where it stood: its nation's group in the row and its place in that group.
    public sealed class BattleStack
    {
        public UnitView Unit { get; }
        public int Count { get; }
        public int Group { get; }
        public int Place { get; }

        public BattleStack(UnitView unit, int count, int group, int place)
        {
            Unit = unit;
            Count = count;
            Group = group;
            Place = place;
        }

        public bool Same(BattleStack other) => Unit.Nation == other.Unit.Nation && Unit.Kind == other.Unit.Kind;
    }
}
