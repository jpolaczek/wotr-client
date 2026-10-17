using UnityEngine;

namespace Wotr.Client
{
    internal enum DraftClickKind { Single, Suppressed }

    // UI-142: the game has no double-click; every click is a click. A press that continues the device's click series
    // after the decision changed (a new revision, decision or connection) is suppressed, so a fast second click never
    // repeats a step on the replacement control. Pointer clickCount belongs to the device, not the object.
    internal sealed class DraftClickSequence
    {
        internal const float MovementTolerance = 6;
        private const long FallbackIntervalMilliseconds = 500;
        private Click previous;
        private Click pending;
        private DraftClickKind kind;

        public DraftClickKind Begin(DraftToken token, string scope, int pointer, Vector2 position,
            long timestamp, int nativeCount)
        {
            pending = new Click(token, scope, pointer, position, timestamp, nativeCount);
            kind = Continues(previous, pending) && previous.Scope != scope ? DraftClickKind.Suppressed : DraftClickKind.Single;
            return kind;
        }

        public void Complete()
        {
            if (pending == null)
                return;
            if (kind == DraftClickKind.Suppressed)
            {
                previous.Timestamp = pending.Timestamp;
                previous.NativeCount = pending.NativeCount;
            }
            else
                previous = pending;
            pending = null;
        }

        public void Cancel()
        {
            // A canceled press must recover with a fresh click immediately. When a completed action merely removes
            // focus, its scope guard stays, so a further click cannot activate the replacement decision underneath.
            if (pending != null)
                previous = null;
            pending = null;
        }

        public static bool Same(DraftToken first, DraftToken second) => first != null && second != null &&
            first.Object == second.Object && first.Id == second.Id && first.Side == second.Side &&
            first.Garrison == second.Garrison && first.Action == second.Action;

        private static bool Continues(Click first, Click second)
        {
            if (first == null || first.Pointer != second.Pointer ||
                Vector2.Distance(first.Position, second.Position) >= MovementTolerance)
                return false;
            // One explicitly starts a fresh native series. A positive increasing counter preserves the system timeout;
            // only absent counts need our elapsed-time fallback. Neither counter alone identifies the object.
            if (second.NativeCount == 1 || second.NativeCount > 0 && second.NativeCount < first.NativeCount)
                return false;
            return second.NativeCount > 1
                ? second.NativeCount > first.NativeCount
                : second.Timestamp - first.Timestamp <= FallbackIntervalMilliseconds;
        }

        private sealed class Click
        {
            public readonly DraftToken Token;
            public readonly string Scope;
            public readonly int Pointer;
            public readonly Vector2 Position;
            public long Timestamp;
            public int NativeCount;

            public Click(DraftToken token, string scope, int pointer, Vector2 position, long timestamp, int nativeCount)
            {
                Token = token;
                Scope = scope;
                Pointer = pointer;
                Position = position;
                Timestamp = timestamp;
                NativeCount = nativeCount;
            }
        }
    }
}
