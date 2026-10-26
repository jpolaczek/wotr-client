using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-347 (EXP-15 D7, owner's decision): an optional decline of the Lords has no button of its own. The console's Pass
    // sends it, as it declines a reaction window (UI-291), lights while it can, and its tooltip and the console's hint name
    // what passing skips: Vilya's Spend the die, the Lore of the Elven Rings' Keep results, Wind from the West's and the
    // Balrog card's Finish, Orderly Withdrawal's Keep the hit and We Won't Go Back's No Corruption.
    public static class OptionalDecline
    {
        public const string BalrogFinish = "balrog.finish";
        public const string WindFinish = "wind.finish";
        public const string WithdrawalKeep = "withdrawal.decline";
        private const string VilyaSkip = "spend the die and keep Vilya";

        // What passing does, completing "Pass to …".
        private static readonly IReadOnlyDictionary<string, string> Skips = new Dictionary<string, string>
        {
            [LoreChoice.Keep] = "keep your dice as they are",
            [BalrogFinish] = "finish without moving the Balrog",
            [WindFinish] = "finish without inspecting the Shadow hand",
            [WithdrawalKeep] = "keep the hit and fight on",
            [HuntCardResponses.NoCorruption] = "take no Corruption and keep your cards"
        };

        // This effect choice is sent by the console's Pass, so its old host (console, tray, Hunt row) leaves it out.
        public static bool Declines(string id) => Skips.ContainsKey(id);

        // The decline the console's Pass sends now, or null.
        public static ConsoleOption Option(PlayerView view, TurnDraft draft)
        {
            if (NamedRings.VilyaDecision(view))
                return Vilya(view);
            var option = Choice(view);
            var id = option?.EligibleIds.FirstOrDefault(Declines);
            if (id == null)
                return null;
            return new ConsoleOption
            {
                Id = id,
                Name = "decline-" + id,
                Label = "Pass",
                Tooltip = Tooltip(Skips[id]),
                Kind = ConsoleOptionKind.Effect
            };
        }

        // The console's hint ends with what Pass does whenever it sends a decline.
        public static string Hint(PlayerView view, TurnDraft draft)
        {
            var decline = Option(view, draft);
            if (decline == null || !decline.Enabled)
                return "";
            return "Or Pass to " + (decline.Command == CommandKind.DeclineVilya ? VilyaSkip : Skips[decline.Id]) + ".";
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

        private static string Tooltip(string skip) => "Pass · " + char.ToUpperInvariant(skip[0]) + skip.Substring(1);

        private static ActionOption Choice(PlayerView view) =>
            view?.Decision?.Kind is DecisionKind.EffectChoice or DecisionKind.CharacterEvent && view.DecisionOwner == view.You &&
            !view.Winner.HasValue ? view.Options.FirstOrDefault(o => o.Enabled && o.Kind == CommandKind.ChooseEffect) : null;
    }
}
