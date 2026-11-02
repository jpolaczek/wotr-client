using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-439 (owner, 2026-11-01): every skill of every character has its own small, characterful icon — no repeats, the
    // Balrog's three different — and "the same icons every time a button stands for the skill". One map (AbilityArt)
    // serves every place; the places' own tests check that each uses it (CharacterAbilityStripTests,
    // ComponentActionTests, BattleArtTests, ReactionSourcesTests, HuntGuideResponsesTests).
    public sealed class AbilityArtTests
    {
        private const string Balrog = "lords.character.balrog";

        [Test]
        public void EveryAbilityOfTheContentHasItsOwnIcon()
        {
            var cards = CharacterAbilityLibrary.CardIds;
            foreach (var expansion in new[] { "base.", "lords.", "kings.", "fate." })
                Assert.IsTrue(cards.Any(card => card.StartsWith(expansion, StringComparison.Ordinal)), expansion + " cards are in the catalogue.");
            var shown = new HashSet<string>();
            foreach (var card in cards)
            {
                var icons = CharacterAbilityLibrary.For(card).Select(ability => AbilityArt.For(card, ability.Id)).ToList();
                foreach (var icon in icons)
                {
                    StringAssert.StartsWith(AbilityArt.Folder, icon, card + " · an ability's own icon, never the scroll.");
                    Assert.IsNotNull(Resources.Load<Texture2D>(icon), icon);
                    shown.Add(icon);
                }
                CollectionAssert.AllItemsAreUnique(icons, card + "'s abilities all have different icons.");
            }
            Assert.AreEqual(77, shown.Count, "76 printed abilities and the Dark Chieftains' own form.");
            CollectionAssert.AreEquivalent(AbilityArt.Keys.Select(key => AbilityArt.Folder + key), shown);
        }

        // No icon is a copy of another: every file differs.
        [Test]
        public void NoTwoAbilitiesShareAPicture()
        {
            var files = AbilityArt.Keys.ToDictionary(key => key, key => Digest(AbilityArt.Folder + key));
            foreach (var group in files.GroupBy(file => file.Value).Where(group => group.Count() > 1))
                Assert.Fail("One picture for " + string.Join(", ", group.Select(file => file.Key)));
            Assert.AreEqual(77, files.Count);
        }

        [Test]
        public void TheBalrogsAbilitiesEachHaveTheirOwnIcon()
        {
            var icons = CharacterAbilityLibrary.For(Balrog).Select(ability => AbilityArt.For(Balrog, ability.Id)).ToList();
            CollectionAssert.AreEqual(new[] { "fire-and-shadow", "flame-of-udun", "balrog-activation", "not-minion-for-gandalf-white" }
                .Select(key => AbilityArt.Folder + key), icons);
            CollectionAssert.AllItemsAreUnique(icons.Select(Digest).ToList(), "Fire and Shadow, Flame of Udûn and Activation differ.");
        }

        // The Dark Chieftains print Gandalf the White entry as Dark Chieftain; the Balrog, Gothmog and the Witch-king do not.
        [TestCase("kings.character.black-serpent", AbilityArt.DarkChieftain)]
        [TestCase("kings.character.shadow-of-mirkwood", AbilityArt.DarkChieftain)]
        [TestCase("kings.character.ugluk", AbilityArt.DarkChieftain)]
        [TestCase(Balrog, "not-minion-for-gandalf-white")]
        [TestCase("lords.character.gothmog", "not-minion-for-gandalf-white")]
        [TestCase("lords.character.witch-king", "not-minion-for-gandalf-white")]
        public void ACardsOwnFormOfAnAbilityHasItsOwnIcon(string card, string icon) =>
            Assert.AreEqual(AbilityArt.Folder + icon, AbilityArt.For(card, "not-minion-for-gandalf-white"));

        // A command, a reaction or a decision that carries its own id shows the printed ability it uses.
        [TestCase("activate-balrog", "balrog-activation")]
        [TestCase("deactivate-balrog", "balrog-activation")]
        [TestCase("use-balrog", "balrog-activation")]
        [TestCase("gothmog-recruit", "fling-them-into-the-fray")]
        [TestCase("gothmog-upgrade", "fling-them-into-the-fray")]
        [TestCase("voice-recruit", "muster-each-isengard-settlement-or-upgrade-two")]
        [TestCase("voice-upgrade", "muster-each-isengard-settlement-or-upgrade-two")]
        [TestCase("lords.nenya.replace", "nenya")]
        [TestCase("lords.chief.spear-of-terror", "spear-of-terror")]
        [TestCase("lords.chief.he-sees-he-knows", "he-sees-he-knows")]
        [TestCase("kings.call-to-arms", "call-to-arms")]
        [TestCase("kings.far-sighted", "far-sighted")]
        [TestCase("kings.dwarvenking", "dwarvenking")]
        [TestCase("character.grey-draw", "guide-draw-after-event-die-card")]
        [TestCase("character.white-rider", "forfeit-leadership-to-cancel-nazgul-for-battle")]
        [TestCase("character.sorcerer", "draw-after-first-round-combat-card")]
        [TestCase("character.merry-guide", "guide-separate-to-reduce-hunt-one")]
        [TestCase("character.pippin-guide", "guide-separate-to-reduce-hunt-one")]
        [TestCase("character.gollum-reveal", "voluntary-reveal-to-reduce-hunt-one")]
        [TestCase("council.boromir-guide", "guide-separate-to-reduce-hunt-one")]
        [TestCase("council.gimli-guide", "guide-separate-to-reduce-hunt-one")]
        [TestCase("council.legolas-guide", "guide-separate-to-reduce-hunt-one")]
        [TestCase("fate.gimli-guide", "guide-separate-to-reduce-hunt-one")]
        [TestCase("council.hobbits-cancel-hunt.lords.character.merry", "guide-cancel-standard-hunt")]
        [TestCase("council.hobbits-cancel-hunt.lords.character.pippin", "guide-cancel-standard-hunt")]
        public void ACommandShowsItsAbilitysIcon(string command, string ability)
        {
            Assert.AreEqual(ability, AbilityArt.Printed(command));
            Assert.AreEqual(AbilityArt.Folder + ability, AbilityArt.For(command));
            Assert.IsNotNull(CharacterAbilityLibrary.Title(ability), ability + " is a printed ability.");
        }

        [TestCase(CommandKind.UseNarya, "narya")]
        [TestCase(CommandKind.UseVilya, "vilya")]
        [TestCase(CommandKind.DeclineVilya, "vilya")]
        [TestCase(CommandKind.UseEvilKnowledge, "evil-knowledge")]
        public void ACommandKindOfAnAbilityShowsItsIcon(CommandKind kind, string ability) =>
            Assert.AreEqual(AbilityArt.Folder + ability, AbilityArt.For(kind));

        [Test]
        public void AnIdThatNamesNoAbilityShowsTheScrollOrNoBadge()
        {
            Assert.AreEqual("", AbilityArt.For(CommandKind.ContinueBattle), "A battle step stands for no ability.");
            Assert.AreEqual(AbilityArt.Unknown, AbilityArt.For("kings.remove-corruption"));
            Assert.AreEqual(AbilityArt.Unknown, AbilityArt.For(""));
            Assert.AreEqual("", AbilityArt.Find("", "an-unknown-reaction"));
            Assert.IsNotNull(Resources.Load<Texture2D>(AbilityArt.Unknown));
        }

        // The character window's icons show the icon of their card's ability; UI-440: all four of the Balrog's in one row.
        [Test]
        public void TheCharacterWindowsLinesUseTheMap()
        {
            var chips = new CharacterAbilityChips();
            chips.Apply(Balrog);
            var abilities = CharacterAbilityLibrary.For(Balrog);
            Assert.AreEqual(4, abilities.Count, "The Balrog prints the most abilities a card has.");
            foreach (var ability in abilities)
                Assert.AreEqual(Texture(AbilityArt.For(Balrog, ability.Id)), ChipIcon(chips, ability.Id), ability.Id);
            const string serpent = "kings.character.black-serpent";
            chips.Apply(serpent);
            Assert.AreEqual(Texture(AbilityArt.Folder + AbilityArt.DarkChieftain), ChipIcon(chips, "not-minion-for-gandalf-white"));
        }

        // Narya's activation in the Politics panel shows Narya's own icon.
        [Test]
        public void NaryasActivationUsesTheMap()
        {
            var board = JsonConvert.DeserializeObject<BoardData>(Resources.Load<TextAsset>("base-board").text);
            var panel = new PoliticalPanel(new VisualElement(), board);
            panel.Apply(new PlayerView
            {
                MatchId = "narya", You = Side.FreePeoples,
                Nations = { new NationView { Id = "north", Active = false, StepsToWar = 2 } }
            });
            var activate = panel.Drawer.Q<Button>("nation-activate-north");
            Assert.AreEqual(Texture(AbilityArt.Folder + "narya"), activate.Q("icon-glyph").style.backgroundImage.value.texture);
            // UI-440 (owner): Narya's activation is its bare skill icon, without the square frame.
            Assert.IsTrue(activate.ClassListContains(SkillIcon.ClassName));
            Assert.IsFalse(activate.ClassListContains(IconButton.ClassName));
            Assert.AreEqual("", activate.text);
        }

        private static Texture2D Texture(string resource) => Resources.Load<Texture2D>(resource);

        // UI-440: every icon of the ability (each of its variants) shows the ability's own icon.
        private static Texture2D ChipIcon(CharacterAbilityChips chips, string ability)
        {
            var icons = chips.Icons.Where(icon => (string)icon.userData == ability).Select(icon => SkillIcon.Glyph(icon).style.backgroundImage.value.texture)
                .Distinct().ToList();
            Assert.AreEqual(1, icons.Count, ability);
            return icons[0];
        }

        private static string Digest(string resource)
        {
            var path = AssetDatabase.GetAssetPath(Resources.Load<Texture2D>(resource));
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(File.ReadAllBytes(path)));
        }
    }
}
