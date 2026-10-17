using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class KingsCards01Tests
    {
        [Test]
        public void AllSixCardsHaveBothEnglishHalvesAndSourceInitiatives()
        {
            var ids = new[] { "character.23", "character.25", "character.26", "strategy.08", "strategy.16", "strategy.19" };
            var initiatives = new[] { 3, 4, 2, 4, 3, 6 };
            for (var i = 0; i < ids.Length; i++)
            {
                var card = CardLibrary.Cards["kings.fp." + ids[i]];
                Assert.IsNotEmpty(card.Title);
                Assert.IsNotEmpty(card.Event);
                Assert.IsNotEmpty(card.Combat);
                Assert.AreEqual(initiatives[i], card.Initiative.Single());
                Assert.IsTrue(card.HasCombat);
            }
        }

        [Test]
        public void InspectionUsesOnlyVisibleTileValuesAndKeepsChoiceLocalUntilConfirmation()
        {
            var view = new PlayerView { Sovereigns = new List<SovereignView>
            {
                new SovereignView { Tiles = new List<CorruptionTileView>
                {
                    new CorruptionTileView { Id = "hidden-slot" },
                    new CorruptionTileView { Id = "tile", TileId = "tile", Value = 2, Reveal = true }
                } }
            } };
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 1, Maximum = 1,
                EligibleIds = new List<string> { "tile", "keep" }, EffectChoices = new List<EffectChoiceView>
                {
                    new EffectChoiceView { Id = "tile", Title = "2 Corruption" },
                    new EffectChoiceView { Id = "keep", Title = "Keep tiles" }
                } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            var root = new VisualElement();
            var refresh = CorruptionInspectionChoices.Render(root, view, option, draft, edit => edit());
            refresh();
            Assert.AreEqual(2, root.Query<Button>().ToList().Count);
            Assert.AreEqual("2 ◉", root.Q<Button>("effect-choice-tile").text);
            Assert.IsNull(root.Q<Button>("effect-choice-hidden-slot"));
            Assert.IsFalse(CardDraft.Ready(draft, option));
            CardDraft.Choose(draft, option, "tile");
            refresh();
            Assert.IsTrue(CardDraft.Ready(draft, option));
            Assert.IsTrue(root.Q<Button>("effect-choice-tile").ClassListContains("selected"));
            CardDraft.Choose(draft, option, "keep");
            draft.Back();
            CollectionAssert.AreEqual(new[] { "tile" }, draft.Payload.Ids);
            Assert.IsEmpty(root.Query<TextField>().ToList());
            Assert.IsEmpty(root.Query<ScrollView>().ToList());
        }
    }
}
