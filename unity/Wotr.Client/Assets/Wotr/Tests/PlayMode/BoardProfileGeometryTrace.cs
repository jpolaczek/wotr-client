using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // Observe only; a recursive layout error still fails the real driver normally.
    internal sealed class BoardProfileGeometryTrace : IDisposable
    {
        public int Step { get; set; } = -1;
        private readonly BoardScreen board;
        private readonly string context;
        private readonly Queue<string> changes = new Queue<string>();
        private readonly VisualElement[] observed;

        public BoardProfileGeometryTrace(BoardScreen board, string kind, int width, float scale)
        {
            this.board = board;
            context = $"{kind}-{width}-{scale:0.00}";
            observed = new[] { board.Root, board.Map.parent, board.Characters.Root,
                board.Characters.Inspector.Root, board.Victory.Tab };
            foreach (var element in observed)
                element.RegisterCallback<GeometryChangedEvent>(Changed);
            Application.logMessageReceived += Logged;
        }

        private void Changed(GeometryChangedEvent value)
        {
            var element = (VisualElement)value.target;
            var remembered = board.Windows.TryGet(board.Characters.Inspector.Root.name, out var position)
                ? position.ToString() : "automatic";
            changes.Enqueue($"frame={Time.frameCount}, step={Step}, figure={board.Characters.Inspector.FigureId}, " +
                $"element={element.name}, old={value.oldRect}, new={value.newRect}, left={element.style.left}, " +
                $"top={element.style.top}, remembered={remembered}, world={element.worldBound}");
            while (changes.Count > 96)
                changes.Dequeue();
        }

        private void Logged(string message, string stack, LogType type)
        {
            if (!message.Contains("Layout update failed to stabilize"))
                return;
            var folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../../.artifacts/screenshots"));
            Directory.CreateDirectory(folder);
            var path = Path.Combine(folder, "ui72-layout-trace-" + context + ".txt");
            File.WriteAllText(path, $"{context}, step={Step}, figure={board.Characters.Inspector.FigureId}\n" +
                message + "\n" + string.Join("\n", changes));
            Debug.Log("WOTR_BOARD_PROFILE_LAYOUT_ERROR " + context + $" step={Step}; trace={path}");
        }

        public void Dispose()
        {
            Application.logMessageReceived -= Logged;
            foreach (var element in observed)
                element.UnregisterCallback<GeometryChangedEvent>(Changed);
        }
    }
}
