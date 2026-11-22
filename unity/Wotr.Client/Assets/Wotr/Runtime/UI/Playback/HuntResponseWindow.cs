using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client
{
    // UI-573 (owner, 2026-11-22: "we need a better visualization of what the Free Peoples player does during the Hunt, which
    // skills of which characters they use"): beside the Hunt window, a window naming the Free Peoples' answer — the
    // character's portrait, the table card or the Ring, the answer's title and printed text — and, as the Hunt's next
    // entries show it, what it did (the damage it took off, the Companion who left, the Corruption). It stays while that
    // Hunt goes on and leaves with the next answer, a new Hunt or the release. Read-only: nothing in it takes the pointer.
    public sealed class HuntResponseWindow
    {
        public const string Name = "presentation-hunt-response";
        public const string TitleName = Name + "-title";
        public const string DetailName = Name + "-detail";
        public const string ResultName = Name + "-result";
        private const float Gap = 8;
        private readonly BoardScreen board;
        private readonly PresentationLayer layer;
        private Label result;

        public HuntResponseWindow(BoardScreen board, PresentationLayer layer)
        {
            this.board = board;
            this.layer = layer;
        }

        // The window on the board now, or null.
        public VisualElement Root { get; private set; }

        public void Show(HuntResponseCaption caption)
        {
            Close();
            Root = new VisualElement { name = Name, pickingMode = PickingMode.Ignore };
            Root.AddToClassList(Name);
            Root.Add(Picture(caption));
            var text = new VisualElement { pickingMode = PickingMode.Ignore };
            text.AddToClassList(Name + "-text");
            text.Add(Text(TitleName, caption.Title));
            if (caption.Detail.Length > 0)
                text.Add(Text(DetailName, caption.Detail));
            result = Text(ResultName, "");
            result.style.display = DisplayStyle.None;
            text.Add(result);
            Root.Add(text);
            Root.RegisterCallback<GeometryChangedEvent>(_ => Place());
            layer.Root.Add(Root);
            Place();
            Appearance.Arrive(Root);
        }

        // What the answer did; a later fact replaces an earlier one.
        public void Result(string text)
        {
            if (Root == null || text.Length == 0)
                return;
            result.text = text;
            result.style.display = DisplayStyle.Flex;
        }

        public void Close()
        {
            Root?.RemoveFromHierarchy();
            Root = null;
            result = null;
        }

        private static VisualElement Picture(HuntResponseCaption caption)
        {
            var frame = new VisualElement { pickingMode = PickingMode.Ignore };
            frame.AddToClassList(Name + "-picture");
            if (caption.Portrait.Length > 0)
            {
                frame.AddToClassList(Name + "-portrait");
                frame.Add(FigureArtwork.Image(FigureAppearances.Character(caption.Portrait, Side.FreePeoples)));
            }
            else
            {
                frame.AddToClassList(caption.Ring ? Name + "-ring" : caption.Card.Length > 0 ? Name + "-card" : Name + "-answer");
            }
            return frame;
        }

        private static Label Text(string name, string text)
        {
            var label = new Label(text) { name = name, pickingMode = PickingMode.Ignore, enableRichText = false };
            label.AddToClassList(name);
            return label;
        }

        // Beside the Hunt window, on the side with room (its right, else its left), level with its top; in the middle of the
        // layer while the Hunt window is not shown.
        private void Place()
        {
            if (Root == null)
                return;
            var area = layer.Root.contentRect;
            var size = Root.layout.size;
            if (float.IsNaN(size.x) || size.x <= 0)
                size = new Vector2(360, 120);
            var at = area.center - size / 2;
            var hunt = board.Hunt.Root;
            if (PresentationTargets.Shown(hunt))
            {
                var window = layer.Local(hunt.worldBound);
                var right = window.xMax + Gap;
                at = new Vector2(right + size.x <= area.width ? right : window.xMin - Gap - size.x, window.yMin);
            }
            Root.style.position = Position.Absolute;
            Root.style.left = Mathf.Clamp(at.x, 0, Mathf.Max(0, area.width - size.x));
            Root.style.top = Mathf.Clamp(at.y, 0, Mathf.Max(0, area.height - size.y));
        }
    }
}
