using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-162: the Hunt window's stage ribbon, drawn like the battle's (UI-115): Roll › Re-roll › Tile › Damage › Result,
    // or Tile › Damage › Step in Mordor. The current stage carries the gilt marker behind its icon.
    public static class HuntStages
    {
        public static VisualElement Create(PlayerView view, HuntView hunt)
        {
            var row = BoardScreen.Row("battle-stages");
            row.AddToClassList("hunt-stages");
            row.name = "hunt-stages";
            var stages = HuntWindowState.Stages(hunt);
            var current = HuntWindowState.Index(view, hunt);
            for (var i = 0; i < stages.Count; i++)
            {
                var stage = new VisualElement { name = "hunt-stage-" + stages[i].Key, pickingMode = PickingMode.Ignore };
                stage.AddToClassList("battle-stage");
                stage.EnableInClassList("current-stage", i == current);
                var marker = new VisualElement { name = "hunt-stage-marker", pickingMode = PickingMode.Ignore };
                marker.AddToClassList("battle-stage-marker");
                stage.Add(marker);
                var icon = new VisualElement { name = "hunt-stage-icon", pickingMode = PickingMode.Ignore };
                icon.AddToClassList("battle-stage-icon");
                icon.AddToClassList("hunt-stage-icon-" + stages[i].Key);
                stage.Add(icon);
                stage.Add(new Label(stages[i].Title) { pickingMode = PickingMode.Ignore, enableRichText = false });
                row.Add(stage);
            }
            row.tooltip = hunt?.Mordor == true ? "Hunt in Mordor · No Hunt roll · BASE p. 43" : "Stages of the Hunt · BASE p. 38–40";
            return row;
        }
    }
}
