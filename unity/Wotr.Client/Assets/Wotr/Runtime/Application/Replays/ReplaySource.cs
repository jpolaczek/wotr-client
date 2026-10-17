using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wotr.Contracts;

namespace Wotr.Client
{
    // REPLAY-05: the frames of a replay, read in chunks of 25 off the main thread with at most three chunks kept, so a
    // long game never sits in memory whole. It also knows where every turn starts.
    public sealed class ReplaySource
    {
        public const int ChunksKept = 3;
        private readonly ReplayArchive archive;
        private readonly LinkedList<KeyValuePair<int, List<ReplayFrame>>> chunks = new LinkedList<KeyValuePair<int, List<ReplayFrame>>>();
        public IReadOnlyList<ReplayStep> Steps { get; }
        public IReadOnlyList<ReplayTurn> Turns { get; }
        public int Count => Steps.Count;
        public int Kept => chunks.Count;

        public ReplaySource(ReplayArchive archive)
        {
            this.archive = archive;
            Steps = archive.Steps();
            Turns = archive.Turns().OrderBy(t => t.Step).ToList();
        }

        public async Task<ReplayFrame> Frame(int step)
        {
            var chunk = step / ReplayFormat.FramesPerChunk;
            var frames = Cached(chunk) ?? Remember(chunk, await Task.Run(() => archive.Frames(chunk)));
            return frames[step % ReplayFormat.FramesPerChunk];
        }

        // Newest first; the oldest chunk goes when a fourth is read.
        private List<ReplayFrame> Cached(int chunk)
        {
            for (var node = chunks.First; node != null; node = node.Next)
            {
                if (node.Value.Key != chunk)
                    continue;
                chunks.Remove(node);
                chunks.AddFirst(node);
                return node.Value.Value;
            }
            return null;
        }

        private List<ReplayFrame> Remember(int chunk, List<ReplayFrame> frames)
        {
            var known = Cached(chunk);
            if (known != null)
                return known;
            chunks.AddFirst(new KeyValuePair<int, List<ReplayFrame>>(chunk, frames));
            while (chunks.Count > ChunksKept)
                chunks.RemoveLast();
            return frames;
        }

        public int Turn(int step) => Steps[step].Turn;

        // The first step of the turn right after (or right before) the turn of `step`; none at the ends.
        public int? NextTurn(int step)
        {
            var turn = Turn(step);
            var next = Turns.FirstOrDefault(t => t.Turn > turn);
            return next == null ? (int?)null : next.Step;
        }

        public int? PreviousTurn(int step)
        {
            var turn = Turn(step);
            var previous = Turns.LastOrDefault(t => t.Turn < turn);
            return previous == null ? (int?)null : previous.Step;
        }
    }
}
