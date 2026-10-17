using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.Client.Tests
{
    // UI-116: parchment Combat tiles play through the shared path. UI-147: one click plays the card.
    public sealed class BattleCombatHandTests
    {
        [Test]
        public void OneClickPlaysTheTileFaceDownThroughTheSharedToken()
        {
            var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
            var option = view.Options.Single(o => o.Kind == CommandKind.CommitCombatCard);
            option.Enabled = true;
            DraftToken played = null;
            var plays = 0;
            var hand = new BattleCombatHand(view, option, token =>
            {
                played = token;
                plays++;
            });
            var tiles = hand.Root.Query<Button>(className: "combat-choice").ToList();
            Assert.AreEqual(option.EligibleIds.Count, tiles.Count);
            var first = tiles[0];
            var id = ((DraftToken)first.userData).Id;
            Assert.AreEqual(CommandKind.CommitCombatCard, ((DraftToken)first.userData).Action);
            Assert.IsNotNull(first.Q<Label>("combat-choice-title"));
            Assert.IsNotNull(first.Q<Label>("combat-choice-text"));
            Assert.IsNull(first.Q<Label>("combat-choice-hint"), "No second click is announced.");
            StringAssert.Contains("Click to play", first.tooltip);

            hand.Choose(id);
            Assert.AreEqual(1, plays, "The first click plays.");
            Assert.AreEqual(id, played.Id);
            Assert.AreEqual(CommandKind.CommitCombatCard, played.Action);
        }

        [Test]
        public void UnavailableCardsStayBehindWithTheirConditionAndNeverPlay()
        {
            var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
            var option = view.Options.Single(o => o.Kind == CommandKind.CommitCombatCard);
            var excluded = option.EligibleIds.Last();
            option.EligibleIds.Remove(excluded);
            var hand = new BattleCombatHand(view, option, _ => Assert.Fail("Nothing plays"));
            var tile = hand.Root.Q<Button>("combat-unavailable-" + excluded);
            Assert.IsNotNull(tile);
            Assert.IsTrue(tile.ClassListContains("combat-choice-unavailable"));
            Assert.IsFalse(tile.enabledSelf);
            Assert.IsNotEmpty(tile.Q<Label>("combat-choice-condition").text);
            Assert.AreSame(hand.Root.Query<Button>(className: "combat-choice").ToList().Last(), tile,
                "Unavailable cards come after the eligible ones.");
            Assert.IsNull(tile.userData, "No token: it cannot be dropped into the battle.");
        }

        [Test]
        public void ForceCardShowsTheLeatherBackWhileSecretAndParchmentOnceRevealed()
        {
            var view = BattleScreenFixture.Create(CommandKind.CommitCombatCard);
            var root = new VisualElement();
            new BattleContext(root, view.Battle, view, new TurnDraft(), _ => { }, _ => { });
            var attacker = root.Q<Button>("battle-card-attacker");
            Assert.IsTrue(attacker.ClassListContains("battle-card-back"));
            Assert.IsFalse(attacker.ClassListContains("battle-card-revealed"));
            StringAssert.Contains("Face down", attacker.text);

            var revealed = BattleScreenFixture.Create(CommandKind.ContinueBattle);
            var shown = new VisualElement();
            new BattleContext(shown, revealed.Battle, revealed, new TurnDraft(), _ => { }, _ => { });
            var card = shown.Q<Button>("battle-card-attacker");
            Assert.IsTrue(card.ClassListContains("battle-card-revealed"));
            Assert.IsFalse(card.ClassListContains("battle-card-back"));
            StringAssert.Contains("Initiative", card.text);
            Assert.IsTrue(card.enabledSelf, "A revealed card can be read.");
        }
    }
}
