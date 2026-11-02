using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.PlayTests
{
    // UI-440 (owner): on the styled board a skill button is the skill's icon alone — its resolved border is 0 wide, it
    // has no picture of its own (the icon is its glyph), no words — at least as large as its context promises; a usable
    // one shows the light behind it, and its tooltip names the skill.
    internal static class SkillIconChecks
    {
        public static void Bare(Button icon, string resource, float minimum = 0f)
        {
            Assert.IsNotNull(icon);
            var label = icon.name;
            Assert.IsTrue(icon.ClassListContains(SkillIcon.ClassName), label + " is a bare skill icon.");
            Assert.AreEqual("", icon.text, label + " has no text.");
            Assert.IsEmpty(icon.Query<TextElement>().ToList().Where(text => text != icon && text.text.Length > 0), label);
            var style = icon.resolvedStyle;
            Assert.AreEqual(0f, style.borderLeftWidth + style.borderRightWidth + style.borderTopWidth + style.borderBottomWidth,
                label + " has no frame.");
            Assert.IsNull(style.backgroundImage.texture, label + " stands on no plate.");
            Assert.IsNull(style.backgroundImage.sprite, label + " stands on no plate.");
            Assert.AreEqual(0f, style.backgroundColor.a, 0.01f, label + " has no fill.");
            var glyph = SkillIcon.Glyph(icon);
            Assert.AreEqual(Resources.Load<Texture2D>(resource), glyph.resolvedStyle.backgroundImage.texture, label);
            Assert.GreaterOrEqual(glyph.worldBound.width, minimum - 0.5f, label + " is large enough to read.");
            Assert.GreaterOrEqual(icon.worldBound.width, glyph.worldBound.width, label + " is hit on its whole picture.");
            Assert.IsNotEmpty(icon.tooltip, label + " names its skill.");
            Assert.AreEqual(icon.ClassListContains(ChoiceGlow.ClassName), Lit(icon), label + " glows exactly while usable.");
        }

        // The light behind a usable icon is shown (it breathes between 1 and 0.45; hover alone lights it to 0.3).
        public static bool Lit(VisualElement icon)
        {
            var halo = icon.Q(className: SkillIcon.HaloClass).resolvedStyle;
            return halo.display == DisplayStyle.Flex && halo.opacity > 0.38f;
        }
    }
}
