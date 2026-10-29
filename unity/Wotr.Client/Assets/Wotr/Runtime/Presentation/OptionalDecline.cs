using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-347 (EXP-15 D7, owner's decision): an optional decline of the Lords has no button of its own. The console's Pass
    // sends it, as it declines a reaction window (UI-291), lights while it can, and its tooltip and the console's hint name
    // what passing skips: Vilya's Spend the die, the Lore of the Elven Rings' Keep results, Wind from the West's and the
    // Balrog card's Finish, Orderly Withdrawal's Keep the hit and We Won't Go Back's No Corruption. UI-348: Palantír of
    // Anárion's Skip extra card and a Combat card replacement's Keep card effects (Brand, the Dark Chieftains), whose ids
    // are generic and count only in their own steps. UI-276: Dismay's Keep tokens. UI-278: the kings' Keep tiles after an
    // inspection (Wisdom of Elrond, It Is Not So Dark Here) and after a political cleanse.
    public static class OptionalDecline
    {
        public const string BalrogFinish = "balrog.finish";
        public const string WindFinish = "wind.finish";
        public const string WithdrawalKeep = "withdrawal.decline";
        private const string VilyaSkip = "spend the die and keep Vilya";
        private const string DismaySkip = "keep your Dismay tokens";

        // What passing does, completing "Pass to …".
        private static readonly IReadOnlyDictionary<string, string> Skips = new Dictionary<string, string>
        {
            [LoreChoice.Keep] = "keep your dice as they are",
            [BalrogFinish] = "finish without moving the Balrog",
            [WindFinish] = "finish without inspecting the Shadow hand",
            [WithdrawalKeep] = "keep the hit and fight on",
            [HuntCardResponses.NoCorruption] = "take no Corruption and keep your cards"
        };

        // Declines with a generic id, by the presentation of their step.
        private static readonly IReadOnlyDictionary<(string Presentation, string Id), string> StepSkips =
            new Dictionary<(string, string), string>
            {
                [("Deck", "decline")] = "complete the action without an extra card",
                [(CombatReplacementChoice.Presentation, CombatReplacementChoice.Keep)] = "keep your card's effects",
                [(SovereignChoices.Inspection, SovereignChoices.Keep)] = "finish looking without returning a tile",
                [(SovereignChoices.Removal, SovereignChoices.Decline)] = "keep the king's tiles in place"
            };

        // This effect choice is sent by the console's Pass, so its old host (console, tray, Hunt row) leaves it out.
        public static bool Declines(PlayerView view, string id) => Skip(view, id).Length > 0;

        private static string Skip(PlayerView view, string id)
        {
            if (Skips.TryGetValue(id, out var skip))
                return skip;
            return StepSkips.TryGetValue((view?.Decision?.Presentation ?? "", id), out var step) ? step : "";
        }

        // The decline the console's Pass sends now, or null.
        public static ConsoleOption Option(PlayerView view, TurnDraft draft)
        {
            if (NamedRings.VilyaDecision(view))
                return Vilya(view);
            if (view?.Decision?.Kind == DecisionKind.Dismay && view.DecisionOwner == view.You && !view.Winner.HasValue)
                return Dismay(view);
            var option = Choice(view);
            var id = option?.EligibleIds.FirstOrDefault(candidate => Declines(view, candidate));
            if (id == null)
                return null;
            return new ConsoleOption
            {
                Id = id,
                Name = "decline-" + id,
                Label = "Pass",
                Tooltip = Tooltip(Skip(view, id)),
                Kind = ConsoleOptionKind.Effect
            };
        }

        // UI-419 (owner): the battle window's Skip effect names the effect it skips and what skipping does: a Combat card
        // replacement's ability (Red Wrath), a card's combat side in a battle (Orderly Withdrawal), otherwise its card.
        public static string SkipTooltip(PlayerView view, TurnDraft draft)
        {
            var decline = Option(view, draft);
            if (decline == null)
                return "";
            var skip = Skip(view, decline.Id);
            return "Skip " + EffectName(view) + " · " + (skip.Length > 0 ? char.ToUpperInvariant(skip[0]) + skip.Substring(1) : decline.Tooltip);
        }

        private static string EffectName(PlayerView view)
        {
            if (CombatReplacementChoice.Chips(view).FirstOrDefault() is { } chip &&
                CombatReplacementChoice.Ability(view, chip.SourceId) is { } ability)
                return ability.Title;
            var source = view?.Decision?.SourceId ?? "";
            if (CardLibrary.Cards.TryGetValue(source, out var card))
                return view.Battle != null && card.CombatTitle.Length > 0 ? card.CombatTitle : card.Title;
            return "this effect";
        }

        // The console's hint ends with what Pass does whenever it sends a decline.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            var decline = Option(view, draft);
            if (decline == null || !decline.Enabled)
                return "";
            return "Or Pass to " + decline.Command switch
            {
                CommandKind.DeclineVilya => VilyaSkip,
                CommandKind.DeclineDismay => DismaySkip,
                _ => Skip(view, decline.Id)
            } + ".";
        }

        private static ConsoleOption Vilya(PlayerView view)
        {
            var spend = NamedRings.SpendTheDie(view);
            if (spend == null)
                return null;
            spend.Label = "Pass";
            spend.Tooltip = spend.Enabled ? Tooltip(VilyaSkip) : "Pass · " + spend.Tooltip;
            return spend;
        }

        private static ConsoleOption Dismay(PlayerView view)
        {
            var keep = view.Options.FirstOrDefault(o => o.Kind == CommandKind.DeclineDismay);
            return keep == null ? null : new ConsoleOption
            {
                Id = "keep-tokens",
                Name = "action-DeclineDismay",
                Label = "Pass",
                Tooltip = keep.Enabled ? Tooltip(DismaySkip) : "Pass · " + keep.Reason,
                Kind = ConsoleOptionKind.Action,
                Command = CommandKind.DeclineDismay,
                Enabled = keep.Enabled
            };
        }

        private static string Tooltip(string skip) => "Pass · " + char.ToUpperInvariant(skip[0]) + skip.Substring(1);

        private static ActionOption Choice(PlayerView view) =>
            view?.Decision?.Kind is DecisionKind.EffectChoice or DecisionKind.CharacterEvent && view.DecisionOwner == view.You &&
            !view.Winner.HasValue ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;
    }
}
