using NUnit.Framework;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public class ContractsTests
    {
        [Test]
        public void SharedAssemblyDoesNotReferenceUnity()
        {
            foreach (var reference in typeof(Protocol).Assembly.GetReferencedAssemblies())
                Assert.That(reference.Name.StartsWith("Unity"), Is.False);
        }

        [Test]
        public void CorrespondenceWireFormatKeepsOwnDecisionAndUnseenRevisionWithoutPrivateState()
        {
            var view = JsonConvert.DeserializeObject<MatchListView>(
                "{\"matches\":[{\"matchId\":\"saved\",\"you\":\"Shadow\",\"phase\":\"ActionRoll\",\"status\":\"YourDecision\"," +
                "\"yourDecision\":\"Dismay\",\"revision\":18,\"lastSeenRevision\":16,\"hasUnseenChanges\":true},{" +
                "\"matchId\":\"waiting\",\"you\":\"FreePeoples\",\"status\":\"Waiting\",\"yourDecision\":null}],\"nextCursor\":\"waiting\"}");
            Assert.AreEqual(DecisionKind.Dismay, view.Matches[0].YourDecision);
            Assert.AreEqual(Side.Shadow, view.Matches[0].You);
            Assert.AreEqual(16, view.Matches[0].LastSeenRevision);
            Assert.IsTrue(view.Matches[0].HasUnseenChanges);
            Assert.IsNull(view.Matches[1].YourDecision);
            Assert.AreEqual(MatchStatus.Waiting, view.Matches[1].Status);
            Assert.AreEqual("waiting", view.NextCursor);
        }
    }
}
