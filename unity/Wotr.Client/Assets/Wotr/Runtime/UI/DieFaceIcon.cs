using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-97: the face glyph drawn on a die medallion. Artwork comes from graphicalassets/ui/src/dice/faces
    // through Styles/DicePanel.uss; an unrolled die shows its blank medallion.
    public sealed class DieFaceIcon : VisualElement
    {
        public const string ClassName = "die-face";

        public DieFaceIcon(DieFace? face)
        {
            pickingMode = PickingMode.Ignore;
            AddToClassList(ClassName);
            AddToClassList(ClassName + "-" + Key(face));
        }

        public static string Key(DieFace? face) => face switch
        {
            DieFace.Character => "character",
            DieFace.Army => "army",
            DieFace.Muster => "muster",
            DieFace.ArmyMuster => "army-muster",
            DieFace.Event => "event",
            DieFace.WillOfTheWest => "will",
            DieFace.Eye => "eye",
            DieFace.CardDraw => "card-draw",
            DieFace.UseBalrog => "use-balrog",
            _ => "unrolled"
        };
    }
}
