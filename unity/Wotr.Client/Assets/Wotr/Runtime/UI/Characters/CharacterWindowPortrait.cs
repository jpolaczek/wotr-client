using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-447 (owner, 2026-11-02: "the character's window should also show its portrait or icon"): who the window is about,
    // as a round gilt medallion at the head of its title bar — the painted portrait of the version it shows (the battle
    // portraits, PortraitArtwork; a ruler's of his state, as the Sovereigns strip shows him), or the figure's own art
    // where no portrait exists. Presentation only: it takes no pointer and is written only when the version changes.
    public sealed class CharacterWindowPortrait
    {
        public const string ClassName = "character-window-portrait";
        public VisualElement Root { get; } = new VisualElement { name = ClassName, pickingMode = PickingMode.Ignore };
        public Sprite Shown { get; private set; }
        private string definition;

        public CharacterWindowPortrait() => Root.AddToClassList(ClassName);

        public void Apply(string definitionId, Side side)
        {
            if (definitionId == definition)
                return;
            definition = definitionId;
            Shown = Art(definitionId, side);
            Root.style.backgroundImage = Shown != null ? new StyleBackground(Shown) : new StyleBackground(StyleKeyword.None);
            Root.style.display = Shown != null ? DisplayStyle.Flex : DisplayStyle.None;
            Root.EnableInClassList("free-peoples", side == Side.FreePeoples);
            Root.EnableInClassList("shadow", side == Side.Shadow);
        }

        private static Sprite Art(string definitionId, Side side)
        {
            if (string.IsNullOrEmpty(definitionId))
                return null;
            return PortraitArtwork.Sprite(definitionId) ?? FigureArtwork.Sprite(FigureAppearances.Character(definitionId, side));
        }
    }
}
