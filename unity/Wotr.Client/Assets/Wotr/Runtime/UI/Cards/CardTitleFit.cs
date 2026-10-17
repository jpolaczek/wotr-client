using System.Collections.Generic;

namespace Wotr.Client
{
    // UI-217: a collapsed hand tile holds two lines of its card's name. A name that needs more gets just enough smaller
    // type for two lines, never below MinimumSize. Word widths measured at one size scale linearly to any other, so the
    // result does not depend on the size the title currently has.
    public static class CardTitleFit
    {
        public const int Lines = 2;
        public const float MinimumSize = 8f;
        private const float Step = .25f;
        // The simulated wrap leaves a little room, so the label never wraps a line earlier than the simulation.
        private const float Margin = .97f;

        public static float Size(IReadOnlyList<float> wordWidths, float spaceWidth, float measuredAt, float normalSize, float available)
        {
            if (wordWidths.Count == 0 || measuredAt <= 0 || normalSize <= MinimumSize || available <= 0)
                return normalSize;
            for (var size = normalSize; size > MinimumSize; size -= Step)
                if (LineCount(wordWidths, spaceWidth, size / measuredAt, available * Margin) <= Lines)
                    return size;
            return MinimumSize;
        }

        // Greedy wrapping at spaces, as the label wraps; a word wider than the line cannot fit at this size.
        public static int LineCount(IReadOnlyList<float> wordWidths, float spaceWidth, float scale, float available)
        {
            var lines = 1;
            var used = 0f;
            foreach (var word in wordWidths)
            {
                var width = word * scale;
                if (width > available)
                    return int.MaxValue;
                if (used > 0 && used + spaceWidth * scale + width > available)
                {
                    lines++;
                    used = width;
                }
                else
                    used += (used > 0 ? spaceWidth * scale : 0) + width;
            }
            return lines;
        }
    }
}
