using System;

namespace Wotr.Client
{
    // UI-424: an Effects tile's title fits inside its tile. Its words are measured once, at the size the title had then, as
    // the width of every run of consecutive words (one measurement per run, so no rounding of word and space widths adds
    // up); widths scale linearly with the type size. The title keeps its arrangement's size when its greedy wrap at the
    // tile's text width needs no more height than the tile has, and otherwise takes the largest smaller size that does —
    // two lines inside the parchment for the longest names, three lines of the smallest type only for a name no two lines
    // hold. The result depends on the text, the tile's fixed width and height and the type size only, never on layout.
    public sealed class EffectTitleFit
    {
        public const float MinimumSize = 8f;
        private const float Step = .25f;
        // The simulated wrap leaves a little room, so the label never wraps a line earlier than the simulation, and the
        // lines leave two points of the height, which rounding to a large screen's pixels can take (two lines of 11.5 points
        // measure 31.6 points at 1366×768 and 32.7 at 1920×1080).
        private const float Margin = .97f;
        private const float HeightAllowance = 2;
        // runs[i, j]: the words i..j joined by single spaces, measured at MeasuredAt.
        private readonly float[,] runs;
        private readonly float firstLine;
        private readonly float nextLine;

        public float MeasuredAt { get; }
        public int Words => runs.GetLength(0);

        // lineHeight: one line's height at measuredAt; twoLines: the height of two lines at measuredAt.
        public EffectTitleFit(float[,] runs, float lineHeight, float twoLines, float measuredAt)
        {
            this.runs = runs;
            firstLine = lineHeight;
            nextLine = Math.Max(0, twoLines - lineHeight);
            MeasuredAt = measuredAt;
        }

        public float Size(float normalSize, float width, float height)
        {
            if (Words == 0 || MeasuredAt <= 0 || width <= 0 || height <= 0)
                return normalSize;
            for (var size = normalSize; size > MinimumSize; size -= Step)
                if (Fits(size, width, height))
                    return size;
            return MinimumSize;
        }

        public bool Fits(float size, float width, float height)
        {
            var scale = size / MeasuredAt;
            var lines = LineCount(scale, width * Margin);
            return lines != int.MaxValue && (firstLine + nextLine * (lines - 1)) * scale <= height - HeightAllowance;
        }

        // Greedy wrapping at spaces, as the label wraps; a word wider than the line cannot fit at this scale.
        public int LineCount(float scale, float available)
        {
            var lines = 0;
            for (var start = 0; start < Words; lines++)
            {
                if (runs[start, start] * scale > available)
                    return int.MaxValue;
                var end = start;
                while (end + 1 < Words && runs[start, end + 1] * scale <= available)
                    end++;
                start = end + 1;
            }
            return Math.Max(1, lines);
        }

        // The words of a title, split at spaces as the label wraps them.
        public static string[] Split(string text) => (text ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

        // Measures every run of consecutive words with the given single-line measurement.
        public static float[,] Measure(string[] words, Func<string, float> width)
        {
            var runs = new float[words.Length, words.Length];
            for (var start = 0; start < words.Length; start++)
                for (var end = start; end < words.Length; end++)
                    runs[start, end] = width(string.Join(" ", words, start, end - start + 1));
            return runs;
        }
    }
}
