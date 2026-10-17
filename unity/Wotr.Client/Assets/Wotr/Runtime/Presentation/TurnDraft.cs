using System;
using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class TurnDraft
    {
        private readonly List<TurnDraft> history = new List<TurnDraft>();
        private int gesture;
        public CommandKind? Kind { get; private set; }
        public CommandPayload Payload { get; private set; } = new CommandPayload();
        public int MoveFocus { get; internal set; } = -1;
        public string ReserveId { get; internal set; } = "";
        public int CompanionFocus { get; internal set; } = -1;
        public HuntResponse? HuntChoice { get; internal set; }
        // UI-212: what a distribution arms locally (a region or a figure), never sent with the command.
        public string ArmedSource { get; internal set; } = "";
        public void Choose(CommandKind kind, string dieId = "")
        {
            Remember();
            Kind = kind;
            Payload = new CommandPayload { DieId = dieId };
            MoveFocus = -1;
            ReserveId = "";
            CompanionFocus = -1;
            HuntChoice = null;
            ArmedSource = "";
        }
        public void Clear()
        {
            Kind = null;
            Payload = new CommandPayload();
            MoveFocus = -1;
            ReserveId = "";
            CompanionFocus = -1;
            HuntChoice = null;
            ArmedSource = "";
            history.Clear();
        }
        public void Toggle(string id)
        {
            Remember();
            if (!Payload.Ids.Remove(id))
                Payload.Ids.Add(id);
        }

        public void Change(Action<CommandPayload> change)
        {
            Remember();
            change(Payload);
        }

        public void Back()
        {
            if (history.Count == 0)
            {
                Clear();
                return;
            }
            var previous = history[history.Count - 1];
            history.RemoveAt(history.Count - 1);
            Kind = previous.Kind;
            Payload = previous.Payload;
            MoveFocus = previous.MoveFocus;
            ReserveId = previous.ReserveId;
            CompanionFocus = previous.CompanionFocus;
            HuntChoice = previous.HuntChoice;
            ArmedSource = previous.ArmedSource;
        }

        // UI-127: one gesture of the player is one undo step, however many edits it makes to the draft.
        public void Gesture(Action change)
        {
            Remember();
            gesture++;
            try
            {
                change();
            }
            finally
            {
                gesture--;
            }
        }

        private void Remember()
        {
            if (gesture > 0)
                return;
            if (history.Count == 32)
                history.RemoveAt(0);
            history.Add(Fork());
        }

        public TurnDraft Fork() => new TurnDraft
        {
            Kind = Kind,
            Payload = Copy(Payload),
            MoveFocus = MoveFocus,
            ReserveId = ReserveId,
            CompanionFocus = CompanionFocus,
            HuntChoice = HuntChoice,
            ArmedSource = ArmedSource
        };

        public void Adopt(TurnDraft other)
        {
            Remember();
            Kind = other.Kind;
            Payload = Copy(other.Payload);
            MoveFocus = other.MoveFocus;
            ReserveId = other.ReserveId;
            CompanionFocus = other.CompanionFocus;
            HuntChoice = other.HuntChoice;
            ArmedSource = other.ArmedSource;
        }

        public DraftSnapshot Snapshot() => new DraftSnapshot
        {
            Kind = Kind,
            Payload = Copy(Payload),
            MoveFocus = MoveFocus,
            ReserveId = ReserveId,
            CompanionFocus = CompanionFocus,
            HuntChoice = HuntChoice,
            ArmedSource = ArmedSource
        };

        public void Restore(DraftSnapshot saved)
        {
            Clear();
            Kind = saved.Kind;
            Payload = Copy(saved.Payload);
            MoveFocus = saved.MoveFocus;
            ReserveId = saved.ReserveId;
            CompanionFocus = saved.CompanionFocus;
            HuntChoice = saved.HuntChoice;
            ArmedSource = saved.ArmedSource ?? "";
        }

        private static CommandPayload Copy(CommandPayload payload) => new CommandPayload
        {
            Stepwise = payload.Stepwise,
            DieId = payload.DieId,
            RingId = payload.RingId,
            RulerTokenId = payload.RulerTokenId,
            CouncilTokenId = payload.CouncilTokenId,
            Deck = payload.Deck,
            Face = payload.Face,
            CardId = payload.CardId,
            CardCost = payload.CardCost,
            ReactionId = payload.ReactionId,
            RegionId = payload.RegionId,
            CharacterId = payload.CharacterId,
            AbilityId = payload.AbilityId,
            Losses = payload.Losses.Select(l => new BattleLossChoice { UnitId = l.UnitId, Kind = l.Kind }).ToList(),
            GuideId = payload.GuideId,
            Route = new List<string>(payload.Route),
            HuntResponse = payload.HuntResponse,
            CompanionMoves = payload.CompanionMoves.Select(m => new CompanionMoveChoice
            {
                Ids = new List<string>(m.Ids),
                Route = new List<string>(m.Route)
            }).ToList(),
            Ids = new List<string>(payload.Ids),
            NationId = payload.NationId,
            MovementCost = payload.MovementCost,
            Placements = payload.Placements.Select(p => new UnitPlacementChoice { UnitId = p.UnitId, RegionId = p.RegionId }).ToList(),
            Moves = payload.Moves.Select(m => new ArmyMoveChoice
            {
                From = m.From,
                To = m.To,
                Route = new List<string>(m.Route),
                UnitIds = new List<string>(m.UnitIds),
                CharacterIds = new List<string>(m.CharacterIds)
            }).ToList()
        };

        public bool Reconcile(PlayerView view)
        {
            history.Clear(); // Undo must never restore choices from an earlier server revision.
            return DraftReconciliation.Apply(this, view);
        }

        public bool Ready(ActionOption option)
        {
            if (ExpansionDraft.Handles(option.Kind))
                return ExpansionDraft.Ready(this, option);
            if (option.EventRecruitment != null)
                return EventRecruitDraft.Ready(this, option);
            if (option.ArmyEvent?.RouteMovement == true && option.Kind == CommandKind.MoveArmies)
                return EventArmyDraft.Ready(this, option);
            if (CharacterDraft.UsesCharacters(option.Kind))
                return CharacterDraft.Ready(this, option);
            if (BattleDraft.UsesBattle(option.Kind))
                return BattleDraft.Ready(this, option);
            if (FellowshipDraft.UsesFellowship(option.Kind))
                return FellowshipDraft.Ready(this, option);
            if (CardDraft.UsesCards(option.Kind))
                return CardDraft.Ready(this, option);
            switch (option.Kind)
            {
                case CommandKind.ChooseCorruptionTile:
                    return EffectChoiceDraft.Ready(this, option);
                case CommandKind.AllocateHunt:
                    return Payload.Ids.Count >= option.Minimum && Payload.Ids.Count <= option.Maximum &&
                        Payload.Ids.All(option.EligibleIds.Contains);
                case CommandKind.DiscardCards:
                case CommandKind.RemoveExcess:
                    return Payload.Ids.Count >= option.Minimum && Payload.Ids.Count <= option.Maximum;
                case CommandKind.AdvancePolitics:
                    return Payload.DieId.Length > 0 && option.Targets.Contains(Payload.NationId);
                case CommandKind.Recruit:
                    if (Payload.Stepwise)
                        return option.EligibleDice.Contains(Payload.DieId) && Payload.Placements.Count == 1 &&
                            option.Recruitment.Any(g => g.ReserveIds.Contains(Payload.Placements[0].UnitId) &&
                                g.Targets.Contains(Payload.Placements[0].RegionId));
                    return Payload.DieId.Length > 0 && Payload.Placements.Count > 0 && ReserveId.Length == 0;
                case CommandKind.MoveArmies:
                case CommandKind.AttackArmy:
                    return (option.ArmyEvent != null || Payload.DieId.Length > 0) && Payload.Moves.Count > 0 && Payload.Moves.Count <= option.Maximum &&
                        Payload.Moves.All(m => m.To.Length > 0 && m.UnitIds.Count > 0 && option.Armies.Any(a => a.RegionId == m.From &&
                            (a.RequiredAnyIds.Count == 0 || m.UnitIds.Concat(m.CharacterIds).Any(a.RequiredAnyIds.Contains)) &&
                            a.Targets.Any(t => t.RegionId == m.To && m.UnitIds.Concat(m.CharacterIds).All(t.EligibleIds.Contains))));
                case CommandKind.DrawCard:
                    return CardDrawDraft.Ready(this, option);
                case CommandKind.DiscardWelcome:
                case CommandKind.DiscardGates:
                case CommandKind.DiscardDenethor:
                    return option.EligibleDice.Contains(Payload.DieId);
                case CommandKind.DiscardDie:
                    return Payload.DieId.Length > 0;
                case CommandKind.UseRing:
                    return Payload.RingId.Length > 0 && Payload.DieId.Length > 0 && Payload.Face.HasValue;
                case CommandKind.DiscardLore:
                case CommandKind.DiscardBaradPalantir:
                case CommandKind.DiscardPalantir:
                    return option.EligibleDice.Contains(Payload.DieId) &&
                        (Payload.RingId.Length == 0 ? option.DiceWithoutRing.Contains(Payload.DieId) : option.EligibleIds.Contains(Payload.RingId));
                default:
                    return true;
            }
        }

        public GameCommand Command(PlayerView view) => new GameCommand
        {
            MatchId = view.MatchId,
            CommandId = Guid.NewGuid().ToString("N"),
            ExpectedRevision = view.Revision,
            DecisionId = view.Decision.Id,
            Kind = Kind.Value,
            Payload = Copy(Payload)
        };
    }
}
