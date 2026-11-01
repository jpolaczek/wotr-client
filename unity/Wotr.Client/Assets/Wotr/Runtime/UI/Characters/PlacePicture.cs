using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-436 (owner): where a character or a sovereign is, as a picture of the place captioned with its name, in place of
    // the old text button. A region's picture shows the region on the map when clicked, exactly as the old button did; a
    // place off the map (the Fellowship, not in play, removed) keeps the picture's slot, disabled, and its tooltip says
    // what being there means. The pictures are replaceable art (Art/Places, scripts/export_place_pictures.py).
    public sealed class PlacePicture
    {
        public const string ClassName = "place-picture";
        public const string OffMapClass = "place-off-map";
        private static readonly Dictionary<string, Texture2D> Pictures = new Dictionary<string, Texture2D>();
        public Button Root { get; }
        public PlaceView Place { get; private set; } = new PlaceView();
        private readonly VisualElement image = new VisualElement { name = "place-picture-image", pickingMode = PickingMode.Ignore };
        private readonly Label caption = new Label { name = "place-picture-caption", pickingMode = PickingMode.Ignore, enableRichText = false };
        private string shownKey;

        public PlacePicture(string name, Action<string> focus)
        {
            Root = new Button(() => Show(focus)) { name = name };
            Root.AddToClassList(ClassName);
            image.AddToClassList("place-picture-image");
            caption.AddToClassList("place-picture-caption");
            Root.Add(image);
            Root.Add(caption);
        }

        public string Caption => caption.text;

        public void Apply(PlaceView place)
        {
            Place = place;
            if (place.Key != shownKey)
            {
                shownKey = place.Key;
                var picture = Picture(place.Key);
                image.style.backgroundImage = picture == null ? new StyleBackground(StyleKeyword.None) : new StyleBackground(picture);
            }
            caption.text = place.Caption;
            Root.tooltip = place.Tooltip;
            Root.SetEnabled(place.OnMap);
            Root.EnableInClassList(OffMapClass, !place.OnMap);
        }

        // Loaded once per place; null for a place without a picture (PlacePictureTests keep every place covered).
        public static Texture2D Picture(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;
            if (!Pictures.TryGetValue(key, out var picture))
            {
                picture = Resources.Load<Texture2D>(PlacePresentation.Resource(key));
                Pictures[key] = picture;
            }
            return picture;
        }

        private void Show(Action<string> focus)
        {
            if (Place.OnMap)
                focus(Place.RegionId);
        }
    }
}
