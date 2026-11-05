using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-436: where a character or a sovereign is. Key names its picture (Art/Places/<key>, scripts/export_place_pictures.py):
    // a region's id, or one of the places off the map. Caption names the place under the picture, Meaning says in words
    // what being there means, and RegionId is the region a click shows on the map (empty off the map).
    public sealed class PlaceView
    {
        public string Key { get; set; } = "";
        public string Caption { get; set; } = "";
        public string Meaning { get; set; } = "";
        public string RegionId { get; set; } = "";
        public bool OnMap => RegionId.Length > 0;
        public string Tooltip => OnMap ? TooltipText.Lines(Caption, "Click to show this region on the map")
            : TooltipText.Lines(Caption, Meaning, "Not on the map");
    }

    // Presentation only: the place follows the public projection (the roster's lifecycle and region, a king's status and
    // region); it grants nothing.
    public static class PlacePresentation
    {
        public const string Folder = "Wotr/Art/Places/";
        public const string Fellowship = "fellowship";
        public const string RemovedSovereign = "removed-sovereign";
        private const string NotInPlay = "not-in-play-";
        private const string Removed = "removed-";

        // Every place off the map that has a picture; with the regions they are every place a figure can be. No character
        // stands on the Mordor track: only the Fellowship does, its Companions inside it (InFellowship).
        public static IReadOnlyList<string> SpecialPlaces { get; } = new[]
        {
            Fellowship, NotInPlay + "free-peoples", NotInPlay + "shadow", Removed + "free-peoples", Removed + "shadow", RemovedSovereign
        };

        public static string Resource(string key) => Folder + key;

        // UI-449 (owner): where a Characters row's click brings the camera — the region of a character on the board, the
        // Fellowship's for a companion inside it (as this player sees it); "" for one off the board.
        public static string CameraRegion(PlayerView view, CharacterRosterView row, IReadOnlyDictionary<string, RegionDefinition> regions)
        {
            var place = For(row, regions);
            if (place.OnMap)
                return place.RegionId;
            var fellowship = view?.Fellowship?.RegionId ?? "";
            return row.Lifecycle == CharacterRosterLifecycle.InFellowship && regions.ContainsKey(fellowship) ? fellowship : "";
        }

        public static PlaceView For(CharacterRosterView row, IReadOnlyDictionary<string, RegionDefinition> regions)
        {
            if (row.Lifecycle == CharacterRosterLifecycle.OnBoard && regions.TryGetValue(row.RegionId, out var region))
                return OnBoard(region, row.Garrison);
            var side = SideKey(row.Side);
            return row.Lifecycle switch
            {
                CharacterRosterLifecycle.InFellowship => Off(Fellowship, "The Fellowship", "Travels with the Fellowship"),
                CharacterRosterLifecycle.AwaitingPlacement => Off(Fellowship, "The Fellowship", "Leaves the Fellowship; placed on the map next"),
                CharacterRosterLifecycle.Eliminated => Off(Removed + side, "Removed", "Eliminated; out of the game"),
                CharacterRosterLifecycle.EliminatedWithReplacement => Off(Removed + side, "Removed", ReturnMeaning(row)),
                CharacterRosterLifecycle.AwaitingReturn => Off(NotInPlay + side, "Not in play", "Returns to play now"),
                CharacterRosterLifecycle.NotEntered => Off(NotInPlay + side, "Not in play", "Not entered yet"),
                _ => Off(NotInPlay + side, "Not in play", "Not on the board")
            };
        }

        // An Initial king leads his force, a Corrupted one stays where he fell; a removed king is out of play.
        public static PlaceView For(SovereignView king, IReadOnlyDictionary<string, RegionDefinition> regions)
        {
            if (king.Status != SovereignStatus.Removed && regions.TryGetValue(king.RegionId, out var region))
                return OnBoard(region, king.Garrison);
            return Off(RemovedSovereign, "Out of play", "Removed from play");
        }

        private static PlaceView OnBoard(RegionDefinition region, bool garrison) => new PlaceView
        {
            Key = region.Id, Caption = region.Name, RegionId = region.Id,
            Meaning = garrison ? "On the board · In the stronghold's garrison" : "On the board"
        };

        private static PlaceView Off(string key, string caption, string meaning) =>
            new PlaceView { Key = key, Caption = caption, Meaning = meaning };

        private static string SideKey(Side side) => side == Side.Shadow ? "shadow" : "free-peoples";

        // Gandalf the Grey eliminated may come back as his next version.
        private static string ReturnMeaning(CharacterRosterView row) => row.FutureVersions.Count > 0
            ? "Eliminated; may return as " + CharacterLibrary.FullName(row.FutureVersions[0].DefinitionId)
            : "Eliminated; may return";
    }
}
