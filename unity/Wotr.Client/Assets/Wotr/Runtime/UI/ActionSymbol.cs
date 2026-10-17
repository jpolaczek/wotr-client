using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // Neutral vector placeholders; independent of rule IDs and future artwork.
    public sealed class ActionSymbol : VisualElement
    {
        // UI-91: symbols on parchment cards are drawn in brown ink instead of the light default.
        public const string InkClass = "ink-symbol";
        private static readonly Color Light = new Color(.94f, .90f, .75f);
        private static readonly Color Ink = new Color(.23f, .14f, .07f);
        private readonly DieFace? face;

        public ActionSymbol(DieFace? face)
        {
            this.face = face;
            pickingMode = PickingMode.Ignore;
            AddToClassList("action-symbol");
            generateVisualContent += Draw;
        }

        private void Draw(MeshGenerationContext context)
        {
            var p = context.painter2D;
            p.strokeColor = ClassListContains(InkClass) ? Ink : Light;
            p.lineWidth = 1.7f;
            switch (face)
            {
                case DieFace.Army:
                    Line(p, .2f, .82f, .82f, .18f);
                    Line(p, .18f, .18f, .8f, .82f);
                    Line(p, .12f, .64f, .37f, .9f);
                    Line(p, .63f, .9f, .89f, .64f);
                    break;
                case DieFace.Muster:
                    Line(p, .24f, .9f, .24f, .1f);
                    Path(p, true, new Vector2(.24f, .12f), new Vector2(.83f, .12f), new Vector2(.68f, .38f), new Vector2(.83f, .59f), new Vector2(.24f, .59f));
                    break;
                case DieFace.Character:
                    Path(p, true, new Vector2(.3f, .1f), new Vector2(.7f, .1f), new Vector2(.85f, .52f), new Vector2(.85f, .85f), new Vector2(.15f, .85f), new Vector2(.15f, .52f));
                    Line(p, .15f, .5f, .85f, .5f);
                    Line(p, .5f, .5f, .5f, .85f);
                    break;
                case DieFace.Event:
                case DieFace.CardDraw:
                    Path(p, true, new Vector2(.23f, .1f), new Vector2(.8f, .1f), new Vector2(.8f, .85f), new Vector2(.23f, .85f));
                    Line(p, .35f, .34f, .66f, .34f);
                    Line(p, .35f, .55f, .66f, .55f);
                    if (face == DieFace.CardDraw)
                        Path(p, false, new Vector2(.1f, .25f), new Vector2(.1f, .95f), new Vector2(.65f, .95f));
                    break;
                case DieFace.Eye:
                    Path(p, true, new Vector2(.05f, .5f), new Vector2(.3f, .25f), new Vector2(.7f, .25f), new Vector2(.95f, .5f), new Vector2(.7f, .75f), new Vector2(.3f, .75f));
                    Line(p, .5f, .27f, .5f, .73f);
                    break;
                case DieFace.ArmyMuster:
                    Line(p, .15f, .85f, .78f, .13f);
                    Line(p, .22f, .8f, .22f, .12f);
                    Path(p, false, new Vector2(.22f, .12f), new Vector2(.75f, .12f), new Vector2(.55f, .4f), new Vector2(.22f, .4f));
                    break;
                case DieFace.UseBalrog:
                    Path(p, true, new Vector2(.48f, .08f), new Vector2(.55f, .5f), new Vector2(.85f, .25f), new Vector2(.78f, .8f), new Vector2(.5f, .95f), new Vector2(.22f, .8f), new Vector2(.14f, .4f));
                    break;
                default:
                    Line(p, .5f, .1f, .5f, .9f);
                    Line(p, .1f, .5f, .9f, .5f);
                    if (face == DieFace.WillOfTheWest)
                    {
                        Line(p, .24f, .24f, .76f, .76f);
                        Line(p, .24f, .76f, .76f, .24f);
                    }
                    break;
            }
        }

        private void Line(Painter2D painter, float x1, float y1, float x2, float y2) =>
            Path(painter, false, new Vector2(x1, y1), new Vector2(x2, y2));

        private void Path(Painter2D painter, bool close, params Vector2[] points)
        {
            painter.BeginPath();
            for (var i = 0; i < points.Length; i++)
            {
                var point = new Vector2(points[i].x * contentRect.width, points[i].y * contentRect.height);
                if (i == 0)
                    painter.MoveTo(point);
                else
                    painter.LineTo(point);
            }
            if (close)
                painter.ClosePath();
            painter.Stroke();
        }
    }
}
