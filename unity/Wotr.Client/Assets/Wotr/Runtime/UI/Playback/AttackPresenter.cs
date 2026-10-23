namespace Wotr.Client
{
    // UI-306 (OPPONENT_ACTIONS_UX §8 attack; storyboard C2): a recorded attack is intent, not occupation. The attackers are
    // traced where they stand and the defended region and force are traced too; nothing moves. The battle window opens at
    // the release, at its normal host.
    public sealed class AttackPresenter : ICuePresenter
    {
        public const double Seconds = 1;
        private readonly BoardScreen board;
        private readonly MapObservedTrace trace;

        public AttackPresenter(BoardScreen board, MapObservedTrace trace)
        {
            this.board = board;
            this.trace = trace;
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            var battle = cue.Battle;
            if (cue.Kind != PresentationCueKind.Attack || battle == null || !board.Map.Data.Shapes.ContainsKey(battle.Defender.RegionId))
                return null;
            var set = trace.Begin()
                .Force(battle.Attacker.RegionId, battle.Attacker.Side, battle.Attacker.Garrison)
                .Force(battle.Defender.RegionId, battle.Defender.Side, battle.Defender.Garrison)
                .Region(battle.Defender.RegionId);
            return new TracedBeat(set, now, Seconds);
        }
    }

    // A trace held for a dwell, then faded.
    public sealed class TracedBeat : IPresentationBeat
    {
        private readonly MapObservedTrace.TraceSet set;
        private readonly double end;

        public TracedBeat(MapObservedTrace.TraceSet set, double now, double seconds)
        {
            this.set = set;
            end = now + seconds;
        }

        public bool Advance(double now)
        {
            if (now < end)
                return false;
            set.End();
            return true;
        }

        public void Finish() => set.End();
    }
}
