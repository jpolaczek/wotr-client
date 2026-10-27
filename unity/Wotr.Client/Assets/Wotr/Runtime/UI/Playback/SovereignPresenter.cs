using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-344 (owner's decisions EXP-15 D1, D2, D5): the opponent's change of a Kings sovereign plays on the map at his
    // region; the Characters panel never opens by itself, and the panel's portrait follows the staged board.
    // - A hidden tile attached (D2): a tile back travels from the Hunt plaque to his force, where a plate of his meter shows
    //   the new hatched pip, never a value; a tile returned travels back to the plaque.
    // - A corruption (D1, D5): his force glows, then his Leader leaves and the dark marker emerges by his capital; the tiles
    //   the action revealed stand briefly beside it as a plate of their faces.
    // - A removal (D1): the dark marker glows, then fades out.
    // - An awakening is his figure's transformation (FigureChangePresenter, CharacterPresenter); his status follows here
    //   without a beat of its own.
    public sealed class SovereignPresenter : ICuePresenter
    {
        public const string PlateName = "presentation-sovereign-plate";
        public const string TileBackClass = "presentation-tile-back";
        private static readonly Vector2 TileBack = new Vector2(30, 30);
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly MapObservedTrace trace;
        private readonly PresentationMarks marks;
        private MapObservedTrace.TraceSet set;

        public SovereignPresenter(BoardScreen board, StagedBoard staged, MapObservedTrace trace, PresentationLayer layer)
        {
            this.board = board;
            this.staged = staged;
            this.trace = trace;
            marks = new PresentationMarks(layer, PlateName);
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            End();
            var after = cue.Sovereign;
            var before = after == null ? null : staged.View.Sovereigns.FirstOrDefault(s => s.Id == after.Id);
            if (cue.Kind != PresentationCueKind.Sovereign || before == null)
                return null;
            var region = before.RegionId;
            if (!board.Map.Data.Shapes.ContainsKey(region) || after.Status == SovereignStatus.Awakened)
                return Quiet(after);
            if (after.Status == SovereignStatus.Corrupted && before.Status != SovereignStatus.Corrupted)
                return Corruption(cue, region, now);
            if (after.Status == SovereignStatus.Removed && before.Status != SovereignStatus.Removed)
                return Removal(after, now);
            if (after.Tiles.Count != before.Tiles.Count)
                return Tiles(cue, before, region, now);
            return Quiet(after);
        }

        public void Released() => End();

        private void End()
        {
            marks.End();
            set?.End();
            set = null;
        }

        // A change with nothing to show on the map: the staged board takes it.
        private IPresentationBeat Quiet(SovereignView after)
        {
            Stage(after);
            return null;
        }

        private void Stage(SovereignView after)
        {
            staged.Sovereign(after);
            staged.Show();
            board.StageSovereigns(staged.View);
        }

        private IPresentationBeat Tiles(PresentationCue cue, SovereignView before, string region, double now)
        {
            var after = cue.Sovereign;
            var attached = after.Tiles.Count > before.Tiles.Count;
            var plaque = board.Hunt.Tab;
            Func<VisualElement> force = () => Force(region) ?? plaque;
            if (board.ReducedMotion())
                return new StepsBeat(now, End, new List<(Action Start, double Seconds)>
                {
                    (() => marks.Place(() => marks.Light(Force(region))), PresentationPace.MarkSeconds),
                    (() => Changed(cue, region), PresentationPace.HoldSeconds)
                });
            var origin = attached ? plaque : Force(region);
            Func<VisualElement> target = attached ? force : () => plaque;
            return new DrawFlight(now, board.Dissolve.Root, target, new[] { origin },
                () => marks.Place(() => marks.Light(attached ? plaque : Force(region))),
                _ => Stage(after),
                () => Meter(cue, region), End, TileBackClass, TileBack);
        }

        private void Changed(PresentationCue cue, string region)
        {
            Stage(cue.Sovereign);
            Meter(cue, region);
        }

        // His meter beside his force: a pip per point of resistance, hatched for each hidden tile; the revealed tiles' faces.
        private void Meter(PresentationCue cue, string region) => marks.Add(() =>
        {
            var anchor = Force(region);
            marks.Light(anchor);
            marks.Plate(anchor, MeterPlate(cue.Sovereign), PlateSide.Under);
            if (cue.Tiles.Count > 0)
                marks.Plate(anchor, Faces(cue.Tiles), PlateSide.Inside);
        });

        private IPresentationBeat Corruption(PresentationCue cue, string region, double now)
        {
            set = trace.Begin().Region(region).Force(region, Side.FreePeoples, false);
            return new StepsBeat(now, Settled, new List<(Action Start, double Seconds)>
            {
                (() => { }, PresentationPace.MarkSeconds),
                (() => Corrupt(cue), PresentationPace.HoldSeconds + (cue.Tiles.Count > 0 ? PresentationPace.MarkSeconds : 0))
            });
        }

        private void Corrupt(PresentationCue cue)
        {
            Stage(cue.Sovereign);
            if (cue.Tiles.Count == 0)
                return;
            marks.Add(() => marks.Plate(board.Map.SovereignMarkers.Find(cue.Sovereign.Id), Faces(cue.Tiles), PlateSide.Under));
        }

        private IPresentationBeat Removal(SovereignView after, double now) => new StepsBeat(now, Settled, new List<(Action Start, double Seconds)>
        {
            (() => marks.Place(() => marks.Light(board.Map.SovereignMarkers.Find(after.Id))), PresentationPace.MarkSeconds),
            (() => { marks.End(); Stage(after); }, PresentationPace.HoldSeconds)
        });

        private void Settled()
        {
            End();
            staged.Settle();
        }

        private VisualElement Force(string region) => board.Map.MarkerArtwork(region, Side.FreePeoples, false);

        // The pips as the Characters panel's meter shows them: filled for a known corruption, hatched for hidden tiles.
        private static VisualElement MeterPlate(SovereignView king)
        {
            var plate = new VisualElement();
            plate.AddToClassList("presentation-sovereign-meter");
            var hidden = king.Corruption.HasValue ? 0 : king.Tiles.Count(t => !t.Value.HasValue && !t.Kings);
            var known = king.Corruption ?? 0;
            for (var index = 0; index < king.Resistance; index++)
            {
                var pip = new VisualElement { pickingMode = PickingMode.Ignore };
                pip.AddToClassList("presentation-sovereign-pip");
                pip.EnableInClassList("filled", index < known);
                pip.EnableInClassList("hidden", !king.Corruption.HasValue && index < hidden);
                plate.Add(pip);
            }
            return plate;
        }

        // The revealed tiles' faces, at most three, as the Hunt pool shows them.
        private VisualElement Faces(IEnumerable<string> tiles)
        {
            var plate = new VisualElement();
            plate.AddToClassList("presentation-revealed-tiles");
            foreach (var id in tiles.Take(HuntTileDrop.MostTiles))
            {
                if (!board.Presentation.HuntTiles.TryGetValue(id, out var tile))
                    continue;
                var face = new HuntTileFace("large");
                face.Apply(new HuntTileFaceView { Pool = tile.Pool, Value = tile.Value, Reveal = tile.Reveal, Stop = tile.Stop });
                plate.Add(face.Root);
            }
            return plate;
        }
    }
}
