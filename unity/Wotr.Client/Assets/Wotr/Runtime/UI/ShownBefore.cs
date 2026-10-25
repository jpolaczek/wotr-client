using System.Collections.Generic;
using UnityEngine;

namespace Wotr.Client
{
    // UI-322: what a view showed in the frames before this one, by key, so a change since then can move smoothly. A view
    // may be drawn more than once in a frame; every drawing of that frame compares with what was on screen before it.
    public sealed class ShownBefore<TKey, TValue>
    {
        private readonly Dictionary<TKey, Entry> entries = new Dictionary<TKey, Entry>();
        private readonly TValue none;

        public ShownBefore(TValue none)
        {
            this.none = none;
        }

        // What `key` showed before this frame (`none` the first time); `now` is what it shows from this frame.
        public TValue Exchange(TKey key, TValue now)
        {
            var frame = Time.frameCount;
            if (!entries.TryGetValue(key, out var entry))
                entries[key] = entry = new Entry { Before = none, Frame = frame };
            else if (entry.Frame != frame)
            {
                entry.Before = entry.Latest;
                entry.Frame = frame;
            }
            entry.Latest = now;
            return entry.Before;
        }

        public void Clear() => entries.Clear();

        private sealed class Entry
        {
            public TValue Before;
            public TValue Latest;
            public int Frame;
        }
    }
}
