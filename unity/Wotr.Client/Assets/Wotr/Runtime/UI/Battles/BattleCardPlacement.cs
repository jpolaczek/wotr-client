using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // Automatic placement shares space; a player's saved window positions still take priority.
    internal sealed class BattleCardPlacement
    {
        private readonly CardInspector cards;
        private readonly RegionalBattlePanel battle;

        internal BattleCardPlacement(CardInspector cards, RegionalBattlePanel battle)
        {
            this.cards = cards;
            this.battle = battle;
            cards.Root.RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            battle.Root.RegisterCallback<GeometryChangedEvent>(_ => Refresh());
            battle.Closed += Refresh;
        }

        private void Refresh()
        {
            var shown = battle.IsOpen && cards.Root.resolvedStyle.display != DisplayStyle.None;
            var width = shown ? cards.Root.layout.width : 0;
            if (float.IsNaN(width))
                return;
            cards.SetDefaultAnchor(new Vector2(shown ? 8 : 342, 8));
            battle.CardPreview(width);
        }
    }
}
