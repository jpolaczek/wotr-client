using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class BattlePolicyTests
    {
        [Test]
        public void WaitingPlayerCanBuildAnIndependentVersionedSettingAndNeverChooseForTheOpponent()
        {
            var view = new PlayerView
            {
                MatchId = "match",
                Revision = 20,
                You = Side.Shadow,
                DecisionOwner = Side.FreePeoples,
                Battle = new BattleView { Id = "battle" },
                AutoDecision = new AutoDecisionView { Version = 3, HandVersion = 7 }
            };
            var enable = BattlePolicyInteraction.Command(view);
            Assert.AreEqual(CommandKind.SetCombatCardPolicy, enable.Kind);
            Assert.AreEqual("", enable.DecisionId);
            Assert.AreEqual("battle", enable.Payload.AutoDecision.BattleId);
            Assert.AreEqual(3, enable.Payload.AutoDecision.ExpectedVersion);
            Assert.AreEqual(7, enable.Payload.AutoDecision.HandVersion);
            Assert.IsTrue(enable.Payload.AutoDecision.Enabled);
            view.AutoDecision.Policy = new AutoDecisionPolicy { Owner = view.You, BattleId = "battle" };
            var cancel = BattlePolicyInteraction.Command(view);
            Assert.IsFalse(cancel.Payload.AutoDecision.Enabled);
            Assert.IsTrue(enable.Payload.AutoDecision.Enabled, "An in-flight request is immutable.");
            Assert.AreNotEqual(enable.CommandId, cancel.CommandId);
        }
    }
}
