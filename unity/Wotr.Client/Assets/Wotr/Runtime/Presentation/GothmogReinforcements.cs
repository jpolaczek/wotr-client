using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-271: Gothmog's "fling into the fray" in the Reserve panel. With a Muster die selected his row offers a Sauron
    // Regular (recruited beside him) and an Elite (replacing a Regular there); a token's click uses the ability at once.
    public static class GothmogReinforcements
    {
        public const string RowKey = "gothmog";
        private const string TokenPrefix = "ability:";

        public static IEnumerable<ReinforcementRow> Rows(PlayerView view, IReadOnlyList<ComponentAction> components, BoardPresentation data)
        {
            var abilities = components.Where(CharacterAbilityPlaces.InReserve).ToList();
            if (abilities.Count == 0)
                yield break;
            var region = data.Regions.TryGetValue(abilities[0].Region, out var definition) ? definition.Name : abilities[0].Region;
            var row = new ReinforcementRow { Key = RowKey, Nation = "sauron", Name = "Gothmog · " + region, Status = "Fling into the fray" };
            row.Figures.Add(Figure(view, abilities, CharacterAbilityPlaces.GothmogRecruit, UnitKind.Regular, region));
            row.Figures.Add(Figure(view, abilities, CharacterAbilityPlaces.GothmogUpgrade, UnitKind.Elite, region));
            ReinforcementPlans.Close(row);
            yield return row;
        }

        // The ability behind a token of Gothmog's row, or null for a reserve figure.
        public static ComponentAction Ability(IEnumerable<ComponentAction> components, string token) =>
            token != null && token.StartsWith(TokenPrefix, System.StringComparison.Ordinal)
                ? components.FirstOrDefault(item => item.Enabled && item.Character?.AbilityId == token.Substring(TokenPrefix.Length))
                : null;

        private static ReinforcementFigure Figure(PlayerView view, List<ComponentAction> abilities, string ability, UnitKind kind, string region)
        {
            var item = abilities.FirstOrDefault(a => a.Character.AbilityId == ability);
            var upgrade = kind == UnitKind.Elite;
            return new ReinforcementFigure
            {
                Kind = kind,
                Reserve = ReinforcementRows.InReserve(view, "sauron", kind),
                ReserveId = item?.Enabled == true ? TokenPrefix + ability : null,
                Hint = upgrade ? "Click to replace a Sauron Regular in " + region + " with an Elite · Gothmog"
                    : "Click to recruit a Sauron Regular in " + region + " · Gothmog",
                Reason = item != null ? "Select a Muster die" : upgrade ? "No Sauron Regular beside Gothmog, or no Elite in reserve"
                    : "No Sauron Regular in reserve"
            };
        }
    }
}
