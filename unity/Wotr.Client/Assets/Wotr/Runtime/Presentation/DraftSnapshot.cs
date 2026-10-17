using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class DraftSnapshot
    {
        public CommandKind? Kind { get; set; }
        public CommandPayload Payload { get; set; } = new CommandPayload();
        public int MoveFocus { get; set; } = -1;
        public string ReserveId { get; set; } = "";
        public int CompanionFocus { get; set; } = -1;
        public HuntResponse? HuntChoice { get; set; }
        public string ArmedSource { get; set; } = "";
    }

    public sealed class SavedTurnDraft
    {
        public DraftSnapshot Draft { get; set; }
        public DraftContext Context { get; set; }
        public string Origin { get; set; } = "";
    }

    // Continuity needs only this header, never a saved hand, legal options or a full board.
    public sealed class DraftContext
    {
        public string MatchId { get; set; }
        public Side You { get; set; }
        public int Turn { get; set; }
        public GamePhase Phase { get; set; }
        public Side? DecisionOwner { get; set; }
        public DecisionView Decision { get; set; }
        public bool InBattle { get; set; }
        public bool InHunt { get; set; }
        public bool InEventLoss { get; set; }

        public static DraftContext From(PlayerView view) => new DraftContext
        {
            MatchId = view.MatchId,
            You = view.You,
            Turn = view.Turn,
            Phase = view.Phase,
            DecisionOwner = view.DecisionOwner,
            Decision = view.Decision == null ? null : new DecisionView
            {
                Id = view.Decision.Id,
                Kind = view.Decision.Kind,
                Window = view.Decision.Window,
                SourceId = view.Decision.SourceId
            },
            InBattle = view.Battle != null,
            InHunt = view.Hunt != null,
            InEventLoss = view.EventLoss != null
        };

        public PlayerView View() => new PlayerView
        {
            MatchId = MatchId,
            You = You,
            Turn = Turn,
            Phase = Phase,
            DecisionOwner = DecisionOwner,
            Decision = Decision,
            Battle = InBattle ? new BattleView() : null,
            Hunt = InHunt ? new HuntView() : null,
            EventLoss = InEventLoss ? new EventLossView() : null
        };
    }
}
