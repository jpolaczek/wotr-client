using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class CharacterRosterContractTests
    {
        [Test]
        public void OlderResponseDoesNotInventCharactersDeathOrActions()
        {
            var old = JsonConvert.DeserializeObject<PlayerView>("{\"characters\":[]}");
            Assert.IsEmpty(old.OwnCharacterRoster);
            var row = JsonConvert.DeserializeObject<CharacterRosterView>("{}");
            Assert.AreEqual(CharacterRosterLifecycle.Unknown, row.Lifecycle);
            Assert.IsEmpty(row.CurrentDefinitionId);
            Assert.IsEmpty(row.RegionId);
            Assert.IsNull(row.Level);
            Assert.IsNull(row.MovementLevel);
            Assert.IsNull(row.Leadership);
            Assert.IsEmpty(row.FutureVersions);
        }

        [Test]
        public void MissingEntryDescriptionIsNeutralAndHasNoExecutionPermission()
        {
            var version = JsonConvert.DeserializeObject<CharacterEntryVersionView>("{}");
            Assert.AreEqual(CharacterEntryMode.Unknown, version.Mode);
            Assert.IsFalse(version.Entry.ConditionsMet);
            Assert.IsFalse(version.Entry.WindowOpen);
            Assert.IsFalse(version.Entry.CanAct);
            Assert.IsEmpty(version.Entry.Targets);
            Assert.IsEmpty(version.Entry.EligibleDice);
            Assert.IsNull(version.CouncilEntry);
            Assert.IsEmpty(version.CouncilTokens);
        }

        [Test]
        public void ActualFreePeoplesHttpWireKeepsTheSameFigureAndWhiteReturnOutsideItsWindow()
        {
            var view = Read("CharacterRosterWire.json");
            Assert.AreEqual(Side.FreePeoples, view.You);
            Assert.IsTrue(view.OwnCharacterRoster.All(r => r.Side == view.You));
            var gandalf = view.OwnCharacterRoster.Single(r => r.FigureId == "base.character.gandalf-grey");
            Assert.AreEqual("lords.character.gandalf-grey", gandalf.CurrentDefinitionId);
            Assert.AreEqual(CharacterRosterLifecycle.EliminatedWithReplacement, gandalf.Lifecycle);
            Assert.IsEmpty(gandalf.RegionId);
            var white = gandalf.FutureVersions.Single();
            Assert.AreEqual("base.character.gandalf-white", white.DefinitionId);
            Assert.AreEqual(CharacterEntryMode.ReturnAsNewVersion, white.Mode);
            Assert.IsTrue(white.Entry.ConditionsMet);
            Assert.IsFalse(white.Entry.WindowOpen);
            Assert.IsFalse(white.Entry.CanAct);
            CollectionAssert.Contains(white.Entry.Targets, "fangorn");
            Assert.IsNotEmpty(white.Entry.Source);
            Assert.AreEqual(1, white.ExtraDiceNextRecovery);
            Assert.IsTrue(white.Entry.EligibleDice.All(id => view.Dice.Single(d => d.Id == id).Owner == view.You));
            Assert.IsFalse(view.OwnCharacterRoster.Any(r => r.FigureId == white.DefinitionId));
        }

        [Test]
        public void ActualFateFigurePreservesItsPrivateVersionAndSeparateMovementLevel()
        {
            var view = Read("CharacterRosterWire.json");
            var gimli = view.OwnCharacterRoster.Single(r => r.FigureId == "base.character.gimli");
            Assert.AreEqual("fate.character.gimli-dwarven-lord", gimli.CurrentDefinitionId);
            Assert.AreEqual(CharacterRosterLifecycle.OnBoard, gimli.Lifecycle);
            Assert.AreEqual("iron-hills", gimli.RegionId);
            Assert.AreEqual(1, gimli.Level);
            Assert.AreEqual(2, gimli.MovementLevel);
            Assert.IsEmpty(gimli.FutureVersions);
        }

        [Test]
        public void ActualShadowWireContainsOnlyOwnNamedFiguresAndExplicitCouncilCost()
        {
            var view = Read("CharacterRosterShadowWire.json");
            Assert.AreEqual(Side.Shadow, view.You);
            Assert.AreEqual(8, view.OwnCharacterRoster.Count);
            Assert.IsTrue(view.OwnCharacterRoster.All(r => r.Side == view.You));
            Assert.AreEqual(view.OwnCharacterRoster.Count, view.OwnCharacterRoster.Select(r => r.FigureId).Distinct().Count());
            Assert.IsFalse(view.OwnCharacterRoster.Any(r => r.Identity == "gandalf" || r.Identity == "gimli"));
            var chieftain = view.OwnCharacterRoster.Single(r => r.FigureId == "kings.character.black-serpent").FutureVersions.Single();
            Assert.IsFalse(chieftain.Entry.ConditionsMet);
            Assert.IsTrue(chieftain.CouncilEntry.CanAct);
            CollectionAssert.AreEqual(new[] { "council.move-shadow-characters" }, chieftain.CouncilTokens);
            Assert.IsNotEmpty(chieftain.CouncilEntry.Targets);
            Assert.IsNotEmpty(chieftain.CouncilEntry.EligibleDice);
            Assert.IsTrue(view.OwnCharacterRoster.All(r => r.Lifecycle == CharacterRosterLifecycle.NotEntered));
        }

        private static PlayerView Read(string name)
        {
            var path = Path.Combine(Application.dataPath, "Wotr/Tests/Editor", name);
            return JsonConvert.DeserializeObject<PlayerView>(File.ReadAllText(path));
        }
    }
}
