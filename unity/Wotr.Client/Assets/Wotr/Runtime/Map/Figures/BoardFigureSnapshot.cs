using System;
using System.Collections.Generic;
using Wotr.Contracts;

namespace Wotr.Client
{
    public sealed class BoardFigurePosition
    {
        public string Key { get; }
        public string Region { get; }
        public Side Side { get; }
        public bool Garrison { get; }
        public FigureArt Appearance { get; }
        public string Identity { get; }

        public BoardFigurePosition(string key, string region, Side side, bool garrison,
            FigureArt appearance, string identity = "")
        {
            Key = key;
            Region = region;
            Side = side;
            Garrison = garrison;
            Appearance = appearance;
            Identity = identity;
        }
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
            FellowshipRegion = !view.Fellowship.MordorStep.HasValue && isRegion(view.Fellowship.RegionId)
                ? view.Fellowship.RegionId : "";
            FellowshipPositionKnown = FellowshipRegion.Length > 0 && view.Fellowship.Progress == 0;
            foreach (var unit in view.Units)
                if (isRegion(unit.Location))
                    Add(new BoardFigurePosition("unit:" + unit.Id, unit.Location, unit.Side,
                        unit.Garrison, FigureAppearances.Art(unit)));
            foreach (var character in view.Characters)
                if (isRegion(character.Location))
                    Add(new BoardFigurePosition("character:" + character.Id, character.Location,
                        character.Side, character.Garrison, FigureAppearances.Art(character),
                        CharacterLibrary.Identity(character.DefinitionId)));
            if (FellowshipRegion.Length > 0)
                Add(new BoardFigurePosition("fellowship", FellowshipRegion, Side.FreePeoples,
                    false, FigureAppearances.Fellowship));
        }

        private void Add(BoardFigurePosition position) => figures.Add(position.Key, position);

        public bool ContainedCompanion(string identity) => companions.Contains(identity);
    }
}
