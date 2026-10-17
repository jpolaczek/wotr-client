#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    // Offline gestures edit the existing local draft. No connection or command is fabricated.
    internal sealed class BoardProfileScene
    {
        public static readonly string[] Kinds = { "characters-fp", "characters-sh", "setup-lords-fp",
            "setup-council-fp", "setup-fate-fp", "setup-waiting-sh", "homelands-fp", "homelands-sh",
            "menu-battle", "menu-hunt" };
        private readonly BoardScreen board;
        private readonly IBoardProfileWorkload workload;
        private readonly long revision;
        private readonly string decision;
        private readonly GamePhase phase;
        private Vector2 camera;
        private int gestures;

        public string DiagnosticState => $"gestures={gestures}, {workload.DiagnosticState}";

        public BoardProfileScene(BoardScreen board, string kind, Action<Button> click, Action<VisualElement, KeyCode> key, Action<VisualElement> hover)
        {
            this.board = board;
            revision = board.View.Revision;
            decision = board.View.Decision?.Id;
            phase = board.View.Phase;
            workload = kind.StartsWith("characters-", StringComparison.Ordinal)
                ? new CharacterProfileScene(board, click, hover)
                : kind == "homelands-fp" ? new HomelandProfileScene(board, click, key)
                : kind.EndsWith("-sh", StringComparison.Ordinal) ? new WaitingBoardProfileScene(board, click)
                : kind.StartsWith("setup-", StringComparison.Ordinal) ? new CompanionSetupProfileScene(board, click)
                : new MenuProfileScene(board, click, kind == "menu-battle");
        }

        public bool Tick(int frame)
        {
            if (frame % 15 != 0)
                return false;
            camera = board.Map.Camera.Pan;
            workload.Tick();
            if (workload is HomelandProfileScene { NavigationPerformed: true } homeland)
                camera = homeland.ExpectedCamera;
            gestures++;
            return true;
        }

        public void VerifyRendered()
        {
            if (gestures == 0)
                return;
            if (board.View.Revision != revision || board.View.Decision?.Id != decision || board.View.Phase != phase)
                throw new InvalidOperationException("A presentation gesture changed the authoritative decision.");
            if (board.Map.Camera.Pan != camera)
                throw new InvalidOperationException("Board inspection moved the camera without a Locate gesture.");
            workload.Verify();
        }
    }

    internal interface IBoardProfileWorkload
    {
        string DiagnosticState { get; }
        void Tick();
        void Verify();
    }

    internal static class BoardProfileCheck
    {
        public static bool Visible(VisualElement element)
        {
            if (element?.panel == null || element.worldBound.width <= 0 || element.worldBound.height <= 0)
                return false;
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility != Visibility.Visible)
                    return false;
            return true;
        }

        public static void Require(bool condition, string message)
        {
            if (!condition)
                throw new InvalidOperationException(message);
        }

        public static void Selected(Button button, bool expected) => Require(button != null &&
            button.ClassListContains("selected") == expected, "Wrong rendered selection: " + button?.name);
    }
}
#endif
