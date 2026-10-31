using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-431 (owner's rule, UI-430): options the board draws are chosen on the board. The Black Captain's orders on the
    // Nazgûl, Lure of the Ring's on the companion's portrait and the corruption marker, Monsters Roused's halves on their
    // regions; every lit object's tooltip names the action and the card that offers it.
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
            CollectionAssert.AreEqual(new[] { "base.character.witch-king" }, BlackCaptainOrder.FlyFigures(view));
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

        [Test]
        public void MonstersRousedsHalvesAreTheirRegionsAndCarryTheRegionOn()
        {
            var view = new PlayerView
            {
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "monsters", Kind = DecisionKind.EffectChoice, Presentation = "EffectTargets", SourceId = "fate.sh.strategy.22" },
                Options =
                {
                    new ActionOption
                    {
                        Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1,
                        EligibleIds = { MonstersHalves.West, MonstersHalves.East },
                        EffectChoices =
                        {
                            new EffectChoiceView { Id = MonstersHalves.West, Title = "West of the mountains", Detail = "One Sauron Regular each in Angmar, Ettenmoors and Weather Hills, and a Sauron Elite in Trollshaws." },
                            new EffectChoiceView { Id = MonstersHalves.East, Title = "East of the mountains", Detail = "One Sauron Regular each in Western Mirkwood, Northern Mirkwood and Withered Heath, and a Sauron Elite in one of them." }
                        }
                    }
                }
            };
            var draft = Draft();
            Assert.IsEmpty(CardConsoleOptions.For(view, draft), "No buttons: the regions choose.");
            CollectionAssert.AreEquivalent(new[] { "angmar", "ettenmoors", "weather-hills", "trollshaws", "western-mirkwood", "northern-mirkwood", "withered-heath" },
                MonstersHalves.Regions(view, draft));
            Assert.AreEqual(MonstersHalves.West, MonstersHalves.Half(view, "trollshaws"));
            Assert.AreEqual(MonstersHalves.East, MonstersHalves.Half(view, "withered-heath"));
            Assert.AreEqual("", MonstersHalves.Half(view, "dol-guldur"));
            Assert.AreEqual(MonstersHalves.EastStep, MonstersHalves.NextStep(MonstersHalves.East));
            Assert.AreEqual(MonstersHalves.WestStep, MonstersHalves.NextStep(MonstersHalves.West));
            StringAssert.StartsWith("East of the mountains · Click to recruit east of the mountains because of " + CardLibrary.Title("fate.sh.strategy.22"),
                MonstersHalves.Offers(view)["northern-mirkwood"]);
            Assert.AreEqual("Click a lit region west or east of the Misty Mountains to recruit on that side", MonstersHalves.Hint(view));

            // The carried region waits for its half's next step only.
            var carry = new BranchCarry();
            carry.Set(view, MonstersHalves.NextStep(MonstersHalves.East), "withered-heath");
            view.Decision = new DecisionView { Id = "elite", Kind = DecisionKind.EffectChoice, Presentation = MonstersHalves.EastStep };
            Assert.AreEqual("", carry.Take(view, MonstersHalves.WestStep));
            Assert.AreEqual("withered-heath", carry.Take(view, MonstersHalves.EastStep));
            Assert.AreEqual("", carry.Take(view, MonstersHalves.EastStep), "Taken once.");
        }
    }
}
