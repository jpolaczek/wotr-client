using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class HuntTileView
    {
        public Button Root { get; } = BoardScreen.Button("", null);
        public event Action Selected;
        private readonly Label face = new Label { name = "hunt-tile-face", pickingMode = PickingMode.Ignore };
        // UI-141: the drawn tile's artwork; the face's name stays only for an older report without HuntView.Face.
        private readonly HuntTileFace art = new HuntTileFace("large");
        private readonly Label damage = new Label { name = "hunt-damage", pickingMode = PickingMode.Ignore };
        private readonly Label reveal = new Label("◆ Reveal") { name = "hunt-reveal", pickingMode = PickingMode.Ignore };
        private readonly Label stop = new Label("■ Stop") { name = "hunt-stop", pickingMode = PickingMode.Ignore };
        private readonly Dictionary<string, string> faces;

        public HuntTileView(BoardPresentation data)
        {
            faces = data.Board.HuntTiles.ToDictionary(t => t.Id, t => FigureGrid.Name(t.Value));
            Root.name = "hunt-tile";
            Root.AddToClassList("hunt-tile-card");
            Root.userData = new DraftToken(DraftObject.HuntTile);
            Root.clicked += () => Selected?.Invoke();
            art.Root.name = "hunt-tile-art";
            Root.Add(art.Root);
            Root.Add(face);
            var values = BoardScreen.Row("hunt-tile-values");
            values.pickingMode = PickingMode.Ignore;
            values.Add(damage);
            values.Add(reveal);
            values.Add(stop);
            Root.Add(values);
        }

        public void Apply(HuntView hunt)
        {
            Root.style.display = hunt?.TileId.Length > 0 ? DisplayStyle.Flex : DisplayStyle.None;
            if (hunt == null || hunt.TileId.Length == 0)
                return;
            face.text = hunt.TileId.StartsWith("lords.hunt.smeagol.", StringComparison.Ordinal) ? "Sméagol" :
                faces.TryGetValue(hunt.TileId, out var text) ? text : "Hunt tile";
            art.Root.style.display = hunt.Face == null ? DisplayStyle.None : DisplayStyle.Flex;
            face.style.display = hunt.Face == null ? DisplayStyle.Flex : DisplayStyle.None;
            if (hunt.Face != null)
                art.Apply(hunt.Face);
            var damageLabel = hunt.SeparatesCompanions ? "Separate " : hunt.CorruptionOnly ? "Corruption " : "Damage ";
            damage.text = hunt.DamagePending ? damageLabel + "?"
                : hunt.Healing > 0 ? "Heal " + hunt.Healing : damageLabel + hunt.Damage;
            reveal.style.display = hunt.Reveal ? DisplayStyle.Flex : DisplayStyle.None;
            stop.style.display = hunt.Stop ? DisplayStyle.Flex : DisplayStyle.None;
            Root.tooltip = (hunt.Face == null ? "" : HuntTileText.Tooltip(hunt.Face) + "\n\n") + "Drawn tile · " + damage.text +
                (hunt.DamagePending ? "\nResolve replacement before determining damage." : "") +
                (hunt.TileDie.HasValue ? "\nTile die: " + hunt.TileDie : "") +
                (hunt.Mordor ? "\nMordor · Eye value " + hunt.EyeDamage : "") +
                (hunt.CorruptionOnly ? "\nAll damage becomes Corruption; no damage reduction or casualty." : "") +
                "\nReveal and Stop are independent effects." +
                (hunt.Mordor ? "\nReveal changes hidden status; it does not create a map route." +
                    "\nStop prevents this move. Replacing or cancelling the tile removes its effects." +
                    "\nResolve the tile before any advance; 12 Corruption ends the game first." : "");
        }
    }
}
