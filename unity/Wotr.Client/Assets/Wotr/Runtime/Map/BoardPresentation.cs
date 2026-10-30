using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-126: the figures of one nation inside a force; Leaders here exclude the Nazgûl.
    public sealed class NationForce
    {
        public string Nation;
        public int Regular, Elite, Leaders, Nazgul;
        public int Total => Regular + Elite + Leaders + Nazgul;
    }

    // UI-402: one unit of a force, by its id (empty for a ghost or a fixture), nation and kind.
    public readonly struct ArmyUnit
    {
        public ArmyUnit(string id, string nation, UnitKind kind)
        {
            Id = id ?? "";
            Nation = nation ?? "";
            Kind = kind;
        }

        public string Id { get; }
        public string Nation { get; }
        public UnitKind Kind { get; }
    }

    // UI-126, UI-402: one force's units on the map — the army's base with its figures, emblems and count. Its characters
    // stand beside it as their own miniatures (BoardPresentation.Characters), so they are neither its figures nor counted.
    public sealed class ArmyMarker
    {
        public string RegionId;
        public Side Side;
        public bool Garrison;
        // Leaders include the Nazgûl; Nazgul counts them alone.
        public int Regular, Elite, Leaders;
        public int Nazgul;
        public string Effect = "";
        // UI-126: nations with the most figures first.
        public List<NationForce> Nations = new List<NationForce>();
        public List<ArmyUnit> Units = new List<ArmyUnit>();
        // UI-402: only a ghost of planned figures (a move, a card's recruits or journeys) carries characters on its base;
        // on the board a character stands on its own.
        public List<CharacterFigure> Carried = new List<CharacterFigure>();
        // UI-274: the kings in this force; an Initial king is its crowned leader (CrownedTitles). An Awakened one stands
        // on his own character figure (CharacterFigure.SovereignId).
        public List<string> SovereignIds = new List<string>();
        public List<string> CrownedTitles = new List<string>();
        public int Total => Regular + Elite + Leaders;
        // The figures its base shows the count of: its units and, on a ghost, the characters it carries.
        public int FigureCount => Total + Carried.Count;

        public static ArmyMarker From(string region, Side side, bool garrison, IEnumerable<(string Nation, UnitKind Kind)> units) =>
            From(region, side, garrison, units.Select(u => new ArmyUnit("", u.Nation, u.Kind)));

        public static ArmyMarker From(string region, Side side, bool garrison, IEnumerable<UnitView> units) =>
            From(region, side, garrison, units.Select(u => new ArmyUnit(u.Id, u.Nation, u.Kind)));

        public static ArmyMarker From(string region, Side side, bool garrison, IEnumerable<ArmyUnit> units)
        {
            var army = new ArmyMarker { RegionId = region, Side = side, Garrison = garrison, Units = units.ToList() };
            foreach (var group in army.Units.GroupBy(u => u.Nation))
                army.Nations.Add(new NationForce
                {
                    Nation = group.Key,
                    Regular = group.Count(u => u.Kind == UnitKind.Regular),
                    Elite = group.Count(u => u.Kind == UnitKind.Elite),
                    Leaders = group.Count(u => u.Kind == UnitKind.Leader),
                    Nazgul = group.Count(u => u.Kind == UnitKind.Nazgul)
                });
            army.Nations.Sort((a, b) => a.Total != b.Total ? b.Total.CompareTo(a.Total) : string.CompareOrdinal(a.Nation, b.Nation));
            army.Regular = army.Nations.Sum(n => n.Regular);
            army.Elite = army.Nations.Sum(n => n.Elite);
            army.Nazgul = army.Nations.Sum(n => n.Nazgul);
            army.Leaders = army.Nations.Sum(n => n.Leaders) + army.Nazgul;
            return army;
        }

        public bool Is(string region, Side side, bool garrison) => RegionId == region && Side == side && Garrison == garrison;
    }
    public sealed class BoardPresentation
    {
        public BoardData Board;
        public MapGeometry Geometry;
        public MapContourData Contours { get; private set; }
        public MapBorderData Borders { get; private set; }
        public MapCanvasLayout Canvas { get; private set; }
        public MordorTrackGeometry MordorTrack { get; private set; }
        private static MapGeometry sharedGeometry;
        private static MapCanvasLayout sharedCanvas;
        private static MordorTrackGeometry sharedMordorTrack;
        private static MapContourData sharedContours;
        private static MapBorderData sharedBorders;
        public IReadOnlyDictionary<string, HuntTileDefinition> HuntTiles { get; private set; }
        public HashSet<string> ProtectedRegions { get; } = new HashSet<string>();
        // UI-265: the shown game's expansion modules (ExpansionModules ids), for the map's module-dependent drawings.
        public List<string> Modules { get; private set; } = new List<string>();
        public List<ArmyMarker> Armies = new List<ArmyMarker>();
        // UI-402: every character on the map, each its own miniature beside its region's army.
        public List<CharacterFigure> Characters = new List<CharacterFigure>();
        // UI-274: the corrupted kings, each a dark marker by his capital.
        public List<SovereignView> CorruptedSovereigns = new List<SovereignView>();
        private Dictionary<string, RegionDefinition> printedRegions;
        private readonly HashSet<string> overriddenRegions = new HashSet<string>();
        public Dictionary<string, RegionDefinition> Regions
        {
            get; private set;
        }
        public Dictionary<string, RegionGeometry> Shapes
        {
            get; private set;
        }
        public Dictionary<string, Side?> Control
        {
            get; private set;
        }
        public Rect Bounds
        {
            get; private set;
        }
        public static BoardPresentation Load()
        {
            var board = JsonConvert.DeserializeObject<BoardData>(Resources.Load<TextAsset>("base-board").text);
            if (sharedGeometry == null)
            {
                var resource = Resources.Load<TextAsset>("map-geometry");
                var geometry = JsonConvert.DeserializeObject<MapGeometry>(resource.text);
                var contours = new MapContourData(geometry, board.Regions.Select(r => r.Id));
                var borders = MapBorderData.Load(geometry, board, resource.bytes);
                var canvas = MapCanvasLayout.Load(geometry);
                var mordor = MordorTrackGeometry.Load(geometry);
                // Shared only once every part has loaded, so a failed load never leaves half the map cached.
                sharedContours = contours;
                sharedBorders = borders;
                sharedCanvas = canvas;
                sharedMordorTrack = mordor;
                sharedGeometry = geometry;
            }
            var result = new BoardPresentation { Board = board, Geometry = sharedGeometry, Contours = sharedContours,
                Borders = sharedBorders, Canvas = sharedCanvas, MordorTrack = sharedMordorTrack };
            result.HuntTiles = board.HuntTiles.ToDictionary(t => t.Id);
            result.Regions = board.Regions.ToDictionary(r => r.Id);
            result.printedRegions = new Dictionary<string, RegionDefinition>(result.Regions);
            result.Shapes = sharedGeometry.Regions.ToDictionary(r => r.Id);
            result.Control = board.Regions.ToDictionary(r => r.Id, r => r.OriginalOwner);
            result.Bounds = sharedContours.Bounds;
            return result;
        }
        public void UseSetup()
        {
            ResetRegions();
            Control = Board.Regions.ToDictionary(r => r.Id, r => r.OriginalOwner);
            Characters = new List<CharacterFigure>();
            Armies = Board.Units.Where(u => u.Location != "reserve").GroupBy(u => u.Location).Select(group => ArmyMarker.From(
                group.Key, Board.Nations.Single(n => n.Id == group.First().Nation).Side, false,
                group.Select(u => (u.Nation, u.Kind)))).ToList();
        }
        public void ClearGameState()
        {
            ResetRegions();
            Modules = new List<string>();
            Armies.Clear();
            Characters.Clear();
            CorruptedSovereigns.Clear();
            ProtectedRegions.Clear();
            Control.Clear();
        }
        public void Apply(PlayerView view)
        {
            ApplyRegions(view.RegionOverrides);
            Modules = ExpansionModules.Of(view.Configuration ?? new MatchConfiguration());
            ProtectedRegions.Clear();
            ProtectedRegions.UnionWith(view.Effects.Where(e => e.Modifier == "PreventShadowArmyEntry").Select(e => e.TargetId));
            Control = new Dictionary<string, Side?>(view.Control);
            Armies = view.Units.Where(u => Regions.ContainsKey(u.Location))
                .GroupBy(u => (u.Location, u.Side, u.Garrison))
                .Select(group => ArmyMarker.From(group.Key.Location, group.Key.Side, group.Key.Garrison, group)).ToList();
            Characters = view.Characters.Where(c => Regions.ContainsKey(c.Location)).Select(CharacterFigure.From).ToList();
            foreach (var king in view.Sovereigns)
                AddSovereign(view, king);
            CorruptedSovereigns = view.Sovereigns.Where(s => s.Status == SovereignStatus.Corrupted && Regions.ContainsKey(s.RegionId)).ToList();
        }

        // UI-274: an Initial king crowns his force; an Awakened one stands on his own character figure (UI-402).
        private void AddSovereign(PlayerView view, SovereignView king)
        {
            if (king.Status == SovereignStatus.Awakened)
            {
                var figure = Characters.FirstOrDefault(c => c.Id == king.Id);
                if (figure != null)
                    figure.SovereignId = king.Id;
                return;
            }
            if (king.Status != SovereignStatus.Initial)
                return;
            var army = view.Units.Where(u => u.Id == king.Id).Select(u => Force(u.Location, u.Side, u.Garrison)).FirstOrDefault();
            if (army == null)
                return;
            army.SovereignIds.Add(king.Id);
            army.CrownedTitles.Add(king.Title);
        }

        private ArmyMarker Force(string region, Side side, bool garrison) =>
            Armies.FirstOrDefault(a => a.Is(region, side, garrison));

        // UI-402: how many figures a force shows on the map: its army's units and the characters standing beside it.
        public int FigureCount(string region, Side side, bool garrison) =>
            (Force(region, side, garrison)?.Total ?? 0) + Characters.Count(c => c.In(region, side, garrison));

        // The same for both of a side's forces in a region, its field and its garrison.
        public int FigureCount(string region, Side side) => FigureCount(region, side, false) + FigureCount(region, side, true);

        public void UseStressFixture()
        {
            UseSetup();
            Armies.RemoveAll(a => a.RegionId == "helms-deep");
            Armies.Add(Stress("helms-deep", Side.FreePeoples, true, "Defence", ("rohan", 3, 2, 2, 0)));
            Armies.Add(Stress("helms-deep", Side.Shadow, false, "Siege", ("isengard", 7, 3, 0, 0), ("sauron", 0, 0, 0, 3)));
            Armies.Add(Stress("westemnet", Side.FreePeoples, false, "Relief", ("rohan", 4, 2, 2, 0), ("gondor", 3, 1, 2, 0)));
            StressCharacters("helms-deep", Side.FreePeoples, true, "strider", "legolas", "gimli");
            StressCharacters("helms-deep", Side.Shadow, false, "witch-king");
            StressCharacters("westemnet", Side.FreePeoples, false, "gandalf-white", "boromir", "merry", "pippin");
        }

        private static ArmyMarker Stress(string region, Side side, bool garrison, string effect,
            params (string Nation, int Regular, int Elite, int Leaders, int Nazgul)[] forces)
        {
            var units = forces.SelectMany(f => Enumerable.Repeat((f.Nation, UnitKind.Regular), f.Regular)
                .Concat(Enumerable.Repeat((f.Nation, UnitKind.Elite), f.Elite))
                .Concat(Enumerable.Repeat((f.Nation, UnitKind.Leader), f.Leaders))
                .Concat(Enumerable.Repeat((f.Nation, UnitKind.Nazgul), f.Nazgul)));
            var army = ArmyMarker.From(region, side, garrison, units);
            army.Effect = effect;
            return army;
        }

        // The stress fixture's characters by their base game names ("strider" is base.character.strider).
        private void StressCharacters(string region, Side side, bool garrison, params string[] names)
        {
            foreach (var name in names)
                Characters.Add(CharacterFigure.From(new CharacterView
                {
                    Id = "stress-" + name,
                    DefinitionId = "base.character." + name,
                    Side = side,
                    Location = region,
                    Garrison = garrison
                }));
        }

        private void ResetRegions()
        {
            foreach (var id in overriddenRegions)
                Regions[id] = printedRegions[id];
            overriddenRegions.Clear();
        }

        private void ApplyRegions(IEnumerable<RegionDefinition> overrides)
        {
            ResetRegions();
            foreach (var region in overrides)
            {
                if (!printedRegions.ContainsKey(region.Id))
                    continue;
                Regions[region.Id] = region;
                overriddenRegions.Add(region.Id);
            }
        }
    }
}
