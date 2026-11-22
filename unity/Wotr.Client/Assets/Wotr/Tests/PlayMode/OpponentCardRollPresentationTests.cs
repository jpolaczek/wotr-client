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
    // UI-492 (owner, 2026-11-04) and UI-541 (owner, 2026-11-17): a card's dice rolled outside a battle open a window of their
    // own above the region: the card's name and what its dice count, then the dice (the battle's faces, the hit face for a
    // hit) with the dice sound, then the result; it stays while the losses they cause play and leaves at the release. A roll
    // with no region stands in the middle of the safe area; nothing in it takes the pointer. The player's own Athelas plays
    // its dice and then the Ring's move on the corruption track. Frames: ui541-*.
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
            VisualElement window = null;
            yield return stage.Until(() => (window = Window(stage)) != null);
            var started = Time.realtimeSinceStartup;
            Assert.AreEqual(before, stage.Army(Region), "The figures are still there: the roll comes first.");
            Assert.IsNull(stage.Board.Map.Q("observed-delta"));
            Assert.AreEqual("Hits on 5+", window.Q<Label>(CardRollPresenter.RuleName).text);
            Assert.IsTrue(Waiting(window, CardRollPresenter.DiceName), "The window opens before its dice.");
            CollectionAssert.AreEqual(new[] { 5, 6, 1 }, Faces(window).Select(face => face.Value));
            CollectionAssert.AreEqual(new[] { true, true, false }, Faces(window).Select(face => face.ClassListContains("battle-pips-hit")),
                "The faces that hit say so.");
            Assert.AreEqual(PickingMode.Ignore, window.pickingMode, "Nothing to click.");
            Assert.AreEqual(0, window.Query<Button>().ToList().Count, "A presentation's window has no button.");
            Assert.IsTrue(stage.Presenters.Camera.SafeArea().Contains(stage.Board.Map.Camera.ToScreen(stage.Anchor(Region))), "The camera framed it first.");
            yield return stage.Until(() => !Waiting(window, CardRollPresenter.DiceName));
            CollectionAssert.Contains(sounds.Played, SoundCue.DiceRoll, "The dice land with their sound.");
            Assert.IsTrue(Waiting(window, CardRollPresenter.ResultName), "The result follows the dice.");
            yield return stage.Until(() => !Waiting(window, CardRollPresenter.ResultName));
            Assert.AreEqual("2 hits", window.Q<Label>(CardRollPresenter.ResultName).text);
            var anchor = stage.Board.Map.Camera.ToScreen(stage.Anchor(Region));
            var bounds = new Rect(stage.Board.Map.WorldToLocal(window.worldBound.position), window.worldBound.size);
            Assert.AreEqual(anchor.x, bounds.center.x, 2f, "Centred on the region.");
            Assert.Less(bounds.yMax, anchor.y, "Above its anchor.");
            Assert.IsTrue(stage.Presenters.Camera.SafeArea().Contains(bounds.min) && stage.Presenters.Camera.SafeArea().Contains(bounds.max),
                "Inside the safe area.");
            LayoutTests.Save(stage.Texture, "ui541-roll-" + viewer + ".png");
            yield return stage.Until(() => stage.Board.Map.Q("observed-delta") != null);
            Assert.AreEqual(CardRollPresenter.Seconds, Time.realtimeSinceStartup - started, .4f, "The window's beats, then the losses.");
            Assert.AreEqual(before - 2, stage.Army(Region), "Then the losses.");
            Assert.IsNotNull(Window(stage), "The window stays while its losses play.");
            yield return stage.Released();
            yield return new WaitForSecondsRealtime((MapObservedTrace.FadeOutMilliseconds + 100) / 1000f);
            Assert.IsNull(Window(stage));
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
            VisualElement window = null;
            yield return stage.Until(() => (window = Window(stage)) != null);
            yield return new WaitForSecondsRealtime(.2f);
            var safe = stage.Presenters.Camera.SafeArea();
            var bounds = new Rect(stage.Board.Map.WorldToLocal(window.worldBound.position), window.worldBound.size);
            Assert.AreEqual(safe.center.x, bounds.center.x, 2f, "In the middle of the safe area.");
            Assert.AreEqual(safe.center.y, bounds.center.y, 2f);
            Assert.AreEqual(pan, stage.Board.Map.Camera.Pan, "With no place to frame the camera stays.");
            yield return stage.Released();
            yield return new WaitForSecondsRealtime((MapObservedTrace.FadeOutMilliseconds + 100) / 1000f);
            Assert.IsNull(Window(stage));
        }

        [UnityTest]
        public IEnumerator SkipAndSkipAnimationsEndOnTheLatestState([Values(false, true)] bool reduced)
        {
            using var stage = new PresentedBoardStage(1366, 1f, reduced);
            yield return stage.Ready();
            var latest = stage.Latest();
            stage.Play(latest, null, Choice(stage), Roll(stage, 2, new[] { 5, 2 }, 5, Region));
            yield return stage.Until(() => Window(stage) != null);
            if (!reduced)
                PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            yield return new WaitForSecondsRealtime((MapObservedTrace.FadeOutMilliseconds + 100) / 1000f);
            Assert.IsNull(Window(stage), "It leaves with the presentation.");
            Assert.IsEmpty(stage.Lit);
        }

        // UI-541 (owner, 2026-11-17): the player's own Athelas: no reveal of their own card, the window names it and its dice,
        // and the Ring moves down the corruption track after them, while the window stays.
        [UnityTest]
        public IEnumerator OwnAthelasShowsItsDiceAndThenTheRingMovesOnTheCorruptionTrack()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.FreePeoples);
            const int corrupted = 4;
            const int healed = 2;
            stage.Before.Fellowship.Corruption = corrupted;
            yield return stage.Ready();
            var latest = stage.Latest();
            latest.Fellowship.Corruption = healed;
            var played = stage.Step(1);
            played.Actor = Side.FreePeoples;
            played.Kind = "EventPlayed";
            played.Facts.FellowshipBefore = Outcome(stage.Before.Fellowship, corrupted);
            played.Facts.FellowshipAfter = Outcome(stage.Before.Fellowship, healed);
            var roll = Roll(stage, 2, new[] { 3, 5, 1 }, 3);
            roll.Kind = "HealingRoll";
            roll.ActionId = played.ActionId;
            roll.Facts.SourceId = "base.fp.character.09";
            var corruption = stage.Board.Fellowship.Status.Q<Label>("fellowship-status-corruption");
            var shown = corruption.text;
            stage.Play(latest, null, played, roll);
            VisualElement window = null;
            yield return stage.Until(() => (window = Window(stage)) != null);
            Assert.AreEqual("Athelas", window.Q<Label>(CardRollPresenter.TitleName).text);
            Assert.AreEqual("Each 3+ heals one Corruption", window.Q<Label>(CardRollPresenter.RuleName).text);
            Assert.AreEqual(shown, corruption.text, "The Ring waits for the dice.");
            yield return stage.Until(() => !Waiting(window, CardRollPresenter.ResultName));
            Assert.AreEqual("2 successes", window.Q<Label>(CardRollPresenter.ResultName).text);
            LayoutTests.Save(stage.Texture, "ui541-own-athelas.png");
            yield return stage.Until(() => corruption.text != shown);
            Assert.IsNotNull(Window(stage), "The window stays while the Ring moves.");
            LayoutTests.Save(stage.Texture, "ui541-own-athelas-ring.png");
            yield return stage.Released();
            yield return new WaitForSecondsRealtime((MapObservedTrace.FadeOutMilliseconds + 100) / 1000f);
            Assert.IsNull(Window(stage));
            stage.AssertFinal(latest);
        }

        // UI-565 (owner, 2026-11-22: "an extra UI that shows what is drawn, as in Challenge of the King; the user must see the
        // draw clearly"): the card's three Hunt tiles turn up one by one in its window, each with the card sound, for the side
        // that played it and for the opponent; then the result, and the two Eyes that leave the game dim. Frames: ui565-*.
        [UnityTest]
        public IEnumerator ChallengeOfTheKingsTilesTurnUpOneByOne([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            yield return stage.Ready();
            var sounds = new Recorded();
            stage.Presenters.Sounds.Target = sounds;
            var latest = stage.Latest();
            var played = stage.Step(1);
            played.Actor = Side.FreePeoples;
            played.Kind = "EventPlayed";
            var draw = Automatic(stage, played, "ChallengeDraw", "base.fp.character.14");
            draw.Facts.Targets.AddRange(new[] { "base.hunt.standard.eye.1.1", "base.hunt.standard.3.0.1", "base.hunt.standard.eye.1.2" });
            stage.Play(latest, null, played, draw);
            VisualElement window = null;
            yield return stage.Until(() => (window = Window(stage)) != null);
            Assert.AreEqual("Challenge of the King", window.Q<Label>(CardRollPresenter.TitleName).text);
            Assert.AreEqual(CardRollPresenter.DrawRule("ChallengeDraw"), window.Q<Label>(CardRollPresenter.RuleName).text);
            var items = window.Query(className: CardRollPresenter.ItemClass).ToList();
            Assert.AreEqual(3, items.Count);
            Assert.AreEqual(3, window.Query(className: HuntTileFace.ClassName).ToList().Count, "Three printed tiles.");
            Assert.IsTrue(items.All(Hidden), "The window opens before its tiles.");
            yield return stage.Until(() => !Hidden(items[0]));
            Assert.IsTrue(Hidden(items[2]), "The tiles turn up one by one.");
            LayoutTests.Save(stage.Texture, "ui565-challenge-first-" + viewer + ".png");
            yield return stage.Until(() => !Waiting(window, CardRollPresenter.ResultName));
            Assert.AreEqual("2 Eyes leave the game", window.Q<Label>(CardRollPresenter.ResultName).text);
            CollectionAssert.AreEqual(new[] { true, false, true }, items.Select(item => item.ClassListContains(CardRollPresenter.OutClass)),
                "The Eyes that leave the game dim.");
            Assert.GreaterOrEqual(sounds.Played.Count(cue => cue == SoundCue.CardMove), 3, "Each tile with its sound.");
            Assert.AreEqual(PickingMode.Ignore, window.pickingMode, "Nothing to click.");
            Assert.AreEqual(0, window.Query<Button>().ToList().Count);
            var safe = stage.Presenters.Camera.SafeArea();
            var bounds = new Rect(stage.Board.Map.WorldToLocal(window.worldBound.position), window.worldBound.size);
            Assert.AreEqual(safe.center.x, bounds.center.x, 2f, "In the middle of the safe area.");
            Assert.IsTrue(safe.Contains(bounds.min) && safe.Contains(bounds.max), "Inside the safe area.");
            LayoutTests.Save(stage.Texture, "ui565-challenge-" + viewer + ".png");
            yield return stage.Released();
            yield return new WaitForSecondsRealtime((MapObservedTrace.FadeOutMilliseconds + 100) / 1000f);
            Assert.IsNull(Window(stage));
        }

        // UI-565: Lure of the Ring's random Companion: a light runs over the Fellowship's portraits and stays on the one picked,
        // the others dim, and the result names him; three Eyes of Challenge of the King name the King they eliminate.
        [UnityTest]
        public IEnumerator LureOfTheRingsLightStopsOnTheCompanionPicked([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var companions = new[] { "base.character.gimli", "base.character.legolas", "base.character.boromir" };
            // The pick changes nothing in the Fellowship: the view before it and after it list the same Companions.
            stage.Before.Fellowship.CompanionIdentities.AddRange(companions);
            if (viewer == Side.FreePeoples)
                stage.Before.OwnCompanions.AddRange(companions.Select(id =>
                    new CharacterView { Id = id, DefinitionId = id, Side = Side.FreePeoples, Location = "fellowship" }));
            yield return stage.Ready();
            var latest = stage.Latest();
            var played = stage.Step(1);
            played.Actor = Side.Shadow;
            played.Kind = "EventPlayed";
            var pick = Automatic(stage, played, "CompanionSelected", "base.sh.character.13");
            pick.Facts.Targets.Add("base.character.legolas");
            stage.Play(latest, null, played, pick);
            VisualElement window = null;
            yield return stage.Until(() => (window = Window(stage)) != null);
            Assert.AreEqual("Lure of the Ring", window.Q<Label>(CardRollPresenter.TitleName).text);
            var items = window.Query(className: CardRollPresenter.ItemClass).ToList();
            CollectionAssert.AreEqual(companions.Select(id => CardRollPresenter.ItemClass + "-" + id), items.Select(item => item.name));
            var lit = new HashSet<string>();
            while (Waiting(window, CardRollPresenter.ResultName))
            {
                foreach (var item in items.Where(item => item.ClassListContains(CardRollPresenter.LitClass)))
                    lit.Add(item.name);
                yield return null;
            }
            Assert.Greater(lit.Count, 1, "The light runs over the Fellowship.");
            Assert.AreEqual("Legolas is picked", window.Q<Label>(CardRollPresenter.ResultName).text);
            CollectionAssert.AreEqual(new[] { false, true, false }, items.Select(item => item.ClassListContains(CardRollPresenter.LitClass)));
            CollectionAssert.AreEqual(new[] { true, false, true }, items.Select(item => item.ClassListContains(CardRollPresenter.OutClass)));
            LayoutTests.Save(stage.Texture, "ui565-lure-" + viewer + ".png");
            yield return stage.Released();
            Assert.AreEqual("Three Eyes · Aragorn is eliminated", CardRollPresenter.TilesResult(3, "Aragorn"));
            Assert.AreEqual("No Eye · the tiles return to the pool", CardRollPresenter.TilesResult(0, ""));
            Assert.AreEqual("1 Eye leaves the game", CardRollPresenter.TilesResult(1, ""));
        }

        // A card's automatic step of the given action, as EffectContext.Record makes it.
        private static ActionLogStep Automatic(PresentedBoardStage stage, ActionLogStep played, string kind, string card)
        {
            var step = stage.Step(2);
            step.Actor = null;
            step.Automatic = true;
            step.Kind = kind;
            step.ActionId = played.ActionId;
            step.Context.DefinitionId = card;
            return step;
        }

        private static bool Hidden(VisualElement item) => item.ClassListContains(CardRollPresenter.WindowName + "-waiting");

        private static HuntOutcomeView Outcome(FellowshipView fellowship, int corruption) => new HuntOutcomeView
        {
            RegionId = fellowship.RegionId, Progress = fellowship.Progress, MordorStep = fellowship.MordorStep,
            Corruption = corruption, Hidden = fellowship.Hidden, GuideId = fellowship.Guide
        };

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

        private static VisualElement Window(PresentedBoardStage stage) => stage.Board.Root.Q(CardRollPresenter.WindowName);

        private static bool Waiting(VisualElement window, string part) =>
            window.Q(part).ClassListContains(CardRollPresenter.WindowName + "-waiting");

        private static List<DiePips> Faces(VisualElement window) => window.Query<DiePips>().ToList();

        private sealed class Recorded : IPresentationSounds
        {
            public List<SoundCue> Played { get; } = new List<SoundCue>();

            public void Play(SoundRequest request) => Played.Add(request.Cue);

            public void Away(PlayerView view) { }
        }
    }
}
