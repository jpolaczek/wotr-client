using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-400 (TABLETOP_FIGURES_UX, "Hit shape"): a map army is clicked on its miniature — the base's ellipse and the
    // figures' columns — so the transparent headroom above a figure never takes a press; it goes to whatever lies behind.
    public sealed class MiniatureButton : Button
    {
        public Func<Vector2, bool> Shape { get; set; }
        // UI-522: a shape that reaches past the button's box (an army's row stand, wider than its holder) takes a press
        // there too; the panel finds it through the box that holds its overhanging parts.
        public bool ShapeOverhangs { get; set; }

        public MiniatureButton(Action clicked) : base(clicked)
        {
        }

        public override bool ContainsPoint(Vector2 localPoint) => Shape == null
            ? base.ContainsPoint(localPoint)
            : (ShapeOverhangs || base.ContainsPoint(localPoint)) && Shape(localPoint);
    }
}
