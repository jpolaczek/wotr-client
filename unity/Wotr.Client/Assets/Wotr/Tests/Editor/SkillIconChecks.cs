using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-440 (owner): a skill button is the skill's icon alone — no frame class, no plate, no words — and it names the
    // skill in its tooltip. PlayMode's SkillIconChecks measures the same on the styled board.
    internal static class SkillIconChecks
    {
        public static void Bare(Button icon, string resource)
        {
            Assert.IsNotNull(icon);
            Assert.IsTrue(icon.ClassListContains(SkillIcon.ClassName), icon.name + " is a bare skill icon.");
            Assert.IsFalse(icon.ClassListContains("action"), icon.name + " wears no button frame.");
            Assert.IsFalse(icon.ClassListContains(IconButton.ClassName), icon.name + " wears no square frame.");
            Assert.AreEqual("", icon.text, icon.name + " has no text.");
            Assert.IsEmpty(icon.Query<TextElement>().ToList().Where(text => text != icon && text.text.Length > 0), icon.name);
            Assert.AreEqual(StyleKeyword.Null, icon.style.backgroundImage.keyword, icon.name + " has no picture of its own.");
            Assert.AreEqual(Resources.Load<Texture2D>(resource), SkillIcon.Glyph(icon).style.backgroundImage.value.texture, icon.name);
            Assert.IsNotEmpty(icon.tooltip, icon.name + " names its skill.");
        }
    }
}
