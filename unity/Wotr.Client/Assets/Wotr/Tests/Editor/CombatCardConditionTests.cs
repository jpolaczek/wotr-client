using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // UI-111: a Combat card the server does not allow stays visible, dimmed, with its printed condition.
    public sealed class CombatCardConditionTests
    {
        private const string Stench = "base.sh.character.09";
        private const string Kill = "base.sh.strategy.13";
        private const string DarkLord = "base.sh.character.01";

        [Test]
        public void TheConditionIsTheFirstSentenceOfTheCombatPart()
        {
            Assert.AreEqual("Requires a Shadow Elite in the battle.", CombatCardCondition.Text(Kill));
            Assert.AreEqual("Requires the defending Army in the Fellowship’s region.", CombatCardCondition.Text(DarkLord));
        }

        [Test]
        public void TheBattleWindowListsUnavailableCardsWithoutAGesture()
        {
            var view = new PlayerView
            {
                You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "card", Kind = DecisionKind.CombatCard },
                Hand = { Stench, Kill, DarkLord }
            };
            var option = new ActionOption { Kind = CommandKind.CommitCombatCard, EligibleIds = { Stench }, Maximum = 1, Enabled = true };
            view.Options.Add(option);
            var hand = new BattleCombatHand(view, option).Root;
            var playable = hand.Q<Button>("combat-choice-" + Stench);
            Assert.IsTrue(playable.enabledSelf);
            Assert.IsInstanceOf<DraftToken>(playable.userData);
            foreach (var id in new[] { Kill, DarkLord })
            {
                var card = hand.Q<Button>("combat-unavailable-" + id);
                Assert.IsFalse(card.enabledSelf, id);
                Assert.IsNull(card.userData, "No drop can commit " + id);
                Assert.AreEqual(CombatCardCondition.Text(id), card.Q<Label>("combat-choice-condition").text);
            }
            CollectionAssert.AreEquivalent(new[] { Stench }, CombatCardCondition.Eligible(view));
        }
    }
}
