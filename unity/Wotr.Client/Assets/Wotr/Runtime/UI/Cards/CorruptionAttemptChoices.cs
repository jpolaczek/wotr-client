using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CorruptionAttemptChoices
    {
        public static Action Render(VisualElement root, PlayerView view, ActionOption option, TurnDraft draft, Action<Action> edit, Action<CommandKind> choose = null)
        {
            var attempt = view.CorruptionAttempt;
            var row = BoardScreen.Row();
            row.name = "corruption-attempt";
            root.Add(row);
            if (!SovereignPresentation.OwnsDecision(view, draft))
            {
                var title = view.Sovereigns.FirstOrDefault(s => s.Id == attempt.SovereignId)?.Title ?? "Sovereign";
                row.Add(new Label(title + "  " + string.Join(" ", Enumerable.Range(0, attempt.DrawLimit)
                    .Select(i => i < attempt.DrawnCount ? "●" : "○"))));
            }
            var refresh = new List<Action>();
            foreach (var tile in attempt.OwnChoices)
            {
                var stop = view.Options.FirstOrDefault(o => o.Kind == CommandKind.ChooseCorruptionTile);
                var button = BoardScreen.Button((attempt.Sequential ? "Stop · " : "") + ExpansionLabels.Tile(tile), () =>
                {
                    if (draft.Kind != CommandKind.ChooseCorruptionTile)
                        choose?.Invoke(CommandKind.ChooseCorruptionTile);
                    if (draft.Kind == CommandKind.ChooseCorruptionTile && stop != null)
                        edit(() => CardDraft.Choose(draft, stop, tile.TileId));
                });
                button.name = "corruption-choice-" + tile.TileId;
                button.tooltip = view.Configuration.VisibleCorruption
                    ? "Place this tile face up; return the others to the Hunt Pool."
                    : "Place this tile face down; return the others to the Hunt Pool.";
                button.AddToClassList("effect-companion");
                row.Add(button);
                refresh.Add(() =>
                {
                    button.SetEnabled(stop?.EligibleIds.Contains(tile.TileId) == true);
                    button.EnableInClassList("selected", draft.Payload.Ids.Contains(tile.TileId));
                });
            }
            return () => { foreach (var update in refresh) update(); };
        }
    }
}
