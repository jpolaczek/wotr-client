using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-454 (owner, 2026-11-03: "there should also be the option to drag the unit on exactly the same principle as moving
    // it on the map"): a Reserve figure carried by the pointer, in the map's carry layer above the HUD. Over the Reserve
    // panel and the rest of the HUD it is the slot's own picture, a little larger (UI-393), centred under the pointer; over
    // the map it is the miniature it will be, held up as a carried figure is (UI-405, UI-428: the hand's piece classes,
    // Styles/Armies.uss), its feet under the pointer and shifted inward just enough to show whole on the screen (UI-444).
    // While it moves only translates change; it switches between the two looks only when it crosses onto or off the map.
    // A drop that places it hands its point and pose to its flight (Release); any other end puts it back: it glides to its
    // slot and goes (Return).
    public sealed class MapRecruitCarry
    {
        public const string IconName = "carried-recruit-icon";
        public const string PieceName = "carried-recruit";
        // UI-393: the slot's picture travels a little larger than it stands in its slot.
        public const float IconGrowth = 1.15f;
        public const long ReturnMilliseconds = 220;
        public const string ReturningClass = "carried-recruit-returning";
        public bool Active { get; private set; }
        public bool OverMap { get; private set; }
        public string Caption { get; private set; } = "";
        public string UnitId { get; private set; } = "";
        // Where the miniature's feet stand (map-local) and the pointer it follows.
        public Vector2 Feet { get; private set; }
        public VisualElement Icon => icon;
        public VisualElement Piece => stand?.Root;
        private readonly MapCanvas map;
        private readonly MapCarryLayer layer;
        private readonly Image icon = new Image { name = IconName, pickingMode = PickingMode.Ignore, scaleMode = ScaleMode.ScaleToFit };
        private MapStand stand;
        private Vector2 iconSize;
        private Side side;
        private bool planned;
        private IVisualElementScheduledItem leaving;

        internal MapRecruitCarry(MapCanvas map, MapCarryLayer layer)
        {
            this.map = map;
            this.layer = layer;
            icon.AddToClassList("drag-figure");
            icon.AddToClassList(IconName);
            icon.usageHints = UsageHints.DynamicTransform;
            icon.style.left = 0;
            icon.style.top = 0;
        }

        // The recruit is taken up at a map-local point, first as its slot's picture (`art`, drawn at `artSize`). `planned`:
        // a card's or an ability's plan, whose recruits stand on their ghost's place.
        public void Take(ArmyUnit unit, Side owner, bool plan, Sprite art, Vector2 artSize, Vector2 pointer)
        {
            Remove();
            Active = true;
            UnitId = unit.Id;
            side = owner;
            planned = plan;
            Caption = "Recruit " + TooltipText.Unit(unit.Nation, unit.Kind);
            iconSize = artSize * IconGrowth;
            icon.sprite = art;
            icon.style.width = iconSize.x;
            icon.style.height = iconSize.y;
            stand = Miniature(unit, owner);
            layer.Take(icon);
            layer.Take(stand.Root);
            OverMap = true;
            Follow(pointer, false);
        }

        // The miniature held up as the hand holds a carried piece: its shadow on the table, the figure raised above it.
        private static MapStand Miniature(ArmyUnit unit, Side owner)
        {
            var feet = MapArmyArtwork.Feet(false, true);
            var root = new VisualElement { name = PieceName, pickingMode = PickingMode.Ignore };
            root.AddToClassList(MapHandPiece.PieceClass);
            root.AddToClassList(MapHandPiece.LiftedClass);
            root.AddToClassList(MapHandPiece.CarriedClass);
            root.style.position = Position.Absolute;
            root.style.width = MapArmyArtwork.HolderSize.x;
            root.style.height = MapArmyArtwork.HolderSize.y;
            var shadow = new VisualElement { name = "hand-shadow", pickingMode = PickingMode.Ignore };
            shadow.AddToClassList("hand-shadow");
            shadow.AddToClassList("hand-army-shadow");
            root.Add(shadow);
            var lift = new VisualElement { name = "hand-lift", pickingMode = PickingMode.Ignore, usageHints = UsageHints.DynamicTransform };
            lift.AddToClassList("hand-lift");
            lift.style.transformOrigin = new TransformOrigin(feet.x, feet.y, 0);
            root.Add(lift);
            var button = new Button { name = "carried-recruit-miniature", focusable = false, pickingMode = PickingMode.Ignore };
            button.AddToClassList("army");
            button.AddToClassList(owner == Side.FreePeoples ? "fp" : "sh");
            button.AddToClassList("selected-force");
            var artwork = new MapArmyArtwork(button);
            artwork.Layout(true);
            artwork.Apply(ArmyMarker.From("", owner, false, new[] { unit }));
            lift.Add(button);
            var holder = new MapStand(root, MapArmyArtwork.HolderSize);
            holder.Design(feet, MapArmyArtwork.Visible(false, true));
            return holder;
        }

        // The pointer moved (map-local): over the map the miniature stands under it, whole on the screen; elsewhere the
        // slot's picture is centred under it.
        public void Follow(Vector2 pointer, bool overMap)
        {
            if (!Active)
                return;
            if (overMap != OverMap)
                Show(overMap);
            if (overMap)
                Stand(pointer);
            else
                icon.style.translate = new Translate(pointer.x - iconSize.x / 2, pointer.y - iconSize.y / 2);
            map.UpdateLanding();
        }

        private void Show(bool overMap)
        {
            OverMap = overMap;
            icon.style.display = overMap ? DisplayStyle.None : DisplayStyle.Flex;
            stand.Root.style.display = overMap ? DisplayStyle.Flex : DisplayStyle.None;
        }

        // UI-444: its feet under the pointer, unless it would cross the screen's edge; then just enough inward.
        private void Stand(Vector2 pointer)
        {
            var scale = map.Stands.FigureScale;
            stand.Place(pointer, scale);
            var shift = layer.Inward(Shown(pointer, scale));
            Feet = pointer + shift;
            if (shift != Vector2.zero)
                stand.Place(Feet, scale);
        }

        // All it shows with its feet at `feet`: its shadow and base on the table and the raised miniature.
        private Rect Shown(Vector2 feet, float scale)
        {
            var table = stand.Bounds;
            var raised = Raised(table, feet, scale);
            return Rect.MinMaxRect(Mathf.Min(table.xMin, raised.xMin), Mathf.Min(table.yMin, raised.yMin),
                Mathf.Max(table.xMax, raised.xMax), Mathf.Max(table.yMax, raised.yMax));
        }

        // The miniature's visible part grown round its feet and held up as a carried piece (MapHandPiece.LiftedBounds).
        private static Rect Raised(Rect table, Vector2 feet, float scale)
        {
            var rise = MapHandPiece.CarriedRise * scale;
            return Rect.MinMaxRect(Grown(table.xMin, feet.x), Grown(table.yMin, feet.y) - rise,
                Grown(table.xMax, feet.x), Grown(table.yMax, feet.y) - rise);
        }

        private static float Grown(float edge, float feet) => feet + (edge - feet) * MapHandPiece.LiftScale;

        // Where the raised miniature shows (map-local), for the caption above it; null while it is not over the map.
        public Rect? RaisedBounds => Active && OverMap ? Raised(stand.Bounds, Feet, map.Stands.FigureScale) : (Rect?)null;

        // The place its recruit would stand on in a region: a planned one on its ghost's place, a sent one on its force's.
        public Vector2 Place(string region) => planned ? map.Stands.Stands(region).Planned : map.RecruitPlace(region, side);

        // Dropped: the carry ends. Over the map, where its flight begins — its feet held up at the figures' scale, as a
        // dropped piece hands over (MapHandPiece.Handoff); null elsewhere.
        public ReserveFlightStart? Release()
        {
            if (!Active)
                return null;
            var scale = map.Stands.FigureScale;
            var start = OverMap
                ? new ReserveFlightStart(Feet, scale, new FlightStart(Vector2.zero, 1, (MapHandPiece.CarriedRise - MapHandPiece.Rise) * scale))
                : (ReserveFlightStart?)null;
            Active = false;
            map.UpdateLanding();
            return start;
        }

        // Nothing took it: as its slot's picture it glides back to the slot's centre (map-local) and goes; at once without
        // motion.
        public void Return(Vector2 slot, bool moving)
        {
            Active = false;
            map.UpdateLanding();
            if (stand == null)
                return;
            if (!moving)
            {
                Remove();
                return;
            }
            if (OverMap)
            {
                icon.style.translate = new Translate(Feet.x - iconSize.x / 2, Feet.y - iconSize.y);
                Show(false);
            }
            icon.AddToClassList(ReturningClass);
            icon.schedule.Execute(() => icon.style.translate = new Translate(slot.x - iconSize.x / 2, slot.y - iconSize.y / 2));
            leaving = icon.schedule.Execute(Remove);
            leaving.ExecuteLater(ReturnMilliseconds + 40);
        }

        // Its elements leave the layer.
        public void Remove()
        {
            leaving?.Pause();
            leaving = null;
            Active = false;
            icon.RemoveFromClassList(ReturningClass);
            icon.RemoveFromHierarchy();
            icon.style.display = DisplayStyle.Flex;
            stand?.Remove();
            stand = null;
        }
    }
}
