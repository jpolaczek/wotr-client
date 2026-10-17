using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class EventArmySummary
    {
        public static string Describe(ActionPreview preview, BoardPresentation data)
        {
            var parts = Consequences(preview, data);
            return parts.Count > 0 ? string.Join(" · ", parts) : "Move the selected army along this route · No extra die.";
        }

        public static string DescribeLanding(ActionPreview preview, BoardPresentation data)
        {
            var parts = Consequences(preview, data);
            return (preview.HasUnknownResult ? "Land before battle" : "Land in the chosen region") + " · No extra die." +
                (parts.Count > 0 ? " " + string.Join(" · ", parts) + "." : "");
        }

        private static List<string> Consequences(ActionPreview preview, BoardPresentation data)
        {
            var parts = new List<string>();
            if (preview.RemovedIds.Count > 0)
                parts.Add("Leaders left without an army will be lost");
            foreach (var change in preview.ControlChanges)
                parts.Add(data.Regions[change.Key].Name + " → " + BoardScreen.SideName(change.Value.Value));
            if (preview.Excess.Count > 0)
                parts.Add("Then return " + preview.Excess.Sum(e => e.Count) + " excess units");
            return parts;
        }
    }
}
