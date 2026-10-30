using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-403 (TABLETOP_FIGURES_UX, "The lifted figure"): one object in the player's hand — the lifted units of one force on
    // their own base, or one character on its stand — drawn by the same miniature class as on the table and at the same
    // scale. Its holder stands on the source's place and follows the camera by translate and scale only (MapStand). Inside
    // it the pose raises the miniature (scale 1.10 round its feet and 12 design points up, so 12 px × the figure's scale on
    // the screen) while a contact shadow stays on the table (scale .8, opacity .35): a class whose translate, scale and
    // opacity transition under .motion-enabled (Styles/Armies.uss), so nothing is written per frame. Reduced motion shows
    // the static raised pose. States: InPlace (raised over its place), Returning (dropping back onto its base, then gone)
    // and Planned (dissolving into the planned ghost at its destination, then gone).
    internal sealed class MapHandPiece
    {
        public enum PieceState
        {
            InPlace,
            Returning,
            Planned
        }

        public const string PieceClass = "figure-hand-piece";
        public const string LiftedClass = "figure-lifted";
        public const string ReturningClass = "hand-returning";
        public const string PlannedClass = "hand-planned";
        // The pose in design points: the miniature grows round its feet and rises (Styles/Armies.uss).
        public const float LiftScale = 1.1f;
        public const float Rise = 12;
        public const float ShadowScale = .8f;
        public const float ShadowOpacity = .35f;
        public const long ReturnMilliseconds = 220;
        public const long DissolveMilliseconds = 250;
        // A little after the transition ends, so its last frame is drawn.
        private const long Margin = 40;

        public string Key { get; }
        public string Region { get; }
        public bool Units { get; }
        public PieceState State { get; private set; } = PieceState.InPlace;
        // The figures it holds, by id.
        public HashSet<string> Ids { get; } = new HashSet<string>();
        public MapStand Stand { get; }
        public bool Visible { get; private set; }
        private readonly VisualElement root;
        private readonly Button miniature;
        private readonly MapStand source;
        private MapArmyArtwork army;
        private Vector2 shownFeet;
        private float shownScale;
        private IVisualElementScheduledItem ending;
        private Action afterLayout;

        private MapHandPiece(string key, string region, bool units, MapStand source, Vector2 feet, Vector2 size)
        {
            Key = key;
            Region = region;
            Units = units;
            this.source = source;
            root = new VisualElement { name = "hand-" + key, pickingMode = PickingMode.Ignore };
            root.AddToClassList(PieceClass);
            root.style.position = Position.Absolute;
            root.style.width = size.x;
            root.style.height = size.y;
            root.Add(Shadow(units));
            var lift = PosePart("hand-lift");
            lift.style.transformOrigin = new TransformOrigin(feet.x, feet.y, 0);
            root.Add(lift);
            miniature = new Button { name = "hand-miniature-" + key, focusable = false, pickingMode = PickingMode.Ignore };
            lift.Add(miniature);
            Stand = new MapStand(root, size);
        }

        // The contact shadow that stays on the table while the miniature is raised (the miniature's own one lifts with it
        // and is hidden).
        private static VisualElement Shadow(bool units)
        {
            var shadow = PosePart("hand-shadow");
            shadow.AddToClassList(units ? "hand-army-shadow" : "hand-character-shadow");
            return shadow;
        }

        // A part of the pose: its transform changes, never its layout. The name doubles as its class (Styles/Armies.uss).
        private static VisualElement PosePart(string name)
        {
            var element = new VisualElement
            {
                name = name,
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.DynamicTransform
            };
            element.AddToClassList(name);
            return element;
        }

        // The lifted units of a force, on their own base in the force's colours.
        public static MapHandPiece Of(MapArmyStand force, IEnumerable<ArmyUnit> units)
        {
            var garrison = force.Army.Garrison;
            var feet = MapArmyArtwork.Feet(garrison, true);
            var piece = new MapHandPiece(force.Button.name, force.Region, true, force.Stand, feet, MapArmyArtwork.HolderSize);
            var button = piece.miniature;
            button.AddToClassList("army");
            button.AddToClassList(force.Army.Side == Side.FreePeoples ? "fp" : "sh");
            button.EnableInClassList("garrison", garrison);
            button.AddToClassList("selected-force");
            piece.army = new MapArmyArtwork(button);
            piece.army.Layout(true);
            piece.Stand.Design(feet, MapArmyArtwork.Visible(garrison, true));
            piece.Hold(force, units);
            return piece;
        }

        // A lifted character on its own stand.
        public static MapHandPiece Of(MapCharacterStand character, CharacterFigure figure)
        {
            var feet = MapCharacterArtwork.Feet(true);
            var piece = new MapHandPiece("character-" + figure.Id, figure.RegionId, false, character.Stand, feet,
                MapCharacterArtwork.HolderSize);
            var artwork = new MapCharacterArtwork(piece.miniature);
            artwork.Apply(figure, null, 0);
            artwork.Layout(true);
            piece.miniature.AddToClassList(MapCharacterArtwork.SelectedClass);
            piece.Stand.Design(feet, artwork.Visible(true));
            piece.Ids.Add(figure.Id);
            return piece;
        }

        // The units it holds of its force (a split changes them while it is raised).
        public void Hold(MapArmyStand force, IEnumerable<ArmyUnit> units)
        {
            var held = units.ToList();
            Ids.Clear();
            Ids.UnionWith(held.Select(unit => unit.Id));
            army.Apply(ArmyMarker.From(force.Army.RegionId, force.Army.Side, force.Army.Garrison, held));
        }

        // Whether its source still stands on the table.
        public bool HasSource => source.Root.parent != null;

        // UI-404: the board point it hovers over (its source's place), where the flight that carries it on begins.
        public Vector2 SourceFeet => source.Feet;

        // UI-404: one of its figures as the board's figure positions name it (BoardFigurePosition.Key).
        public string FigureKey(string id) => (Units ? BoardFigurePosition.UnitPrefix : BoardFigurePosition.CharacterPrefix) + id;

        // It rises from its base: at once into the raised pose under Reduced motion, otherwise once it has been laid out,
        // so the pose transitions.
        public void Lift(bool moving)
        {
            State = PieceState.InPlace;
            if (!moving)
            {
                root.AddToClassList(LiftedClass);
                return;
            }
            root.RegisterCallback<GeometryChangedEvent>(Raise);
        }

        private void Raise(GeometryChangedEvent evt)
        {
            root.UnregisterCallback<GeometryChangedEvent>(Raise);
            if (State == PieceState.InPlace)
                root.AddToClassList(LiftedClass);
        }

        // Esc, undo or an attack: it drops back onto its base, then `gone` takes it away.
        public void Return(bool moving, Action gone)
        {
            State = PieceState.Returning;
            root.RemoveFromClassList(LiftedClass);
            root.AddToClassList(ReturningClass);
            End(moving, ReturnMilliseconds, gone);
        }

        // A new piece of figures that were raised with others and leave them — left behind in the region panel, or flown by
        // a card's planned flight: it shows raised first and, once laid out, drops back onto its base or dissolves.
        public void LeaveRaised(bool moving, bool planned, Action gone)
        {
            State = planned ? PieceState.Planned : PieceState.Returning;
            if (!moving)
            {
                gone();
                return;
            }
            root.AddToClassList(LiftedClass);
            afterLayout = planned ? () => Dissolve(true, gone) : (Action)(() => Return(true, gone));
            root.RegisterCallback<GeometryChangedEvent>(Lower);
        }

        private void Lower(GeometryChangedEvent evt)
        {
            root.UnregisterCallback<GeometryChangedEvent>(Lower);
            afterLayout();
        }

        // A planned ghost shows it at its destination now: it dissolves where it hovers, then `gone` takes it away.
        public void Dissolve(bool moving, Action gone)
        {
            State = PieceState.Planned;
            root.AddToClassList(PlannedClass);
            End(moving, DissolveMilliseconds, gone);
        }

        private void End(bool moving, long milliseconds, Action gone)
        {
            ending?.Pause();
            if (!moving)
            {
                gone();
                return;
            }
            ending = root.schedule.Execute(gone);
            ending.ExecuteLater(milliseconds + Margin);
        }

        // Stands it over its source's place at the figures' scale; hidden while the place is out of reach of the view.
        public void Layout(MapCanvas map, float scale)
        {
            Visible = map.Stands.Place(Stand, source.Feet, scale);
            Stand.Root.style.display = Visible ? DisplayStyle.Flex : DisplayStyle.None;
            shownFeet = map.Camera.ToScreen(source.Feet);
            shownScale = scale;
        }

        // Where its raised miniature shows on the map (map-local): its visible part grown round its feet and risen.
        public Rect LiftedBounds
        {
            get
            {
                var bounds = Stand.Bounds;
                var rise = Rise * shownScale;
                return Rect.MinMaxRect(Grown(bounds.xMin, shownFeet.x), Grown(bounds.yMin, shownFeet.y) - rise,
                    Grown(bounds.xMax, shownFeet.x), Grown(bounds.yMax, shownFeet.y) - rise);
            }
        }

        private static float Grown(float edge, float feet) => feet + (edge - feet) * LiftScale;

        public void Remove()
        {
            ending?.Pause();
            Stand.Remove();
        }
    }
}
