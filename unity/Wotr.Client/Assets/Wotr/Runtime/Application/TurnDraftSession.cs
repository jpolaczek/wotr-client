using Wotr.Contracts;

namespace Wotr.Client
{
    // Keeps an unsent action through a brief opponent turn, without displaying stale targets.
    public sealed class TurnDraftSession
    {
        private PlayerView context;
        private TurnDraft parked;
        private string origin = "";
        private string parkedOrigin = "";
        public TurnDraft Draft { get; } = new TurnDraft();
        private bool HasChoice => Draft.Kind.HasValue || Draft.Payload.DieId.Length > 0 ||
            Draft.Payload.RulerTokenId.Length > 0 || Draft.Payload.CouncilTokenId.Length > 0;

        public SavedTurnDraft Save() => context == null || !HasChoice && parked == null ? null : new SavedTurnDraft
        {
            Draft = (parked ?? Draft).Snapshot(),
            Context = DraftContext.From(context),
            Origin = parked == null ? origin : parkedOrigin
        };

        public void Restore(SavedTurnDraft saved)
        {
            if (saved?.Draft?.Payload == null || saved.Context?.Decision == null)
                return;
            Draft.Restore(saved.Draft);
            context = saved.Context.View();
            origin = saved.Origin;
        }

        public string Update(PlayerView latest)
        {
            if (latest.Decision == null)
            {
                ParkOrClear(latest);
                return "";
            }
            if (parked != null && DraftContinuity.CanCarry(context, latest))
            {
                Draft.Adopt(parked);
                origin = parkedOrigin;
            }
            parked = null;
            parkedOrigin = "";
            var hadChoice = HasChoice;
            var carry = DraftContinuity.CanCarry(context, latest);
            if (!carry)
            {
                Draft.Clear();
                origin = latest.Decision.Id;
            }
            var changed = Draft.Reconcile(latest);
            context = latest;
            return hadChoice && carry ? changed ? "Choices changed · Review your draft." : "Game updated · Review your draft." : "";
        }

        public void Edited(PlayerView latest)
        {
            context = latest;
            origin = latest.Decision?.Id ?? "";
        }

        public void Complete(GameCommand command)
        {
            if (origin == command.DecisionId)
                Draft.Clear();
            if (parkedOrigin == command.DecisionId)
            {
                parked = null;
                parkedOrigin = "";
            }
        }

        private void ParkOrClear(PlayerView latest)
        {
            if (CanPark(latest))
            {
                if (HasChoice)
                {
                    parked = Draft.Fork();
                    parkedOrigin = origin;
                }
            }
            else
            {
                parked = null;
                parkedOrigin = "";
                context = null;
            }
            Draft.Clear();
        }

        private bool CanPark(PlayerView latest) => context?.Decision?.Kind == DecisionKind.Action &&
            latest.MatchId == context.MatchId && latest.You == context.You && latest.Turn == context.Turn &&
            latest.Phase == context.Phase && !latest.Winner.HasValue &&
            latest.Battle == null && latest.Hunt == null && latest.EventLoss == null &&
            context.Battle == null && context.Hunt == null && context.EventLoss == null;
    }
}
