using System.Collections.Generic;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-267: the Lords special dice in the pool — their origin, the dice set aside after the choice, and what the
    // choice's lit dice say.
    public sealed class SpecialDiceTests
    {
        [TestCase("lords.die.vilya", "vilya")]
        [TestCase("lords.die.nenya", "nenya")]
        [TestCase("lords.die.narya", "narya")]
        [TestCase("lords.die.balrog", "balrog")]
        [TestCase("lords.die.gothmog", "gothmog")]
        [TestCase("standard", "")]
        [TestCase("ruler", "")]
        public void EachLordsDieHasItsOrigin(string source, string origin) =>
            Assert.AreEqual(origin, SpecialDice.Origin(new DieView { Source = source }));

        [Test]
        public void OnlyALordsDieNotKeptThisTurnIsSetAside()
        {
            Assert.IsTrue(SpecialDice.SetAside(Die("lords.die.nenya", DieFace.Event, DieLocation.Discarded)));
            Assert.IsFalse(SpecialDice.SetAside(Die("standard", DieFace.Event, DieLocation.Discarded)));
            var kept = Die("lords.die.vilya", DieFace.Eye, DieLocation.Discarded);
            kept.SpecialDieChosen = true;
            Assert.IsFalse(SpecialDice.SetAside(kept));
            var removed = Die("lords.die.vilya", DieFace.Eye, DieLocation.Discarded);
            removed.PermanentlyRemoved = true;
            Assert.IsFalse(SpecialDice.SetAside(removed));
        }

        [Test]
        public void AnEyeAmongTheDiceForcesTheChoiceAndTheHintSaysSo()
        {
            var vilya = Die("lords.die.vilya", DieFace.Eye, DieLocation.AwaitingChoice);
            var nenya = Die("lords.die.nenya", DieFace.Character, DieLocation.AwaitingChoice);
            var view = Choice(vilya, nenya);
            Assert.IsTrue(SpecialDice.Candidate(view, nenya));
            Assert.IsTrue(SpecialDice.EyeForced(view));
            StringAssert.StartsWith("An Eye must be kept", SpecialDice.Hint(view));
            nenya.Face = DieFace.Eye;
            Assert.IsFalse(SpecialDice.EyeForced(view));
            StringAssert.StartsWith("Click a highlighted die", SpecialDice.Hint(view));
            view.DecisionOwner = Side.Shadow;
            Assert.IsFalse(SpecialDice.Choosing(view), "The opponent's choice lights nothing of ours.");
        }

        [Test]
        public void ALitDieSaysWhatKeepingItDoesAndWhatItsRemoveStarMeans()
        {
            var eye = Die("lords.die.vilya", DieFace.Eye, DieLocation.AwaitingChoice);
            eye.RemoveSymbol = true;
            var tooltip = SpecialDice.KeepTooltip(eye);
            StringAssert.StartsWith("Click to keep Vilya · Eye.", tooltip);
            StringAssert.Contains("strengthens the Shadow's Hunt", tooltip);
            StringAssert.Contains("★ Remove · kept, this die leaves the game at the next Recover if Gandalf the White", tooltip);
            var gothmog = Die("lords.die.gothmog", DieFace.Muster, DieLocation.AwaitingChoice);
            gothmog.Owner = Side.Shadow;
            tooltip = SpecialDice.KeepTooltip(gothmog);
            StringAssert.Contains("Use it as an action die this turn.", tooltip);
            StringAssert.DoesNotContain("★", tooltip);
            gothmog.RemoveSymbol = true;
            StringAssert.Contains("the Witch-king (either version)", SpecialDice.KeepTooltip(gothmog));
        }

        [Test]
        public void ADieNotKeptSaysItWaitsAndItsStarRemovesNothing()
        {
            var nenya = Die("lords.die.nenya", DieFace.Character, DieLocation.Discarded);
            nenya.RemoveSymbol = true;
            var tooltip = SpecialDice.SetAsideTooltip(nenya);
            StringAssert.Contains("Not kept this turn", tooltip);
            StringAssert.Contains("removes nothing", tooltip);
            StringAssert.Contains("must be kept", SpecialDice.BlockedTooltip(nenya));
        }

        [Test]
        public void TheDiceHintsOfTheChoiceSayWhatEachDieDoes()
        {
            var vilya = Die("lords.die.vilya", DieFace.Eye, DieLocation.AwaitingChoice);
            var nenya = Die("lords.die.nenya", DieFace.Character, DieLocation.AwaitingChoice);
            var view = Choice(vilya, nenya);
            view.Options.Add(new ActionOption { Kind = CommandKind.ChooseSpecialDie, Enabled = true, EligibleDice = { vilya.Id } });
            StringAssert.StartsWith("Click to keep Vilya · Eye.", DieHints.Describe(view, vilya));
            StringAssert.Contains("must be kept", DieHints.Describe(view, nenya));
            nenya.Location = DieLocation.Discarded;
            StringAssert.Contains("Not kept this turn", DieHints.Describe(view, nenya));
        }

        private static DieView Die(string source, DieFace face, DieLocation location) => new DieView
        {
            Id = source, Source = source, Owner = Side.FreePeoples, Face = face, Location = location
        };

        private static PlayerView Choice(params DieView[] dice) => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "d", Kind = DecisionKind.SpecialDieChoice },
            Dice = new List<DieView>(dice)
        };
    }
}
