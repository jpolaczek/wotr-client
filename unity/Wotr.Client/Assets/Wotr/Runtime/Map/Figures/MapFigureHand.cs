using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-403 (TABLETOP_FIGURES_UX, "The lifted figure"): the player's hand — a pointer-ignoring layer above the stand layer,
    // inside the map, so the map's edge clips it. A click that chooses figures lifts exactly those off the table: the units
    // of a force rise on a base of their own, each character on its stand (MapHandPiece), and their place shows only what
    // stays behind (MapCanvas.SetLiftMask). What it holds comes from the draft (LiftedFigures): the unsent move, and the
    // figures sent on their way until the view takes them from their place. A figure that leaves the hand drops back onto
    // its base (Esc, undo, a figure left behind in the region panel, an attack), dissolves where a planned ghost now shows
    // it, or, once the view has carried it away, goes at once for the flight to show (UI-404 hands the flight its pose).
    internal sealed class MapFigureHand
    {
        public VisualElement Layer { get; }
        // A piece came down or dissolved: the places show what stands there again.
        public event Action Changed;
        private readonly MapCanvas map;
        private readonly List<MapHandPiece> pieces = new List<MapHandPiece>();
        private readonly HashSet<string> held = new HashSet<string>();
        private readonly HashSet<string> planned = new HashSet<string>();
        // Every figure standing on the table, by the piece that would lift it ("<piece key>|<region>|<figure id>").
        private readonly HashSet<string> standing = new HashSet<string>();

        public MapFigureHand(MapCanvas map)
        {
            this.map = map;
            Layer = new VisualElement { name = "map-hand", pickingMode = PickingMode.Ignore };
            Layer.AddToClassList("map-hand");
            Layer.StretchToParentSize();
            map.Add(Layer);
        }

        public IReadOnlyList<MapHandPiece> Pieces => pieces;

        // Every figure off the table: held, dropping back or dissolving.
        public IEnumerable<string> Lifted => pieces.SelectMany(piece => piece.Ids);

        // A region whose figures are in the hand shows figures, so the hand's miniatures match its own.
        public bool Holds(string region)
        {
            foreach (var piece in pieces)
                if (piece.Region == region)
                    return true;
            return false;
        }

        // Whether the raised figures include units (they land as an army) or only characters.
        public bool HoldsUnits => pieces.Any(piece => piece.State == MapHandPiece.PieceState.InPlace && piece.Units);

        // Where the raised figures show (map-local), or null while none is in view.
        public Rect? RaisedBounds
        {
            get
            {
                Rect? union = null;
                foreach (var piece in pieces.Where(piece => piece.State == MapHandPiece.PieceState.InPlace && piece.Visible))
                    union = union is Rect rect ? Union(rect, piece.LiftedBounds) : piece.LiftedBounds;
                return union;
            }
        }

        // A board change: a piece whose figures the view carried away goes at once (the flight shows them); nothing new
        // lifts until the draft says so.
        public void Settle(IReadOnlyCollection<MapArmyStand> forces, IReadOnlyCollection<MapCharacterStand> characters)
        {
            KeepStanding(forces, characters);
            foreach (var piece in pieces.ToList())
                if (!piece.HasSource || !AtSource(piece))
                    Drop(piece);
        }

        // The draft's figures (`lifted`) and the ones a planned ghost shows (`ghosts`), on every draft change.
        public void Hold(IReadOnlyCollection<MapArmyStand> forces, IReadOnlyCollection<MapCharacterStand> characters,
            ICollection<string> lifted, ICollection<string> ghosts)
        {
            held.Clear();
            held.UnionWith(lifted);
            planned.Clear();
            planned.UnionWith(ghosts);
            KeepStanding(forces, characters);
            var wanted = new Dictionary<string, Func<MapHandPiece>>();
            var updates = new Dictionary<string, Action<MapHandPiece>>();
            foreach (var force in forces)
                WantForce(force, wanted, updates);
            foreach (var character in characters)
                WantCharacter(character, wanted);
            foreach (var piece in pieces.ToList())
                Keep(piece, wanted, updates);
            foreach (var create in wanted.Values)
                Lift(create());
        }

        private void KeepStanding(IEnumerable<MapArmyStand> forces, IEnumerable<MapCharacterStand> characters)
        {
            standing.Clear();
            foreach (var force in forces)
                foreach (var unit in force.Army.Units)
                    standing.Add(Spot(force.Button.name, force.Region, unit.Id));
            foreach (var character in characters)
                standing.Add(Spot(CharacterKey(character.Figure), character.Region, character.Figure.Id));
        }

        private static string Spot(string key, string region, string id) => key + "|" + region + "|" + id;

        private static string CharacterKey(CharacterFigure figure) => "character-" + figure.Id;

        // A force with held units wants a piece of those units; one already raised takes the new units.
        private void WantForce(MapArmyStand force, Dictionary<string, Func<MapHandPiece>> wanted,
            Dictionary<string, Action<MapHandPiece>> updates)
        {
            var units = force.Army.Units.Where(unit => held.Contains(unit.Id)).ToList();
            if (units.Count == 0)
                return;
            var key = force.Button.name;
            wanted[key] = () => MapHandPiece.Of(force, units);
            updates[key] = piece => Regather(piece, force, units);
        }

        private void WantCharacter(MapCharacterStand character, Dictionary<string, Func<MapHandPiece>> wanted)
        {
            if (held.Contains(character.Figure.Id))
                wanted[CharacterKey(character.Figure)] = () => MapHandPiece.Of(character, character.Figure);
        }

        // A raised piece still wanted stays up (a dropping one is replaced by a new lift); one no longer wanted leaves.
        private void Keep(MapHandPiece piece, Dictionary<string, Func<MapHandPiece>> wanted,
            Dictionary<string, Action<MapHandPiece>> updates)
        {
            var raised = piece.State == MapHandPiece.PieceState.InPlace;
            if (raised && wanted.ContainsKey(piece.Key))
            {
                if (updates.TryGetValue(piece.Key, out var update))
                    update(piece);
                wanted.Remove(piece.Key);
            }
            else if (wanted.ContainsKey(piece.Key) || !piece.HasSource)
                Drop(piece);
            else if (raised)
                Leave(piece);
        }

        // A split in the region panel: the units left behind drop back onto the base, the rest stay raised; a card's flown
        // Nazgûl dissolves into its planned ghost while the next one stays raised.
        private void Regather(MapHandPiece piece, MapArmyStand force, List<ArmyUnit> units)
        {
            var ids = units.Select(unit => unit.Id).ToHashSet();
            if (piece.Ids.SetEquals(ids))
                return;
            var leaving = force.Army.Units.Where(unit => piece.Ids.Contains(unit.Id) && !ids.Contains(unit.Id)).ToList();
            piece.Hold(force, units);
            LeaveRaised(force, leaving.Where(unit => !planned.Contains(unit.Id)).ToList(), false);
            LeaveRaised(force, leaving.Where(unit => planned.Contains(unit.Id)).ToList(), true);
        }

        private void LeaveRaised(MapArmyStand force, List<ArmyUnit> units, bool toGhost)
        {
            if (units.Count == 0)
                return;
            var leaving = MapHandPiece.Of(force, units);
            Add(leaving);
            leaving.LeaveRaised(Moving, toGhost, () => Drop(leaving));
        }

        // A piece whose figures left the hand: dissolving into its planned ghost, gone with the view that carried it away,
        // or dropping back onto its base.
        private void Leave(MapHandPiece piece)
        {
            if (piece.Ids.All(planned.Contains))
                piece.Dissolve(Moving, () => Drop(piece));
            else if (!AtSource(piece))
                Drop(piece);
            else
                piece.Return(Moving, () => Drop(piece));
        }

        // Whether any of its figures still stands where it was lifted from.
        private bool AtSource(MapHandPiece piece) => piece.Ids.Any(id => standing.Contains(Spot(piece.Key, piece.Region, id)));

        private void Lift(MapHandPiece piece)
        {
            Add(piece);
            piece.Lift(Moving);
        }

        private void Add(MapHandPiece piece)
        {
            pieces.Add(piece);
            Layer.Add(piece.Stand.Root);
            piece.Layout(map, map.Stands.FigureScale);
        }

        private void Drop(MapHandPiece piece)
        {
            if (!pieces.Remove(piece))
                return;
            piece.Remove();
            Changed?.Invoke();
        }

        // Each camera change: every piece over its place at the figures' scale (transforms only).
        public void Layout()
        {
            var scale = map.Stands.FigureScale;
            foreach (var piece in pieces)
                piece.Layout(map, scale);
        }

        private bool Moving => Appearance.Moving(map);

        private static Rect Union(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
    }
}
