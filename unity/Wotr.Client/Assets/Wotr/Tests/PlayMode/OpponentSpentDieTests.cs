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
    // UI-487 (owner, 2026-11-04) on a mounted board, for both viewers: the die that paid leaves. Ringed at the action's start,
    // it stays in its row through the action's effects; its final move then flies a copy into the count where it lies, the
    // count steps as it lands and the ring is gone. A second step of the die flies nothing, a die back in the pool neither.
    // Frames go to .artifacts/screenshots (ui487-*).
    public sealed class OpponentSpentDieTests
    {
        [UnityTest]
        public IEnumerator TheSpentDieFliesFromItsRowIntoTheSpentChip([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            yield return stage.Ready();
            var id = stage.OpponentDie;
            var chip = Chip(stage, "opponent-spent");
            var count = Value(chip);
            var row = stage.Board.OpponentDice.Q<Button>(id);
            var face = Face(stage);
            var latest = stage.Latest();
            stage.Play(latest, null, stage.Moves(stage.Paid(stage.Step(1), face), DieLocation.Available, DieLocation.Spent, face, face));
            yield return stage.Until(() => stage.Board.Root.Q(CostEmphasis.MarkName) != null);
            yield return stage.Until(() => Piece(stage) != null);
            var started = Time.realtimeSinceStartup;
            Assert.IsFalse(stage.Board.Root.Q(CostEmphasis.MarkName).ClassListContains("shown"), "The ring ends as the die leaves.");
            Assert.AreEqual(Visibility.Hidden, row.style.visibility.value, "The die leaves its place, which stays until the landing.");
            Assert.IsNotNull(Piece(stage).Q(className: "die-face-" + DieFaceIcon.Key(face)), "The copy wears the die's face.");
            Assert.IsTrue(Piece(stage).ClassListContains(viewer == Side.FreePeoples ? "die-shadow" : "die-free"));
            Assert.AreEqual(PickingMode.Ignore, Piece(stage).pickingMode, "Nothing to click.");
            Assert.AreEqual(count, Value(chip), "The count waits for the die.");
            var target = chip.worldBound.center;
            var from = Piece(stage).worldBound.center;
            yield return stage.Until(() => Piece(stage) == null || Vector2.Distance(Piece(stage).worldBound.center, target) < Vector2.Distance(from, target) * .6f);
            yield return stage.Until(() => PresentationGlow.IsLit(chip));
            LayoutTests.Save(stage.Texture, "ui487-spent-" + viewer + ".png");
            yield return stage.Until(() => Value(chip) != count);
            Assert.AreEqual("1", Value(chip), "One die, one step.");
            Assert.IsTrue(chip.ClassListContains(DrawFlight.FlashClass), "The count flashes as the die lands.");
            Assert.IsNull(Piece(stage));
            yield return null;
            Assert.IsNull(stage.Board.OpponentDice.Q<Button>(id), "The row closes at the landing.");
            yield return stage.Released();
            Assert.Less(Time.realtimeSinceStartup - started, 1.3f, "It stays short.");
            Assert.IsEmpty(stage.Lit, "Nothing glows after the release.");
            Assert.AreEqual("1", Value(chip), "The release does not count it again.");
        }

        [UnityTest]
        public IEnumerator AForfeitedDieFliesIntoTheOutChip([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            yield return stage.Ready();
            var chip = Chip(stage, "opponent-out");
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Location = DieLocation.Discarded;
            var forfeit = stage.Moves(stage.Paid(stage.Step(1), Face(stage)), DieLocation.Available, DieLocation.Discarded, Face(stage), Face(stage));
            forfeit.Command = CommandKind.DiscardDie;
            stage.Play(latest, null, forfeit);
            yield return stage.Until(() => Piece(stage) != null);
            yield return stage.Until(() => Value(chip) == "1");
            Assert.AreEqual("0", Value(Chip(stage, "opponent-spent")));
            yield return stage.Released();
        }

        // E14 of the audit: the Free Peoples' die enters the Hunt Box at its own count, after the move's effects.
        [UnityTest]
        public IEnumerator TheFreePeoplesDieEntersTheHuntBoxCount()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.Shadow);
            yield return stage.Ready();
            var count = stage.Board.Hunt.Box.Count(Side.FreePeoples);
            var dice = stage.Board.Hunt.Box.Counts.Q<Label>("hunt-free-dice");
            var eyes = stage.Board.Hunt.Box.Counts.Q<Label>("hunt-eyes").text;
            Assert.AreEqual("0", dice.text);
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Location = DieLocation.Hunt;
            var move = stage.Moves(stage.Paid(stage.Step(1), DieFace.Character), DieLocation.Available, DieLocation.Hunt, DieFace.WillOfTheWest, DieFace.Character);
            stage.Play(latest, null, move);
            yield return stage.Until(() => Piece(stage) != null);
            yield return stage.Until(() => PresentationGlow.IsLit(count));
            var target = count.worldBound.center;
            var from = Piece(stage).worldBound.center;
            yield return stage.Until(() => Piece(stage) == null || Vector2.Distance(Piece(stage).worldBound.center, target) < Vector2.Distance(from, target) * .6f);
            LayoutTests.Save(stage.Texture, "ui487-hunt-box.png");
            Assert.AreEqual("0", dice.text, "The count waits for the die.");
            yield return stage.Until(() => dice.text == "1");
            Assert.AreEqual(eyes, stage.Board.Hunt.Box.Counts.Q<Label>("hunt-eyes").text, "The Eyes stay.");
            Assert.AreEqual("0", Value(Chip(stage, "opponent-spent")), "It is in the Box, not spent.");
            yield return stage.Released();
            Assert.AreEqual("1", dice.text);
        }

        // An Eye paid from the Hunt Box (a Kings corruption attempt) leaves the plaque, not a row that never held it.
        [UnityTest]
        public IEnumerator AnEyePaidFromTheHuntBoxLeavesThePlaque()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            stage.Before.Dice.Single(d => d.Id == PresentedBoardStage.Die).Location = DieLocation.Hunt;
            stage.Before.Dice.Single(d => d.Id == PresentedBoardStage.Die).Face = DieFace.Eye;
            yield return stage.Ready();
            var plaque = stage.Board.Hunt.Box.EyeCount;
            var eyes = stage.Board.Hunt.Box.Counts.Q<Label>("hunt-eyes");
            Assert.AreEqual("1", eyes.text);
            var latest = stage.Latest();
            stage.Play(latest, null, stage.Moves(stage.Paid(stage.Step(1), DieFace.Eye), DieLocation.Hunt, DieLocation.Spent, DieFace.Eye, DieFace.Eye));
            yield return stage.Until(() => Piece(stage) != null);
            Assert.Less(Vector2.Distance(Piece(stage).worldBound.center, plaque.worldBound.center), 40, "It starts at the plaque.");
            yield return stage.Until(() => eyes.text == "0" && Value(Chip(stage, "opponent-spent")) == "1");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator AMusterDieWithTwoPointsLeavesOnceAtTheEnd()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var id = stage.OpponentDie;
            var reserve = stage.Reserve("sauron", UnitKind.Regular, 2);
            var first = stage.Latest();
            first.Dice.Single(d => d.Id == id).Location = DieLocation.Resolving;
            PresentedBoardStage.Place(first, reserve.Take(1), "dol-guldur");
            var one = stage.Moves(stage.Recruits(stage.Paid(stage.Step(1), DieFace.Muster), reserve.Take(1), "dol-guldur"),
                DieLocation.Available, DieLocation.Resolving, DieFace.Muster, DieFace.Muster);
            stage.Play(first, null, one);
            var flown = false;
            while (stage.Playback.Holding)
            {
                flown |= Piece(stage) != null;
                yield return null;
            }
            Assert.IsFalse(flown, "The first point spends nothing yet.");
            yield return stage.Released();
            Assert.IsNotNull(stage.Board.OpponentDice.Q<Button>(id), "It stays in its row, for the second point.");
            Assert.AreEqual("0", Value(Chip(stage, "opponent-spent")));

            var second = stage.Next(first);
            PresentedBoardStage.Place(second, reserve.Skip(1), "mount-gundabad");
            var two = stage.Moves(stage.Recruits(stage.Step(1, 2), reserve.Skip(1), "mount-gundabad"),
                DieLocation.Resolving, DieLocation.Spent, DieFace.Muster, DieFace.Muster);
            two.ActionId = one.ActionId;
            stage.Play(second, null, two);
            yield return stage.Until(() => stage.Board.Map.Q("observed-delta") != null);
            Assert.IsNull(Piece(stage), "The second recruit lands first.");
            Assert.IsFalse(stage.Board.Root.Query(CostEmphasis.MarkName).ToList().Any(mark => mark.ClassListContains("shown")), "The cost is not charged again.");
            var pieces = 0;
            var flying = false;
            while (stage.Playback.Holding)
            {
                var now = Piece(stage) != null;
                if (now && !flying)
                    pieces++;
                flying = now;
                yield return null;
            }
            Assert.AreEqual(1, pieces, "It leaves once.");
            yield return stage.Released();
            Assert.AreEqual("1", Value(Chip(stage, "opponent-spent")));
        }

        // The face it left with, when an Elven Ring or a Ruler token changed it in the paying command.
        [UnityTest]
        public IEnumerator ADieChangedAsItPaysLeavesWithItsNewFace([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Face = DieFace.Muster;
            var step = stage.Moves(stage.Paid(stage.Step(1), DieFace.Muster), DieLocation.Available, DieLocation.Spent, Face(stage), DieFace.Muster);
            step.Costs.Add(new ActionCost { Kind = viewer == Side.FreePeoples ? ActionCostKind.Ring : ActionCostKind.RulerToken, Id = "changer", Owner = stage.Opponent });
            stage.Play(latest, null, step);
            yield return stage.Until(() => Piece(stage) != null);
            Assert.IsNotNull(Piece(stage).Q(className: "die-face-muster"));
            yield return stage.Released();
            Assert.AreEqual("1", Value(Chip(stage, "opponent-spent")));
        }

        [UnityTest]
        public IEnumerator ADieBackInThePoolFliesNowhere()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.Shadow);
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Location = DieLocation.Available;
            stage.Play(latest, null, stage.Moves(stage.Paid(stage.Step(1), Face(stage)), DieLocation.Resolving, DieLocation.Available, Face(stage), Face(stage)));
            var flown = false;
            while (stage.Playback.Holding)
            {
                flown |= Piece(stage) != null;
                yield return null;
            }
            Assert.IsFalse(flown, "Vilya keeps it where it is.");
            yield return stage.Released();
            Assert.IsNotNull(stage.Board.OpponentDice.Q<Button>(stage.OpponentDie));
        }

        [UnityTest]
        public IEnumerator UnderSkipAnimationsTheCountStepsWithoutAFlight([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, true, viewer);
            yield return stage.Ready();
            var chip = Chip(stage, "opponent-spent");
            var latest = stage.Latest();
            stage.Play(latest, null, stage.Moves(stage.Paid(stage.Step(1), Face(stage)), DieLocation.Available, DieLocation.Spent, Face(stage), Face(stage)));
            var flown = false;
            while (Value(chip) != "1")
            {
                flown |= Piece(stage) != null;
                yield return null;
            }
            var stepped = Time.realtimeSinceStartup;
            Assert.IsTrue(stage.Playback.Holding, "It steps inside the presentation.");
            Assert.IsFalse(flown, "Nothing flies.");
            yield return stage.Until(() => PresentationGlow.IsLit(chip));
            LayoutTests.Save(stage.Texture, "ui487-reduced-" + viewer + ".png");
            yield return stage.Released();
            Assert.Less(Time.realtimeSinceStartup - stepped, .8f, "It stays short.");
            Assert.IsNull(stage.Board.OpponentDice.Q<Button>(stage.OpponentDie));
        }

        [UnityTest]
        public IEnumerator SkipDuringTheFlightEndsOnTheLatestCounts()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var chip = Chip(stage, "opponent-spent");
            var latest = stage.Latest();
            stage.Play(latest, null, stage.Moves(stage.Paid(stage.Step(1), Face(stage)), DieLocation.Available, DieLocation.Spent, Face(stage), Face(stage)));
            yield return stage.Until(() => Piece(stage) != null);
            PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            Assert.IsNull(Piece(stage), "Skip removes the copy.");
            Assert.AreEqual("1", Value(chip));
            Assert.IsNull(stage.Board.OpponentDice.Q<Button>(stage.OpponentDie));
            Assert.IsEmpty(stage.Lit);
        }

        // The die leaves after the action's effects: a recruit lands while it still stands ringed in its row.
        [UnityTest]
        public IEnumerator TheDieLeavesAfterItsActionsEffects()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var reserve = stage.Reserve("sauron", UnitKind.Regular, 1);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve, "dol-guldur");
            var step = stage.Moves(stage.Recruits(stage.Paid(stage.Step(1), DieFace.Muster), reserve, "dol-guldur"),
                DieLocation.Available, DieLocation.Spent, DieFace.Muster, DieFace.Muster);
            stage.Play(latest, null, step);
            yield return stage.Until(() => stage.Board.Map.Q("observed-delta") != null);
            Assert.IsNull(Piece(stage), "The recruit first.");
            Assert.IsTrue(stage.Board.Root.Q(CostEmphasis.MarkName).ClassListContains("shown"), "Its source stays ringed.");
            Assert.IsNotNull(stage.Board.OpponentDice.Q<Button>(stage.OpponentDie));
            yield return stage.Until(() => Piece(stage) != null);
            Assert.AreEqual(1, stage.Army("dol-guldur"));
            yield return stage.Released();
        }

        private static DieFace Face(PresentedBoardStage stage) => stage.Viewer == Side.FreePeoples ? DieFace.Army : DieFace.WillOfTheWest;

        private static VisualElement Piece(PresentedBoardStage stage) => stage.Board.Root.Q(className: SpentDieFlight.PieceClass);

        private static VisualElement Chip(PresentedBoardStage stage, string name) => stage.Board.TopBar.Opponent.Root.Q(name);

        private static string Value(VisualElement chip) => chip.Q<Label>(className: "summary-value").text;
    }
}
