using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-310 (OPPONENT_ACTIONS_UX §8 Fellowship/Hunt; storyboard D6–D7): a recorded change of the Fellowship on its
    // existing objects, after its revision's Hunt has shown its result (PresentationCueExtraction orders it so). The panel
    // opens read-only for the beat (FellowshipReading).
    // - A new public position flies the Fellowship's figure between the two regions (a declaration, a reveal, a revealed
    //   Fellowship's move; the camera framed it); entering Mordor puts it on the Mordor track; hiding or revealing in
    //   place marks the plaque's Ring as the symbol changes. A hidden move never relocates anything on the map.
    // - The tracks move as separate stages in TrackSequence's order — on the journey progress then corruption, in Mordor
    //   corruption then the step; healing is corruption going down. Each stage traces its track and sounds its cue once.
    // - A new Guide is marked on its portrait.
    // What the beat showed is the release's point of comparison (BoardScreen.StageFellowship), so nothing moves twice.
    public sealed class FellowshipPresenter : ICuePresenter
    {
        public const double SettleSeconds = .25;
        public const double StageSeconds = .6;
        public const double FlightSeconds = 1.1;

        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly FellowshipReading reading;
        private readonly PresentationSoundSink sounds;
        private readonly PresentationMark mark;

        public FellowshipPresenter(BoardScreen board, StagedBoard staged, PresentationLayer layer, FellowshipReading reading,
            PresentationSoundSink sounds)
        {
            this.board = board;
            this.staged = staged;
            this.reading = reading;
            this.sounds = sounds;
            mark = new PresentationMark(layer);
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            if (cue.Kind != PresentationCueKind.Fellowship || cue.FellowshipAfter == null)
                return null;
            var before = Copy(staged.View.Fellowship);
            staged.Fellowship(cue.FellowshipAfter);
            var after = Copy(staged.View.Fellowship);
            var steps = Steps(cue, before, after);
            if (steps.Count == 0)
            {
                board.StageFellowship(after);
                return null;
            }
            steps.Insert(0, (() => reading.Open(), SettleSeconds));
            return new StepsBeat(now, End, steps);
        }

        public void Released() => EndRing();

        private List<(Action Start, double Seconds)> Steps(PresentationCue cue, FellowshipView before, FellowshipView after)
        {
            var steps = new List<(Action Start, double Seconds)>();
            var current = before;
            if (Relocated(before, after))
            {
                var from = current;
                var placed = Copy(after);
                placed.Corruption = before.Corruption;
                steps.Add((() => Place(from, placed), Flight(from, placed) != null ? FlightSeconds : StageSeconds));
                current = placed;
            }
            else if (TrackSequence.Both(before, after))
            {
                var first = TrackSequence.FirstStage(before, after);
                steps.Add((() => Track(cue, before, first, 1), StageSeconds));
                current = first;
            }
            if (Moved(current, after))
            {
                var from = current;
                steps.Add((() => Track(cue, from, after, 2), StageSeconds));
            }
            if (before.Guide != after.Guide)
                steps.Add((() => Guide(after.Guide), StageSeconds));
            return steps;
        }

        // A new public position or condition: a declaration, a reveal, a revealed move, hiding, entering Mordor.
        private static bool Relocated(FellowshipView a, FellowshipView b) =>
            a.RegionId != b.RegionId || a.Hidden != b.Hidden || a.MordorStep.HasValue != b.MordorStep.HasValue;

        private static bool Moved(FellowshipView a, FellowshipView b) =>
            a.Progress != b.Progress || a.MordorStep != b.MordorStep || a.Corruption != b.Corruption;

        private void Place(FellowshipView from, FellowshipView to)
        {
            var flight = Flight(from, to);
            board.StageFellowship(to);
            if (flight != null)
                board.Map.Figures.Fly(new[] { flight });
            mark.Show(() => from.Hidden != to.Hidden ? board.Fellowship.Status.Q("fellowship-ring") : Row(from, to));
        }

        // The figure's own relocation between two public map regions; the Mordor track places it without a flight.
        private BoardFigureMove Flight(FellowshipView from, FellowshipView to)
        {
            var origin = BoardFigureSnapshot.Region(from, board.Map.Data.Regions.ContainsKey);
            var destination = BoardFigureSnapshot.Region(to, board.Map.Data.Regions.ContainsKey);
            if (origin.Length == 0 || destination.Length == 0 || origin == destination)
                return null;
            return new BoardFigureMove(Position(origin), Position(destination));
        }

        private static BoardFigurePosition Position(string region) =>
            new BoardFigurePosition("fellowship", region, Side.FreePeoples, false, FigureAppearances.Fellowship);

        private void Track(PresentationCue cue, FellowshipView from, FellowshipView to, int stage)
        {
            board.StageFellowship(to);
            mark.Show(() => Row(from, to));
            if (TrackSequence.Marched(from, to))
                sounds.Play(SoundCue.FellowshipAdvanced, "presented/" + cue.Id + "/" + stage + "/march");
            if (TrackSequence.Corrupted(from, to))
                sounds.Play(SoundCue.CorruptionChanged, "presented/" + cue.Id + "/" + stage + "/corruption");
        }

        // The row that moved — corruption alone, else the journey or the Mordor track — or its value on the plaque while
        // the panel is closed.
        private VisualElement Row(FellowshipView from, FellowshipView to)
        {
            var corruption = from.Corruption != to.Corruption && from.Progress == to.Progress && from.MordorStep == to.MordorStep;
            var row = board.Fellowship.Root.Q(corruption ? "corruption-track" : to.MordorStep.HasValue ? "mordor-track" : "journey-track");
            var value = board.Fellowship.Status.Q(corruption ? "fellowship-status-corruption" : "fellowship-status-progress");
            return PresentationTargets.First(row, value, board.Fellowship.Status);
        }

        private void Guide(string id)
        {
            board.StageRoster(staged.View);
            mark.Show(() => reading.Portrait(id));
        }

        private void End()
        {
            EndRing();
            staged.Settle();
        }

        private void EndRing() => mark.End();

        private static FellowshipView Copy(FellowshipView fellowship) =>
            JsonConvert.DeserializeObject<FellowshipView>(JsonConvert.SerializeObject(fellowship));
    }
}
