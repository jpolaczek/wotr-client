using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BoardFigurePosition
    {
        public const string UnitPrefix = "unit:";
        public const string CharacterPrefix = "character:";
        public const string FellowshipKey = "fellowship";

        public string Key { get; }
        public string Region { get; }
        public Side Side { get; }
        public bool Garrison { get; }
        public FigureArt Appearance { get; }
        public string Identity { get; }
        // UI-404: a unit's nation and kind, so its flight draws the army's own miniature ("" and null for a character).
        public string Nation { get; }
        public UnitKind? UnitKind { get; }
        // UI-404: the figure stands inside the Fellowship's figure (a separating Companion's source), so it flies from the
        // Fellowship's place.
        public bool InFellowship { get; }

        public BoardFigurePosition(string key, string region, Side side, bool garrison,
            FigureArt appearance, string identity = "", string nation = "", UnitKind? unitKind = null,
            bool inFellowship = false)
        {
            Key = key;
            Region = region;
            Side = side;
            Garrison = garrison;
            Appearance = appearance;
            Identity = identity;
            Nation = nation ?? "";
            UnitKind = unitKind;
            InFellowship = inFellowship;
        }

        public bool IsUnit => Key.StartsWith(UnitPrefix, StringComparison.Ordinal);
        public bool IsCharacter => Key.StartsWith(CharacterPrefix, StringComparison.Ordinal);
        public bool IsFellowship => Key == FellowshipKey;

        // The unit's or character's id, without its prefix.
        public string FigureId => IsUnit ? Key.Substring(UnitPrefix.Length)
            : IsCharacter ? Key.Substring(CharacterPrefix.Length) : Key;
    }

    // Copies public positions only. Never retains a mutable PlayerView or a private roster.
    public sealed class BoardFigureSnapshot
    {
        public string MatchId { get; }
        public long Revision { get; }
        public Side Viewer { get; }
        public IReadOnlyDictionary<string, BoardFigurePosition> Figures => figures;
        public IReadOnlyCollection<string> Companions => companions;
        public string FellowshipRegion { get; }
        public bool FellowshipPositionKnown { get; }
        private readonly Dictionary<string, BoardFigurePosition> figures = new Dictionary<string, BoardFigurePosition>();
        private readonly HashSet<string> companions;

        public BoardFigureSnapshot(PlayerView view, Func<string, bool> isRegion)
        {
            MatchId = view.MatchId;
            Revision = view.Revision;
            Viewer = view.You;
            companions = new HashSet<string>(view.Fellowship.CompanionIdentities);
            FellowshipRegion = Region(view.Fellowship, isRegion);
            FellowshipPositionKnown = PositionKnown(view.Fellowship, isRegion);
            foreach (var unit in view.Units)
                if (isRegion(unit.Location))
                    Add(new BoardFigurePosition(BoardFigurePosition.UnitPrefix + unit.Id, unit.Location, unit.Side,
                        unit.Garrison, FigureAppearances.Art(unit), nation: unit.Nation, unitKind: unit.Kind));
            foreach (var character in view.Characters)
                if (isRegion(character.Location))
                    Add(new BoardFigurePosition(BoardFigurePosition.CharacterPrefix + character.Id, character.Location,
                        character.Side, character.Garrison, FigureAppearances.Art(character),
                        CharacterLibrary.Identity(character.DefinitionId)));
            if (FellowshipRegion.Length > 0)
                Add(new BoardFigurePosition(BoardFigurePosition.FellowshipKey, FellowshipRegion, Side.FreePeoples,
                    false, FigureAppearances.Fellowship));
        }

        // The Fellowship's public region off the Mordor track ("" on the track).
        public static string Region(FellowshipView fellowship, Func<string, bool> isRegion) =>
            !fellowship.MordorStep.HasValue && isRegion(fellowship.RegionId) ? fellowship.RegionId : "";

        // Its position there is known only while no hidden progress was made since it was last declared.
        public static bool PositionKnown(FellowshipView fellowship, Func<string, bool> isRegion) =>
            Region(fellowship, isRegion).Length > 0 && fellowship.Progress == 0;

        private void Add(BoardFigurePosition position) => figures.Add(position.Key, position);

        public bool ContainedCompanion(string identity) => companions.Contains(identity);
    }
}
