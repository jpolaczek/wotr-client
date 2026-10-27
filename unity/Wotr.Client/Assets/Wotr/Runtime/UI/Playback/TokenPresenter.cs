using System;
using System.Collections.Generic;
using Wotr.Contracts;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-344 (owner's decision EXP-15 D3): the opponent's Ruler, Dismay and Council tokens and the Elven Rings' holders at
    // their fixed places — the opponent's chips in the top bar summary, the player's own tokens in their pool. A token the
    // opponent pays with lights its chip; a token or Ring that changes hands lights where it stands, then the count changes
    // while lit and holds, at the shared pace, as the hand count does (ResourcePresenter).
    public sealed class TokenPresenter : ICuePresenter
    {
        public const string PlateName = "presentation-token-plate";
        private readonly BoardScreen board;
        private readonly StagedBoard staged;
        private readonly PresentationMarks marks;

        public TokenPresenter(BoardScreen board, StagedBoard staged, PresentationLayer layer)
        {
            this.board = board;
            this.staged = staged;
            marks = new PresentationMarks(layer, PlateName);
        }

        public IPresentationBeat Begin(PresentationCue cue, double now)
        {
            marks.End();
            if (cue.Kind == PresentationCueKind.Cost && cue.Cost?.Kind is ActionCostKind.RulerToken or ActionCostKind.CouncilToken)
                return Paid(cue.Cost, now);
            if (cue.Kind == PresentationCueKind.Resource && cue.Resource?.Kind is ActionCostKind.RulerToken or ActionCostKind.CouncilToken or
                    ActionCostKind.Ring)
                return Moved(cue.Resource, now);
            return null;
        }

        public void Released() => marks.End();

        private IPresentationBeat Paid(ActionCost cost, double now)
        {
            var place = Place(cost.Kind, cost.Id, cost.Owner);
            if (place == null)
                return null;
            marks.Place(() => marks.Light(place));
            return new TimedBeat(now, PresentationPace.MarkSeconds);
        }

        private IPresentationBeat Moved(ActionResourceChange change, double now) => new StepsBeat(now, marks.End,
            new List<(Action Start, double Seconds)>
            {
                (() => marks.Place(() => Light(change)), PresentationPace.MarkSeconds),
                (() => Change(change), PresentationPace.HoldSeconds)
            });

        private void Change(ActionResourceChange change)
        {
            staged.Token(change);
            board.StageOpponentSummary(staged.View);
            board.StageDice(staged.View.You, staged.View);
            marks.Add(() => Light(change));
        }

        // Every place the change touches: where the token or Ring was and where it goes.
        private void Light(ActionResourceChange change)
        {
            foreach (var owner in Owners(change))
            {
                var place = Place(change.Kind, change.Id, owner);
                if (place != null && !PresentationGlow.IsLit(place))
                    marks.Light(place);
            }
        }

        private static IEnumerable<Side> Owners(ActionResourceChange change)
        {
            foreach (var location in new[] { change.From, change.To })
            {
                var owner = Owner(change.Kind, location);
                if (owner.HasValue)
                    yield return owner.Value;
            }
        }

        // Ruler tokens are the Free Peoples' and Dismay tokens the Shadow's; Council tokens and Rings name their holder.
        private static Side? Owner(ActionCostKind kind, string location) => kind switch
        {
            ActionCostKind.RulerToken => location == "free-peoples" ? Side.FreePeoples : location == "dismay" ? Side.Shadow : null,
            ActionCostKind.CouncilToken => location == "Shadow" ? Side.Shadow : null,
            _ => Enum.TryParse<Side>(location, out var side) ? side : (Side?)null
        };

        // The opponent's chip in the top bar, or the player's own token in their pool.
        private VisualElement Place(ActionCostKind kind, string id, Side? owner)
        {
            if (owner == null)
                return null;
            if (!board.Shown(owner))
                return board.TopBar.Opponent.Root.Q(OpponentSummary.ChipName(kind, owner.Value));
            return kind switch
            {
                ActionCostKind.Ring => board.Console.Rings.For(id),
                ActionCostKind.RulerToken => board.OwnDice.Q("ruler-token-" + id),
                _ => board.OwnDice.Q("pool-" + id)
            };
        }
    }
}
