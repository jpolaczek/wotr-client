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
    // TEST-20 (owner, 2026-11-04): each scene also plays with the Shadow watching the Free Peoples.
    public sealed class OpponentResourcePresentationTests
    {
        private const string Ring = "base.ring.1";

        [UnityTest]
        public IEnumerator ARingIsChargedAtItsSocketAndItsDieShowsTheOldAndNewFace([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Watching(viewer);
            stage.Before.Rings.Add(new RingView { Id = Ring, Owner = stage.Opponent });
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Location = DieLocation.Available;
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Face = DieFace.Muster;
            latest.Rings.Single().Owner = null;
            var step = stage.Step(1);
            step.Costs.Add(new ActionCost { Kind = ActionCostKind.Ring, Id = Ring, Owner = stage.Opponent });
            step.Facts.Dice.Add(new ActionDieChange
            {
                Id = stage.OpponentDie, Owner = stage.Opponent, Before = DieFace.Army, After = DieFace.Muster, From = DieLocation.Available,
                To = DieLocation.Available
            });
            step.Facts.Resources.Add(new ActionResourceChange { Kind = ActionCostKind.Ring, Id = Ring, From = stage.Opponent.ToString(), To = "removed" });
            var camera = stage.Board.Map.Camera.Pan;
            stage.Play(latest, null, step);
            yield return stage.Until(() => PresentationGlow.IsLit(stage.Board.Console.Rings.For(Ring)));
            yield return stage.Until(() => stage.Board.Root.Q(ResourcePresenter.PlateName) != null);
            var faces = stage.Board.Root.Q(ResourcePresenter.PlateName).Query(className: DieFaceIcon.ClassName).ToList();
            CollectionAssert.AreEqual(new[] { "die-face-army", "die-face-muster" }, faces.Select(f => f.GetClasses().Last()), "Old face → new face.");
            Assert.IsTrue(PresentationGlow.IsLit(stage.Board.OpponentDice.Q<Button>(stage.OpponentDie)), "The die glows.");
            Assert.AreEqual(camera, stage.Board.Map.Camera.Pan, "Resources never move the camera.");
            // UI-314: the glow has faded in.
            yield return new WaitForSecondsRealtime(.3f);
            stage.Save("ui312-ring.png");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator TheHuntAllocationAndTheRollAreOneBeatInThePoolAndTheHuntBox([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Watching(viewer);
            for (var i = 0; i < 3; i++)
                stage.Before.Dice.Add(new DieView { Id = "rolled-" + i, Owner = stage.Opponent, Source = "standard", Location = DieLocation.Unrolled });
            yield return stage.Ready();
            var eyes = stage.Board.Hunt.Box.Counts.Q<Label>("hunt-eyes").text;
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Location = DieLocation.Available;
            var step = stage.Step(1);
            step.Costs.Add(new ActionCost { Kind = ActionCostKind.Free, Owner = stage.Opponent });
            // The Free Peoples roll no Eye: the Shadow watches three faces in their row, its Hunt Box unchanged.
            var hunting = viewer == Side.FreePeoples;
            Roll(stage, step, latest, "rolled-0", hunting ? DieFace.Eye : DieFace.Muster, hunting ? DieLocation.Hunt : DieLocation.Available);
            Roll(stage, step, latest, "rolled-1", DieFace.Army, DieLocation.Available);
            Roll(stage, step, latest, "rolled-2", DieFace.Character, DieLocation.Available);
            stage.Play(latest, null, step);
            var started = Time.realtimeSinceStartup;
            yield return stage.Until(() => stage.Lit.Count == 3);
            if (hunting)
            {
                Assert.AreNotEqual(eyes, stage.Board.Hunt.Box.Counts.Q<Label>("hunt-eyes").text, "The Hunt Box counts the allocated die.");
                Assert.IsTrue(PresentationGlow.IsLit(stage.Board.Hunt.Box.EyeCount), "UI-318: the Eye and its number glow.");
            }
            else
                Assert.AreEqual(eyes, stage.Board.Hunt.Box.Counts.Q<Label>("hunt-eyes").text, "The Free Peoples' roll adds no Eye.");
            Assert.IsFalse(PresentationGlow.IsLit(stage.Board.Hunt.Tab), "UI-318: not the whole Hunt plaque.");
            yield return stage.Until(() => stage.Board.OpponentDice.Q<Button>("rolled-1")?.Q(className: "die-face-army") != null);
            // UI-314: the glow has faded in.
            yield return new WaitForSecondsRealtime(.3f);
            stage.Save("ui312-allocation.png");
            yield return stage.Released();
            Assert.Less(Time.realtimeSinceStartup - started, 2.2f, "Three dice, one beat.");
        }

        // UI-315 (owner review): the opponent's card counter lights up first, then its number changes while it is lit.
        [UnityTest]
        public IEnumerator TheHandCountLightsUpBeforeItsNumberChanges([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Watching(viewer);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Players.Single(p => p.Side == stage.Opponent).HandCount = 5;
            var draw = stage.Step(1);
            draw.Facts.Cards.Add(new ActionCardFact { Owner = stage.Opponent, Use = ActionCardUse.Draw, Deck = "character", Count = 1 });
            var hand = Hand(stage);
            var chip = stage.Board.TopBar.Opponent.Root.Q("opponent-cards");
            stage.Play(latest, null, draw);
            yield return stage.Until(() => PresentationGlow.IsLit(chip));
            var lit = Time.realtimeSinceStartup;
            Assert.AreEqual(hand, Hand(stage), "The counter lights up before its number changes.");
            yield return stage.Until(() => Hand(stage) != hand);
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - lit, PresentationPace.MarkSeconds - .1f, "It stays lit for the mark first.");
            Assert.IsTrue(PresentationGlow.IsLit(stage.Board.TopBar.Opponent.Root.Q("opponent-cards")), "The number changes while it is lit.");
            yield return stage.Released();
            Assert.AreEqual("5", Hand(stage));
        }

        // UI-329 (owner 2026-10-25): a drawn card's back slides out of its deck on the right and flies into the lit hand
        // count, which flashes and steps as it lands; two draws fly from their own decks.
        [UnityTest]
        public IEnumerator ADrawnCardFliesFromItsDeckIntoTheLitHandCount([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Watching(viewer);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Players.Single(p => p.Side == stage.Opponent).HandCount = 5;
            var draws = new[] { "strategy", "character" }.Select((deck, index) =>
            {
                var step = stage.Step(index + 1);
                step.Facts.Cards.Add(new ActionCardFact { Owner = stage.Opponent, Use = ActionCardUse.Draw, Deck = deck, Count = 1 });
                return step;
            }).ToArray();
            var hand = Hand(stage);
            var chip = stage.Board.TopBar.Opponent.Root.Q("opponent-cards");
            var strategy = stage.Board.Decks.Root.Q<Button>("deck-strategy").worldBound;
            var character = stage.Board.Decks.Root.Q<Button>("deck-character").worldBound;
            stage.Play(latest, null, draws);
            yield return stage.Until(() => Backs(stage).Count > 0);
            Assert.IsTrue(PresentationGlow.IsLit(HandChip(stage)), "The count is lit before the card leaves its deck.");
            Assert.AreEqual(hand, Hand(stage), "Its number waits for the card.");
            var first = Backs(stage)[0].worldBound;
            Assert.Less(Vector2.Distance(first.center, strategy.center), strategy.height, "The first comes out of the Strategy deck.");
            yield return stage.Until(() => Backs(stage).Count == 2);
            var second = Backs(stage)[1].worldBound;
            Assert.Less(Vector2.Distance(second.center, character.center), character.height + 20, "The second out of the Character deck.");
            var target = HandChip(stage).worldBound.center;
            yield return stage.Until(() => Backs(stage).Count > 0 &&
                Vector2.Distance(Backs(stage)[0].worldBound.center, target) < Vector2.Distance(first.center, target) / 2);
            stage.Save("ui329-draw-flight.png");
            Assert.AreEqual(hand, Hand(stage), "Still in flight.");
            Assert.AreEqual(PickingMode.Ignore, Backs(stage)[0].pickingMode, "Nothing to click.");
            yield return stage.Until(() => Hand(stage) != hand);
            Assert.IsTrue(HandChip(stage).ClassListContains(DrawFlight.FlashClass), "The count flashes as the card lands.");
            yield return stage.Until(() => Backs(stage).Count == 0);
            yield return stage.Released();
            Assert.AreEqual("5", Hand(stage));
        }

        private static System.Collections.Generic.List<VisualElement> Backs(PresentedBoardStage stage) =>
            stage.Board.Root.Query(className: DrawFlight.BackClass).ToList();

        private static VisualElement HandChip(PresentedBoardStage stage) => stage.Board.TopBar.Opponent.Root.Q("opponent-cards");

        [UnityTest]
        public IEnumerator DrawsAreOneBeatOfCardBacksAndTheHandCount([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Watching(viewer);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Players.Single(p => p.Side == stage.Opponent).HandCount = 5;
            var draws = new[] { "character", "strategy" }.Select((deck, index) =>
            {
                var step = stage.Step(index + 1);
                step.Actor = null;
                step.Automatic = true;
                step.Kind = "CardsDrawn";
                step.Facts.Cards.Add(new ActionCardFact { Owner = stage.Opponent, Use = ActionCardUse.Draw, Deck = deck, Count = 1 });
                return step;
            }).ToList();
            draws[0].Actor = stage.Opponent;
            var hand = Hand(stage);
            stage.Play(latest, null, draws.ToArray());
            yield return stage.Until(() => stage.Board.Root.Q(ResourcePresenter.PlateName) != null);
            var plate = stage.Board.Root.Q(ResourcePresenter.PlateName);
            Assert.AreEqual(2, plate.Query(className: "presentation-card-back").ToList().Count, "Two backs, one beat.");
            Assert.AreEqual("+2", plate.Q<Label>("presentation-card-count").text);
            Assert.AreNotEqual(hand, Hand(stage), "The hand count changes with the beat.");
            Assert.IsFalse(plate.Query<Label>().ToList().Any(l => l.text.Contains(".")), "No card identity.");
            Assert.AreEqual(PickingMode.Ignore, plate.pickingMode, "Nothing to click.");
            // UI-314: the glow has faded in.
            yield return new WaitForSecondsRealtime(.3f);
            stage.Save("ui312-draws.png");
            yield return stage.Released();
            Assert.AreEqual("5", Hand(stage));
        }

        [UnityTest]
        public IEnumerator ADiscardShowsItsCountAtTheHand([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Watching(viewer);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Players.Single(p => p.Side == stage.Opponent).HandCount = 2;
            var discard = stage.Step(1);
            discard.Facts.Cards.Add(new ActionCardFact { Owner = stage.Opponent, Use = ActionCardUse.Discard, Count = 1 });
            stage.Play(latest, null, discard);
            yield return stage.Until(() => stage.Board.Root.Q(ResourcePresenter.PlateName)?.Q<Label>("presentation-card-count")?.text == "−1");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator APassAndQuietDeclinesPlayNoScene([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Watching(viewer);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Location = DieLocation.Available;
            var pass = stage.Step(1);
            pass.Command = CommandKind.Pass;
            pass.Costs.Add(new ActionCost { Kind = ActionCostKind.Free, Owner = stage.Opponent });
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
        public IEnumerator AForfeitedDieKeepsItsCostCopy([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = Watching(viewer);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Location = DieLocation.Discarded;
            var forfeit = stage.Paid(stage.Step(1), DieFace.Army);
            forfeit.Command = CommandKind.DiscardDie;
            stage.Play(latest, null, forfeit);
            yield return stage.Until(() => stage.Board.Root.Q(CostEmphasis.MarkName) != null);
            Assert.IsEmpty(stage.Lit, "No other scene.");
            yield return stage.Released();
        }

        // The Free Peoples' board, with the Shadow holding three cards.
        private static PresentedBoardStage Watching(Side viewer)
        {
            var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            stage.Hands(viewer == Side.FreePeoples ? 4 : 3, viewer == Side.FreePeoples ? 3 : 4);
            return stage;
        }

        private static void Roll(PresentedBoardStage stage, ActionLogStep step, PlayerView latest, string id, DieFace face, DieLocation to)
        {
            step.Facts.Dice.Add(new ActionDieChange { Id = id, Owner = stage.Opponent, After = face, From = DieLocation.Unrolled, To = to });
            var die = latest.Dice.Single(d => d.Id == id);
            die.Face = face;
            die.Location = to;
        }



        private static string Hand(PresentedBoardStage stage) =>
            stage.Board.TopBar.Opponent.Root.Q("opponent-cards")?.Q<Label>(className: "summary-value")?.text ?? "";
    }
}
