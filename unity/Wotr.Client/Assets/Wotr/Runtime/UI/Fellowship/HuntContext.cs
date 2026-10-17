using System.Linq;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class HuntContext
    {
        public static void Add(VisualElement root, PlayerView view, BoardPresentation data)
        {
            var hunt = view.Hunt;
            if (hunt == null)
                return;
            var row = BoardScreen.Row("hunt-context");
            row.name = "hunt-context";
            root.Add(row);
            var source = data.Regions.TryGetValue(hunt.SourceId, out var region) ? "Extra Hunt · " + region.Name
                : hunt.SourceId == "fellowship-move" ? hunt.Mordor ? "Mordor Hunt" : "Hunt" : CardLibrary.Title(hunt.SourceId);
            row.Add(new Label(source) { name = "hunt-source" });
            var tile = new Label { name = "hunt-tile" };
            tile.AddToClassList("hunt-tile");
            data.HuntTiles.TryGetValue(hunt.TileId, out var definition);
            var smeagol = hunt.TileId.StartsWith("lords.hunt.smeagol.", System.StringComparison.Ordinal);
            var face = smeagol ? "Sméagol" : definition == null ? "?" : FigureGrid.Name(definition.Value);
            tile.text = hunt.TileId.Length == 0 ? "▧ Undrawn" : "◆ " + face +
                (smeagol ? "" : hunt.SeparatesCompanions ? hunt.DamagePending ? " · Separation ?" : " · Separate " + System.Math.Min(hunt.Damage, view.Fellowship.CompanionCount) :
                    hunt.DamagePending ? " · Damage ?" : definition?.Value != hunt.Damage.ToString() ? " → " + hunt.Damage + " damage" : "") +
                (hunt.Reveal ? " · Reveal" : "") + (hunt.Stop ? " · Stop" : "") + (hunt.CorruptionOnly ? " · Corruption" : "");
            tile.tooltip = hunt.TileId.Length == 0 ? "The next tile is unknown" : "Drawn tile · Remaining Hunt damage" +
                (hunt.DamagePending ? " · Resolve tile replacement before determining damage" : "") +
                (hunt.TileDie.HasValue ? " · Die " + hunt.TileDie : "") + (hunt.Mordor ? " · Eye value " + hunt.EyeDamage : "") +
                (hunt.CorruptionOnly ? " · All damage becomes Corruption; damage reduction and Companion casualties are unavailable" : "");
            row.Add(tile);
            var pool = new Label { name = "hunt-pool", text = "▧ " + hunt.PoolCount };
            pool.tooltip = hunt.PoolCount == 0 ? "Pool empty · Eligible standard discards refill it on the next draw" : "Tiles in the Hunt pool";
            row.Add(pool);
            row.tooltip = (hunt.Dice.Count > 0 ? "Roll: " + string.Join(", ", hunt.Dice.Select(d => d.Rerolled ? d.First + " → " + d.Result : d.Result.ToString())) +
                " · Bonus +" + hunt.Bonus + "\n" : "") + "Reroll sources: " + string.Join(", ", hunt.RerollSources);
        }
    }
}
