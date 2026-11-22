using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-570 (owner, 2026-11-22: "every draw of a tile or a die or anything else should not happen by itself but need a
    // confirmation, pressing a button with a die's picture or 'Perform hunt' or the like"; the owner chose the client and
    // the player's own draws only, never the action dice of a turn's start): with Settings' "Roll my own dice and draws" on,
    // the beat that would show one of the player's own draws waits behind this button. The server has already drawn; the
    // click only lets the result show, and the beat then plays as it would have. The button stands in the presentation
    // layer (which otherwise takes only Skip) over the place the result will show, or in the middle when that place is not
    // on screen; it takes focus, so Enter or Space presses it. Skip or Esc end the presentation and show the result at once.
    public sealed class DrawGate : IPresentationBeat
    {
        public const string Name = "presentation-draw-gate";
        private readonly PresentationLayer layer;
        private readonly Func<VisualElement> anchor;
        private readonly Func<double, IPresentationBeat> start;
        private IPresentationBeat beat;
        private bool clicked;
        private bool ended;

        public Button Button { get; }

        public DrawGate(PresentationLayer layer, string text, bool die, Func<VisualElement> anchor, Func<double, IPresentationBeat> start)
        {
            this.layer = layer;
            this.anchor = anchor;
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

        // Centred over the place the result will show, or in the middle of the layer.
        private void Place()
        {
            var target = anchor?.Invoke();
            var area = layer.Root.contentRect;
            var centre = target != null && PresentationTargets.Shown(target)
                ? layer.Local(target.worldBound).center
                : area.center;
            var size = Button.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0)
                size = new Vector2(120, 44);
            Button.style.position = Position.Absolute;
            Button.style.left = Mathf.Clamp(centre.x - size.x / 2, 0, Mathf.Max(0, area.width - size.x));
            Button.style.top = Mathf.Clamp(centre.y - size.y / 2, 0, Mathf.Max(0, area.height - size.y));
        }
    }
}
