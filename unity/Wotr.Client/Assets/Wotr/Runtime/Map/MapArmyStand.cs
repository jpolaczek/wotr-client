using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-126, UI-401: one force on the map — its button and miniature, its projection and the place it stands on.
    internal sealed class MapArmyStand
    {
        public Button Button;
        public MapArmyArtwork Artwork;
        public MapStand Stand;
        public ArmyMarker Army;
        public bool Selected;
        public bool Visible;
        // The level it was last laid out at: figures on a base, or a token.
        public bool? Figures;

        public string Region => Army.RegionId;
        public Rect Bounds => Stand.Bounds;

        // Switches between figures and the token, with the point under its feet that each level stands on.
        public void ShowLevel(bool figures)
        {
            if (Figures == figures)
                return;
            Figures = figures;
            Artwork.Layout(figures);
            Stand.Design(MapArmyArtwork.Feet(Army.Garrison, figures), MapArmyArtwork.Visible(Army.Garrison, figures));
        }
    }
}
