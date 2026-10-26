using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-309: who a character presentation names — a moving group's identities, a character's moment, a die ability's
    // public holder — and when travelling Companions leave the Fellowship from a public origin.
    public sealed class CharacterPresentationTests
    {
        private static readonly BoardPresentation Data = BoardPresentation.Load();

        [Test]
        public void AnArmyWithCharactersReadsDifferentlyFromAGroupTravellingAlone()
        {
            var march = new[]
            {
                Unit("orc-1", "sauron", UnitKind.Regular), Unit("orc-2", "sauron", UnitKind.Elite), Unit("ring-1", "sauron", UnitKind.Nazgul),
                Character("base.character.witch-king", Side.Shadow)
            };
            var marks = CharacterMarks.Group(march);
            CollectionAssert.AreEqual(new[] { "×2", "×1", "Witch-king" }, marks.Select(mark => mark.Text));
            Assert.AreEqual("", marks[0].DefinitionId, "The army's units are one entry, never named.");
            var travel = CharacterMarks.Group(new[] { Character("base.character.gimli", Side.FreePeoples), Character("base.character.legolas", Side.FreePeoples) });
            CollectionAssert.AreEqual(new[] { "Gimli", "Legolas" }, travel.Select(mark => mark.Text));
            CollectionAssert.AreEqual(new[] { "base.character.gimli", "base.character.legolas" }, travel.Select(mark => mark.DefinitionId));
            Assert.IsEmpty(CharacterMarks.Group(march.Take(2).ToList()), "A plain army march names no one.");
        }

        [Test]
        public void SeveralNazgulAreOneEntryWithTheirCount()
        {
            var marks = CharacterMarks.Group(new[] { Unit("n1", "sauron", UnitKind.Nazgul), Unit("n2", "sauron", UnitKind.Nazgul), Unit("n3", "sauron", UnitKind.Nazgul) });
            Assert.AreEqual("×3", marks.Single().Text);
        }

        [Test]
        public void AMomentNamesTheVersionByItsTitle()
        {
            var white = CharacterMarks.Moment(Character("base.character.gandalf-white", Side.FreePeoples));
            Assert.AreEqual("Gandalf the White", white.Text);
            Assert.IsFalse(white.Removed);
            Assert.IsTrue(CharacterMarks.Moment(Character("base.character.boromir", Side.FreePeoples), removed: true).Removed);
        }

        [Test]
        public void ADieAbilityNamesOnlyTheOneIdentityThePublicCatalogGivesIt()
        {
            Assert.AreEqual("gimli", CharacterLibrary.AbilityHolder("politics-dwarves-at-erebor"));
            Assert.AreEqual("legolas", CharacterLibrary.AbilityHolder("politics-elves-at-stronghold"));
            Assert.IsNull(CharacterLibrary.AbilityHolder("combat-strength-plus-one"), "Several identities share it.");
            Assert.IsNull(CharacterLibrary.AbilityHolder("voice-recruit"), "The catalog does not list it.");
        }

        [Test]
        public void TravellingCompanionsOutOfTheFellowshipLeaveItFromAKnownPositionOnly()
        {
            var view = new PlayerView { Fellowship = new FellowshipView { RegionId = "rivendell", CompanionIdentities = { "gimli", "legolas" } } };
            var travel = new ActionMovement
            {
                Kind = ActionMovementKind.Travel, From = "rivendell", To = "fords-of-bruinen",
                Participants = { Character("base.character.gimli", Side.FreePeoples) }
            };
            Assert.AreEqual("base.character.gimli", FellowshipSeparation.Leaving(travel, view).Single().Id);
            Assert.IsFalse(FellowshipSeparation.HiddenOrigin(travel, view, Data.Regions.ContainsKey));
            view.Fellowship.Progress = 2;
            Assert.IsTrue(FellowshipSeparation.HiddenOrigin(travel, view, Data.Regions.ContainsKey), "Hidden progress hides the origin.");
            view.Characters.Add(new CharacterView { Id = "base.character.gimli", DefinitionId = "base.character.gimli", Location = "rivendell" });
            Assert.IsEmpty(FellowshipSeparation.Leaving(travel, view), "A Companion already on the board only travels.");
            travel.Kind = ActionMovementKind.March;
            view.Characters.Clear();
            Assert.IsEmpty(FellowshipSeparation.Leaving(travel, view));
        }

        private static ActionParticipant Unit(string id, string nation, UnitKind kind) =>
            new ActionParticipant { Id = id, Side = Side.Shadow, NationId = nation, UnitKind = kind };

        private static ActionParticipant Character(string definition, Side side) =>
            new ActionParticipant { Id = definition, Side = side, DefinitionId = definition };
    }
}
