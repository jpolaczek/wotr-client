#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Diagnostics
{
    // Recorded HTTP projections, with an explicitly offline presentation-only playback path.
    internal sealed class FigureMotionProfileScene
    {
        public static readonly string[] Kinds = { "motion-army-fp", "motion-army-sh", "motion-character-fp", "motion-character-sh",
            "motion-hidden-fp", "motion-hidden-sh", "motion-separation-fp", "motion-separation-sh" };
        private readonly BoardScreen board;
        private readonly PlayerView before;
        private readonly PlayerView after;
        private readonly Action<PlayerView> apply;
        private readonly Action<VisualElement, KeyCode> key;
        private readonly string region;
        private readonly IReadOnlyList<BoardFigureMove> movements;
        private readonly List<CapturedFrame> captured = new List<CapturedFrame>();
        private bool started;
        private double captureStarted;
        private bool reduced;
        private string movingName;

        public string DiagnosticState => $"recorded {before.Revision}->{after.Revision}, groups={board.Map.Figures.ActiveCount}";
        public object Evidence => new
        {
            Source = "Recorded public HTTP/WebSocket projections; offline rendering only",
            MatchId = before.MatchId,
            BeforeRevision = before.Revision,
            AfterRevision = after.Revision,
            ExpectedFigures = movements.Count,
            ExpectedTranslation = !reduced && movements.Any(move => !move.IsArrival),
            Frames = captured
        };

        public FigureMotionProfileScene(BoardScreen board, Dictionary<string, PlayerView> views, string kind,
            Action<PlayerView> apply, Action<VisualElement, KeyCode> key)
        {
            this.board = board;
            this.apply = apply;
            this.key = key;
            before = views[kind];
            after = views[kind + "-after"];
            if (before.MatchId != after.MatchId || before.You != after.You || after.Revision != before.Revision + 1)
                throw new InvalidOperationException("Motion profiling requires a consecutive pair of recorded projections.");
            movements = BoardFigureChanges.Difference(new BoardFigureSnapshot(before, board.Presentation.Regions.ContainsKey),
                new BoardFigureSnapshot(after, board.Presentation.Regions.ContainsKey));
            region = movements.Count > 0 ? movements[0].To.Region : before.Fellowship.RegionId;
            var hidden = kind.StartsWith("motion-hidden", StringComparison.Ordinal);
            if (hidden && (movements.Count != 0 || after.Fellowship.Progress <= before.Fellowship.Progress) ||
                !hidden && movements.Count == 0)
                throw new InvalidOperationException("The recorded pair does not represent its named movement workload.");
        }

        public bool Tick(int frame)
        {
            if (frame % 15 != 0)
                return false;
            if (frame % 120 == 0 || !started)
                Begin();
            else if (frame % 30 == 15)
            {
                // A right click reads the region and preserves an outstanding strategic choice such as Guide.
                BoardTerrainInput.Inspect(board.Map, region);
            }
            else
                key(board.Root, KeyCode.Escape);
            return true;
        }

        private void Begin()
        {
            started = true;
            board.Hover.Close();
            board.Information.Close();
            board.Map.Figures.Reset();
            apply(before);
            apply(after);
            // Recorded separation may require a new Guide. Keep that decision intact,
            // but close its inspection window before exercising the map underneath.
            board.Fellowship.Close();
            board.Map.Focus(region);
            board.Map.Figures.PlayRecordedTransition(before, after);
        }

        public void BeginCapture()
        {
            captured.Clear();
            Begin();
            captureStarted = Time.realtimeSinceStartupAsDouble;
            movingName = board.Map.Figures.Root.Children().FirstOrDefault()?.name;
            reduced = false;
            for (var element = board.Root; element != null; element = element.parent)
                reduced |= element.ClassListContains("reduced-motion");
        }

        public void Capture(string phase)
        {
            Verify();
            var moving = movingName == null ? null : board.Map.Figures.Root.Q(movingName);
            var expectedActive = movements.Count > 0 && phase != "finished";
            if (expectedActive != (moving != null))
                throw new InvalidOperationException("Incorrect movement lifetime in captured phase: " + phase);
            var position = moving?.transform.position ?? Vector3.zero;
            var frame = new CapturedFrame
            {
                Phase = phase, Elapsed = Time.realtimeSinceStartupAsDouble - captureStarted,
                ActiveGroups = board.Map.Figures.ActiveCount, X = position.x, Y = position.y
            };
            if (phase == "middle" && expectedActive)
            {
                var distance = Vector2.Distance(new Vector2(captured[0].X, captured[0].Y), position);
                var translates = !reduced && movements.Any(move => !move.IsArrival);
                if (translates && distance <= 1 || !translates && distance > .1f)
                    throw new InvalidOperationException("The captured middle frame has incorrect translation.");
            }
            captured.Add(frame);
        }

        public void Verify()
        {
            if (!started)
                return;
            if (!ReferenceEquals(board.View, after) || board.View.Revision != after.Revision || board.View.Phase != after.Phase)
                throw new InvalidOperationException("Motion playback changed its recorded authoritative projection.");
            if (board.Connection.text == "Live")
                throw new InvalidOperationException("An offline movement profile must never claim to be connected.");
            if (movements.Count == 0 && board.Map.Figures.ActiveCount != 0)
                throw new InvalidOperationException("Hidden fellowship progress created a regional animation.");
        }

        private sealed class CapturedFrame
        {
            public string Phase;
            public double Elapsed;
            public int ActiveGroups;
            public float X, Y;
        }
    }
}
#endif
