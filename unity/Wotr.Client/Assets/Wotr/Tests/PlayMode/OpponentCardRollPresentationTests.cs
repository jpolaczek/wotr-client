using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-492 (owner, 2026-11-04): a card's dice rolled outside a battle stand as a plate of die faces at the region for about a
    // second, with the dice sound, before the losses they cause. The faces that hit wear the battle dice's hit face, a roll
    // with no region stands in the middle of the map, and nothing in it takes the pointer. Frames: ui492-*.
    public sealed class OpponentCardRollPresentationTests
    {
        private const string Region = "osgiliath";

        [UnityTest]
        public IEnumerator TheDiceStandAtTheRegionBeforeTheLossesTheyCause([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var ids = stage.Put(stage.Main, UnitKind.Regular, Region, 3);
            yield return stage.Ready();
            var sounds = new Recorded();
            stage.Presenters.Sounds.Target = sounds;
            var before = stage.Army(Region);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, ids.Take(2), "casualties");
            var choice = stage.Step(1);
            foreach (var id in ids.Take(2))
                choice.Facts.Figures.Add(stage.Change(id, Region, "casualties"));
            stage.Play(latest, "grey-havens", choice, Roll(stage, 2, new[] { 5, 6, 1 }, 5, Region));
            VisualElement plate = null;
            yield return stage.Until(() => (plate = Plate(stage)) != null);
            var started = Time.realtimeSinceStartup;
            Assert.AreEqual(before, stage.Army(Region), "The figures are still there: the roll comes first.");
            Assert.IsNull(stage.Board.Map.Q("observed-delta"));
            CollectionAssert.AreEqual(new[] { 5, 6, 1 }, Faces(plate).Select(face => face.Value));
            CollectionAssert.AreEqual(new[] { true, true, false }, Faces(plate).Select(face => face.ClassListContains("battle-pips-hit")),
                "The faces that hit say so.");
            Assert.AreEqual(PickingMode.Ignore, plate.pickingMode, "Nothing to click.");
            Assert.IsTrue(plate.Query<Button>().ToList().Count == 0);
            CollectionAssert.Contains(sounds.Played, SoundCue.DiceRoll, "With the dice sound.");
            Assert.IsTrue(stage.Presenters.Camera.SafeArea().Contains(stage.Board.Map.Camera.ToScreen(stage.Anchor(Region))), "The camera framed it first.");
            yield return new WaitForSecondsRealtime(.3f);
            var anchor = stage.Board.Map.Camera.ToScreen(stage.Anchor(Region));
            var bounds = new Rect(stage.Board.Map.WorldToLocal(plate.worldBound.position), plate.worldBound.size);
            Assert.AreEqual(anchor.x, bounds.center.x, 2f, "Centred on the region.");
            Assert.Less(bounds.yMax, anchor.y, "Above its anchor.");
            LayoutTests.Save(stage.Texture, "ui492-roll-" + viewer + ".png");
            yield return stage.Until(() => stage.Board.Map.Q("observed-delta") != null);
            Assert.AreEqual(CardRollPresenter.Seconds, Time.realtimeSinceStartup - started, .35f, "About a second.");
            Assert.AreEqual(before - 2, stage.Army(Region), "Then the losses.");
            yield return stage.Released();
            Assert.IsNull(Plate(stage));
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator ARollWithoutARegionStandsInTheMiddleOfTheMap()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = stage.Latest();
            var pan = stage.Board.Map.Camera.Pan;
            stage.Play(latest, null, Choice(stage), Roll(stage, 2, new[] { 6 }, 5));
            VisualElement plate = null;
            yield return stage.Until(() => (plate = Plate(stage)) != null);
            yield return new WaitForSecondsRealtime(.2f);
            Assert.AreEqual(stage.Board.Map.worldBound.center.x, plate.worldBound.center.x, 2f);
            Assert.AreEqual(pan, stage.Board.Map.Camera.Pan, "With no place to frame the camera stays.");
            LayoutTests.Save(stage.Texture, "ui492-roll-no-region.png");
            yield return stage.Released();
            Assert.IsNull(Plate(stage));
        }

        [UnityTest]
        public IEnumerator SkipAndSkipAnimationsEndOnTheLatestState([Values(false, true)] bool reduced)
        {
            using var stage = new PresentedBoardStage(1366, 1f, reduced);
            yield return stage.Ready();
            var latest = stage.Latest();
            stage.Play(latest, null, Choice(stage), Roll(stage, 2, new[] { 5, 2 }, 5, Region));
            yield return stage.Until(() => Plate(stage) != null);
            if (!reduced)
                PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            yield return new WaitForSecondsRealtime((MapObservedTrace.FadeOutMilliseconds + 100) / 1000f);
            Assert.IsNull(Plate(stage), "It leaves with the beat.");
            Assert.IsEmpty(stage.Lit);
        }

        // The opponent's decision whose effect rolls.
        private static ActionLogStep Choice(PresentedBoardStage stage)
        {
            var step = stage.Step(1);
            step.Command = CommandKind.ChooseEffect;
            return step;
        }

        // The automatic step of a card, as EffectContext.Record makes it.
        private static ActionLogStep Roll(PresentedBoardStage stage, int order, int[] rolls, int? hitTarget, string region = null)
        {
            var step = stage.Step(order);
            step.Actor = null;
            step.Automatic = true;
            step.Kind = "EventArmyRoll";
            step.ActionId = "presented-action-1";
            step.Facts.Rolls.AddRange(rolls);
            step.Facts.HitTarget = hitTarget;
            step.Facts.Hits = rolls.Count(roll => roll >= hitTarget);
            if (region != null)
                step.Facts.Targets.Add(region);
            return step;
        }

        private static VisualElement Plate(PresentedBoardStage stage) => stage.Board.Root.Q(CardRollPresenter.PlateName);

        private static List<DiePips> Faces(VisualElement plate) => plate.Query<DiePips>().ToList();

        private sealed class Recorded : IPresentationSounds
        {
            public List<SoundCue> Played { get; } = new List<SoundCue>();

            public void Play(SoundRequest request) => Played.Add(request.Cue);

            public void Away(PlayerView view) { }
        }
    }
}
