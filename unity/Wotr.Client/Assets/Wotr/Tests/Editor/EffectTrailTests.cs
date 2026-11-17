using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class EffectTrailTests
    {
        // UI-297: the effect trail is recalled only in the turn console.
        public static VisualElement Console(PlayerView view)
        {
            var status = new ConsoleStatus();
            status.SetTrail(view, BoardPresentation.Load(), _ => { });
            return status.Trail;
        }

        // UI-543 (owner, 2026-11-17): the player's decision that a card brought (the opponent's card above all, which never
        // stands in the player's trail) names that card in the console; a click opens it. Nothing for the opponent's
        // decision, for a source already in the trail, or for an unknown source.
        [Test]
        public void TheCardBehindThePlayersDecisionStandsInTheConsole()
        {
            const string card = "base.sh.strategy.13";
            var view = new PlayerView { You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Kind = DecisionKind.Losses, SourceId = card } };
            string inspected = null;
            var status = new ConsoleStatus();
            status.SetTrail(view, BoardPresentation.Load(), id => inspected = id);
            var cause = status.Trail.Q<Button>(EffectTrailChoices.CauseName);
            Assert.IsNotNull(cause);
            Assert.AreEqual("◇ " + CardLibrary.Title(card), cause.text);
            StringAssert.Contains("comes from " + CardLibrary.Title(card), cause.tooltip);
            Assert.AreEqual(DisplayStyle.Flex, status.Trail.style.display.value, "The trail shows it.");
            var invoke = typeof(Clickable).GetMethod("Invoke", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Public, null, new[] { typeof(EventBase) }, null);
            invoke.Invoke(cause.clickable, new object[] { null });
            Assert.AreEqual(card, inspected, "A click opens the card.");

            view.DecisionOwner = Side.Shadow;
            Assert.AreEqual("", EffectTrailChoices.Cause(view), "The opponent's decision is theirs.");
            view.DecisionOwner = Side.FreePeoples;
            view.EffectTrail.Add(new EffectTrailView { SourceId = card });
            Assert.AreEqual("", EffectTrailChoices.Cause(view), "A source already in the trail is not named twice.");
            view.EffectTrail.Clear();
            view.Decision.SourceId = "hunt.tile.unknown";
            Assert.AreEqual("", EffectTrailChoices.Cause(view), "An unknown source is not named.");
        }

        [Test]
        public void CompactFiguresKeepTypesNationDetailsAndOriginalHistory()
        {
            var view = new PlayerView();
            Add(view, "r1", "sauron", UnitKind.Regular, "source", "moria");
            Add(view, "r2", "isengard", UnitKind.Regular, "source", "moria");
            Add(view, "e", "sauron", UnitKind.Elite, "source", "moria");
            Add(view, "n", "sauron", UnitKind.Nazgul, "source", "moria");
            var original = view.EffectTrail.ToArray();
            var root = Console(view);
            var button = root.Query<Button>().ToList().Single();
            Assert.That(button.text, Does.Contain("●2").And.Contain("◆1").And.Contain("N1").And.Contain("Moria"));
            Assert.That(button.tooltip, Does.Contain("1 × Sauron Regular").And.Contain("1 × Isengard Regular")
                .And.Contain("1 × Sauron Elite").And.Contain("1 × Sauron Nazgul"));
            CollectionAssert.AreEqual(original, view.EffectTrail);
        }

        [Test]
        public void DifferentSourcesAndRegionsRemainSeparateAndNonFigureChoicesRemainVisible()
        {
            var view = new PlayerView();
            Add(view, "r1", "sauron", UnitKind.Regular, "first", "moria");
            Add(view, "r2", "sauron", UnitKind.Regular, "second", "moria");
            Add(view, "r3", "sauron", UnitKind.Regular, "first", "orthanc");
            view.EffectTrail.Add(new EffectTrailView { SourceId = "first", ChoiceId = "moria" });
            var root = Console(view);
            var buttons = root.Query<Button>().ToList();
            Assert.AreEqual(4, buttons.Count);
            Assert.IsNotNull(root.Q<Button>("committed-figures-first-moria"));
            Assert.IsNotNull(root.Q<Button>("committed-figures-second-moria"));
            Assert.IsNotNull(root.Q<Button>("committed-figures-first-orthanc"));
            Assert.That(buttons.Last().text, Does.Contain("Moria"));
        }

        // UI-417 (owner): an option of the card's effect joins its card's button; two buttons opened the same card.
        [Test]
        public void AnEffectOptionJoinsItsCardsButton()
        {
            var view = new PlayerView();
            view.EffectTrail.Add(new EffectTrailView { SourceId = "lords.sh.character.17" });
            view.EffectTrail.Add(new EffectTrailView { SourceId = "lords.sh.character.17", ChoiceId = BalrogCardOrder.Alone });
            var buttons = Console(view).Query<Button>().ToList();
            Assert.AreEqual(1, buttons.Count);
            Assert.AreEqual("✓ A Balrog is Come! · Balrog alone", buttons[0].text);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void SovereignChoiceKeepsItsNameWithOrWithoutLeaderFigure(bool hasLeader)
        {
            const string id = "kings.sovereign.denethor";
            var view = new PlayerView();
            view.Sovereigns.Add(new SovereignView { Id = id, Title = "Denethor" });
            if (hasLeader)
                view.Units.Add(new UnitView { Id = id, Nation = "gondor", Kind = UnitKind.Leader, Location = "minas-tirith" });
            view.EffectTrail.Add(new EffectTrailView { SourceId = "kings.sh.strategy.26", ChoiceId = id, TargetId = "minas-tirith" });
            var root = Console(view);
            Assert.AreEqual("✓ Denethor → Minas Tirith", root.Query<Button>().ToList().Single().text);
        }

        private static void Add(PlayerView view, string id, string nation, UnitKind kind, string source, string region)
        {
            view.Units.Add(new UnitView { Id = id, Nation = nation, Kind = kind, Location = region });
            view.EffectTrail.Add(new EffectTrailView { SourceId = source, ChoiceId = id, TargetId = region });
        }
    }
}
