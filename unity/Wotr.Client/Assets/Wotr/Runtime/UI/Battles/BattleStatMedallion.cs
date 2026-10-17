using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-115: one statistic of a force as an icon with the server's number; the text explains the modifiers.
    public sealed class BattleStatMedallion : VisualElement
    {
        public BattleStatMedallion(string key, string value, string description)
        {
            name = "battle-stat-" + key;
            AddToClassList("battle-stat");
            pickingMode = PickingMode.Position;
            tooltip = description;
            var icon = new VisualElement { name = "battle-stat-icon", pickingMode = PickingMode.Ignore };
            icon.AddToClassList("battle-stat-icon");
            icon.AddToClassList("battle-stat-icon-" + key);
            Add(icon);
            var number = new Label(value) { name = "battle-stat-value", pickingMode = PickingMode.Ignore, enableRichText = false };
            number.AddToClassList("battle-stat-value");
            Add(number);
        }
    }
}
