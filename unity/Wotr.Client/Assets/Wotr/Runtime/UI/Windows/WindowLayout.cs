using System;
using System.Collections.Generic;
using UnityEngine;

namespace Wotr.Client
{
    // Owned by the client session. Never written to PlayerPrefs or match recovery.
    public sealed class WindowLayout
    {
        private readonly Dictionary<string, Vector2> positions = new Dictionary<string, Vector2>();
        public event Action Reset;
        // UI-324 (owner 2026-10-25: "every window should open in the middle"): the middle of the board, in world points,
        // where a window the player has not moved opens.
        public Func<Rect?> Middle { get; set; }
        // The middle changed without the board's own size changing (a left panel opened or closed).
        public event Action MiddleMoved;
        public void MoveMiddle() => MiddleMoved?.Invoke();
        public bool TryGet(string id, out Vector2 position) => positions.TryGetValue(id, out position);
        public void Remember(string id, Vector2 position) => positions[id] = position;
        public void Forget(string id) => positions.Remove(id);
        public void Clear()
        {
            positions.Clear();
            Reset?.Invoke();
        }

        public static Vector2 Clamp(Vector2 position, Vector2 size, Rect viewport)
        {
            return new Vector2(Mathf.Clamp(position.x, viewport.xMin, Mathf.Max(viewport.xMin, viewport.xMax - size.x)),
                Mathf.Clamp(position.y, viewport.yMin, Mathf.Max(viewport.yMin, viewport.yMax - size.y)));
        }
    }
}
