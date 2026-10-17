using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class InGameContractTests
    {
        [Test]
        public void OlderProjectionHasNeutralOptionalDescriptions()
        {
            var view = JsonConvert.DeserializeObject<PlayerView>("{\"effects\":[{\"sourceId\":\"old-card\"}],\"sovereigns\":[{}]}");
            Assert.IsEmpty(view.Availability);
            Assert.IsEmpty(view.MilitaryVictory);
            Assert.IsEmpty(view.Effects[0].Targets);
            Assert.IsEmpty(view.Effects[0].Description);
            Assert.IsNull(view.Sovereigns[0].Corruption);
            var missing = JsonConvert.DeserializeObject<ActionAvailabilityView>("{}");
            Assert.IsFalse(missing.ConditionsMet);
            Assert.IsFalse(missing.WindowOpen);
            Assert.IsFalse(missing.CanAct);
        }

        [Test]
        public void ReadsServerDescriptionsWithoutInferringPermissionOrHiddenCorruption()
        {
            var path = Path.Combine(Application.dataPath, "Wotr/Tests/Editor/InGameWire.json");
            var view = JsonConvert.DeserializeObject<PlayerView>(File.ReadAllText(path));
            Assert.AreEqual(Side.FreePeoples, view.You);
            var awakening = view.Availability.Single(a => a.SubjectId == "kings.sovereign.dain" && a.ActionId == "awaken");
            Assert.IsTrue(awakening.ConditionsMet);
            Assert.IsFalse(awakening.WindowOpen);
            Assert.IsFalse(awakening.CanAct);
            Assert.IsNotEmpty(awakening.EligibleDice);
            Assert.IsNotEmpty(awakening.Source);
            Assert.IsTrue(awakening.Conditions.All(c => c.Met));
            Assert.IsNull(view.Sovereigns.Single(s => s.Id == "kings.sovereign.dain").Corruption);
            foreach (var victory in view.MilitaryVictory)
                Assert.AreEqual(victory.Points, victory.Regions.Sum(r => r.Points));
            Assert.AreEqual(10, view.MilitaryVictory.Single(v => v.Side == Side.Shadow).Threshold);
            Assert.IsTrue(view.Effects.Any(e => e.SourceKind == "Card" && e.Description.Length > 0));
            Assert.IsNull(view.Winner);
        }
    }
}
