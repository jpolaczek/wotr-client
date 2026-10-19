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

    public sealed class ArmyMarker
    {
        public string RegionId;
        public Side Side;
        public bool Garrison;
        // Leaders include the Nazgûl; Nazgul counts them alone.
        public int Regular, Elite, Leaders, Characters;
        public int Nazgul;
        public FigureArt CharacterAppearance = FigureAppearance.Companion;
        public string Effect = "";
        // UI-126: nations with the most figures first, and the characters by name.
        public List<NationForce> Nations = new List<NationForce>();
        public List<string> CharacterNames = new List<string>();
        // UI-274: the kings in this force; an Initial king is its crowned leader (CrownedTitles), an Awakened one
        // stands among its characters.
        public List<string> SovereignIds = new List<string>();
        public List<string> CrownedTitles = new List<string>();
        public int Total => Regular + Elite + Leaders + Characters;

        public static ArmyMarker From(string region, Side side, bool garrison, IEnumerable<(string Nation, UnitKind Kind)> units)
        {
            var army = new ArmyMarker { RegionId = region, Side = side, Garrison = garrison };
            foreach (var group in units.GroupBy(u => u.Nation))
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
            Armies = Board.Units.Where(u => u.Location != "reserve").GroupBy(u => u.Location).Select(group => ArmyMarker.From(
                group.Key, Board.Nations.Single(n => n.Id == group.First().Nation).Side, false,
                group.Select(u => (u.Nation, u.Kind)))).ToList();
        }
        public void ClearGameState()
        {
            ResetRegions();
            Modules = new List<string>();
            Armies.Clear();
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
                .GroupBy(u => (u.Location, u.Side, u.Garrison)).Select(group => ArmyMarker.From(
                    group.Key.Location, group.Key.Side, group.Key.Garrison, group.Select(u => (u.Nation, u.Kind)))).ToList();
            foreach (var group in view.Characters.Where(c => Regions.ContainsKey(c.Location)).GroupBy(c => (c.Location, c.Side, c.Garrison)))
            {
                var army = Armies.FirstOrDefault(a => a.RegionId == group.Key.Location && a.Side == group.Key.Side && a.Garrison == group.Key.Garrison);
                if (army == null)
                {
                    army = new ArmyMarker { RegionId = group.Key.Location, Side = group.Key.Side, Garrison = group.Key.Garrison };
                    Armies.Add(army);
                }
                army.Characters = group.Count();
                army.CharacterAppearance = FigureAppearances.Art(group.First());
                army.CharacterNames = group.Select(c => CharacterLibrary.Name(c.DefinitionId)).ToList();
            }
            foreach (var king in view.Sovereigns)
                AddSovereign(view, king);
            CorruptedSovereigns = view.Sovereigns.Where(s => s.Status == SovereignStatus.Corrupted && Regions.ContainsKey(s.RegionId)).ToList();
        }

        private void AddSovereign(PlayerView view, SovereignView king)
        {
            var army = king.Status switch
            {
                SovereignStatus.Initial => view.Units.Where(u => u.Id == king.Id).Select(u => Force(u.Location, u.Side, u.Garrison)).FirstOrDefault(),
                SovereignStatus.Awakened => view.Characters.Where(c => c.Id == king.Id).Select(c => Force(c.Location, c.Side, c.Garrison)).FirstOrDefault(),
                _ => null
            };
            if (army == null)
                return;
            army.SovereignIds.Add(king.Id);
            if (king.Status == SovereignStatus.Initial)
                army.CrownedTitles.Add(king.Title);
        }

        private ArmyMarker Force(string region, Side side, bool garrison) =>
            Armies.FirstOrDefault(a => a.RegionId == region && a.Side == side && a.Garrison == garrison);
        public void UseStressFixture()
        {
            UseSetup();
            Armies.RemoveAll(a => a.RegionId == "helms-deep");
            Armies.Add(Stress("helms-deep", Side.FreePeoples, true, "Defence", 3, ("rohan", 3, 2, 2, 0)));
            Armies.Add(Stress("helms-deep", Side.Shadow, false, "Siege", 1, ("isengard", 7, 3, 0, 0), ("sauron", 0, 0, 0, 3)));
            Armies.Add(Stress("westemnet", Side.FreePeoples, false, "Relief", 4, ("rohan", 4, 2, 2, 0), ("gondor", 3, 1, 2, 0)));
        }

        private static ArmyMarker Stress(string region, Side side, bool garrison, string effect, int characters,
            params (string Nation, int Regular, int Elite, int Leaders, int Nazgul)[] forces)
        {
            var units = forces.SelectMany(f => Enumerable.Repeat((f.Nation, UnitKind.Regular), f.Regular)
                .Concat(Enumerable.Repeat((f.Nation, UnitKind.Elite), f.Elite))
                .Concat(Enumerable.Repeat((f.Nation, UnitKind.Leader), f.Leaders))
                .Concat(Enumerable.Repeat((f.Nation, UnitKind.Nazgul), f.Nazgul)));
            var army = ArmyMarker.From(region, side, garrison, units);
            army.Effect = effect;
            army.Characters = characters;
            return army;
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
