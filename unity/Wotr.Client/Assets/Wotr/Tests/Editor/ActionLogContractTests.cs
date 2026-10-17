using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public class ActionLogContractTests
    {
        [Test]
        public void ReadsActualHttpMovementPageWithHistoricalFaceAndComposition()
        {
            var raw = File.ReadAllText(Path.Combine(Application.dataPath, "Wotr/Tests/Editor/ActionLogWire.json"));
            var page = JsonConvert.DeserializeObject<ActionLogPage>(raw);
            Assert.AreEqual(1, page.Steps.Count);
            Assert.AreEqual(1, page.Actions.Count);
            var action = page.Actions[0];
            var step = page.Steps[0];
            Assert.AreEqual(action.Id, step.ActionId);
            Assert.AreEqual(Side.Shadow, step.Actor);
            Assert.AreEqual(CommandKind.MoveArmies, step.Command);
            Assert.AreEqual(ActionLogStatus.InProgress, action.Status);
            Assert.AreEqual(DieFace.WillOfTheWest, action.Costs.Single().Face);
            Assert.AreEqual(DieFace.Army, action.Costs.Single().Use);
            Assert.AreEqual("morannon", step.Movements.Single().From);
            Assert.AreEqual("dagorlad", step.Movements.Single().To);
            Assert.AreEqual(2, step.Movements[0].Participants.Count(p => p.UnitKind == UnitKind.Regular));
            Assert.AreEqual(3, step.Movements[0].Participants.Count(p => p.UnitKind == UnitKind.Elite));
            Assert.IsNotEmpty(page.LatestCursor);
            Assert.IsFalse(raw.Contains("commandId"));
            Assert.IsFalse(raw.Contains("payload"));
        }

        [Test]
        public void LegacyAndMissingOptionalFieldsNeverInventAnActorCostOrResult()
        {
            var page = JsonConvert.DeserializeObject<ActionLogPage>(
                "{\"steps\":[{\"id\":\"legacy\",\"legacy\":true,\"text\":\"Original text\"}]," +
                "\"actions\":[{\"id\":\"legacy\",\"legacy\":true,\"partialHistory\":true}]}");
            Assert.IsNull(page.Steps[0].Actor);
            Assert.IsNull(page.Steps[0].Turn);
            Assert.IsNull(page.Steps[0].Command);
            Assert.AreEqual(ActionLogStatus.Unknown, page.Actions[0].Status);
            Assert.IsEmpty(page.Actions[0].Costs);
            Assert.IsEmpty(page.Steps[0].Movements);
            Assert.AreEqual("Original text", page.Steps[0].Text);
        }
    }
}
