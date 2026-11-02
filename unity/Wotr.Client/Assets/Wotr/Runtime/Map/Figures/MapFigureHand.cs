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
    // UI-444 (owner, 2026-11-02): while carried, a piece stands in its own pointer-ignoring layer laid over the map in the
    // board's root, above the HUD and the windows (CarryLayerName), so neither the map's edge nor the HUD cuts it; the
    // carried pieces shift inward together just enough to show whole on the screen (KeepOnScreen). Set down — dropped,
    // sent, home or Esc — a piece is back in the hand's layer, clipped to the map.
    internal sealed class MapFigureHand
    {
        public const string CarryLayerName = "map-hand-carried";
        // UI-444: how far a carried piece keeps from the screen's edge; its gold rim and plaque reach a little past the
        // visible part its stand measures.
        public const float ScreenMargin = 6;
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
        // UI-444: where carried pieces stand, made on the first carry.
        private VisualElement carryLayer;

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
            foreach (var piece in pieces.Where(Chosen).ToList())
            {
                piece.Grab(pointer);
                Carry(piece);
                taken = true;
            }
            return taken;
        }

        // The carried pieces stand under the pointer (map-local), whole on the screen (UI-444).
        public void Follow(Vector2 pointer)
        {
            var scale = map.Stands.FigureScale;
            var carried = pieces.Where(piece => piece.Carried).ToList();
            foreach (var piece in carried)
            {
                piece.Follow(pointer);
                piece.Layout(map, scale);
            }
            KeepOnScreen(carried, scale);
        }

        // UI-444: when the pointer is so near the screen's edge that a carried piece would cross it, the carried pieces
        // shift inward together just enough; elsewhere they stay exactly under the pointer.
        private void KeepOnScreen(IReadOnlyList<MapHandPiece> carried, float scale)
        {
            if (carried.Count == 0 || !(Screen() is Rect screen))
                return;
            var shown = carried.Select(piece => piece.CarriedBounds).Aggregate(Union);
            var shift = new Vector2(Inward(shown.xMin, shown.xMax, screen.xMin, screen.xMax),
                Inward(shown.yMin, shown.yMax, screen.yMin, screen.yMax));
            if (shift == Vector2.zero)
                return;
            foreach (var piece in carried)
            {
                piece.Shift(shift);
                piece.Layout(map, scale);
            }
        }

        // What the screen shows of the map (map-local), ScreenMargin inside its edges: the map within its host; null before
        // layout.
        private Rect? Screen()
        {
            var frame = map.layout;
            if (float.IsNaN(frame.width) || frame.width <= 0 || frame.height <= 0)
                return null;
            var area = new Rect(0, 0, frame.width, frame.height);
            if (map.parent?.layout is Rect host && !float.IsNaN(host.width))
                area = Rect.MinMaxRect(Mathf.Max(0, -frame.x), Mathf.Max(0, -frame.y),
                    Mathf.Min(frame.width, host.width - frame.x), Mathf.Min(frame.height, host.height - frame.y));
            return Rect.MinMaxRect(area.xMin + ScreenMargin, area.yMin + ScreenMargin, area.xMax - ScreenMargin, area.yMax - ScreenMargin);
        }

        // How far an extent moves to lie inside a range: none when it does; one too large starts where the range starts.
        private static float Inward(float min, float max, float low, float high)
        {
            if (max - min >= high - low || min < low)
                return low - min;
            return max > high ? high - max : 0;
        }

        // UI-444: the carried piece moves to the carry layer, laid over the map and in front of everything in its host.
        private void Carry(MapHandPiece piece)
        {
            var host = map.parent;
            if (host == null)
                return;
            if (carryLayer == null)
            {
                carryLayer = new VisualElement { name = CarryLayerName, pickingMode = PickingMode.Ignore };
                carryLayer.AddToClassList(CarryLayerName);
                carryLayer.style.position = Position.Absolute;
            }
            if (carryLayer.parent != host)
                host.Add(carryLayer);
            carryLayer.BringToFront();
            var frame = map.layout;
            carryLayer.style.left = frame.x;
            carryLayer.style.top = frame.y;
            carryLayer.style.width = frame.width;
            carryLayer.style.height = frame.height;
            carryLayer.Add(piece.Stand.Root);
        }

        // UI-444: every piece no longer carried stands in the hand's layer again, clipped to the map.
        private void SetDown()
        {
            if (carryLayer == null || carryLayer.childCount == 0)
                return;
            foreach (var piece in pieces.Where(piece => !piece.Carried && piece.Stand.Root.parent == carryLayer))
                Layer.Add(piece.Stand.Root);
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
            SetDown();
            return dropped;
        }

        // After a drop: a raised piece away from its source that no flight will take glides home over its source.
        public void Home()
        {
            foreach (var piece in pieces.Where(piece => piece.State == MapHandPiece.PieceState.InPlace && !piece.Ids.Overlaps(awaited)))
                piece.Home(Moving);
            SetDown();
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
        // dropped on; UI-428: and the height it is held at there.
        public void Expect(ICollection<string> sent)
        {
            var scale = map.Stands.FigureScale;
            foreach (var piece in pieces.Where(piece => piece.State == MapHandPiece.PieceState.InPlace))
            {
                var ids = piece.Ids.Where(sent.Contains).ToList();
                if (ids.Count == 0)
                    continue;
                map.Figures.Expect(ids.Select(piece.FigureKey), piece.Handoff(scale));
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
            SetDown();
            foreach (var piece in pieces)
                piece.Layout(map, scale);
        }

        private bool Moving => Appearance.Moving(map);

        private static Rect Union(Rect a, Rect b) =>
            Rect.MinMaxRect(Mathf.Min(a.xMin, b.xMin), Mathf.Min(a.yMin, b.yMin), Mathf.Max(a.xMax, b.xMax), Mathf.Max(a.yMax, b.yMax));
    }
}
