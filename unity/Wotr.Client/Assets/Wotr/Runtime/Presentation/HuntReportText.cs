using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    public static class HuntReportText
    {
        public static string Entry(HuntReportEntryView entry, BoardPresentation data, string face = "")
        {
            var hunt = entry.Hunt;
            var name = CharacterLibrary.Name(entry.CharacterId);
            return entry.Kind switch
            {
                HuntReportKind.Started => "Hunt started · " + Source(entry.SourceId, data),
                HuntReportKind.Rolled => "Roll · " + Roll(hunt),
                HuntReportKind.Rerolled => "Reroll · " + Roll(hunt),
                HuntReportKind.TileDrawn => "Tile drawn · " + face + " · " + Tile(hunt),
                HuntReportKind.TileReplaced => "Tile replaced · " + face + " · " + Tile(hunt),
                HuntReportKind.DamageDetermined or HuntReportKind.DamageChanged => face + " · " + Tile(hunt),
                HuntReportKind.Response => entry.Response == HuntResponse.Ring ? "Use the Ring" :
                    entry.Response == HuntResponse.Guide ? "Guide casualty" : "Random companion drawn",
                HuntReportKind.Reaction => Source(entry.SourceId, data),
                HuntReportKind.CompanionLeft => name + " left the Fellowship",
                HuntReportKind.CompanionPlaced => name + " → " + Region(entry.Destination, data),
                HuntReportKind.CharacterEliminated => name + " eliminated",
                HuntReportKind.GuideChanged => "Guide · " + CharacterLibrary.Name(entry.Outcome.GuideId),
                HuntReportKind.Corruption => "Corruption " + entry.Outcome.Corruption + "/12",
                HuntReportKind.Revealed => "Revealed · " + Region(entry.Outcome.RegionId, data),
                HuntReportKind.Progressed => entry.Outcome.MordorStep.HasValue ? "Mordor " + entry.Outcome.MordorStep + "/5" : "Progress " + entry.Outcome.Progress,
                HuntReportKind.HuntCompleted => "Hunt resolved · Corruption " + entry.Outcome.Corruption + "/12",
                _ => "Hunt"
            };
        }

        public static string Source(string id, BoardPresentation data) => id == "fellowship-move" ? "Fellowship movement" :
            data.Regions.TryGetValue(id, out var region) ? "Extra Hunt · " + region.Name : CardLibrary.Title(id);

        private static string Region(string id, BoardPresentation data) => data.Regions.TryGetValue(id, out var region) ? region.Name : id;
        private static string Roll(HuntView hunt) => hunt == null ? "No recorded roll" :
            hunt.Dice.Count + " dice · " + hunt.Dice.Count(d => d.Success) + " hits";
        private static string Tile(HuntView hunt) => hunt == null ? "No recorded tile" :
            (hunt.DamagePending ? "Damage ?" : hunt.Healing > 0 ? "Heal " + hunt.Healing : hunt.SeparatesCompanions ? "Separate " + hunt.Damage : "Damage " + hunt.Damage) +
            (hunt.Reveal ? " · Reveal" : "") + (hunt.Stop ? " · Stop" : "");
    }
}
