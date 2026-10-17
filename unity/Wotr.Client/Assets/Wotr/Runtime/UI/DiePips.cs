using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    public class DiePips : VisualElement
    {
        public const string FaceClassPrefix = "die-pips-";
        private int value;
        private bool illustrated;
        // UI-117: an illustrated die takes its face from the art kit (battle/die-N) through a class; nothing is painted.
        public bool Illustrated
        {
            get => illustrated;
            set
            {
                illustrated = value;
                MarkDirtyRepaint();
            }
        }
        public int Value => value;
        public DiePips()
        {
            name = "die-pips";
            pickingMode = PickingMode.Ignore;
            generateVisualContent += Draw;
        }

        public void Apply(int result)
        {
            if (value == result)
                return;
            for (var face = 1; face <= 6; face++)
                EnableInClassList(FaceClassPrefix + face, face == result);
            value = result;
            MarkDirtyRepaint();
        }

        private void Draw(MeshGenerationContext context)
        {
            if (illustrated)
                return;
            var painter = context.painter2D;
            painter.fillColor = new Color(.94f, .91f, .79f);
            if (value % 2 == 1)
                Pip(painter, .5f, .5f);
            if (value >= 2)
            {
                Pip(painter, .25f, .25f);
                Pip(painter, .75f, .75f);
            }
            if (value >= 4)
            {
                Pip(painter, .75f, .25f);
                Pip(painter, .25f, .75f);
            }
            if (value == 6)
            {
                Pip(painter, .25f, .5f);
                Pip(painter, .75f, .5f);
            }
        }

        private void Pip(Painter2D painter, float x, float y)
        {
            painter.BeginPath();
            painter.Arc(new Vector2(contentRect.width * x, contentRect.height * y), 2.1f, 0, 360);
            painter.Fill();
        }
    }
}
