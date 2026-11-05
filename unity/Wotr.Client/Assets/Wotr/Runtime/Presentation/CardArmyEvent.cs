using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-216 (CARD_CHOICES_UX): a card's army event on the map, as with an Army die (UI-105). The armies the event may
    // command glow; a click on one starts its move with the whole army and opens its panel, whose lit figures toggle;
    // a lit destination completes the move, and another lit army starts the next. The console counts the armies and its
    // ✓ sends.
    // UI-431 (owner's rule, UI-430): no "Move armies", "Attack" or "Skip maneuver" buttons. An event that may move or
    // attack lights both steps together, as the Army die does (UI-389): the armies of either step glow, and the chosen
    // army's move destinations light with its attack targets in red (UI-403's red landing mark); a click on an attack
    // target turns the planned army's step into the attack with that target, as the old Attack button and the same
    // clicks did. Skip maneuver ends the event, so it is the console's ✓ while no army is chosen (FinishingOption).
    public static class CardArmyEvent
    {
        public static bool Active(PlayerView view, TurnDraft draft) => CardDecisions.Owns(view, draft) &&
            view.Decision.Kind == DecisionKind.ArmyEvent && draft.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy;

        // The armies a click may start: the current step's, then the other step's not already among them.
        public static IEnumerable<ArmyOption> Sources(PlayerView view, TurnDraft draft)
        {
            var current = Current(view, draft)?.Armies ?? new List<ArmyOption>();
            var other = Other(view, draft)?.Armies.Where(a => !current.Any(c => c.RegionId == a.RegionId && c.Garrison == a.Garrison));
            return other == null ? current : current.Concat(other);
        }

        public static bool IsSource(PlayerView view, TurnDraft draft, string region) => Sources(view, draft).Any(a => a.RegionId == region);

        public static bool IsTarget(PlayerView view, TurnDraft draft, string region) => draft.Kind == CommandKind.AttackArmy
            ? AttackDraft.Targets(draft, view).Contains(region)
            : ArmyDraft.Targets(draft, view).Contains(region);

        // UI-431: the other step's targets for the planned army's chosen figures — an attack's while it moves, a move's while
        // it attacks — until its destination is chosen; none while another army is planned too (an attack takes one army)
        // or the figures do not fit.
        public static IEnumerable<string> OtherTargets(PlayerView view, TurnDraft draft)
        {
            var trial = Switched(view, draft);
            if (trial == null)
                return Enumerable.Empty<string>();
            return trial.Kind == CommandKind.AttackArmy ? AttackDraft.Targets(trial, view).ToList() : ArmyDraft.Targets(trial, view).ToList();
        }

        // UI-431 (UI-403): the targets a click attacks now, drawn red: the attack being planned, or the other step's attack.
        public static IEnumerable<string> AttackTargets(PlayerView view, TurnDraft draft)
        {
            if (!Active(view, draft))
                return Enumerable.Empty<string>();
            return draft.Kind == CommandKind.AttackArmy ? AttackDraft.Targets(draft, view).ToList() : OtherTargets(view, draft);
        }

        // The battle an attack on this region from that army fights (the server's mode), or null.
        public static BattleMode? AttackMode(PlayerView view, string from, string region) =>
            view.Options.FirstOrDefault(o => o.Kind == CommandKind.AttackArmy)?.Armies.FirstOrDefault(a => a.RegionId == from)?
                .Targets.FirstOrDefault(t => t.RegionId == region)?.BattleMode;

        // UI-431: a click on the other step's target makes the planned army's step that step, with that target. The chosen
        // figures keep their order in the other step's army, so the step is the one its own button and the same clicks made.
        public static bool Switch(PlayerView view, TurnDraft draft, string region)
        {
            if (!Active(view, draft) || !OtherTargets(view, draft).Contains(region))
                return false;
            var trial = Switched(view, draft);
            trial.Change(p => p.Moves[0].To = region);
            draft.Gesture(() => draft.Adopt(trial));
            return true;
        }

        // UI-431: a lit army of the other step only (an army that may attack but not move) starts that step, in one undo step.
        public static bool Start(PlayerView view, TurnDraft draft, string region)
        {
            var other = Other(view, draft);
            if (Current(view, draft)?.Armies.Any(a => a.RegionId == region) == true || other?.Armies.Any(a => a.RegionId == region) != true)
                return BoardDraft.SelectRegion(draft, view, region);
            var started = false;
            draft.Gesture(() =>
            {
                DraftGesture.Choose(draft, view, other.Kind);
                started = BoardDraft.SelectRegion(draft, view, region);
            });
            return started;
        }

        // The region of the army being planned.
        public static string ArmyRegion(TurnDraft draft) => ArmyDraft.Current(draft)?.From ?? "";

        // The planned army's figures a click may toggle: its own, less those another planned army moves.
        public static IEnumerable<string> Movable(PlayerView view, TurnDraft draft)
        {
            var army = Army(view, draft);
            if (army == null)
                return Enumerable.Empty<string>();
            var used = ArmyDraft.UsedElsewhere(draft, ArmyDraft.VisibleIndex(draft));
            return army.UnitIds.Concat(army.CharacterIds).Where(id => !used.Contains(id));
        }

        public static IEnumerable<string> Moving(TurnDraft draft)
        {
            var move = ArmyDraft.Current(draft);
            return move == null ? Enumerable.Empty<string>() : move.UnitIds.Concat(move.CharacterIds);
        }

        public static bool Toggle(PlayerView view, TurnDraft draft, string id)
        {
            var army = Army(view, draft);
            if (army == null || !Movable(view, draft).Contains(id))
                return false;
            var character = army.CharacterIds.Contains(id);
            if (draft.Kind == CommandKind.MoveArmies && EventArmyDraft.Option(view) != null)
                EventArmyDraft.Toggle(draft, new[] { id }, character);
            else
                ArmyDraft.Toggle(draft, new[] { id }, character);
            return true;
        }

        // Nothing planned yet: no army has a chosen figure, so the console's ✓ finishes the event (FinishingOption).
        public static bool NothingPlanned(TurnDraft draft) => draft.Payload.Moves.All(m => m.UnitIds.Count + m.CharacterIds.Count == 0);

        private static ArmyOption Army(PlayerView view, TurnDraft draft)
        {
            var region = ArmyRegion(draft);
            return region.Length == 0 ? null : Current(view, draft)?.Armies.FirstOrDefault(a => a.RegionId == region);
        }

        // The draft turned into the other step for the one planned army, with no target yet; null when it cannot be. An army
        // with its destination chosen lights nothing more (↶ takes the destination back).
        private static TurnDraft Switched(PlayerView view, TurnDraft draft)
        {
            var other = Other(view, draft);
            var move = ArmyDraft.Current(draft);
            if (other == null || move == null || draft.Payload.Moves.Count != 1 || move.To.Length > 0 ||
                move.UnitIds.Count + move.CharacterIds.Count == 0)
                return null;
            var army = other.Armies.FirstOrDefault(a => a.RegionId == move.From);
            var chosen = move.UnitIds.Concat(move.CharacterIds).ToHashSet();
            if (army == null || !chosen.All(id => army.UnitIds.Contains(id) || army.CharacterIds.Contains(id)))
                return null;
            var trial = draft.Fork();
            DraftGesture.Choose(trial, view, other.Kind);
            trial.Change(p =>
            {
                p.Moves.Add(new ArmyMoveChoice
                {
                    From = move.From,
                    UnitIds = army.UnitIds.Where(chosen.Contains).ToList(),
                    CharacterIds = army.CharacterIds.Where(chosen.Contains).ToList()
                });
                trial.MoveFocus = 0;
            });
            return trial;
        }

        private static ActionOption Current(PlayerView view, TurnDraft draft) => DraftOptions.For(view, draft);

        // The event's other army step (Attack while moving, Move while attacking), when the server offers it now.
        private static ActionOption Other(PlayerView view, TurnDraft draft) => view.Options.FirstOrDefault(o => o.Enabled &&
            o.ArmyEvent != null && o.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy && o.Kind != draft.Kind);

        private static string Label(CommandKind kind) => kind switch
        {
            CommandKind.MoveArmies => "Move armies",
            CommandKind.AttackArmy => "Attack",
            CommandKind.FinishArmyEvent => "Skip maneuver",
            _ => DecisionTray.Label(kind)
        };

        // UI-420 (owner): an army event's other step says what it does with the card's army step. UI-431: only the decision
        // window's fallback, while the console does not hold the event, still shows these buttons.
        public static string Tooltip(PlayerView view, ActionOption option)
        {
            if (!option.Enabled)
                return TooltipText.Parts(Label(option.Kind), option.Reason);
            var card = ConfirmSummary.Source(view);
            var step = card.Length > 0 ? card + "'s army step" : "the army step";
            return Label(option.Kind) + " · " + option.Kind switch
            {
                CommandKind.FinishArmyEvent => "End " + step + " without moving or attacking",
                CommandKind.AttackArmy => "Use " + step + " to attack instead",
                CommandKind.MoveArmies => "Use " + step + " to move instead",
                _ => "Use " + step + " this way"
            };
        }

        // UI-431: the hint names both steps' targets; before an army is chosen the console adds the ✓'s Finish.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            var move = ArmyDraft.Current(draft);
            if (move == null)
                return Steps(view, draft) + ".";
            if (move.To.Length == 0 && OtherTargets(view, draft).Any())
                return draft.Kind == CommandKind.AttackArmy ? "Click a red region to attack or a lit destination to move"
                    : "Click a lit destination to move or a red region to attack";
            var hint = BoardDraft.Hint(draft, view);
            return hint.Length > 0 ? hint : Steps(view, draft) + " · ✓ confirms.";
        }

        private static string Steps(PlayerView view, TurnDraft draft) =>
            Other(view, draft) != null ? "Click a lit army, then where it moves or a red region it attacks"
            : draft.Kind == CommandKind.AttackArmy ? "Click a lit army, then the region it attacks"
            : "Click a lit army, then a lit destination";
    }
}
