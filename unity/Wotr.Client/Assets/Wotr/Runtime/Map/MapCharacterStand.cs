using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-402: one character on the map — its button and miniature, its projection, the place it stands on and, on a
    // crowded region's last place, the characters stacked behind it.
    internal sealed class MapCharacterStand
    {
        public Button Button;
        public MapCharacterArtwork Artwork;
        public MapStand Stand;
        public CharacterFigure Figure;
        public bool Visible;
        // Hidden behind the front of a stacked pair (the region has more characters than places).
        public bool Stacked;
        // The level it was last laid out at: a miniature on its stand, or a token.
        public bool? Figures;
        // The front of a stacked pair: itself first, then every character standing behind it.
        public readonly List<CharacterFigure> Members = new List<CharacterFigure>();

        public string Region => Figure.RegionId;
        public Rect Bounds => Stand.Bounds;

        // The characters it shows, its own figure and, on a stacked pair, the second one and how many more.
        public void Show(IReadOnlyList<CharacterFigure> members)
        {
            Members.Clear();
            Members.AddRange(members);
            var second = members.Count > 1 ? members[1] : null;
            Artwork.Apply(Figure, second, Mathf.Max(0, members.Count - 2));
            Fit();
        }

        // Switches between the miniature and the token, with the point under its feet that each level stands on.
        public void ShowLevel(bool figures)
        {
            if (Figures == figures)
                return;
            Figures = figures;
            Artwork.Layout(figures);
            Fit();
        }

        private void Fit()
        {
            if (Figures is bool figures)
                Stand.Design(MapCharacterArtwork.Feet(figures), Artwork.Visible(figures));
        }
    }
}
