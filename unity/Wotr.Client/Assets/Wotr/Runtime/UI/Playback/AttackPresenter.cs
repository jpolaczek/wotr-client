using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-306 (OPPONENT_ACTIONS_UX §8 attack; storyboard C2): a recorded attack is intent, not occupation. The attackers are
    // traced where they stand and the defended region and force are traced too. UI-542 (owner, 2026-11-17: "we must see
    // that this group of units heads in that direction, then the battle window opens"): after the camera has brought the
    // attack into view, the attacking force lunges towards the defended region and back (its figures only: the force's
    // stand keeps its place, nothing changes region), and the battle window opens after it. Reduced motion: no lunge.
    public sealed class AttackPresenter : ICuePresenter
    {
        // UI-315: both sides glow long enough to be read; UI-542: the lunge plays inside it, then the window opens.
        public const double LeadSeconds = .3;
        public const double LungeSeconds = 1;
        public const double Seconds = LeadSeconds + LungeSeconds + .4;
        // How far the attackers lunge: a share of the way to the defended region, at most a few figures' widths.
        public const float LungeShare = .4f;
        public const float MostLunge = 70;
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
            var lunge = board.ReducedMotion() || !board.Map.Data.Shapes.ContainsKey(battle.Attacker.RegionId) ? null
                : new Lunge(board.Map, battle.Attacker.RegionId, battle.Attacker.Side, battle.Attacker.Garrison, battle.Defender.RegionId);
            return new AttackBeat(set, lunge, now);
        }

        // The attacking force's figures towards the defended region and back: out with an ease, a short stop, back.
        public sealed class Lunge
        {
            private readonly MapCanvas map;
            private readonly List<VisualElement> figures;
            private readonly Vector2 offset;
            private List<Vector2> shifts;

            public Lunge(MapCanvas map, string attacker, Side side, bool garrison, string defender)
            {
                this.map = map;
                figures = map.ForceArtworks(attacker, side, garrison).ToList();
                var from = map.Camera.ToScreen(Anchor(map, attacker));
                var to = map.Camera.ToScreen(Anchor(map, defender));
                var way = to - from;
                offset = way.sqrMagnitude < 1 ? Vector2.zero : way.normalized * Mathf.Min(way.magnitude * LungeShare, MostLunge);
            }

            public IReadOnlyList<VisualElement> Figures => figures;
            public Vector2 Offset => offset;

            // 0..1 of the lunge: out over the first 40%, held to 55%, back by the end.
            public void At(double fraction)
            {
                var reach = (float)(fraction < .4 ? Ease(fraction / .4) : fraction < .55 ? 1 : 1 - Ease((fraction - .55) / .45));
                shifts ??= figures.Select(Local).ToList();
                for (var i = 0; i < figures.Count; i++)
                    figures[i].style.translate = new Translate(shifts[i].x * reach, shifts[i].y * reach);
            }

            // The lunge in the figure's own points: its stand is placed and scaled to the map's figure scale.
            private Vector2 Local(VisualElement figure)
            {
                var parent = figure.parent;
                if (parent == null)
                    return Vector2.zero;
                var origin = parent.WorldToLocal(map.LocalToWorld(Vector2.zero));
                return parent.WorldToLocal(map.LocalToWorld(offset)) - origin;
            }

            public void End()
            {
                foreach (var figure in figures)
                    figure.style.translate = StyleKeyword.Null;
            }

            private static Vector2 Anchor(MapCanvas map, string region) =>
                new Vector2(map.Data.Shapes[region].Anchor[0], map.Data.Shapes[region].Anchor[1]);

            private static double Ease(double t) => 1 - (1 - t) * (1 - t);
        }

        // The trace for the whole beat; the lunge after a short lead.
        private sealed class AttackBeat : IPresentationBeat
        {
            private readonly MapObservedTrace.TraceSet set;
            private readonly Lunge lunge;
            private readonly double start;
            private bool ended;

            public AttackBeat(MapObservedTrace.TraceSet set, Lunge lunge, double now)
            {
                this.set = set;
                this.lunge = lunge;
                start = now;
            }

            public bool Advance(double now)
            {
                var elapsed = now - start;
                if (lunge != null && elapsed >= LeadSeconds && elapsed <= LeadSeconds + LungeSeconds)
                    lunge.At((elapsed - LeadSeconds) / LungeSeconds);
                if (elapsed < Seconds)
                    return false;
                Finish();
                return true;
            }

            public void Finish()
            {
                if (ended)
                    return;
                ended = true;
                lunge?.End();
                set.End();
            }
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

    // UI-306/UI-307: a traced dwell after which the staged board becomes the map's figure baseline.
    public sealed class SettledBeat : IPresentationBeat
    {
        private readonly TracedBeat traced;
        private readonly StagedBoard staged;

        public SettledBeat(MapObservedTrace.TraceSet set, StagedBoard staged, double now, double seconds)
        {
            traced = new TracedBeat(set, now, seconds);
            this.staged = staged;
        }

        public bool Advance(double now)
        {
            if (!traced.Advance(now))
                return false;
            staged.Settle();
            return true;
        }

        public void Finish()
        {
            traced.Finish();
            staged.Settle();
        }
    }
}
