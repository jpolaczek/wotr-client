using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public sealed class BattleWindowHover
    {
        public BattleWindowHover(VisualElement root, MapCanvas map, RegionalBattlePanel battle, CardInspector cards)
        {
            battle.Root.RegisterCallback<KeyDownEvent>(e =>
            {
                if (e.keyCode == KeyCode.Escape && !FloatingWindow.IsDragging(e.target as VisualElement))
                {
                    battle.Close();
                    e.StopImmediatePropagation();
                }
            });
        }
    }
}
