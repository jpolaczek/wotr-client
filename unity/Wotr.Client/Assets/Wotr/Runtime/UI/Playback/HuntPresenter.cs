using System;
using System.Collections.Generic;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-310 (OPPONENT_ACTIONS_UX §8 Fellowship/Hunt; storyboard D2–D7): a shared Hunt in its existing window, entry by
    // entry from the recorded snapshots (StagedBoard.Hunt). The window opens on the Hunt's start with a trace on the Hunt
    // Box; a roll shows its dice and successes with one dice sound; the drawn tile turns face up; the damage is set; a
    // response marks its object (the Ring, a portrait, a table card); a casualty leaves the Fellowship's roster; a new
    // Guide takes the badge. The tracks, a reveal and the progress move with the Fellowship's change after the Hunt
    // (FellowshipPresenter). The window hosts no choice here: a real response opens at the release.
    public sealed class HuntPresenter : ICuePresenter
    {
        public const double SettleSeconds = .25;
        public const string RingName = "presentation-hunt-ring";
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly FellowshipReading reading;
        private readonly PresentationSoundSink sounds;
        private readonly PresentationMark mark;

        public HuntPresenter(BoardScreen board, StagedBoard staged, PresentationLayer layer, FellowshipReading reading,
            PresentationSoundSink sounds)
        {
            this.board = board;
            this.staged = staged;
            this.reading = reading;
            this.sounds = sounds;
            mark = new PresentationMark(layer, RingName);
        }

        // How long an entry holds; the tracks, the reveal, the progress and a Companion's placing have their own objects.
        public static double Seconds(HuntReportKind kind) => kind switch
        {
            HuntReportKind.Rolled or HuntReportKind.Rerolled or HuntReportKind.TileDrawn or HuntReportKind.TileReplaced => 1,
            HuntReportKind.Response or HuntReportKind.Reaction or HuntReportKind.GuideChanged => .8,
            HuntReportKind.Started or HuntReportKind.DamageDetermined or HuntReportKind.DamageChanged or HuntReportKind.HuntCompleted => .6,
            HuntReportKind.CompanionLeft or HuntReportKind.CharacterEliminated => .6,
            _ => 0
        };

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            var entry = cue.Hunt;
            if (cue.Kind != PresentationCueKind.Hunt || entry == null || cue.HuntChainId.Length == 0)
                return null;
            staged.Hunt(cue.HuntChainId, entry);
            board.StageHunt(staged.View);
            var steps = Steps(cue, entry);
            return steps.Count == 0 ? null : new StepsBeat(now, EndRing, steps);
        }

        public void Released() => EndRing();

        private List<(Action Start, double Seconds)> Steps(PresentationCue cue, HuntReportEntryView entry)
        {
            var seconds = Seconds(entry.Kind);
            var steps = new List<(Action Start, double Seconds)>();
            switch (entry.Kind)
            {
                case HuntReportKind.Started:
                    steps.Add((() => mark.Show(() => board.Hunt.Tab), seconds));
                    break;
                case HuntReportKind.Rolled or HuntReportKind.Rerolled:
                    steps.Add((() => sounds.Play(SoundCue.DiceRoll, "presented/" + cue.Id), seconds));
                    break;
                case HuntReportKind.CompanionLeft or HuntReportKind.CharacterEliminated:
                    steps.Add((() => reading.Open(), SettleSeconds));
                    steps.Add((() => mark.Show(() => reading.Portrait(entry.CharacterId)), seconds));
                    steps.Add((() => Leave(entry.CharacterId), seconds));
                    break;
                case HuntReportKind.GuideChanged:
                    steps.Add((() => reading.Open(), SettleSeconds));
                    steps.Add((() => Guide(entry.Outcome.GuideId), seconds));
                    break;
                case HuntReportKind.Response or HuntReportKind.Reaction:
                    steps.Add((() => reading.Open(), SettleSeconds));
                    steps.Add((() => mark.Show(() => Response(entry)), seconds));
                    break;
                default:
                    if (seconds > 0)
                        steps.Add((EndRing, seconds));
                    break;
            }
            return steps;
        }

        // What answered the Hunt: its character's portrait, the Ring, the Guide, or the table card it came from.
        private VisualElement Response(HuntReportEntryView entry)
        {
            if (entry.CharacterId.Length > 0)
                return reading.Portrait(entry.CharacterId);
            return entry.Response switch
            {
                HuntResponse.Ring => board.Fellowship.Status.Q("fellowship-ring"),
                HuntResponse.Guide => reading.Portrait(staged.View.Fellowship.Guide),
                _ => CardLibrary.Cards.ContainsKey(entry.SourceId) ? board.TableEffects.Tab : null
            };
        }

        private void Leave(string id)
        {
            EndRing();
            staged.Leave(id);
            board.StageRoster(staged.View);
        }

        private void Guide(string id)
        {
            staged.Guide(id);
            board.StageRoster(staged.View);
            mark.Show(() => reading.Portrait(id));
        }

        private void EndRing() => mark.End();
    }
}
