using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-137: the whole Combat card over its tile while the pointer rests on it, like a hand card that expands on
    // hover. It is a picture, never a control: it takes no pointer and hides on leave or press.
    public sealed class CombatCardPreview
    {
        public const string PreviewName = "combat-card-preview";
        public VisualElement Root { get; } = new VisualElement { name = PreviewName, pickingMode = PickingMode.Ignore };
        private readonly Label heading = new Label { name = "combat-card-preview-title", pickingMode = PickingMode.Ignore };
        private readonly Label body = new Label { name = "combat-card-preview-text", pickingMode = PickingMode.Ignore };
        private readonly Label source = new Label { name = "combat-card-preview-card", pickingMode = PickingMode.Ignore };

        public CombatCardPreview()
        {
            Root.AddToClassList(PreviewName);
            heading.enableRichText = body.enableRichText = source.enableRichText = false;
            Root.Add(heading);
            Root.Add(body);
            Root.Add(source);
            Root.style.display = DisplayStyle.None;
        }

        // Above the tile inside the row of tiles, so neighbours never move.
        public void Show(VisualElement tile, string id, string text)
        {
            if (!CardLibrary.Cards.TryGetValue(id, out var card) || tile.parent == null)
                return;
            heading.text = string.Join("/", card.Initiative) + " · " + card.CombatTitle;
            body.text = text;
            source.text = card.Title;
            Root.style.left = tile.layout.x;
            Root.style.bottom = tile.parent.layout.height - tile.layout.y + 4;
            Root.style.display = DisplayStyle.Flex;
            Root.BringToFront();
        }

        public void Hide() => Root.style.display = DisplayStyle.None;
    }
}
