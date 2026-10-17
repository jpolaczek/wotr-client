using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-115: a named character in the battle as a medallion. Until the portrait atlas (UI-120) exists the
    // medallion shows the figure silhouette from the board atlas and the character's initial.
    public sealed class PortraitMedallion : VisualElement
    {
        public const string CancelledClass = "portrait-cancelled";
        // UI-120: set when the portrait atlas supplies this character; the initial then stays out of the way.
        public const string IllustratedClass = "portrait-illustrated";

        public PortraitMedallion(CharacterView character, string fullName, string shortName, bool cancelled)
        {
            name = "portrait-" + character.Id;
            AddToClassList("portrait-medallion");
            AddToClassList(character.Side == Side.Shadow ? "portrait-shadow" : "portrait-free");
            EnableInClassList(CancelledClass, cancelled);
            pickingMode = PickingMode.Position;
            tooltip = fullName + (cancelled ? "\nLeadership and abilities cancelled this round" : "");
            var figure = FigureArtwork.Image(FigureAppearances.Art(character));
            figure.name = "portrait-figure";
            figure.AddToClassList("portrait-figure");
            var portrait = PortraitArtwork.Sprite(character.DefinitionId);
            if (portrait != null)
            {
                figure.sprite = portrait;
                AddToClassList(IllustratedClass);
            }
            Add(figure);
            var initial = new Label(shortName.Length > 0 ? shortName.Substring(0, 1) : "?")
                { name = "portrait-initial", pickingMode = PickingMode.Ignore, enableRichText = false };
            initial.AddToClassList("portrait-initial");
            Add(initial);
        }
    }
}
