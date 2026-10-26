using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class TopBarTests
    {
        [Test]
        public void SideCountersFollowThePublicProjection()
        {
            var view = View(handFp: 4, handShadow: 6);
            var shadow = SideResourceCounts.From(view, Side.Shadow);
            var free = SideResourceCounts.From(view, Side.FreePeoples);
            Assert.AreEqual((6, 1, 1, 2, 1), (shadow.Cards, shadow.Rings, shadow.Spent, shadow.Hunt, shadow.Out));
            // The Shadow die spent through a Free Peoples effect counts for the Free Peoples.
            Assert.AreEqual((4, 2, 2, 1, 0), (free.Cards, free.Rings, free.Spent, free.Hunt, free.Out));
        }

        [Test]
        public void OpponentSummaryUpdatesValuesInPlaceAndTheFixtureHidesIt()
        {
            var summary = new OpponentSummary();
            summary.Apply(View(handFp: 4, handShadow: 6), Side.Shadow);
            // UI-98: the Hunt Box moved to its own plaque at the right end of the bar.
            Assert.IsNull(summary.Root.Q("opponent-hunt"));
            var chips = new[] { "cards", "rings", "spent", "out" }.Select(key => summary.Root.Q("opponent-" + key)).ToArray();
            var values = chips.Select(chip => chip.Q<Label>(className: "summary-value")).ToArray();
            CollectionAssert.AreEqual(new[] { "6", "1", "1", "1" }, values.Select(label => label.text));
            // UI-361: the card counter says what the rules make public (OpponentCardsTests).
            StringAssert.StartsWith("Shadow · 6 cards in hand", chips[0].tooltip);
            Assert.AreEqual("Shadow · Dice out of play: 1", chips[3].tooltip);
            Assert.IsTrue(chips.All(chip => chip.pickingMode == PickingMode.Position), "Chips carry the hover tooltip.");
            Assert.IsTrue(chips.All(chip => chip.Query<VisualElement>().ToList().Skip(1).All(child => child.pickingMode == PickingMode.Ignore)));
            Assert.AreEqual(3, summary.Root.Query(className: "summary-divider").ToList().Count);

            summary.Apply(View(handFp: 4, handShadow: 7), Side.Shadow);
            Assert.AreSame(values[0], chips[0].Q<Label>(className: "summary-value"), "Values update without rebuilding chips.");
            Assert.AreEqual("7", values[0].text);
            Assert.AreEqual(DisplayStyle.Flex, summary.Root.style.display.value);
            summary.Hide();
            Assert.AreEqual(DisplayStyle.None, summary.Root.style.display.value);
        }

        [Test]
        public void TopBarComposesExistingControlsBehindNonInteractiveArtwork()
        {
            var phase = new Label("Turn 4 · Hunt allocation");
            var dice = new VisualElement { name = "opponent-dice" };
            var connection = new Label("Live");
            var menu = new Button { name = "open-board-menu" };
            var bar = new TopBarView(phase, dice, connection, menu);
            var children = bar.Root.Children().ToList();
            // UI-263: the expansions' crests follow the phase.
            CollectionAssert.AreEqual(new VisualElement[] { bar.Frame, phase, bar.Crests, children[3], dice, bar.Opponent.Root, connection, menu },
                children);
            Assert.IsTrue(bar.Root.ClassListContains("topbar"));
            Assert.IsTrue(bar.Root.ClassListContains("map-edge-control"), "UI-98: the slim bar pans only on the outer rim.");
            Assert.IsTrue(children[3].ClassListContains("topbar-divider"));
            Assert.AreEqual(PickingMode.Ignore, bar.Frame.pickingMode);
            Assert.AreEqual(PickingMode.Ignore, bar.Shadow.pickingMode);
            Assert.AreEqual(PickingMode.Ignore, children[3].pickingMode);
            Assert.IsNull(bar.Shadow.parent, "The board stage owns the cast shadow.");
        }

        [Test]
        public void OpponentPoolShowsItsOwnerInsteadOfCountsAndIgnoresHandChanges()
        {
            var pool = new ActionPoolView(_ => { }, _ => { });
            var root = new VisualElement();
            pool.Apply(root, View(handFp: 4, handShadow: 6), Side.Shadow, false);
            var owner = root.Q<Label>(className: "opponent-side");
            Assert.AreEqual("SHADOW", owner.text);
            Assert.IsFalse(root.Query<Label>().ToList().Any(label => label.text.Contains("Cards")));
            var die = root.Q<Button>("sh-available");
            pool.Apply(root, View(handFp: 4, handShadow: 7), Side.Shadow, false);
            Assert.AreSame(die, root.Q<Button>("sh-available"), "Drawing a card keeps the opponent dice and their focus.");
        }

        private static PlayerView View(int handFp, int handShadow) => new PlayerView
        {
            You = Side.FreePeoples,
            Players = new List<PlayerSummary>
            {
                new PlayerSummary { Side = Side.FreePeoples, HandCount = handFp },
                new PlayerSummary { Side = Side.Shadow, HandCount = handShadow }
            },
            Rings = new List<RingView>
            {
                new RingView { Id = "ring-1", Owner = Side.FreePeoples }, new RingView { Id = "ring-2", Owner = Side.FreePeoples },
                new RingView { Id = "ring-3", Owner = Side.Shadow }, new RingView { Id = "ring-4", Owner = null }
            },
            Dice = new List<DieView>
            {
                new DieView { Id = "sh-available", Owner = Side.Shadow, Face = DieFace.Army, Location = DieLocation.Available },
                new DieView { Id = "sh-spent", Owner = Side.Shadow, Location = DieLocation.Spent },
                new DieView { Id = "sh-spent-by-fp", Owner = Side.Shadow, Location = DieLocation.Spent, SpentWith = Side.FreePeoples },
                new DieView { Id = "sh-hunt-1", Owner = Side.Shadow, Location = DieLocation.Hunt },
                new DieView { Id = "sh-hunt-2", Owner = Side.Shadow, Location = DieLocation.Hunt },
                new DieView { Id = "sh-out", Owner = Side.Shadow, Location = DieLocation.Discarded },
                new DieView { Id = "fp-spent", Owner = Side.FreePeoples, Location = DieLocation.Spent },
                new DieView { Id = "fp-hunt", Owner = Side.FreePeoples, Location = DieLocation.Hunt },
                new DieView { Id = "fp-available", Owner = Side.FreePeoples, Face = DieFace.Character, Location = DieLocation.Available }
            }
        };
    }
}
