using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-448 (owner, 2026-11-03: "cancel to the bottom panel as a general option, at the very bottom"): while a Fellowship
    // route, a declared or revealed position or a separation is drawn, Cancel is the console's last option; its click clears
    // what is drawn (FellowshipDraft.CancelRoute) and it waits until there is something to clear (UI-298's forced
    // separation starts with its companions chosen).
    public static class RouteCancel
    {
        public const string Name = "console-cancel-route";

        public static IReadOnlyList<ConsoleOption> Append(IReadOnlyList<ConsoleOption> options, PlayerView view, TurnDraft draft)
        {
            var cancel = Option(view, draft);
            return cancel == null ? options : options.Concat(new[] { cancel }).ToList();
        }

        public static ConsoleOption Option(PlayerView view, TurnDraft draft)
        {
            if (view == null || draft == null || !FellowshipDraft.SingleRoute(draft.Kind))
                return null;
            return new ConsoleOption
            {
                Id = Name,
                Name = Name,
                Label = "Cancel",
                Kind = ConsoleOptionKind.CancelRoute,
                Enabled = FellowshipDraft.CancelChanges(draft, view),
                Tooltip = draft.Kind == CommandKind.SeparateCompanions
                    ? "Cancel · Clear the chosen companions and their route"
                    : "Cancel · Clear the drawn route"
            };
        }
    }
}
