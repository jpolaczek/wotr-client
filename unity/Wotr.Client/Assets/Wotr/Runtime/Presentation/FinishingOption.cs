using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-430 (owner's rule, 2026-10-30): a Finish that ends a card's event has no button of its own. The console's ✓ (the
    // standard confirm choice) sends it while nothing else is chosen — enabled, lit and pulsing — and its tooltip names the
    // card and what finishing leaves undone: The Nazgûl Strike!'s Finish, There Is Another Way's Stay and, with Lords, Wind
    // from the West's and A Balrog is Come!'s Finish. A decline of a response stays the console's Pass and a window's Skip
    // effect (OptionalDecline, ReactionSkip). UI-431: so is a card army event's Skip maneuver (FinishArmyEvent: The Black
    // Captain Commands, The Ringwraiths Are Abroad, A Balrog is Come!'s army step), while no army is chosen.
    public static class FinishingOption
    {
        public const string StrikeFinish = "nazgul-strike.decline";
        public const string AnotherWayStay = "fellowship.stay";
        public const string WindFinish = "wind.finish";
        public const string BalrogFinish = "balrog.finish";
        // UI-431: the army event's Skip maneuver, a command of its own rather than an effect choice.
        public const string ArmyStepFinish = nameof(CommandKind.FinishArmyEvent);

        // What finishing leaves undone, completing "Finish · End <card> …".
        private static readonly IReadOnlyDictionary<string, string> Ends = new Dictionary<string, string>
        {
            [StrikeFinish] = "without discarding a table card or rolling for the Hunt",
            [AnotherWayStay] = "without moving or hiding the Fellowship",
            [WindFinish] = "without inspecting the Shadow hand",
            [BalrogFinish] = "without moving the Balrog",
            [ArmyStepFinish] = "without moving or attacking"
        };

        // This effect choice is sent by the console's ✓, so no console button stands for it.
        public static bool Finishes(string id) => Ends.ContainsKey(id);

        // The finishing choice the console's ✓ sends now, or null: the player's own card decision offers it and nothing
        // else is chosen yet.
        public static ConsoleOption Option(PlayerView view, TurnDraft draft)
        {
            if (ArmyStep(view, draft) is { } army)
                return army;
            var option = Choice(view, draft);
            var id = option?.EligibleIds.FirstOrDefault(Finishes);
            if (id == null || draft.Payload.Ids.Count > 0)
                return null;
            return new ConsoleOption
            {
                Id = id,
                Name = "finish-" + id,
                Label = "Finish",
                Tooltip = Tooltip(view, id),
                Kind = ConsoleOptionKind.Effect,
                Enabled = EffectChoiceDraft.CanChoose(draft, option, id)
            };
        }

        // The console's hint ends with what ✓ does whenever it sends a finish, as OptionalDecline.Hint does for the Pass.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            var finish = Option(view, draft);
            return finish == null || !finish.Enabled ? "" : "Or ✓ to finish " + Ends[finish.Id] + ".";
        }

        // UI-420 (TOOLTIP_AUDIT rule 1): Finish names the card it ends and what finishing leaves undone.
        public static string Tooltip(PlayerView view, string id)
        {
            var card = ConfirmSummary.Source(view);
            return "Finish · End " + (card.Length > 0 ? card : "this effect") + " " + Ends[id];
        }

        // UI-431: the army event's Finish while no army is chosen; once one is, the ✓ confirms its step instead.
        private static ConsoleOption ArmyStep(PlayerView view, TurnDraft draft)
        {
            if (view == null || draft == null || view.Winner.HasValue || !CardArmyEvent.Active(view, draft) || !CardArmyEvent.NothingPlanned(draft))
                return null;
            var finish = view.Options.FirstOrDefault(o => o.Kind == CommandKind.FinishArmyEvent);
            if (finish == null)
                return null;
            return new ConsoleOption
            {
                Id = ArmyStepFinish,
                Name = "finish-" + ArmyStepFinish,
                Label = "Finish",
                Tooltip = finish.Enabled ? Tooltip(view, ArmyStepFinish) : TooltipText.Parts("Finish", finish.Reason),
                Kind = ConsoleOptionKind.Action,
                Command = CommandKind.FinishArmyEvent,
                Enabled = finish.Enabled
            };
        }

        private static ActionOption Choice(PlayerView view, TurnDraft draft) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.DecisionOwner == view.You && !view.Winner.HasValue &&
            draft?.Kind == CommandKind.ChooseEffect
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect)
                : null;
    }
}
