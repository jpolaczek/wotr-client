using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-312 on a mounted Free Peoples board: the Shadow's resources at their own HUD objects — a Ring's cost at its
    // console socket with the die's old → new face, its Hunt allocation and roll as one beat in its pool and Hunt Box,
    // draws and discards as card backs and the hand count, a pass or a quiet decline with no scene, a forfeited die as its
    // cost copy. Nothing added can be clicked, and the camera never moves. Frames go to .artifacts/screenshots (ui312-*).
    public sealed class OpponentResourcePresentationTests
    {
        private const string Ring = "base.ring.1";

        [UnityTest]
        public IEnumerator ARingIsChargedAtItsSocketAndItsDieShowsTheOldAndNewFace()
        {
            using var stage = Watching();
            stage.Before.Rings.Add(new RingView { Id = Ring, Owner = Side.Shadow });
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == PresentedBoardStage.Die).Location = DieLocation.Available;
            latest.Dice.Single(d => d.Id == PresentedBoardStage.Die).Face = DieFace.Muster;
            latest.Rings.Single().Owner = null;
            var step = stage.Step(1);
            step.Costs.Add(new ActionCost { Kind = ActionCostKind.Ring, Id = Ring, Owner = Side.Shadow });
            step.Facts.Dice.Add(new ActionDieChange
            {
                Id = PresentedBoardStage.Die, Owner = Side.Shadow, Before = DieFace.Army, After = DieFace.Muster, From = DieLocation.Available,
                To = DieLocation.Available
            });
            step.Facts.Resources.Add(new ActionResourceChange { Kind = ActionCostKind.Ring, Id = Ring, From = "Shadow", To = "removed" });
            var camera = stage.Board.Map.Camera.Pan;
            stage.Play(latest, null, step);
            yield return stage.Until(() => Mark(stage) is { } ring && ring.worldBound.Overlaps(stage.Board.Console.Rings.For(Ring).worldBound));
            yield return stage.Until(() => stage.Board.Root.Q(ResourcePresenter.PlateName) != null);
            var faces = stage.Board.Root.Q(ResourcePresenter.PlateName).Query(className: DieFaceIcon.ClassName).ToList();
            CollectionAssert.AreEqual(new[] { "die-face-army", "die-face-muster" }, faces.Select(f => f.GetClasses().Last()), "Old face → new face.");
            Assert.IsTrue(Mark(stage).worldBound.Overlaps(stage.Board.OpponentDice.Q<Button>(PresentedBoardStage.Die).worldBound), "The die is ringed.");
            Assert.AreEqual(camera, stage.Board.Map.Camera.Pan, "Resources never move the camera.");
            LayoutTests.Save(stage.Texture, "ui312-ring.png");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator TheHuntAllocationAndTheRollAreOneBeatInThePoolAndTheHuntBox()
        {
            using var stage = Watching();
            for (var i = 0; i < 3; i++)
                stage.Before.Dice.Add(new DieView { Id = "shadow-" + i, Owner = Side.Shadow, Source = "standard", Location = DieLocation.Unrolled });
            yield return stage.Ready();
            var eyes = stage.Board.Hunt.Box.Counts.Q<Label>("hunt-eyes").text;
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == PresentedBoardStage.Die).Location = DieLocation.Available;
            var step = stage.Step(1);
            step.Costs.Add(new ActionCost { Kind = ActionCostKind.Free, Owner = Side.Shadow });
            Roll(step, latest, "shadow-0", DieFace.Eye, DieLocation.Hunt);
            Roll(step, latest, "shadow-1", DieFace.Army, DieLocation.Available);
            Roll(step, latest, "shadow-2", DieFace.Character, DieLocation.Available);
            stage.Play(latest, null, step);
            var started = Time.realtimeSinceStartup;
            yield return stage.Until(() => stage.Board.Root.Query(ResourcePresenter.RingName).ToList().Count == 3);
            Assert.AreNotEqual(eyes, stage.Board.Hunt.Box.Counts.Q<Label>("hunt-eyes").text, "The Hunt Box counts the allocated die.");
            Assert.IsTrue(stage.Board.OpponentDice.Q<Button>("shadow-1").Q(className: "die-face-army") != null, "The rolled faces show.");
            LayoutTests.Save(stage.Texture, "ui312-allocation.png");
            yield return stage.Released();
            Assert.Less(Time.realtimeSinceStartup - started, 2.2f, "Three dice, one beat.");
        }

        [UnityTest]
        public IEnumerator DrawsAreOneBeatOfCardBacksAndTheHandCount()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Players.Single(p => p.Side == Side.Shadow).HandCount = 5;
            var draws = new[] { "character", "strategy" }.Select((deck, index) =>
            {
                var step = stage.Step(index + 1);
                step.Actor = null;
                step.Automatic = true;
                step.Kind = "CardsDrawn";
                step.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Draw, Deck = deck, Count = 1 });
                return step;
            }).ToList();
            draws[0].Actor = Side.Shadow;
            var hand = Hand(stage);
            stage.Play(latest, null, draws.ToArray());
            yield return stage.Until(() => stage.Board.Root.Q(ResourcePresenter.PlateName) != null);
            var plate = stage.Board.Root.Q(ResourcePresenter.PlateName);
            Assert.AreEqual(2, plate.Query(className: "presentation-card-back").ToList().Count, "Two backs, one beat.");
            Assert.AreEqual("+2", plate.Q<Label>("presentation-card-count").text);
            Assert.AreNotEqual(hand, Hand(stage), "The hand count changes with the beat.");
            Assert.IsFalse(plate.Query<Label>().ToList().Any(l => l.text.Contains(".")), "No card identity.");
            Assert.AreEqual(PickingMode.Ignore, plate.pickingMode, "Nothing to click.");
            LayoutTests.Save(stage.Texture, "ui312-draws.png");
            yield return stage.Released();
            Assert.AreEqual("5", Hand(stage));
        }

        [UnityTest]
        public IEnumerator ADiscardShowsItsCountAtTheHand()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Players.Single(p => p.Side == Side.Shadow).HandCount = 2;
            var discard = stage.Step(1);
            discard.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Discard, Count = 1 });
            stage.Play(latest, null, discard);
            yield return stage.Until(() => stage.Board.Root.Q(ResourcePresenter.PlateName)?.Q<Label>("presentation-card-count")?.text == "−1");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator APassAndQuietDeclinesPlayNoScene()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == PresentedBoardStage.Die).Location = DieLocation.Available;
            var pass = stage.Step(1);
            pass.Command = CommandKind.Pass;
            pass.Costs.Add(new ActionCost { Kind = ActionCostKind.Free, Owner = Side.Shadow });
            var declines = Enumerable.Range(2, 5).Select(order =>
            {
                var decline = stage.Step(order);
                decline.Command = CommandKind.DeclineReaction;
                return decline;
            });
            stage.Play(latest, null, declines.Prepend(pass).ToArray());
            for (var frame = 0; frame < 30; frame++)
            {
                Assert.IsNull(stage.Presenters.Skip.Button.parent, "No scene: no Skip target.");
                yield return null;
            }
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator AForfeitedDieKeepsItsCostCopy()
        {
            using var stage = Watching();
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == PresentedBoardStage.Die).Location = DieLocation.Discarded;
            var forfeit = stage.Paid(stage.Step(1), DieFace.Army);
            forfeit.Command = CommandKind.DiscardDie;
            stage.Play(latest, null, forfeit);
            yield return stage.Until(() => stage.Board.Root.Q(CostEmphasis.MarkName) != null);
            Assert.IsNull(stage.Board.Root.Q(ResourcePresenter.RingName), "No other scene.");
            yield return stage.Released();
        }

        // The Free Peoples' board, with the Shadow holding three cards.
        private static PresentedBoardStage Watching()
        {
            var stage = new PresentedBoardStage(1366, 1f, false);
            stage.Before.Players.Add(new PlayerSummary { Side = Side.FreePeoples, HandCount = 4 });
            stage.Before.Players.Add(new PlayerSummary { Side = Side.Shadow, HandCount = 3 });
            return stage;
        }

        private static void Roll(ActionLogStep step, PlayerView latest, string id, DieFace face, DieLocation to)
        {
            step.Facts.Dice.Add(new ActionDieChange { Id = id, Owner = Side.Shadow, After = face, From = DieLocation.Unrolled, To = to });
            var die = latest.Dice.Single(d => d.Id == id);
            die.Face = face;
            die.Location = to;
        }

        private static VisualElement Mark(PresentedBoardStage stage) => stage.Board.Root.Q(ResourcePresenter.RingName);

        private static string Hand(PresentedBoardStage stage) =>
            stage.Board.TopBar.Opponent.Root.Q("opponent-cards")?.Q<Label>(className: "summary-value")?.text ?? "";
    }
}
