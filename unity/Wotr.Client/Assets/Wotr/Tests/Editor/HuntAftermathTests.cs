using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class HuntAftermathTests
    {
        [TestCase(DecisionKind.CompanionSeparation)]
        [TestCase(DecisionKind.EffectChoice)]
        public void MordorRemovalDoesNotDescribeATripToARegion(DecisionKind kind)
        {
            var view = View(kind);
            view.Decision.Presentation = "FellowshipCompanions";
            view.Fellowship.MordorStep = 2;
            Assert.AreEqual("Remove companions from play", HuntActions.DecisionText(view));
            view.Fellowship.MordorStep = null;
            Assert.AreEqual(kind == DecisionKind.CompanionSeparation ? "Place companions on the map"
                : "Separate companions", HuntActions.DecisionText(view));
            view.You = Side.Shadow;
            view.Decision = null;
            Assert.AreEqual("Waiting for Free Peoples", HuntActions.DecisionText(view));
        }

        [Test]
        public void OneHostOwnsGuideRoutePlacementAndTheNextTile()
        {
            var view = View(DecisionKind.Guide);
            var draft = new TurnDraft();
            foreach (var kind in new[] { DecisionKind.Guide, DecisionKind.FellowshipRoute,
                DecisionKind.CompanionSeparation, DecisionKind.HuntDamage })
            {
                view.Decision.Kind = kind;
                var inHunt = kind is DecisionKind.Guide or DecisionKind.HuntDamage;
                Assert.AreEqual(inHunt, HuntActions.UsesPanel(view, draft), kind.ToString());
                Assert.AreEqual(!inHunt, FellowshipActions.UsesPanel(view, draft), kind.ToString());
            }
        }

        [Test]
        public void MordorExplainsRevealStopAndVictoryWithoutInventingARollOrAdvance()
        {
            var view = View(DecisionKind.HuntDamage);
            view.Fellowship.MordorStep = 4;
            view.Fellowship.Corruption = 11;
            view.Hunt.Mordor = true;
            view.Hunt.TileId = "base.hunt.special-sh.1.1.1";
            view.Hunt.Damage = 1;
            view.Hunt.Reveal = true;
            view.Hunt.Stop = true;
            var tile = new HuntTileView(BoardPresentation.Load());
            tile.Apply(view.Hunt);
            Assert.AreEqual(HuntFocus.Tile, HuntPresentation.Focus(view));
            StringAssert.Contains("does not create a map route", tile.Root.tooltip);
            StringAssert.Contains("12 Corruption ends the game first", tile.Root.tooltip);
            Assert.AreEqual(DisplayStyle.Flex, tile.Root.Q<Label>("hunt-reveal").style.display.value);
            Assert.AreEqual(DisplayStyle.Flex, tile.Root.Q<Label>("hunt-stop").style.display.value);
            Assert.AreEqual(4, view.Fellowship.MordorStep);
            Assert.IsNull(view.Winner);
            view.Hunt.TileId = "";
            view.Hunt.Dice.Add(new HuntDieView { Result = 6 });
            Assert.AreEqual(HuntFocus.None, HuntPresentation.Focus(view));
        }

        private static PlayerView View(DecisionKind kind) => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Kind = kind },
            Hunt = new HuntView()
        };
    }
}
