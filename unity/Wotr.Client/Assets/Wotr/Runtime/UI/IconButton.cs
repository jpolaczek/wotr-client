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
        // The picture's element. UI-440: a bare skill icon (SkillIcon) names its picture the same way, so SetPicture and
        // AddBadge serve it too.
        public const string GlyphName = "icon-glyph";
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
            button.Q(GlyphName).style.backgroundImage = Background(resource);
            return button;
        }

        // UI-391: the picture of an icon made by Picture (UI-440: or by SkillIcon) changes without rebuilding the button.
        public static void SetPicture(Button button, string resource)
        {
            var glyph = button.Q(GlyphName);
            if (glyph != null)
                glyph.style.backgroundImage = Background(resource);
        }

        // UI-439: a small badge in the frame's corner says what this button does with its picture (AbilityBadges).
        public const string BadgeClass = "icon-badge";

        public static void AddBadge(Button button, string resource)
        {
            var badge = new VisualElement { name = "icon-badge", pickingMode = PickingMode.Ignore };
            badge.AddToClassList(BadgeClass);
            badge.style.backgroundImage = Background(resource);
            button.Add(badge);
        }

        public static void SetIcon(Button button, string icon)
        {
            var glyph = button.Q(GlyphName);
            if (glyph == null)
                return;
            glyph.style.backgroundImage = Background(Folder + icon);
            foreach (var old in new List<string>(button.GetClasses()))
                if (old.StartsWith("icon-") && old != ClassName && old != GlyphName)
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
            var glyph = new VisualElement { name = GlyphName, pickingMode = PickingMode.Ignore };
            glyph.AddToClassList(GlyphName);
            button.Add(glyph);
            return button;
        }

        // A picture of Art/UI by its full resource path, loaded once (UI-439: also an ability's icon on a chip or badge).
        public static StyleBackground Background(string resource)
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
