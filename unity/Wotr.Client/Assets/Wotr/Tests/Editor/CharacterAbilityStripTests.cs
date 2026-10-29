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
                StringAssert.StartsWith(ability.Title + "\n" + ability.Text, icon.tooltip);
                Assert.IsFalse(icon.ClassListContains(ChoiceGlow.ClassName), "Nothing can be used yet.");
            }
            Assert.AreEqual("Wotr/Art/UI/actions/battle", AbilityIcons.For("combat-strength-plus-one"), "Captain of the West is a battle.");
            Assert.AreEqual(AbilityPictures.Politics, AbilityIcons.For("politics-dwarves-at-erebor"));
            Assert.AreEqual(AbilityPictures.Muster, AbilityIcons.For("kings.iron-guard"), "An expansion's prefix is dropped.");
            Assert.AreEqual(AbilityPictures.Ability, AbilityIcons.For("an-unknown-ability"));
            var slots = strip.Root.Query<Button>(className: CharacterAbilityStrip.IconClass).ToList();
            Assert.AreEqual(4, slots.Count, "Four slots for the most abilities a card prints.");
            Assert.AreEqual(2, slots.Count(slot => slot.style.display.value == DisplayStyle.Flex));
        }

        // UI-408: an icon whose art is missing shows an empty frame on the row.
        [Test]
        public void EveryAbilityIconsArtExists()
        {
            foreach (var picture in AbilityIcons.Pictures)
                Assert.IsNotNull(UnityEngine.Resources.Load<UnityEngine.Texture2D>(picture), picture);
        }

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
            StringAssert.EndsWith("Click to use · Use your Muster die: Move the Dwarves one step toward war.", icon.tooltip);
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
