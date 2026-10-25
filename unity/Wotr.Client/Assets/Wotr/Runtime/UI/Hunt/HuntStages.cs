using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-162: the Hunt window's stage ribbon, drawn like the battle's (UI-115): Roll › Re-roll › Tile › Damage › Result,
    // or Tile › Damage › Step in Mordor. UI-327: one lasting ribbon whose gilt marker glides to the current stage.
    public static class HuntStages
    {
        public static StageRibbon Ribbon()
        {
            var ribbon = new StageRibbon("hunt-stages", "hunt-stage-");
            ribbon.Root.AddToClassList("hunt-stages");
            return ribbon;
        }

        public static void Apply(StageRibbon ribbon, PlayerView view, HuntView hunt) =>
            ribbon.Apply(HuntWindowState.Stages(hunt), HuntWindowState.Index(view, hunt), "hunt-stage-icon-",
                hunt?.Mordor == true ? "Hunt in Mordor · No Hunt roll · BASE p. 43" : "Stages of the Hunt · BASE p. 38–40");
    }
}
