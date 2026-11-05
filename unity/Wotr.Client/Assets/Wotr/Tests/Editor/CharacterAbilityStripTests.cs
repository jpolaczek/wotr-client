using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-391 (owner): a character's abilities are thematic icons on its roster row; an icon's tooltip names the ability
    // and its text; a usable one glows and its click starts it; another's click opens the window at that ability.
    public sealed class CharacterAbilityStripTests
    {
        private const string Gimli = "base.character.gimli";

        [Test]
        public void EveryPrintedAbilityIsAThematicIconWithItsText()
        {
            var strip = Strip();
            var abilities = CharacterAbilityLibrary.For(Gimli);
            Assert.AreEqual(2, abilities.Count);
            foreach (var ability in abilities)
            {
                var icon = strip.Root.Q<Button>("roster-ability-" + Gimli + "-" + ability.Id);
                Assert.IsNotNull(icon, ability.Id);
                // UI-440 (owner): the skill's formatted description, as in the window: its name first, then its text.
                var plain = SkillTooltip.Plain(icon.tooltip);
                StringAssert.StartsWith(ability.Title + "\n", plain);
                StringAssert.Contains(ability.Text, plain);
                StringAssert.EndsWith("\n" + SkillUse.OpenWindow, plain);
                Assert.IsTrue(icon.ClassListContains(RuntimeTooltip.FormattedClass));
                StringAssert.DoesNotContain("\n\n", icon.tooltip);
                Assert.IsFalse(icon.ClassListContains(ChoiceGlow.ClassName), "Nothing can be used yet.");
                // UI-439: each icon is the ability's own; UI-440 (owner): bare, without the small gilt frame.
                Assert.AreEqual(Texture(AbilityArt.Folder + ability.Id), Glyph(icon), ability.Id);
                SkillIconChecks.Bare(icon, AbilityArt.Folder + ability.Id);
            }
            var slots = strip.Root.Query<Button>(className: CharacterAbilityStrip.IconClass).ToList();
            Assert.AreEqual(4, slots.Count, "Four slots for the most abilities a card prints.");
            Assert.AreEqual(2, slots.Count(slot => slot.style.display.value == DisplayStyle.Flex));
        }

        // UI-439: a Dark Chieftain's row shows his own form of the shared ability (AbilityArt's card form), and a row
        // that changes card changes its icons.
        [Test]
        public void ARowShowsItsCardsOwnFormOfAnAbility()
        {
            const string serpent = "kings.character.black-serpent";
            var strip = new CharacterAbilityStrip(serpent);
            strip.Apply(serpent);
            var chieftain = strip.Root.Q<Button>("roster-ability-" + serpent + "-not-minion-for-gandalf-white");
            Assert.AreEqual(Texture(AbilityArt.Folder + AbilityArt.DarkChieftain), Glyph(chieftain));
            strip.Apply("lords.character.gothmog");
            var entry = strip.Root.Q<Button>("roster-ability-" + serpent + "-not-minion-for-gandalf-white");
            Assert.AreEqual(Texture(AbilityArt.Folder + "not-minion-for-gandalf-white"), Glyph(entry), "Gothmog prints Gandalf the White entry.");
        }

        private static UnityEngine.Texture2D Texture(string resource) => UnityEngine.Resources.Load<UnityEngine.Texture2D>(resource);

        private static UnityEngine.Texture2D Glyph(Button icon) => icon.Q("icon-glyph").style.backgroundImage.value.texture;

        [Test]
        public void AUsableAbilityGlowsAndItsClickStartsIt()
        {
            var strip = Strip();
            var politics = CharacterAbilityLibrary.For(Gimli)[1].Id;
            var action = new ComponentAction
            {
                Key = "gimli-politics", Kind = CommandKind.UseCharacterAbility, Enabled = true, DieId = "muster",
                Detail = "Move the Dwarves one step toward war.",
                Character = new CharacterActionOption { CharacterId = Gimli, AbilityId = politics, Title = "Dwarf of Erebor" }
            };
            var used = new List<ComponentAction>();
            var opened = new List<string>();
            strip.Used += used.Add;
            strip.Opened += opened.Add;
            strip.Offer(new[] { action }, true, die => die == "muster" ? "Muster die" : "");
            var icon = strip.Root.Q<Button>("roster-ability-" + Gimli + "-" + politics);
            Assert.IsTrue(icon.ClassListContains(ChoiceGlow.ClassName), "A usable ability's icon glows.");
            Assert.IsTrue(strip.Offered);
            // UI-420: one line says the click, the die and the effect, without a blank line before it.
            StringAssert.EndsWith("\nClick to use your Muster die: Move the Dwarves one step toward war.", SkillTooltip.Plain(icon.tooltip));
            StringAssert.DoesNotContain("\n\n", icon.tooltip);
            strip.Choose(politics);
            CollectionAssert.AreEqual(new[] { action }, used, "Its click starts the ability.");
            var captain = CharacterAbilityLibrary.For(Gimli)[0].Id;
            strip.Choose(captain);
            CollectionAssert.AreEqual(new[] { captain }, opened, "Another icon opens the window at its ability.");
            strip.Offer(new[] { action }, false, null);
            Assert.IsFalse(icon.ClassListContains(ChoiceGlow.ClassName), "While another action is drafted nothing glows.");
            strip.Choose(politics);
            Assert.AreEqual(1, used.Count, "An icon that cannot be used now starts nothing.");
        }

        private static CharacterAbilityStrip Strip()
        {
            var strip = new CharacterAbilityStrip(Gimli);
            strip.Apply(Gimli);
            return strip;
        }
    }
}
