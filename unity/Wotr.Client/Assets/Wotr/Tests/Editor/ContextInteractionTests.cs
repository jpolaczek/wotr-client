using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    public sealed class ContextInteractionTests
    {
        [Test]
        public void DwellGraceAndPanelCrossingDoNotFlicker()
        {
            var hover = new HoverState();
            hover.Observe("dale", 0);
            hover.Observe("dale", .12);
            Assert.IsFalse(hover.Tick(.19));
            Assert.IsTrue(hover.Tick(.21));
            Assert.AreEqual("dale", hover.Visible);
            hover.Observe("", .3);
            hover.Panel(true, .4);
            Assert.IsFalse(hover.Tick(1));
            Assert.AreEqual("dale", hover.Visible);
            hover.Panel(false, 1);
            Assert.IsFalse(hover.Tick(1.14));
            Assert.IsTrue(hover.Tick(1.16));
            Assert.IsEmpty(hover.Visible);
        }

        [Test]
        public void MerelyCrossingARegionNeverOpensItAndClearCancelsPendingOpen()
        {
            var hover = new HoverState();
            hover.Observe("dale", 0);
            hover.Observe("erebor", .1);
            hover.Tick(.21);
            Assert.IsEmpty(hover.Visible);
            hover.Clear();
            hover.Tick(2);
            Assert.IsEmpty(hover.Visible);
            hover.Focus("dale");
            Assert.AreEqual("dale", hover.Visible);
            hover.Blur(3);
            hover.Tick(3.16);
            Assert.IsEmpty(hover.Visible);
        }

        [TestCase(DieFace.ArmyMuster)]
        [TestCase(DieFace.WillOfTheWest)]
        public void MultifunctionDiceExposeAllOrdinaryUsesWithoutChangingTheirFace(DieFace face)
        {
            var fixture = new BoardFixture();
            fixture.View.Dice[0].Face = face;
            fixture.Refresh();
            var context = new ActionDieContext();
            context.Sync(fixture.View, "army");
            Assert.IsTrue(MusterActions.Active(fixture.View, new TurnDraft(), context));
            Assert.IsTrue(context.Supports(DieFace.Army));
            Assert.IsNotEmpty(ContextArmySelection.Eligible(fixture.View, context));
            Assert.AreEqual(face, fixture.View.Dice[0].Face);
            context.Sync(fixture.View, "army");
            Assert.AreEqual(face, context.Face);
            fixture.View.Decision.Id = "next";
            context.Sync(fixture.View, "army");
            Assert.IsTrue(context.Supports(DieFace.Muster));
            fixture.View.Dice[0].Location = DieLocation.Spent;
            context.Sync(fixture.View, "army");
            Assert.IsEmpty(context.DieId);
        }

        [Test]
        public void ContextDoesNotSurviveADifferentMatchFaceOrDecisionOwner()
        {
            var fixture = new BoardFixture();
            fixture.View.Dice[0].Face = DieFace.ArmyMuster;
            var context = new ActionDieContext();
            context.Sync(fixture.View, "army");
            fixture.View.MatchId = "another-match";
            context.Sync(fixture.View, "army");
            Assert.IsTrue(context.Supports(DieFace.Muster));
            fixture.View.Dice[0].Face = DieFace.WillOfTheWest;
            context.Sync(fixture.View, "army");
            Assert.IsTrue(context.Supports(DieFace.Muster));
            fixture.View.DecisionOwner = Side.Shadow;
            context.Sync(fixture.View, "army");
            Assert.IsEmpty(context.DieId);
        }

        [Test]
        public void FigureClickSelectsOnlyThatFigureAndChangingSourceReplacesPendingSelection()
        {
            var fixture = new BoardFixture();
            var context = new ActionDieContext();
            var draft = new TurnDraft();
            draft.Payload.DieId = "army";
            context.Sync(fixture.View, "army");
            var first = fixture.View.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular);
            var second = fixture.View.Units.First(u => u.Location == "dale");
            Assert.IsTrue(ContextArmySelection.Select(draft, fixture.View, context, first.Id));
            CollectionAssert.AreEqual(new[] { first.Id }, ArmyDraft.Current(draft).UnitIds);
            Assert.IsTrue(ContextArmySelection.Select(draft, fixture.View, context, second.Id));
            Assert.AreEqual(1, draft.Payload.Moves.Count);
            CollectionAssert.AreEqual(new[] { second.Id }, ArmyDraft.Current(draft).UnitIds);
            Assert.AreEqual("minas-tirith", first.Location);
            Assert.AreEqual("army", draft.Payload.DieId);
        }

        [Test]
        public void HoverIndexContainsOnlyProjectedBoardCharactersNotPrivateCompanionVersions()
        {
            var fixture = new BoardFixture();
            fixture.View.Characters.Add(new CharacterView { Id = "public", Location = "dale" });
            fixture.View.OwnCompanions.Add(new CharacterView { Id = "private", Location = "dale" });
            var index = new RegionViewIndex(fixture.View);
            CollectionAssert.AreEqual(new[] { "public" }, index.Get("dale").Characters.Select(c => c.Id));
            Assert.IsNull(index.Get("unknown-region"));
        }
    }
}
