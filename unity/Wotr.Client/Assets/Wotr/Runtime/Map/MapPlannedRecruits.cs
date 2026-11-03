using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-131: figures planned by a card or an ability, drawn faintly where they will arrive. Each region's ghost
    // has × that returns its planned figures to the reserve; the ghost itself is never interactive. UI-212: the same
    // ghosts show a card's planned Nazgûl flights, without ×.
    public sealed class MapPlannedRecruits
    {
        // The faint planned figures (Styles/Armies.uss); since UI-403 no other figure on the map is drawn faintly.
        public const string GhostClass = "planned-ghost";
        // Their opacity (Styles/Armies.uss, .army.planned-ghost), which a recruit flying to its ghost lands at (UI-454).
        public const float GhostOpacity = .6f;
        public event Action<string> Removed;
        private readonly MapCanvas map;
        private readonly Dictionary<string, Ghost> ghosts = new Dictionary<string, Ghost>();
        private readonly List<string> stale = new List<string>();
        private readonly HashSet<string> figureIds = new HashSet<string>();
        // UI-454 (owner, 2026-11-03): planned recruits still flying from their Reserve slot; their ghost shows them once they
        // land. The last placements applied, so a landing shows them again.
        private readonly HashSet<string> withheld = new HashSet<string>();
        private IReadOnlyList<PlannedReinforcement> placed = Array.Empty<PlannedReinforcement>();
        private Side placedSide;
        private bool placedInteractive;

        private readonly string prefix;
        private readonly bool removable;

        public MapPlannedRecruits(MapCanvas map, string prefix = "planned-recruit", bool removable = true)
        {
            this.map = map;
            this.prefix = prefix;
            this.removable = removable;
        }

        public IEnumerable<string> Regions => ghosts.Keys;

        // UI-403: the ids of the figures the ghosts show (a planned flight's Nazgûl, a journey's companions); where they
        // stand now shows only what stays behind.
        public IReadOnlyCollection<string> FigureIds => figureIds;

        // UI-416 (owner): a planned character keeps its own figure (Marker); by nation and kind alone the Balrog's planned move
        // showed a Nazgûl.
        public void Apply(IReadOnlyList<PlannedReinforcement> placements, Side side, bool interactive)
        {
            placed = placements;
            placedSide = side;
            placedInteractive = interactive;
            withheld.IntersectWith(placements.Select(p => p.UnitId));
            Apply(placements.Where(p => !withheld.Contains(p.UnitId)).GroupBy(p => p.RegionId)
                .ToDictionary(g => g.Key, g => Marker(g.Key, side, g.ToList())), interactive);
        }

        // UI-454: a planned recruit flies from its Reserve slot; its ghost leaves it out until it lands (Release).
        public void Withhold(string unitId)
        {
            if (withheld.Add(unitId) && placed.Any(p => p.UnitId == unitId))
                Apply(placed, placedSide, placedInteractive);
        }

        // UI-454: the recruit's flight landed (or ended): its ghost shows it, while it is still planned.
        public void Release(string unitId)
        {
            if (withheld.Remove(unitId) && placed.Any(p => p.UnitId == unitId))
                Apply(placed, placedSide, placedInteractive);
        }

        // UI-294: ghosts of other figures, such as the companions a card's journey moves: one marker per region.
        public void Apply(IReadOnlyDictionary<string, ArmyMarker> markers, bool interactive)
        {
            var regions = markers.Where(p => map.Data.Shapes.ContainsKey(p.Key)).ToDictionary(p => p.Key, p => p.Value);
            stale.Clear();
            stale.AddRange(ghosts.Keys.Where(region => !regions.ContainsKey(region)));
            foreach (var region in stale)
            {
                ghosts[region].Remove();
                ghosts.Remove(region);
            }
            foreach (var pair in regions)
            {
                if (!ghosts.TryGetValue(pair.Key, out var ghost))
                {
                    ghost = new Ghost(map, pair.Key, prefix, removable ? () => Removed?.Invoke(pair.Key) : null);
                    ghosts.Add(pair.Key, ghost);
                }
                ghost.Apply(pair.Value, interactive);
            }
            map.Stands.Sort();
            KeepFigureIds(regions.Values);
            Layout();
        }

        private void KeepFigureIds(IEnumerable<ArmyMarker> markers)
        {
            figureIds.Clear();
            foreach (var marker in markers)
            {
                figureIds.UnionWith(marker.Units.Select(unit => unit.Id).Where(id => id.Length > 0));
                figureIds.UnionWith(marker.Carried.Select(character => character.Id));
            }
        }

        // Planned units by nation, kind and id; a moved character (UI-271) shows its own figure.
        private static ArmyMarker Marker(string region, Side side, List<PlannedReinforcement> placements)
        {
            var units = placements.Where(p => p.CharacterId.Length == 0).Select(p => new ArmyUnit(p.UnitId, p.Nation, p.Kind));
            var marker = ArmyMarker.From(region, side, false, units);
            foreach (var character in placements.Where(p => p.CharacterId.Length > 0))
                marker.Carried.Add(new CharacterFigure
                {
                    Id = character.UnitId,
                    DefinitionId = character.CharacterId,
                    RegionId = region,
                    Side = side,
                    Art = FigureAppearances.Character(character.CharacterId, side)
                });
            return marker;
        }

        public void Layout()
        {
            foreach (var pair in ghosts)
                pair.Value.Layout(map);
        }

        // UI-404: the sent step's figures (by id) these ghosts show at their destination fly from there when the view
        // takes them, standing, never from their source.
        public void Expect(ICollection<string> sent, MapFigureMotion motion)
        {
            foreach (var ghost in ghosts.Values)
                ghost.Expect(sent, motion);
        }

        // UI-401: a ghost stands on its region's planned place (UI-422) in the stand layer, scaled as the armies are.
        private sealed class Ghost
        {
            private readonly Button marker;
            private readonly MapArmyArtwork artwork;
            private readonly MapStand stand;
            private readonly Button remove;
            private readonly string region;
            private ArmyMarker shown;

            public Ghost(MapCanvas map, string region, string prefix, Action removed)
            {
                this.region = region;
                marker = new Button { name = prefix + "-" + region, focusable = false, pickingMode = PickingMode.Ignore };
                marker.AddToClassList("army");
                marker.AddToClassList(GhostClass);
                marker.AddToClassList("planned-recruit");
                artwork = new MapArmyArtwork(marker);
                stand = new MapStand(marker, MapArmyArtwork.HolderSize);
                stand.Design(MapArmyArtwork.Feet(false, true), MapArmyArtwork.Visible(false, true));
                map.Stands.Add(stand);
                map.Stands.Stand(stand, map.Stands.Stands(region).Planned);
                if (removed == null)
                    return;
                remove = IconButton.Create("close", "Return the figures planned here to the reserve", removed);
                remove.name = prefix + "-remove-" + region;
                remove.AddToClassList("planned-recruit-remove");
                remove.style.left = 0;
                remove.style.top = 0;
                // A press on the remove icon belongs to the button, not to the map's region click or pan.
                remove.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
                map.Add(remove);
            }

            public void Apply(ArmyMarker army, bool interactive)
            {
                shown = army;
                marker.EnableInClassList("fp", army.Side == Side.FreePeoples);
                marker.EnableInClassList("sh", army.Side == Side.Shadow);
                artwork.Apply(army);
                artwork.Layout(true);
                remove?.SetEnabled(interactive);
            }

            // UI-404: the sent figures it shows start their flight standing on its place.
            public void Expect(ICollection<string> sent, MapFigureMotion motion)
            {
                if (shown == null)
                    return;
                var keys = shown.Units.Where(unit => sent.Contains(unit.Id))
                    .Select(unit => BoardFigurePosition.UnitPrefix + unit.Id)
                    .Concat(shown.Carried.Where(character => sent.Contains(character.Id))
                        .Select(character => BoardFigurePosition.CharacterPrefix + character.Id))
                    .ToList();
                if (keys.Count > 0)
                    motion.Expect(keys, stand.Feet, FlightPose.Standing);
            }

            public void Layout(MapCanvas map)
            {
                var visible = map.Stands.Place(stand, stand.Feet, map.Stands.FigureScale);
                marker.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                if (remove != null)
                    remove.style.display = marker.style.display;
                if (!visible)
                    return;
                var rect = stand.Bounds;
                map.ReserveMarker(rect);
                if (remove != null)
                    remove.style.translate = new Translate(rect.xMax - 10, rect.y - 6);
            }

            public void Remove()
            {
                stand.Remove();
                remove?.RemoveFromHierarchy();
            }
        }
    }
}
