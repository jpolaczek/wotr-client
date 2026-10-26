using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-309: one identity on an observed trace — a named character by its portrait (the painted portrait where one exists,
    // otherwise its figure) and its name, or identical figures (Nazgûl, an army's units) by their figure art and a count.
    public sealed class CharacterMark
    {
        public string DefinitionId { get; set; } = "";
        public FigureArt Art { get; set; }
        public Side Side { get; set; }
        public string Text { get; set; } = "";
        // A character removed from play: its portrait dims.
        public bool Removed { get; set; }

        public Sprite Picture => (DefinitionId.Length > 0 ? PortraitArtwork.Sprite(DefinitionId) : null) ?? FigureArtwork.Sprite(Art);
    }
}
