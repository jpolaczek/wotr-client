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
    // it, or, once the view has carried it away, goes at once for the flight to show.
    // UI-404: when the step that sends raised figures leaves, the hand hands their place and raised pose to the flight
    // (MapFigureMotion.Expect) and keeps them raised, whatever the draft says, until the view carries them away. A
    // rejection, a disconnect or the expiry (Lapse) puts them back on their base, where they stay while the draft still
    // sends them.
    // UI-405: a drag carries the raised pieces of the unsent move under the pointer (Grab, Follow) and drops them where
    // they are (Release): sent, they stay there and their flight leaves from there (Expect reads each piece's Ground);
    // planned, they dissolve there; otherwise they glide home over their source (Home), or onto their base when they leave
    // the hand.
    internal sealed class MapFigureHand
    {
        public VisualElement Layer { get; }
        // A piece came down or dissolved: the places show what stands there again.
        public event Action Changed;
        private readonly MapCanvas map;
        private readonly List<MapHandPiece> pieces = new List<MapHandPiece>();
        private readonly HashSet<string> held = new HashSet<string>();
        private readonly HashSet<string> planned = new HashSet<string>();
        // The draft's unsent move, and the figures it sends on their way.
        private readonly HashSet<string> selection = new HashSet<string>();
        private readonly HashSet<string> sending = new HashSet<string>();
        // UI-404: sent figures raised until the view flies them, and sent ones put back while the draft still sends them.
        private readonly HashSet<string> awaited = new HashSet<string>();
        private readonly HashSet<string> returned = new HashSet<string>();
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

        // UI-405: whether a raised piece of this source (its stand's name) holds figures of the unsent move.
        public bool Raises(string key) => pieces.Any(piece => piece.Key == key && Chosen(piece));

        private bool Chosen(MapHandPiece piece) =>
            piece.State == MapHandPiece.PieceState.InPlace && piece.Ids.Overlaps(selection);

        // UI-405: the pointer (map-local) takes up every raised piece of the unsent move; false when there is none.
        public bool Grab(Vector2 pointer)
        {
            var taken = false;
            foreach (var piece in pieces.Where(Chosen))
            {
                piece.Grab(pointer);
                taken = true;
            }
            return taken;
        }

        // The carried pieces stand under the pointer (map-local).
        public void Follow(Vector2 pointer)
        {
            var scale = map.Stands.FigureScale;
            foreach (var piece in pieces.Where(piece => piece.Carried))
            {
                piece.Follow(pointer);
                piece.Layout(map, scale);
            }
        }

        // Dropped: each carried piece rests on the board point under it. Returns the figures they hold.
        public HashSet<string> Release()
        {
            var dropped = new HashSet<string>();
            foreach (var piece in pieces.Where(piece => piece.Carried))
            {
                dropped.UnionWith(piece.Ids);
                piece.Rest(map.Camera.ToBoard(piece.ShownFeet));
            }
            return dropped;
        }

        // After a drop: a raised piece away from its source that no flight will take glides home over its source.
        public void Home()
        {
            foreach (var piece in pieces.Where(piece => piece.State == MapHandPiece.PieceState.InPlace && !piece.Ids.Overlaps(awaited)))
                piece.Home(Moving);
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
                {
                    awaited.ExceptWith(piece.Ids);
                    Drop(piece);
                }
        }

        // The draft's unsent move (`chosen`), the figures it sends on their way (`sent`) and the ones a planned ghost shows
        // (`ghosts`), on every draft change.
        public void Hold(IReadOnlyCollection<MapArmyStand> forces, IReadOnlyCollection<MapCharacterStand> characters,
            ICollection<string> chosen, ICollection<string> sent, ICollection<string> ghosts)
        {
            selection.Clear();
            selection.UnionWith(chosen);
            sending.Clear();
            sending.UnionWith(sent);
            planned.Clear();
            planned.UnionWith(ghosts);
            Regroup(forces, characters);
        }

        // UI-404: the step that sends `sent` left. Each raised piece holding some of them hands its place and raised pose to
        // the flight the view will bring, and stays up until then. UI-405: a dropped piece hands over the point it was
        // dropped on.
        public void Expect(ICollection<string> sent)
        {
            foreach (var piece in pieces.Where(piece => piece.State == MapHandPiece.PieceState.InPlace))
            {
                var ids = piece.Ids.Where(sent.Contains).ToList();
                if (ids.Count == 0)
                    continue;
                map.Figures.Expect(ids.Select(piece.FigureKey), piece.Ground, FlightPose.Raised);
                awaited.UnionWith(ids);
            }
        }

        // UI-404: the view will not fly these figures (by figure key): they come back down onto their base. False when the
        // hand awaited none of them.
        public bool Lapse(IEnumerable<string> keys, IReadOnlyCollection<MapArmyStand> forces,
            IReadOnlyCollection<MapCharacterStand> characters)
        {
            var ids = keys.Select(FigureId).Where(awaited.Contains).ToList();
            if (ids.Count == 0)
                return false;
            awaited.ExceptWith(ids);
            returned.UnionWith(ids.Where(sending.Contains));
            Regroup(forces, characters);
            return true;
        }

        private static string FigureId(string key) => key.Substring(key.IndexOf(':') + 1);

        // The pieces for what the hand holds: new ones lift, raised ones still wanted stay, the others leave.
        private void Regroup(IReadOnlyCollection<MapArmyStand> forces, IReadOnlyCollection<MapCharacterStand> characters)
        {
            GatherHeld();
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

        // What the hand holds: the unsent move, the figures sent on their way unless a planned ghost shows them or they
        // came back down, and the sent ones awaiting their flight.
        private void GatherHeld()
        {
            returned.IntersectWith(sending);
            held.Clear();
            held.UnionWith(selection);
            held.UnionWith(sending.Where(id => !planned.Contains(id) && !returned.Contains(id)));
            held.UnionWith(awaited);
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

        private static string CharacterKey(CharacterFigure figure) => MapCharacterFigures.Key(figure.Id);

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
            if (piece.Away && !piece.Ids.Overlaps(ids))
            {
                Replace(piece, MapHandPiece.Of(force, units));
                return;
            }
            var leaving = force.Army.Units.Where(unit => piece.Ids.Contains(unit.Id) && !ids.Contains(unit.Id)).ToList();
            piece.Hold(force, units);
            LeaveRaised(piece, force, leaving.Where(unit => !planned.Contains(unit.Id)).ToList(), false);
            LeaveRaised(piece, force, leaving.Where(unit => planned.Contains(unit.Id)).ToList(), true);
        }

        // UI-405: none of a dropped piece's figures is still held (a card flew the carried Nazgûl and arms the next one):
        // it leaves from where it was dropped, and the next one rises at the source. The new piece comes first in the hand,
        // so the leaving one is not taken for a stale piece of a wanted source and ends its own motion.
        private void Replace(MapHandPiece dropped, MapHandPiece next)
        {
            Insert(pieces.IndexOf(dropped), next);
            next.Lift(Moving);
            Leave(dropped);
        }

        // Figures leaving a raised piece go where it stands (UI-405: where it was dropped) and drop or dissolve from there.
        private void LeaveRaised(MapHandPiece from, MapArmyStand force, List<ArmyUnit> units, bool toGhost)
        {
            if (units.Count == 0)
                return;
            var leaving = MapHandPiece.Of(force, units);
            if (from.Away)
                leaving.Rest(from.Ground);
            Add(leaving);
            leaving.LeaveRaised(Moving, toGhost, () => Drop(leaving));
        }

        // A piece whose figures left the hand: dissolving into its planned ghost, gone with the view that carried it away,
        // or dropping back onto its base (UI-405: gliding home from where it was dropped).
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

        private void Add(MapHandPiece piece) => Insert(pieces.Count, piece);

        private void Insert(int index, MapHandPiece piece)
        {
            pieces.Insert(index, piece);
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
