using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-570: where the button stands on its place — centred over it, from its top-left corner or from its bottom-left
    // corner (owner, 2026-11-22, of the battle: "show it in the bottom-left corner, where the row of the actual dice starts").
    // RightCentre: inside a row's right end, centred on its height (the Hunt window's summary row, owner: "the button is
    // terribly large and runs over other parts of the interface").
    public enum GateAlign { Centre, TopLeft, BottomLeft, RightCentre }

    // UI-570 (owner, 2026-11-22: "every draw of a tile or a die or anything else should not happen by itself but need a
    // confirmation, pressing a button with a die's picture or 'Perform hunt' or the like"; the owner chose the client and
    // the player's own draws only, never the action dice of a turn's start): with Settings' "Roll my own dice and draws" on,
    // the beat that would show one of the player's own draws waits behind this button. The server has already drawn; the
    // click only lets the result show, and the beat then plays as it would have. The button stands in the presentation
    // layer (which otherwise takes only Skip) over the place the result will show, or in the middle when that place is not
    // on screen; it takes focus, so Enter or Space presses it. Skip or Esc end the presentation and show the result at once.
    // A draw that follows a click whose only purpose is that draw (Reroll selected, a Hunt reroll, a corruption attempt's
    // Draw) never waits (owner, the same day: "we already have a button for that action — apply it everywhere as a rule").
    public sealed class DrawGate : IPresentationBeat
    {
        public const string Name = "presentation-draw-gate";
        // A smaller button for a window's row (the Hunt window).
        public const string CompactClass = Name + "-compact";
        private const float RowInset = 4;
        private readonly PresentationLayer layer;
        private readonly Func<Rect?> place;
        private readonly GateAlign align;
        private readonly Func<double, IPresentationBeat> start;
        private IPresentationBeat beat;
        private bool clicked;
        private bool ended;

        public Button Button { get; }

        public DrawGate(PresentationLayer layer, string text, bool die, Func<VisualElement> anchor, Func<double, IPresentationBeat> start,
            GateAlign align = GateAlign.Centre)
            : this(layer, text, die, () => Bounds(anchor?.Invoke()), align, start) { }

        // place: the place on the board (world coordinates), or null for the middle of the layer.
        public DrawGate(PresentationLayer layer, string text, bool die, Func<Rect?> place, GateAlign align,
            Func<double, IPresentationBeat> start)
        {
            this.layer = layer;
            this.place = place;
            this.align = align;
            this.start = start;
            Button = Create(text, die, () => clicked = true);
            layer.Root.Add(Button);
            Place();
            Button.schedule.Execute(() =>
            {
                Place();
                Button.Focus();
            });
        }

        // A button with a die's picture (a roll) or the draw's words alone (a tile, a pick).
        // The gate as a window row's smaller button.
        public DrawGate Compact()
        {
            Button.AddToClassList(CompactClass);
            return this;
        }

        public static Button Create(string text, bool die, Action click)
        {
            var button = new Button(click) { name = Name, text = "" };
            button.AddToClassList(Name);
            if (die)
            {
                // The battle's die art (battle/die-N) comes with its class.
                var face = new DiePips { Illustrated = true, pickingMode = PickingMode.Ignore };
                face.AddToClassList("battle-pips");
                face.AddToClassList(Name + "-die");
                face.Apply(6);
                button.Add(face);
            }
            var label = new Label(text) { pickingMode = PickingMode.Ignore, enableRichText = false };
            label.AddToClassList(Name + "-text");
            button.Add(label);
            button.tooltip = text + " · Shows your result";
            return button;
        }

        public bool Advance(double now)
        {
            if (ended)
                return true;
            if (!clicked)
            {
                Place();
                return false;
            }
            if (beat == null)
            {
                Button.RemoveFromHierarchy();
                beat = start(now);
                if (beat == null)
                {
                    ended = true;
                    return true;
                }
            }
            ended = beat.Advance(now);
            return ended;
        }

        public void Finish()
        {
            if (ended)
                return;
            ended = true;
            Button.RemoveFromHierarchy();
            beat?.Finish();
        }

        // An element's place on the board while it is shown, else null.
        public static Rect? Bounds(VisualElement element) =>
            element != null && PresentationTargets.Shown(element) ? element.worldBound : (Rect?)null;

        // On the place the result will show, as aligned, or in the middle of the layer; always inside the layer.
        private void Place()
        {
            var area = layer.Root.contentRect;
            var size = Button.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0)
                size = new Vector2(120, 44);
            var target = place?.Invoke();
            var at = area.center - size / 2;
            if (target is Rect world && !float.IsNaN(world.x))
            {
                var local = layer.Local(world);
                at = align switch
                {
                    GateAlign.TopLeft => local.position,
                    GateAlign.BottomLeft => new Vector2(local.xMin, local.yMax - size.y),
                    GateAlign.RightCentre => new Vector2(local.xMax - size.x - RowInset, local.center.y - size.y / 2),
                    _ => local.center - size / 2
                };
            }
            Button.style.position = Position.Absolute;
            Button.style.left = Mathf.Clamp(at.x, 0, Mathf.Max(0, area.width - size.x));
            Button.style.top = Mathf.Clamp(at.y, 0, Mathf.Max(0, area.height - size.y));
        }
    }
}
