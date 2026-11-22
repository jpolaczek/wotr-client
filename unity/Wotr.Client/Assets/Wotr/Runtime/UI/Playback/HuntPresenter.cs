using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-310 (OPPONENT_ACTIONS_UX §8 Fellowship/Hunt; storyboard D2–D7): a shared Hunt in its existing window, entry by
    // entry from the recorded snapshots (StagedBoard.Hunt). The window opens on the Hunt's start with a trace on the Hunt
    // Box; a roll shows its dice and successes with one dice sound; the drawn tile turns face up; the damage is set; a
    // response marks its object (the Ring, a portrait, a table card); a casualty leaves the Fellowship's roster; a new
    // Guide takes the badge. The tracks, a reveal and the progress move with the Fellowship's change after the Hunt
    // (FellowshipPresenter). The window hosts no choice here: a real response opens at the release. UI-462: a new Guide
    // who is no Companion yet (Sméagol) joins the roster first (FellowshipJoin).
    public sealed class HuntPresenter : ICuePresenter
    {
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly FellowshipReading reading;
        private readonly PresentationSoundSink sounds;
        private readonly PresentationMark mark;
        private readonly FellowshipJoin join;
        // UI-520: a roll's result named under the Hunt's dice.
        private readonly PresentationMarks plates;
        private readonly PresentationLayer layer;

        public HuntPresenter(BoardScreen board, StagedBoard staged, PresentationLayer layer, FellowshipReading reading,
            PresentationSoundSink sounds)
        {
            this.board = board;
            this.staged = staged;
            this.reading = reading;
            this.sounds = sounds;
            this.layer = layer;
            mark = new PresentationMark(layer);
            plates = new PresentationMarks(layer, "presentation-hunt-plate");
            join = new FellowshipJoin(board, staged, reading, mark);
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
            plates.End();
            if (StartsGated(entry))
            {
                // The window opens on the Hunt's start; its steps wait behind Start the Hunt.
                Stage(cue, entry);
                return new DrawGate(layer, "Start the Hunt", true, HuntPlace, GateAlign.RightCentre, at => Play(cue, entry, at)).Compact();
            }
            if (DrawGated(entry))
                return new DrawGate(layer, "Draw a Hunt tile", false, HuntPlace, GateAlign.RightCentre, at => Staged(cue, entry, at)).Compact();
            if (RerollGated(entry))
                return new DrawGate(layer, "Re-roll the Hunt", true, HuntPlace, GateAlign.RightCentre, at => Staged(cue, entry, at)).Compact();
            return Staged(cue, entry, now);
        }

        // UI-570 (owner, 2026-11-22: "the Start the Hunt button is terribly large and runs over other parts of the interface"): the
        // Hunt's gates stand small at the window's right edge, inside it, level with its dice once they show (else with the
        // summary row where they will): the row itself may run wider than the window.
        private UnityEngine.Rect? HuntPlace()
        {
            var window = board.Hunt.Root;
            if (!PresentationTargets.Shown(window))
                return null;
            var dice = board.Hunt.Dice.Root;
            var row = PresentationTargets.Shown(dice) && dice.worldBound.height > 0 ? dice : dice.parent;
            var line = row.worldBound;
            var bounds = window.worldBound;
            // The owner: "the button is uneven" — its right edge on the window content's right edge, where the report's › and the
            // Hunt's owner line end.
            var right = bounds.xMax - window.resolvedStyle.paddingRight - window.resolvedStyle.borderRightWidth;
            return UnityEngine.Rect.MinMaxRect(bounds.xMin, line.yMin, right, line.yMax);
        }

        private IPresentationBeat Staged(PresentationCue cue, HuntReportEntryView entry, double now)
        {
            Stage(cue, entry);
            return Play(cue, entry, now);
        }

        private void Stage(PresentationCue cue, HuntReportEntryView entry)
        {
            staged.Hunt(cue.HuntChainId, entry);
            board.StageHunt(staged.View);
        }

        private IPresentationBeat Play(PresentationCue cue, HuntReportEntryView entry, double now)
        {
            if (entry.Kind == HuntReportKind.GuideChanged && join.Joins(entry.Outcome.GuideId))
                return join.Begin(entry.Outcome.GuideId, now);
            var steps = Steps(cue, entry);
            return steps.Count == 0 ? null : new StepsBeat(now, EndRing, steps);
        }

        // UI-570 (owner, 2026-11-22: "leave it as in the rules, but add a 'start the hunt' button at the beginning for both
        // players"): with Settings' "Roll my own dice and draws" on, every Hunt waits behind Start the Hunt for both players.
        // The Shadow's roll follows that click, as its reroll follows its own Reroll selected ("we already have a button for
        // that action"), so neither waits again.
        private bool StartsGated(HuntReportEntryView entry) =>
            board.ConfirmsOwnDraws && board.View != null && entry.Kind == HuntReportKind.Started;

        // UI-570 (owner, 2026-11-22: "add buttons for the Hunt's re-roll"): the Shadow chooses its re-rolls and clicks Reroll
        // selected (its own button, so its re-roll never waits); the Free Peoples see that re-roll behind Re-roll the Hunt.
        private bool RerollGated(HuntReportEntryView entry) =>
            board.ConfirmsOwnDraws && board.View?.You == Side.FreePeoples && entry.Kind == HuntReportKind.Rerolled;

        // By the rules the Shadow draws the Hunt's tiles, so its own presentation of a tile drawn waits behind Draw a Hunt
        // tile; a tile drawn again for the Free Peoples (Mithril Coat and Sting, Nenya) is theirs and waits for them.
        private bool DrawGated(HuntReportEntryView entry) =>
            board.ConfirmsOwnDraws && board.View != null &&
            (entry.Kind == HuntReportKind.TileDrawn && board.View.You == Side.Shadow ||
             entry.Kind == HuntReportKind.TileReplaced && board.View.You == Side.FreePeoples);

        public void Released()
        {
            join.End();
            EndRing();
            plates.End();
        }

        private List<(Action Start, double Seconds)> Steps(PresentationCue cue, HuntReportEntryView entry)
        {
            var seconds = Seconds(entry.Kind);
            var steps = new List<(Action Start, double Seconds)>();
            switch (entry.Kind)
            {
                case HuntReportKind.Started:
                    // UI-315: the window the Hunt opened settles before its Box is marked.
                    steps.Add((() => { }, PresentationPace.PanelSettleSeconds));
                    steps.Add((() => mark.Show(() => board.Hunt.Tab), seconds));
                    break;
                case HuntReportKind.Rolled or HuntReportKind.Rerolled:
                    // UI-520 (owner): the dice light and their result stands named under them long enough to read.
                    steps.Add((() => Rolled(cue, entry), seconds + PresentationPace.ResultSeconds));
                    break;
                case HuntReportKind.CompanionLeft or HuntReportKind.CharacterEliminated:
                    steps.Add((() => reading.Open(), PresentationPace.PanelSettleSeconds));
                    steps.Add((() => mark.Show(() => reading.Portrait(entry.CharacterId)), seconds));
                    steps.Add((() => Leave(entry.CharacterId), seconds));
                    break;
                case HuntReportKind.GuideChanged:
                    steps.Add((() => reading.Open(), PresentationPace.PanelSettleSeconds));
                    steps.Add((() => Guide(entry.Outcome.GuideId), seconds));
                    break;
                case HuntReportKind.Response or HuntReportKind.Reaction:
                    steps.Add((() => reading.Open(), PresentationPace.PanelSettleSeconds));
                    steps.Add((() => mark.Show(() => Response(entry)), seconds));
                    break;
                default:
                    if (seconds > 0)
                        steps.Add((EndRing, seconds));
                    break;
            }
            return steps;
        }

        // What answered the Hunt: its character's portrait, the Ring, the Guide, or the table card it came from. UI-344: a
        // character's own response (Gimli's or the Council's Guide, Nenya's Galadriel) names him by its source only.
        private VisualElement Response(HuntReportEntryView entry)
        {
            if (entry.CharacterId.Length > 0)
                return reading.Portrait(entry.CharacterId);
            if (CharacterLibrary.Find(entry.SourceId) != null)
                return reading.Portrait(Holder(entry.SourceId));
            return entry.Response switch
            {
                HuntResponse.Ring => board.Fellowship.Status.Q("fellowship-ring"),
                HuntResponse.Guide => reading.Portrait(staged.View.Fellowship.Guide),
                _ => CardLibrary.Cards.ContainsKey(entry.SourceId) ? board.TableEffects.Tab : null
            };
        }

        // The Companion of that identity as the roster shows him (the Free Peoples' own figure id, or the identity).
        private string Holder(string source)
        {
            var identity = CharacterLibrary.Identity(source);
            return staged.View.OwnCompanions.Concat(staged.View.Characters)
                .FirstOrDefault(c => CharacterLibrary.Identity(c.DefinitionId) == identity)?.Id ?? identity;
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

        private void Rolled(PresentationCue cue, HuntReportEntryView entry)
        {
            sounds.Play(SoundCue.DiceRoll, "presented/" + cue.Id);
            mark.Show(() => board.Hunt.Dice.Root);
            var hits = entry.Hunt?.Dice.Count(die => die.Success) ?? 0;
            plates.Place(() =>
            {
                var plate = new Label(PresentationPace.ResultText(hits));
                plate.AddToClassList(PoliticsPresenter.CaptionName);
                plate.AddToClassList(PresentationPace.ResultClass);
                plates.Plate(board.Hunt.Dice.Root, plate, PlateSide.Under);
            });
        }
    }
}
