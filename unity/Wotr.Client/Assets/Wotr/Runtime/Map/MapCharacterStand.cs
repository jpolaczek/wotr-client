using System.Collections.Generic;
using System.Linq;
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
        // UI-403: its characters held in the hand (or shown by a planned ghost elsewhere).
        private readonly HashSet<string> lifted = new HashSet<string>();

        public string Region => Figure.RegionId;
        public Rect Bounds => Stand.Bounds;

        // The characters it shows, its own figure and, on a stacked pair, the second one and how many more.
        public void Show(IReadOnlyList<CharacterFigure> members)
        {
            Members.Clear();
            Members.AddRange(members);
            ShowMembers();
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

        // UI-403 (SetLiftMask): the characters that left the table; the stand shows the ones that stay (on a stacked pair
        // the next in line comes to the front) and disappears when none stays. Nothing changes while the same are lifted.
        public void Mask(ICollection<string> ids)
        {
            var now = Members.Where(member => ids.Contains(member.Id)).Select(member => member.Id).ToList();
            if (lifted.SetEquals(now))
                return;
            lifted.Clear();
            lifted.UnionWith(now);
            ShowMembers();
        }

        private void ShowMembers()
        {
            var staying = Members.Where(member => !lifted.Contains(member.Id)).ToList();
            var empty = staying.Count == 0 && Members.Count > 0;
            Button.EnableInClassList(MapArmyStand.LiftedClass, empty);
            var shown = empty ? Members : staying;
            if (shown.Count == 0)
                return;
            var second = shown.Count > 1 ? shown[1] : null;
            Artwork.Apply(shown[0], second, Mathf.Max(0, shown.Count - 2));
            Fit();
        }

        private void Fit()
        {
            if (Figures is bool figures)
                Stand.Design(MapCharacterArtwork.Feet(figures), Artwork.Visible(figures));
        }
    }
}
