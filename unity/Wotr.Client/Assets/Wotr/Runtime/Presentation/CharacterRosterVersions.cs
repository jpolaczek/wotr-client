using System.Collections.Generic;
using System.Linq;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-271: a Shadow character whose versions are alternatives rather than a progression (the Lords Mouth of Sauron
    // and Witch-king beside their base cards) lists each version not in play as its own row in the Characters panel.
    // Such a row's key names the figure and the version; every other row keeps the figure's id.
    public static class CharacterRosterVersions
    {
        public const char Separator = '@';

        public static string Key(string figure, string version) => figure + Separator + version;

        public static string Figure(string key)
        {
            var index = key.IndexOf(Separator);
            return index < 0 ? key : key.Substring(0, index);
        }

        public static string Version(string key)
        {
            var index = key.IndexOf(Separator);
            return index < 0 ? "" : key.Substring(index + 1);
        }

        // The row that offers this figure's version: its own version row when it has one, otherwise the figure's row.
        public static string RowKey(PlayerView view, string figure, string version)
        {
            if (string.IsNullOrEmpty(version) || view?.OwnCharacterRoster == null)
                return figure;
            var row = view.OwnCharacterRoster.FirstOrDefault(item => item.Side == view.You && item.FigureId == figure);
            return row != null && Alternatives(row).Contains(version) ? Key(figure, version) : figure;
        }

        public static List<string> Alternatives(CharacterRosterView row)
        {
            if (row.Side != Side.Shadow || row.FutureVersions.Count == 0)
                return new List<string>();
            var versions = row.FutureVersions.Select(v => v.DefinitionId).ToList();
            if (!string.IsNullOrEmpty(row.CurrentDefinitionId))
                versions.Add(row.CurrentDefinitionId);
            return row.FutureVersions.Select(v => v.DefinitionId)
                .Where(id => versions.Any(other => other != id && Card(other) == Card(id))).ToList();
        }

        public static IEnumerable<CharacterRosterView> Split(CharacterRosterView row)
        {
            var alternatives = Alternatives(row);
            if (alternatives.Count == 0)
            {
                yield return row;
                yield break;
            }
            if (!string.IsNullOrEmpty(row.CurrentDefinitionId))
                yield return Copy(row, row.FigureId, row.FutureVersions.Where(v => !alternatives.Contains(v.DefinitionId)), row.Lifecycle);
            foreach (var version in row.FutureVersions.Where(v => alternatives.Contains(v.DefinitionId)))
                yield return Copy(row, Key(row.FigureId, version.DefinitionId), new[] { version }, CharacterRosterLifecycle.NotEntered);
        }

        // A version row of a figure already in play changes that figure into this version.
        public static bool Switches(CharacterRosterView row) =>
            Version(row.FigureId).Length > 0 && !string.IsNullOrEmpty(row.CurrentDefinitionId);

        private static string Card(string definition) => definition.Substring(definition.LastIndexOf('.') + 1);

        private static CharacterRosterView Copy(CharacterRosterView row, string key, IEnumerable<CharacterEntryVersionView> versions,
            CharacterRosterLifecycle lifecycle)
        {
            var own = key == row.FigureId;
            return new CharacterRosterView
            {
                FigureId = key, Identity = row.Identity, Side = row.Side, CurrentDefinitionId = row.CurrentDefinitionId,
                Title = own ? row.Title : versions.First().Title, Lifecycle = lifecycle,
                RegionId = own ? row.RegionId : "", Garrison = own && row.Garrison,
                Level = own ? row.Level : null, MovementLevel = own ? row.MovementLevel : null,
                CombatLevel = own ? row.CombatLevel : null, Leadership = own ? row.Leadership : null,
                NationIcon = row.NationIcon, UnlimitedMovement = own && row.UnlimitedMovement,
                FutureVersions = versions.ToList()
            };
        }
    }
}
