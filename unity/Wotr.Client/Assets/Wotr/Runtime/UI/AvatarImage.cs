using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-505 (AVATAR-01 A8): a player's small picture, to the left of their name. Each picture is loaded once into the
    // shared cache of IconButton. An unknown identifier, from a newer server, shows the Eye; an empty one names nobody and
    // shows nothing.
    public static class AvatarImage
    {
        public const int Tiny = 16;
        public const int Small = 20;
        public const int Medium = 32;
        public const int Presentation = 96;
        private const string Folder = "Wotr/Art/Avatars/";
        private const string ClassName = "avatar-image";
        private const float Gap = 4;

        public static VisualElement Create(int size, string id = "")
        {
            var image = new VisualElement { pickingMode = PickingMode.Ignore };
            image.AddToClassList(ClassName);
            image.style.flexShrink = 0;
            image.style.width = size;
            image.style.height = size;
            image.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            Show(image, id);
            return image;
        }

        public static void Show(VisualElement image, string id)
        {
            var named = !string.IsNullOrEmpty(id);
            image.style.display = named ? DisplayStyle.Flex : DisplayStyle.None;
            if (!named)
            {
                image.userData = null;
                return;
            }
            var known = AvatarIds.IsKnown(id) ? id : AvatarIds.Eye;
            if (Equals(image.userData, known))
                return;
            image.userData = known;
            image.style.backgroundImage = IconButton.Background(Folder + known);
        }

        // A picture and a label side by side in a small row that takes the label's place. Not a picture inside the label: UI
        // Toolkit does not measure the text of an element with children, so that would collapse a label sized by its text.
        // The label keeps its size and truncates as before. A row's picture hidden, the label stands at the row's start.
        public static VisualElement Line(Label label, VisualElement image)
        {
            var line = new VisualElement { pickingMode = PickingMode.Ignore };
            line.style.flexDirection = FlexDirection.Row;
            line.style.alignItems = Align.Center;
            line.style.flexShrink = 0;
            line.style.minWidth = 0;
            image.style.marginRight = Gap;
            label.style.flexGrow = 1;
            label.style.flexShrink = 1;
            label.style.minWidth = 0;
            line.Add(image);
            line.Add(label);
            return line;
        }

        // The identifier a holder's visible picture shows, or null.
        public static string Shown(VisualElement holder)
        {
            foreach (var image in holder.Query(className: ClassName).ToList())
                if (image.userData is string id)
                    return id;
            return null;
        }
    }
}
