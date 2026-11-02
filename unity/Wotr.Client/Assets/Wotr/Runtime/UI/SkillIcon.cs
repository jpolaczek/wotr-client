using System;
using UnityEngine.UIElements;

namespace Wotr.Client
{
    // UI-440 (owner, 2026-11-02: "a skill button is not a rectangular button with text and a picture; it is ONLY THE
    // ICON"): every button that stands for a character's skill is the skill's icon (AbilityArt) alone — no frame, plate
    // or text. While the skill is usable (ChoiceGlow) a soft gilt light (ornaments/skill-glow) glows behind the icon and
    // breathes in its host's ChoicePulse rhythm; a chosen skill (an applied replacement, Marker) wears a gold underline;
    // hover brightens it, a press sinks it (UI-113) and keyboard focus rings it (KeyboardFocusRing); an icon a click does
    // not use is read only (ReadOnlyControl): no light, no press, its tooltip kept. The tooltip names the skill. The
    // picture is IconButton's glyph, so IconButton.SetPicture and AddBadge serve it too. Styles/SkillIcons.uss draws every
    // state; a context sheet only sizes the box, which is the hit target.
    public static class SkillIcon
    {
        public const string ClassName = "skill-icon";
        public const string HaloClass = "skill-icon-halo";
        public const string GlyphClass = "skill-icon-glyph";
        public const string MarkClass = "skill-icon-mark";
        public const string RingClass = "skill-icon-ring";
        public const string SelectedClass = "selected";

        public static Button Create(string resource, string tooltip, Action action)
        {
            if (string.IsNullOrEmpty(tooltip))
                throw new ArgumentException("A skill icon names its skill.", nameof(tooltip));
            var button = new Button(action) { tooltip = tooltip };
            Dress(button, resource);
            button.Add(Part(RingClass));
            KeyboardFocusRing.Attach(button);
            return button;
        }

        // A skill shown where nothing is clicked (an applied replacement), in the same look and marked as chosen.
        public static VisualElement Marker(string resource)
        {
            var marker = new VisualElement();
            Dress(marker, resource);
            Select(marker, true);
            return marker;
        }

        public static VisualElement Glyph(VisualElement icon) => icon.Q(IconButton.GlyphName);

        public static void Select(VisualElement icon, bool selected) => icon.EnableInClassList(SelectedClass, selected);

        private static void Dress(VisualElement icon, string resource)
        {
            icon.AddToClassList(ClassName);
            icon.Add(Part(HaloClass));
            var glyph = Part(GlyphClass);
            glyph.name = IconButton.GlyphName;
            glyph.style.backgroundImage = IconButton.Background(resource);
            icon.Add(glyph);
            icon.Add(Part(MarkClass));
        }

        private static VisualElement Part(string className)
        {
            var part = new VisualElement { name = className, pickingMode = PickingMode.Ignore };
            part.AddToClassList(className);
            return part;
        }
    }
}
