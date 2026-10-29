using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-216 (CARD_CHOICES_UX): a card's army event on the map, as with an Army die (UI-105). The armies the event may
    // command glow; a click on one starts its move with the whole army and opens its panel, whose lit figures toggle;
    // a lit destination completes the move, and another lit army starts the next. The console counts the armies,
    // offers the event's alternatives (Skip maneuver) and its ✓ sends.
    public static class CardArmyEvent
    {
        private static readonly IReadOnlyList<ConsoleOption> None = new List<ConsoleOption>();

        public static bool Active(PlayerView view, TurnDraft draft) => CardDecisions.Owns(view, draft) &&
            view.Decision.Kind == DecisionKind.ArmyEvent && draft.Kind is CommandKind.MoveArmies or CommandKind.AttackArmy;

        public static IEnumerable<ArmyOption> Sources(PlayerView view, TurnDraft draft) =>
            DraftOptions.For(view, draft)?.Armies ?? Enumerable.Empty<ArmyOption>();

        public static bool IsSource(PlayerView view, TurnDraft draft, string region) => Sources(view, draft).Any(a => a.RegionId == region);

        public static bool IsTarget(PlayerView view, TurnDraft draft, string region) => draft.Kind == CommandKind.AttackArmy
            ? AttackDraft.Targets(draft, view).Contains(region)
            : ArmyDraft.Targets(draft, view).Contains(region);

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

        private static ArmyOption Army(PlayerView view, TurnDraft draft)
        {
            var region = ArmyRegion(draft);
            return region.Length == 0 ? null : Sources(view, draft).FirstOrDefault(a => a.RegionId == region);
        }

        // The event's other commands (another kind of army step, Skip maneuver) are console buttons.
        public static IReadOnlyList<ConsoleOption> Alternatives(PlayerView view, TurnDraft draft)
        {
            if (!Active(view, draft))
                return None;
            return view.Options.Where(o => o.Kind != draft.Kind).Select(o => new ConsoleOption
            {
                Id = o.Kind.ToString(),
                Name = "action-" + o.Kind,
                Label = Label(o.Kind),
                Tooltip = Tooltip(view, o),
                Kind = ConsoleOptionKind.Action,
                Command = o.Kind,
                Enabled = o.Enabled
            }).ToList();
        }

        private static string Label(CommandKind kind) => kind switch
        {
            CommandKind.MoveArmies => "Move armies",
            CommandKind.AttackArmy => "Attack",
            CommandKind.FinishArmyEvent => "Skip maneuver",
            _ => DecisionTray.Label(kind)
        };

        // UI-420 (owner): an army event's other step says what it does with the card's army step.
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

        public static string Hint(PlayerView view, TurnDraft draft)
        {
            var hint = BoardDraft.Hint(draft, view);
            return ArmyDraft.Current(draft) == null || hint.Length == 0
                ? (draft.Kind == CommandKind.AttackArmy ? "Click a lit army, then the region it attacks · ✓ confirms."
                    : "Click a lit army, then a lit destination · ✓ confirms.")
                : hint;
        }
    }
}
