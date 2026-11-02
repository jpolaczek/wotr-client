using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // UI-440 (owner, 2026-11-02): a skill button is the skill's icon alone; it glows while usable, a selected one is
    // marked, and its tooltip names the skill. PlayMode tests measure the styled states (CharacterWindowLayoutTests).
    public sealed class SkillIconTests
    {
        private const string Voice = AbilityArt.Folder + "muster-each-isengard-settlement-or-upgrade-two";

        [Test]
        public void ASkillIconIsItsPictureWithALightAMarkAndARingAndNoWords()
        {
            var clicks = 0;
            var icon = SkillIcon.Create(Voice, "Saruman's Voice", () => clicks++);
            SkillIconChecks.Bare(icon, Voice);
            Assert.AreEqual("Saruman's Voice", icon.tooltip);
            var parts = icon.Children().ToList();
            CollectionAssert.AreEqual(new[] { SkillIcon.HaloClass, SkillIcon.GlyphClass, SkillIcon.MarkClass, SkillIcon.RingClass },
                parts.Select(part => part.GetClasses().First()), "The light stands behind the picture; the mark and the ring over it.");
            Assert.IsTrue(parts.All(part => part.pickingMode == PickingMode.Ignore), "The button's box is the hit target.");
            Assert.AreEqual(IconButton.GlyphName, SkillIcon.Glyph(icon).name, "IconButton.SetPicture and AddBadge serve it.");
            IconButton.SetPicture(icon, AbilityArt.Folder + "narya");
            Assert.AreEqual(Resources.Load<Texture2D>(AbilityArt.Folder + "narya"), SkillIcon.Glyph(icon).style.backgroundImage.value.texture);
            Assert.AreEqual(0, clicks, "Building it uses nothing.");
        }

        [Test]
        public void ASkillIconNamesItsSkill()
        {
            Assert.Throws<ArgumentException>(() => SkillIcon.Create(Voice, "", () => { }));
        }

        [Test]
        public void TheGlowAndTheMarkAreItsStates()
        {
            var icon = SkillIcon.Create(Voice, "Saruman's Voice", () => { });
            ChoiceGlow.Apply(icon, true);
            Assert.IsTrue(icon.ClassListContains(ChoiceGlow.ClassName));
            SkillIcon.Select(icon, true);
            Assert.IsTrue(icon.ClassListContains(SkillIcon.SelectedClass));
            SkillIcon.Select(icon, false);
            Assert.IsFalse(icon.ClassListContains(SkillIcon.SelectedClass));
            var marker = SkillIcon.Marker(Voice);
            Assert.IsNotInstanceOf<Button>(marker, "An applied skill is shown, not clicked.");
            Assert.IsTrue(marker.ClassListContains(SkillIcon.ClassName) && marker.ClassListContains(SkillIcon.SelectedClass));
            Assert.AreEqual(Resources.Load<Texture2D>(Voice), SkillIcon.Glyph(marker).style.backgroundImage.value.texture);
        }
    }
}
