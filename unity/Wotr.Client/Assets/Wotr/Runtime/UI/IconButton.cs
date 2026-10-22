using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-138: a button that shows an action icon (graphicalassets/ui/src/actions) on the square gilt frame. The
    // tooltip is required: it explains what the symbol does. The icon can change (pin and pinned, select and
    // deselect) without rebuilding the button.
    public static class IconButton
    {
        public const string ClassName = "icon-button";
        private const string Folder = "Wotr/Art/UI/actions/";
        private static readonly Dictionary<string, Texture2D> Icons = new Dictionary<string, Texture2D>();

        public static Button Create(string icon, string tooltip, Action action)
        {
            var button = Frame(tooltip, action);
            SetIcon(button, icon);
            return button;
        }

        // UI-286: a picture from elsewhere in Art/UI (a die face, a character's mark) on the same frame, named by its
        // full resource path.
        public static Button Picture(string resource, string tooltip, Action action)
        {
            var button = Frame(tooltip, action);
            button.Q("icon-glyph").style.backgroundImage = Background(resource);
            return button;
        }

        public static void SetIcon(Button button, string icon)
        {
            var glyph = button.Q("icon-glyph");
            if (glyph == null)
                return;
            glyph.style.backgroundImage = Background(Folder + icon);
            foreach (var old in new List<string>(button.GetClasses()))
                if (old.StartsWith("icon-") && old != ClassName && old != "icon-glyph")
                    button.RemoveFromClassList(old);
            button.AddToClassList("icon-" + icon);
        }

        private static Button Frame(string tooltip, Action action)
        {
            if (string.IsNullOrEmpty(tooltip))
                throw new ArgumentException("An icon button explains its symbol.", nameof(tooltip));
            var button = BoardScreen.Button("", action);
            button.tooltip = tooltip;
            button.AddToClassList(ClassName);
            var glyph = new VisualElement { name = "icon-glyph", pickingMode = PickingMode.Ignore };
            glyph.AddToClassList("icon-glyph");
            button.Add(glyph);
            return button;
        }

        private static StyleBackground Background(string resource)
        {
            if (!Icons.TryGetValue(resource, out var texture))
            {
                texture = Resources.Load<Texture2D>(resource);
                Icons[resource] = texture;
            }
            return texture == null ? new StyleBackground(StyleKeyword.None) : new StyleBackground(texture);
        }

        public static string Icon(Button button)
        {
            foreach (var name in button.GetClasses())
                if (name.StartsWith("icon-") && name != ClassName)
                    return name.Substring("icon-".Length);
            return "";
        }
    }
}
