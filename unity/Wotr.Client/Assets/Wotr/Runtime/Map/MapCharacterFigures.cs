using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-402 (TABLETOP_FIGURES_UX, decision 5): every character on the map is its own miniature beside its region's army,
    // a `character-<id>` button in the stand layer. It stands on its region's character places (CharacterPlacement) at
    // the level and scale of the region's armies (MapStandLayout), lights from the figures' choices the region panel
    // reads too (FigureChoices), and its click is the character's own (MapCanvas.MiniatureSelected), never the army's.
    // Like an army it emerges where it appears, fades where it leaves the map and waits, dimmed, for the flight that
    // brings it.
    internal sealed class MapCharacterFigures
    {
        private readonly MapCanvas map;
        private readonly Action<DraftToken> chosen;
        private readonly Dictionary<string, MapCharacterStand> stands = new Dictionary<string, MapCharacterStand>();
        private readonly Dictionary<string, (StandKind Kind, int Index)> places = new Dictionary<string, (StandKind, int)>();
        private readonly List<MapCharacterStand> emerging = new List<MapCharacterStand>();
        private readonly List<MapCharacterStand> departing = new List<MapCharacterStand>();
        private readonly List<MapCharacterStand> leaving = new List<MapCharacterStand>();
        private readonly HashSet<string> drafted = new HashSet<string>();
        private readonly HashSet<string> owners = new HashSet<string>();
        private readonly HashSet<string> selectedRegions = new HashSet<string>();
        private FigureChoices choices = new FigureChoices();
        private Func<string, string> sovereignHint = _ => null;

        public MapCharacterFigures(MapCanvas map, Action<DraftToken> chosen)
        {
            this.map = map;
            this.chosen = chosen;
        }

        public bool AnyEligible => stands.Values.Any(stand => !stand.Stacked && Lit(stand, choices.Eligible));

        public MapCharacterStand Find(string id) => stands.TryGetValue(id, out var stand) ? stand : null;

        public IEnumerable<MapCharacterStand> In(string region) => stands.Values.Where(stand => stand.Region == region);

        public IEnumerable<MapCharacterStand> Of(string region, Side side, bool garrison) =>
            stands.Values.Where(stand => stand.Figure.In(region, side, garrison));

        // The view's characters: a new one emerges where it appears, one that changed its place emerges there unless a
        // flight brings it (decided a frame later), one gone from the map fades out where it stood.
        public void Sync(IReadOnlyList<CharacterFigure> characters, bool animated, bool reduced)
        {
            var obsolete = new HashSet<string>(stands.Keys);
            foreach (var character in characters)
            {
                obsolete.Remove(character.Id);
                var fresh = !stands.TryGetValue(character.Id, out var stand);
                if (fresh)
                    stand = Create(character);
                var moved = !fresh && !stand.Figure.In(character.RegionId, character.Side, character.Garrison);
                stand.Figure = character;
                stand.Button.userData = Token(character);
                if (animated && !reduced && (fresh || moved))
                    Later(emerging, stand);
                ApplySovereignHint(stand);
            }
            foreach (var id in obsolete)
                Remove(id, animated && !reduced);
        }

        // Every region's characters on their places, each group shown with the characters stacked behind its front.
        public void Assign(Func<string, Side?> fieldSide)
        {
            foreach (var region in stands.Values.GroupBy(stand => stand.Region))
                Assign(region.Key, region.ToList(), fieldSide(region.Key));
            ShowStates();
        }

        private void Assign(string region, List<MapCharacterStand> group, Side? fieldSide)
        {
            var regionStands = map.Stands.Stands(region);
            var placements = CharacterPlacement.Assign(group.Select(stand => stand.Figure), fieldSide, regionStands.HasGarrison,
                id => places.TryGetValue(id, out var place) ? place : ((StandKind, int)?)null);
            foreach (var stand in group)
                stand.Stacked = true;
            foreach (var placement in placements)
            {
                var front = stands[placement.Front.Id];
                front.Stacked = false;
                places[front.Figure.Id] = (placement.Kind, placement.Index);
                foreach (var member in placement.Members.Skip(1))
                    places.Remove(member.Id);
                var feet = placement.Kind == StandKind.GarrisonCharacter
                    ? regionStands.GarrisonCharacter(placement.Index)
                    : regionStands.Character(placement.Index);
                foreach (var member in placement.Members)
                    map.Stands.Stand(stands[member.Id].Stand, feet);
                front.Show(placement.Members);
            }
        }

        // The pass of every frame the camera moves: each character at its region's level and scale; the bounds of the
        // shown ones are reserved, so region names hide under them.
        public void Layout(Func<string, bool> focused, List<Rect> used)
        {
            foreach (var stand in stands.Values)
                if (map.Stands.LayoutCharacter(stand, focused(stand.Region), Arriving(stand)))
                    used.Add(stand.Bounds);
            foreach (var stand in leaving)
                map.Stands.LayoutCharacter(stand, false, false);
        }

        // UI-402: a context that lights a figure's chip lights its miniature.
        public void ShowChoices(FigureChoices latest)
        {
            choices = latest;
            ShowStates();
        }

        // The characters of the player's unsent moves (an army's move, a companion's journey).
        public void ShowDrafted(IEnumerable<string> ids)
        {
            drafted.Clear();
            drafted.UnionWith(ids);
            ShowStates();
        }

        // UI-374: the characters whose ability or order a click uses.
        public void HighlightOwners(IEnumerable<string> ids)
        {
            owners.Clear();
            owners.UnionWith(ids);
            ShowStates();
        }

        // Whether a region holds a selected character, so it shows figures (kept with the states, read every frame).
        public bool HoldsSelected(string region) => selectedRegions.Contains(region);

        private bool Selected(MapCharacterStand stand) => Lit(stand, choices.Selected) || stand.Members.Any(m => drafted.Contains(m.Id));

        private void ShowStates()
        {
            selectedRegions.Clear();
            foreach (var stand in stands.Values)
                ShowState(stand);
        }

        // UI-274: a lit Awakened king's figure is that king on the map, unless a click on it chooses the figure itself.
        public void LightSovereigns(Func<string, string> hint)
        {
            sovereignHint = hint;
            foreach (var stand in stands.Values)
                ApplySovereignHint(stand);
        }

        public string LitSovereign(string id) => stands.TryGetValue(id, out var stand) ? LitSovereign(stand) : null;

        private string LitSovereign(MapCharacterStand stand)
        {
            var king = stand.Figure.SovereignId;
            return king.Length == 0 || choices.Eligible(stand.Figure.Id) || sovereignHint(king) == null ? null : king;
        }

        private void ApplySovereignHint(MapCharacterStand stand)
        {
            var king = LitSovereign(stand);
            var region = map.Data.Regions.TryGetValue(stand.Region, out var definition) ? definition.Name : stand.Region;
            var description = ArmyMarkerText.Describe(stand.Figure, region);
            stand.Button.tooltip = king != null ? sovereignHint(king) + "\n" + description : description;
            stand.Button.EnableInClassList(MapCanvas.SovereignTargetClass, king != null);
        }

        // A stacked pair shows the state of any figure in it: lit when one of them is, marched when all of them are.
        private void ShowState(MapCharacterStand stand)
        {
            if (stand.Stacked || stand.Members.Count == 0)
                return;
            var button = stand.Button;
            var selected = Selected(stand);
            if (selected)
                selectedRegions.Add(stand.Region);
            button.EnableInClassList(MapCharacterArtwork.EligibleClass, Lit(stand, choices.Eligible));
            button.EnableInClassList(MapCharacterArtwork.SelectedClass, selected);
            button.EnableInClassList(MapArmyArtwork.MovedClass, stand.Members.All(m => choices.Moved(m.Id)));
            button.EnableInClassList("ability-owner", stand.Members.Any(m => owners.Contains(m.Id)));
            ApplySovereignHint(stand);
        }

        private static bool Lit(MapCharacterStand stand, Func<string, bool> state) => stand.Members.Any(m => state(m.Id));

        // The frame after a sync: a character a flight brings arrives with it instead of emerging.
        public void StartChanges()
        {
            foreach (var stand in emerging)
                if (stands.ContainsValue(stand) && !Arriving(stand))
                    stand.Artwork.Emerge();
            emerging.Clear();
            foreach (var stand in departing)
                StartLeaving(stand);
            departing.Clear();
        }

        // Skip shows the latest state: every character clear, every departed one gone.
        public void EndEmerging()
        {
            emerging.Clear();
            foreach (var stand in stands.Values)
                stand.Artwork.EndEmerge();
            foreach (var stand in departing.Concat(leaving))
                stand.Stand.Remove();
            departing.Clear();
            leaving.Clear();
        }

        private bool Arriving(MapCharacterStand stand) => map.Figures?.ArrivingCharacter(stand.Figure.Id) == true;

        private void Later(List<MapCharacterStand> list, MapCharacterStand stand)
        {
            list.Add(stand);
            map.ScheduleChanges();
        }

        private void Remove(string id, bool fade)
        {
            var gone = stands[id];
            stands.Remove(id);
            places.Remove(id);
            if (!fade)
            {
                gone.Stand.Remove();
                return;
            }
            gone.Button.name = "leaving-" + gone.Button.name;
            gone.Button.pickingMode = PickingMode.Ignore;
            gone.Button.tooltip = "";
            Later(departing, gone);
        }

        // UI-338 for a character: it fades where it stood while the view moves, then it is gone.
        private void StartLeaving(MapCharacterStand stand)
        {
            if (stand.Stand.Root.parent == null)
                return;
            leaving.Add(stand);
            stand.Artwork.Leave(() =>
            {
                stand.Stand.Remove();
                leaving.Remove(stand);
            });
        }

        private MapCharacterStand Create(CharacterFigure character)
        {
            var id = character.Id;
            var button = new MiniatureButton(() => Choose(id));
            button.name = "character-" + id;
            button.AddToClassList(RuntimeTooltip.RichHoverClass);
            button.RegisterCallback<PointerDownEvent>(e => { if (e.button == 0) e.StopPropagation(); });
            var stand = new MapCharacterStand
            {
                Button = button,
                Figure = character,
                Artwork = new MapCharacterArtwork(button),
                Stand = new MapStand(button, MapCharacterArtwork.HolderSize)
            };
            stand.Members.Add(character);
            stands.Add(id, stand);
            map.Stands.Add(stand.Stand);
            return stand;
        }

        private void Choose(string id)
        {
            if (stands.TryGetValue(id, out var stand))
                chosen(Token(stand.Figure));
        }

        private static DraftToken Token(CharacterFigure character) => new DraftToken(DraftObject.Miniature, character.Id,
            side: character.Side, garrison: character.Garrison, region: character.RegionId);
    }
}
