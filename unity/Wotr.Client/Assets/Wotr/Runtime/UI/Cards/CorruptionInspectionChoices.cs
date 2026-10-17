using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class CorruptionInspectionChoices
    {
        public static Action Render(VisualElement root, PlayerView view, ActionOption option, TurnDraft draft, Action<Action> edit)
        {
            var row = BoardScreen.Row();
            row.name = "inspected-corruption";
            root.Add(row);
            var sovereign = view.Sovereigns.FirstOrDefault(s => option.EffectChoices.Any(c => c.TargetId == s.Id));
            if (sovereign != null)
                row.Add(new Label(sovereign.Title));
            var tiles = view.Sovereigns.SelectMany(s => s.Tiles).Where(t => t.TileId.Length > 0)
                .ToDictionary(t => t.TileId);
            var refresh = new List<Action>();
            foreach (var choice in option.EffectChoices)
            {
                var tile = tiles.TryGetValue(choice.Id, out var found) ? found : null;
                var label = tile == null ? choice.Title : tile.Kings ? "♛" : tile.Value + (tile.Reveal ? " ◉" : "");
                var button = BoardScreen.Button(label, () => edit(() => CardDraft.Choose(draft, option, choice.Id)));
                button.name = "effect-choice-" + choice.Id;
                button.tooltip = choice.Title + " · " + choice.Detail;
                if (tile != null)
                    button.AddToClassList("effect-companion");
                row.Add(button);
                refresh.Add(() =>
                {
                    button.EnableInClassList("selected", draft.Payload.Ids.Contains(choice.Id));
                    button.SetEnabled(EffectChoiceDraft.CanChoose(draft, option, choice.Id));
                });
            }
            return () => { foreach (var update in refresh) update(); };
        }
    }
}
