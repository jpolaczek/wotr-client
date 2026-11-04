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
    // UI-488 (owner, 2026-11-04) on a mounted board: what the opponent gains through the viewer's own click plays on the
    // viewer's own step — its dice rolled or recovered, its automatic draws as backs, the Fellowship's Mordor inactivity —
    // while the camera stays and nothing the viewer did is played back to them. Dice that are in no row yet appear lit at
    // once, without 0.7 s of nothing. Frames go to .artifacts/screenshots (ui488-*).
    public sealed class OpponentOwnClickPresentationTests
    {
        [UnityTest]
        public IEnumerator TheFreePeoplesRollShowsOnTheShadowsOwnAllocation()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.Shadow);
            var free = stage.Before.Dice.Single(d => d.Id == PresentedBoardStage.FreeDie);
            free.Location = DieLocation.Unrolled;
            free.Face = null;
            stage.Before.Dice.Add(new DieView { Id = "free-2", Owner = Side.FreePeoples, Source = "standard", Location = DieLocation.Unrolled });
            stage.Before.Dice.Add(new DieView { Id = "own-1", Owner = Side.Shadow, Source = "standard", Location = DieLocation.Unrolled });
            yield return stage.Ready();
            var latest = stage.Latest();
            var allocation = stage.Step(1);
            allocation.Actor = Side.Shadow;
            allocation.Command = CommandKind.AllocateHunt;
            allocation.Costs.Add(new ActionCost { Kind = ActionCostKind.Free, Owner = Side.Shadow });
            Roll(allocation, latest, PresentedBoardStage.FreeDie, Side.FreePeoples, DieFace.Army);
            Roll(allocation, latest, "free-2", Side.FreePeoples, DieFace.Muster);
            Roll(allocation, latest, "own-1", Side.Shadow, DieFace.Character);
            var camera = stage.Board.Map.Camera.Pan;
            stage.Play(latest, "dol-guldur", allocation);
            camera = stage.Board.Map.Camera.Pan;
            yield return stage.Until(() => stage.Lit.Count == 2);
            var dice = stage.Board.OpponentDice;
            Assert.IsTrue(PresentationGlow.IsLit(dice.Q<Button>(PresentedBoardStage.FreeDie)) && PresentationGlow.IsLit(dice.Q<Button>("free-2")),
                "Their dice light in their row.");
            Assert.IsFalse(PresentationGlow.IsLit(stage.Board.OwnDice.Q<Button>("own-1")), "The Shadow's own roll is its own.");
            yield return stage.Until(() => dice.Q<Button>("free-2").Q(className: "die-face-muster") != null);
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui488-roll-shadow.png");
            Assert.AreEqual(DieLocation.Unrolled, stage.Board.View.Dice.Single(d => d.Id == "own-1").Location, "The view waits for the beat.");
            yield return stage.Released();
            Assert.AreEqual(camera, stage.Board.Map.Camera.Pan, "The camera stays the player's.");
            Assert.AreEqual(DieFace.Character, stage.Board.View.Dice.Single(d => d.Id == "own-1").Face);
            Assert.IsEmpty(stage.Lit);
        }

        [UnityTest]
        public IEnumerator TheOpponentsDrawsFlyWhenOurClickEndedTheTurn([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            stage.Before.Players.Add(new PlayerSummary { Side = Side.FreePeoples, HandCount = 4 });
            stage.Before.Players.Add(new PlayerSummary { Side = Side.Shadow, HandCount = 4 });
            yield return stage.Ready();
            var latest = stage.Latest();
            foreach (var player in latest.Players)
                player.HandCount += 2;
            var pass = stage.Step(1);
            pass.Actor = viewer;
            pass.Command = CommandKind.Pass;
            var own = Draw(stage, 2, viewer, "character", viewer == Side.FreePeoples ? "base.fp.character.01" : "base.sh.character.01");
            var ownCount = Draw(stage, 3, viewer, "character");
            var first = Draw(stage, 4, stage.Opponent, "character");
            var second = Draw(stage, 5, stage.Opponent, "strategy");
            var hand = Hand(stage);
            stage.Play(latest, "dol-guldur", pass, own, ownCount, first, second);
            var camera = stage.Board.Map.Camera.Pan;
            yield return stage.Until(() => Backs(stage).Count > 0);
            yield return stage.Until(() => Backs(stage).Count == 2);
            LayoutTests.Save(stage.Texture, "ui488-draws-" + viewer + ".png");
            Assert.AreEqual(hand, Hand(stage), "The count waits for the cards.");
            Assert.IsFalse(Backs(stage).Any(back => back.Query<Label>().ToList().Any()), "A back shows no identity.");
            yield return stage.Until(() => Hand(stage) == "6");
            yield return stage.Released();
            Assert.AreEqual(camera, stage.Board.Map.Camera.Pan, "The camera stays the player's.");
            Assert.AreEqual("6", Hand(stage));
        }

        // E10 of the audit: a recovered die is in no row until it changes, so it lights as it appears, whoever ended the turn.
        [UnityTest]
        public IEnumerator RecoveredDiceAppearLitAtOnce([Values(false, true)] bool ownClick)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var die = stage.Before.Dice.Single(d => d.Id == PresentedBoardStage.Die);
            die.Location = DieLocation.Spent;
            yield return stage.Ready();
            var row = stage.Board.OpponentDice;
            Assert.IsNull(row.Q<Button>(PresentedBoardStage.Die));
            Assert.AreEqual("1", Value(stage, "opponent-spent"));
            var latest = stage.Latest();
            var recovered = latest.Dice.Single(d => d.Id == PresentedBoardStage.Die);
            recovered.Location = DieLocation.Unrolled;
            recovered.Face = null;
            var step = stage.Moves(stage.Step(1), DieLocation.Spent, DieLocation.Unrolled, DieFace.Army, null);
            step.Actor = ownClick ? Side.FreePeoples : Side.Shadow;
            var started = Time.realtimeSinceStartup;
            stage.Play(latest, null, step);
            yield return stage.Until(() => PresentationGlow.IsLit(row.Q<Button>(PresentedBoardStage.Die)));
            Assert.Less(Time.realtimeSinceStartup - started, .5f, "No 0.7 s of nothing lit first.");
            Assert.IsNull(row.Q<Button>(PresentedBoardStage.Die).Q(className: "die-face-army"), "A recovered die is blank.");
            Assert.AreEqual("0", Value(stage, "opponent-spent"), "The count follows the die.");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui488-recovery-" + (ownClick ? "own" : "opponent") + ".png");
            Assert.IsTrue(stage.Playback.Holding, "It holds lit.");
            yield return stage.Released();
            Assert.IsEmpty(stage.Lit);
        }

        [UnityTest]
        public IEnumerator ADieChangeThatShowsNowhereTakesNoTime()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var die = stage.Before.Dice.Single(d => d.Id == PresentedBoardStage.Die);
            die.Location = DieLocation.Spent;
            yield return stage.Ready();
            var latest = stage.Latest();
            var removed = latest.Dice.Single(d => d.Id == PresentedBoardStage.Die);
            removed.Location = DieLocation.Removed;
            removed.Face = null;
            var started = Time.realtimeSinceStartup;
            stage.Play(latest, null, stage.Moves(stage.Step(1), DieLocation.Spent, DieLocation.Removed, DieFace.Army, null));
            yield return stage.Released();
            Assert.Less(Time.realtimeSinceStartup - started, .4f, "Nothing lights, nothing waits.");
        }

        [UnityTest]
        public IEnumerator MordorInactivityShowsOnOurOwnStep([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            stage.Before.Fellowship.RegionId = "mordor-track";
            stage.Before.Fellowship.MordorStep = 1;
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Fellowship.Corruption = 1;
            var last = stage.Step(1);
            last.Actor = viewer;
            last.Facts.FellowshipBefore = new HuntOutcomeView { RegionId = "mordor-track", MordorStep = 1, Hidden = true };
            last.Facts.FellowshipAfter = new HuntOutcomeView { RegionId = "mordor-track", MordorStep = 1, Hidden = true, Corruption = 1 };
            var inactivity = stage.Step(2);
            inactivity.Actor = null;
            inactivity.Kind = "MordorInactivity";
            var corruption = stage.Board.Fellowship.Status.Q<Label>("fellowship-status-corruption");
            var before = corruption.text;
            stage.Play(latest, "dol-guldur", last, inactivity);
            var camera = stage.Board.Map.Camera.Pan;
            yield return stage.Until(() => corruption.text != before);
            Assert.IsTrue(stage.Playback.Holding, "Its beat plays before the release.");
            Assert.IsTrue(stage.Board.Fellowship.IsOpen, "The panel opens read-only for it.");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui488-mordor-" + viewer + ".png");
            yield return stage.Released();
            Assert.AreEqual(camera, stage.Board.Map.Camera.Pan, "The camera stays the player's.");
            Assert.IsFalse(stage.Board.TracksStaged, "It does not move twice.");
        }

        // The player's own decision stays theirs: their die, their march, their draws play nothing.
        [UnityTest]
        public IEnumerator OurOwnActionIsNotPlayedBack([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            stage.Before.Dice.Add(new DieView { Id = "own-die", Owner = viewer, Face = DieFace.Army, Source = "standard", Location = DieLocation.Available });
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Dice.Single(d => d.Id == stage.OpponentDie).Location = DieLocation.Available;
            latest.Dice.Single(d => d.Id == "own-die").Location = DieLocation.Spent;
            var own = stage.Step(1);
            own.Actor = viewer;
            own.Command = CommandKind.DrawCard;
            own.Costs.Add(new ActionCost { Kind = ActionCostKind.Die, Id = "own-die", Owner = viewer, Face = DieFace.Army, Use = DieFace.Army });
            own.Facts.Dice.Add(new ActionDieChange
            {
                Id = "own-die", Owner = viewer, Before = DieFace.Army, After = DieFace.Army, From = DieLocation.Available, To = DieLocation.Spent
            });
            stage.Play(latest, null, own, Draw(stage, 2, viewer, "character"));
            for (var frame = 0; frame < 20; frame++)
            {
                Assert.IsNull(stage.Presenters.Skip.Button.parent, "No scene: no Skip target.");
                Assert.IsNull(stage.Board.Root.Q(className: SpentDieFlight.PieceClass), "Their own die flies nowhere.");
                yield return null;
            }
            yield return stage.Released();
        }

        private static void Roll(ActionLogStep step, PlayerView latest, string id, Side owner, DieFace face)
        {
            step.Facts.Dice.Add(new ActionDieChange { Id = id, Owner = owner, After = face, From = DieLocation.Unrolled, To = DieLocation.Available });
            var die = latest.Dice.Single(d => d.Id == id);
            die.Face = face;
            die.Location = DieLocation.Available;
        }

        private static ActionLogStep Draw(PresentedBoardStage stage, int order, Side owner, string deck, string id = "")
        {
            var step = stage.Step(order);
            step.Actor = null;
            step.Automatic = true;
            step.Kind = id.Length > 0 ? "CardDrawn" : "CardsDrawn";
            step.Facts.Cards.Add(new ActionCardFact { Owner = owner, Use = ActionCardUse.Draw, Deck = deck, Count = 1, Id = id });
            return step;
        }

        private static System.Collections.Generic.List<VisualElement> Backs(PresentedBoardStage stage) =>
            stage.Board.Root.Query(className: DrawFlight.BackClass).ToList();

        private static string Hand(PresentedBoardStage stage) => Value(stage, "opponent-cards");

        private static string Value(PresentedBoardStage stage, string chip) =>
            stage.Board.TopBar.Opponent.Root.Q(chip).Q<Label>(className: "summary-value").text;
    }
}
