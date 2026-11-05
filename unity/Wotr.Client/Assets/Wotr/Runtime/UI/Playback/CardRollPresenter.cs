using System;
using System.Linq;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-492 (owner, 2026-11-04; audit gap 8): the dice a card rolls outside a battle (Dreadful Spells, The Ents Awake, Dead
    // Men of Dunharrow, Rangers of the North…) were a line in the log. They stand as a plate of die faces at the region for a
    // moment, with the dice sound, before the losses they cause; the faces that hit wear the battle dice's hit face. A roll
    // with no region (a healing roll, the Three Rings) stands in the middle of the map. Nothing in it takes the pointer.
    public sealed class CardRollPresenter : ICuePresenter
    {
        public const double Seconds = 1;
        public const string PlateName = "presentation-card-roll";
        private readonly BoardScreen board;
        private readonly MapObservedTrace trace;
        private readonly PresentationMarks marks;
        private readonly PresentationSoundSink sounds;
        private MapObservedTrace.TraceSet set;

        public CardRollPresenter(BoardScreen board, MapObservedTrace trace, PresentationLayer layer, PresentationSoundSink sounds)
        {
            this.board = board;
            this.trace = trace;
            this.sounds = sounds;
            marks = new PresentationMarks(layer, PlateName);
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            End();
            if (cue.Kind != PresentationCueKind.Roll || cue.Rolls.Count == 0)
                return null;
            sounds.Play(SoundCue.DiceRoll, "presented/" + cue.Id);
            var region = cue.Regions.FirstOrDefault(board.Map.Data.Shapes.ContainsKey);
            if (region != null)
                set = trace.Begin().Region(region).Above(region, Plate(cue));
            else
                marks.Place(() => marks.Plate(board.Map, Plate(cue), PlateSide.Middle));
            return new StepsBeat(now, End, new (Action Start, double Seconds)[] { (() => { }, Seconds) });
        }

        public void Released() => End();

        private void End()
        {
            marks.End();
            set?.End();
            set = null;
        }

        private static VisualElement Plate(PresentationCue cue)
        {
            var plate = new VisualElement { name = PlateName, pickingMode = PickingMode.Ignore };
            plate.AddToClassList(PlateName);
            foreach (var roll in cue.Rolls)
                plate.Add(Face(roll, cue.HitTarget));
            return plate;
        }

        private static DiePips Face(int roll, int? hitTarget)
        {
            var face = new DiePips { Illustrated = true };
            face.AddToClassList("battle-pips");
            face.EnableInClassList("battle-pips-hit", hitTarget.HasValue && roll >= hitTarget);
            face.Apply(roll);
            return face;
        }
    }
}
