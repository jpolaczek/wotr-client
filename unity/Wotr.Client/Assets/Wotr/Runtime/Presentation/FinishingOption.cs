using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-430 (owner's rule, 2026-10-30): a Finish that ends a card's event has no button of its own. The console's ✓ (the
    // standard confirm choice) sends it while nothing else is chosen — enabled, lit and pulsing — and its tooltip names the
    // card and what finishing leaves undone: The Nazgûl Strike!'s Finish and There Is Another Way's Stay. A decline of a
    // response stays the console's Pass and a window's Skip effect (ReactionSkip).
    public static class FinishingOption
    {
        public const string StrikeFinish = "nazgul-strike.decline";
        public const string AnotherWayStay = "fellowship.stay";

        // What finishing leaves undone, completing "Finish · End <card> …".
        private static readonly IReadOnlyDictionary<string, string> Ends = new Dictionary<string, string>
        {
            [StrikeFinish] = "without discarding a table card or rolling for the Hunt",
            [AnotherWayStay] = "without moving or hiding the Fellowship"
        };

        // This effect choice is sent by the console's ✓, so no console button stands for it.
        public static bool Finishes(string id) => Ends.ContainsKey(id);

        // The finishing choice the console's ✓ sends now, or null: the player's own card decision offers it and nothing
        // else is chosen yet.
        public static ConsoleOption Option(PlayerView view, TurnDraft draft)
        {
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

        // The console's hint ends with what ✓ does whenever it sends a finish.
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

        private static ActionOption Choice(PlayerView view, TurnDraft draft) =>
            view?.Decision?.Kind == DecisionKind.EffectChoice && view.DecisionOwner == view.You && !view.Winner.HasValue &&
            draft?.Kind == CommandKind.ChooseEffect
                ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect)
                : null;
    }
}
