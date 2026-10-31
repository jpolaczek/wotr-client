using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-431 (owner's rule, UI-430): options the board draws are chosen on the board. The Black Captain's orders on the
    // Nazgûl, Lure of the Ring's on the companion's portrait and the corruption marker; every lit object's tooltip names
    // the action and the card that offers it.
    public sealed class BoardDrawnChoicesTests
    {
        private const string Captain = "base.sh.character.24";

        private static TurnDraft Draft()
        {
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            return draft;
        }

        [Test]
        public void TheBlackCaptainsRecruitIsTheReservesNazgulAndItsMoveTheMapsNazgul()
        {
            var view = CardConsoleOptionsTests.Captain(reserveNazgul: true);
            Assert.IsTrue(BlackCaptainOrder.RecruitOffered(view));
            Assert.AreEqual("base.sauron.nazgul.02", BlackCaptainOrder.ReserveNazgul(view));
            CollectionAssert.AreEquivalent(new[] { "osgiliath", "morannon" }, BlackCaptainOrder.FlyRegions(view).Keys);
            CollectionAssert.AreEquivalent(new[] { "osgiliath", "morannon" }, BlackCaptainOrder.FlyForces(view).Select(f => f.RegionId));
            Assert.AreEqual("Recruit · 2 Nazgûl · Click to recruit them with the Witch-king because of " + CardLibrary.Title(Captain),
                BlackCaptainOrder.RecruitTooltip(view));
            Assert.AreEqual("Move Nazgûl · Click to move these Nazgûl and the others, the Witch-king among them, because of " +
                CardLibrary.Title(Captain), BlackCaptainOrder.FlyRegions(view)["osgiliath"]);
            Assert.AreEqual("Click a glowing Nazgûl in the Reserve to recruit, or a glowing Nazgûl on the map to move them",
                BlackCaptainOrder.Hint(view));

            var reserve = ReinforcementsState.CaptainRecruit(view, true);
            var nazgul = reserve.Rows.Single().Figures.Single();
            Assert.AreEqual(UnitKind.Nazgul, nazgul.Kind);
            Assert.AreEqual("base.sauron.nazgul.02", nazgul.ReserveId);
            Assert.AreEqual(1, nazgul.Reserve);
            Assert.AreEqual(BlackCaptainOrder.RecruitTooltip(view), reserve.TokenHint);
            Assert.IsTrue(reserve.Open && reserve.Interactive);

            var empty = CardConsoleOptionsTests.Captain(reserveNazgul: false);
            Assert.IsFalse(BlackCaptainOrder.RecruitOffered(empty), "No reserve Nazgûl: the recruit keeps its button.");
            Assert.AreEqual("Click a glowing Nazgûl on the map to move them", BlackCaptainOrder.Hint(empty));
            view.DecisionOwner = Side.FreePeoples;
            CollectionAssert.IsEmpty(BlackCaptainOrder.FlyRegions(view), "The waiting side lights nothing.");
        }

        [Test]
        public void LureOfTheRingsCompanionGlowsRedAndTheCorruptionMarkerIsOffered()
        {
            var view = CardConsoleOptionsTests.Lure();
            var card = CardLibrary.Title("base.sh.character.13");
            Assert.AreEqual("base.character.gimli", LureChoice.Companion(view));
            Assert.IsTrue(LureChoice.Eliminates(view, "base.character.gimli"));
            Assert.IsFalse(LureChoice.Eliminates(view, "base.character.legolas"));
            Assert.AreEqual("Eliminate · Click to eliminate Gimli because of " + card +
                " · Gimli: eliminate this Companion; Take Them Alive still applies.", LureChoice.EliminateTooltip(view));
            Assert.AreEqual("Corruption +2 · Click to keep Gimli and take 2 Corruption because of " + card +
                " · Gimli: keep this Companion in the Fellowship.", LureChoice.CorruptionTooltip(view));
            Assert.AreEqual("Click Gimli's red portrait to eliminate Gimli, or the Fellowship's corruption marker to take the Corruption",
                LureChoice.Hint(view));

            view.OwnCompanions.Add(new CharacterView { Id = "base.character.legolas", DefinitionId = "base.character.legolas", Level = 2 });
            view.Fellowship = new FellowshipView { Guide = "base.character.gandalf-grey" };
            var roster = new FellowshipRoster();
            roster.Apply(view);
            roster.Context(view, Draft(), new ActionDieContext(), true);
            var gimli = roster.Root.Q<Button>("fellowship-portrait-base.character.gimli");
            Assert.IsTrue(gimli.ClassListContains(LureChoice.EliminateClass), "Red, not gold.");
            Assert.IsFalse(gimli.ClassListContains(ChoiceGlow.ClassName));
            StringAssert.StartsWith(LureChoice.EliminateTooltip(view), gimli.tooltip);
            Assert.IsFalse(roster.Root.Q<Button>("fellowship-portrait-base.character.legolas").ClassListContains(LureChoice.EliminateClass));
            Assert.IsTrue(roster.Choosing, "The panel's plaque invites a look.");
            roster.Context(view, Draft(), new ActionDieContext(), false);
            Assert.IsFalse(gimli.ClassListContains(LureChoice.EliminateClass), "Nothing is offered while a command is sent.");

            var status = new FellowshipStatus(() => { });
            status.OfferCorruption(true, LureChoice.CorruptionTooltip(view));
            Assert.IsTrue(status.CorruptionMarker.ClassListContains(ChoiceGlow.ClassName));
            Assert.IsTrue(status.CorruptionMarker.ClassListContains(FellowshipStatus.CorruptionOfferClass));
            Assert.AreEqual(LureChoice.CorruptionTooltip(view), status.CorruptionMarker.tooltip);
            Assert.AreEqual(PickingMode.Position, status.CorruptionMarker.pickingMode);
            status.OfferCorruption(false, "");
            Assert.AreEqual(PickingMode.Ignore, status.CorruptionMarker.pickingMode, "Otherwise the marker is a picture.");
        }
    }
}
