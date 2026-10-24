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
    // UI-308 on a mounted board: the Shadow plays Orcs Multiplying Again (3 Sauron Regulars in Dol Guldur and 3 in Mount
    // Gundabad). The Free Peoples see the actual card read-only for at least 3 s, away from its first effect, then the card
    // docks into the console trail and its two regions follow under that one source; a click holds the reading, Skip ends
    // it, a table card enters and leaves at the Effects tab. Frames go to .artifacts/screenshots (ui308-*).
    public sealed class OpponentCardPresentationTests
    {
        private const string Orcs = "base.sh.strategy.20";
        private const string Crebain = "base.sh.character.16";

        [UnityTest]
        public IEnumerator TheCardIsReadForThreeSecondsAwayFromItsEffectThenDocksAndItsRegionsFollow()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = Play(stage, "grey-havens");
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Orcs && cards.Root.style.display.value == DisplayStyle.Flex);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsFalse(cards.Pinned, "The reveal is the presentation's, not a pinned card.");
            Assert.AreEqual(DisplayStyle.None, cards.Root.Q(className: "card-toolbar").resolvedStyle.display, "Read-only: no Play bar.");
            Assert.IsTrue(cards.Root.ClassListContains(CardRevealPresenter.PresentedClass));
            Assert.IsTrue(cards.Root.Q("card-event-section").ClassListContains("focused-effect"), "The Event half is the used one.");
            var console = stage.Board.Console.Root.parent.worldBound;
            Assert.LessOrEqual(cards.Root.worldBound.yMax, console.yMin + 1, "The card stands clear of the console.");
            var dolGuldur = stage.Board.Map.LocalToWorld(stage.Board.Map.Camera.ToScreen(stage.Anchor("dol-guldur")));
            Assert.IsFalse(cards.Root.worldBound.Contains(dolGuldur), "The card does not cover its first effect.");
            LayoutTests.Save(stage.Texture, "ui308-reveal.png");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(Orcs, cards.CardId, "The reveal holds at least 3 s.");
            var chip = stage.Board.Console.Status.Trail.Q<Button>("console-presented-source");
            yield return stage.Until(() => (chip = stage.Board.Console.Status.Trail.Q<Button>("console-presented-source")) != null);
            Assert.AreEqual("✓ Orcs Multiplying Again", chip.text, "The card docks into the console trail.");
            Assert.AreEqual(DisplayStyle.None, cards.Root.style.display.value);
            yield return stage.Until(() => stage.Army("dol-guldur") == 3);
            Assert.AreEqual(1, stage.Board.Root.Query(CostEmphasis.MarkName).ToList().Count, "One source, one cost.");
            Assert.AreEqual(0, stage.Army("mount-gundabad"));
            yield return stage.Until(() => stage.Army("mount-gundabad") == 3);
            Assert.IsNotNull(stage.Board.Console.Status.Trail.Q("console-presented-source"), "The source stays through every effect.");
            yield return stage.Released();
            stage.AssertFinal(latest);
            Assert.IsNull(stage.Board.Console.Status.Trail.Q("console-presented-source"), "The compact source leaves with the presentation.");
            Assert.AreEqual(DisplayStyle.None, cards.Root.style.display.value);
        }

        [UnityTest]
        public IEnumerator AClickOnTheRevealedCardHoldsThePresentationUntilItIsClosed()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = Play(stage, null);
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Orcs && cards.Root.style.display.value == DisplayStyle.Flex);
            yield return new WaitForSecondsRealtime(.3f);
            PointerGestureEvents.ClickAt(cards.Root.Q("card-event-section"));
            Assert.IsTrue(cards.Pinned, "A click on the shown card pins it.");
            yield return new WaitForSecondsRealtime(4f);
            Assert.AreEqual(PresentationCueKind.Card, stage.Playback.Current?.Kind, "The presentation holds while the card is read.");
            Assert.AreEqual(0, stage.Army("dol-guldur"));
            LayoutTests.Save(stage.Texture, "ui308-reading.png");
            PointerGestureEvents.ClickAt(cards.Root.Q<Button>("close-card"));
            yield return stage.Until(() => stage.Army("dol-guldur") == 3);
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator TheCardStaysReadableAt1366By768At125PercentWithoutShrinking()
        {
            using var stage = new PresentedBoardStage(1366, 1.25f, false);
            yield return stage.Ready();
            Play(stage, "grey-havens");
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Orcs && cards.Root.style.display.value == DisplayStyle.Flex);
            yield return new WaitForSecondsRealtime(.4f);
            foreach (var rules in cards.Root.Query<Label>(className: "card-rules").ToList().Where(label => label.resolvedStyle.display == DisplayStyle.Flex))
            {
                Assert.GreaterOrEqual(rules.resolvedStyle.fontSize, 14f, "Card text is never shrunk.");
                Assert.IsTrue(cards.Root.worldBound.Contains(rules.worldBound.min) && cards.Root.worldBound.yMax + .5f >= rules.worldBound.yMax,
                    "The rules are not clipped.");
            }
            Assert.IsNull(cards.Root.Q<ScrollView>(), "Nothing scrolls.");
            Assert.IsTrue(stage.Board.Map.worldBound.Contains(cards.Root.worldBound.min) && stage.Board.Map.worldBound.Contains(cards.Root.worldBound.max));
            LayoutTests.Save(stage.Texture, "ui308-reveal-1366-1.25.png");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator SkipDuringTheRevealClosesTheCardAndEndsOnTheLatestState()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = Play(stage, null);
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Orcs && cards.Root.style.display.value == DisplayStyle.Flex);
            var before = stage.Buttons();
            PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            stage.AssertFinal(latest);
            Assert.AreEqual(DisplayStyle.None, cards.Root.style.display.value, "Skip never plays or keeps the card.");
            Assert.IsNull(stage.Board.Console.Status.Trail.Q("console-presented-source"));
            Assert.AreEqual(before - 1, stage.Buttons(), "Only the Skip target leaves; nothing else was added.");
            yield return stage.NothingFlies();
        }

        [UnityTest]
        public IEnumerator ATableCardEntersAndLeavesAtTheEffectsTabWithoutANewReveal()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = stage.Latest();
            var entered = stage.Step(1);
            entered.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Event, Id = Crebain, Deck = "character" });
            entered.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.TableEntered, Id = Crebain, Deck = "character" });
            stage.Play(latest, null, entered);
            yield return stage.Until(() => stage.Board.Root.Q("presentation-table-ring") != null);
            var ring = stage.Board.Root.Q("presentation-table-ring").worldBound;
            Assert.IsTrue(ring.Overlaps(stage.Board.TableEffects.Tab.worldBound), "The table card settles at the Effects tab.");
            yield return stage.Released();
            var removal = stage.Next(latest);
            var removed = stage.Step(1, 2);
            removed.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.TableRemoved, Id = Crebain, Deck = "character" });
            stage.Play(removal, null, removed);
            yield return stage.Until(() => stage.Board.Root.Q("presentation-table-ring") != null);
            Assert.AreNotEqual(Crebain, stage.Board.Cards.CardId, "Its removal is a trace, not a new reveal.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator TheCardAndItsEffectsAreRecordedAsAMutedClipWithAReadingClick()
        {
            using var stage = new PresentedBoardStage(1366, 1.25f, false);
            yield return stage.Ready();
            var latest = Play(stage, "grey-havens");
            var cards = stage.Board.Cards;
            var frame = 0;
            var read = false;
            while (stage.Playback.Holding && frame < 90)
            {
                LayoutTests.Save(stage.Texture, "ui308-clip-" + frame.ToString("00") + ".png");
                if (!read && cards.CardId == Orcs && cards.Root.style.display.value == DisplayStyle.Flex && frame >= 5)
                {
                    PointerGestureEvents.ClickAt(cards.Root.Q("card-event-section"));
                    read = true;
                }
                if (read && cards.Pinned && frame >= 15)
                    PointerGestureEvents.ClickAt(cards.Root.Q<Button>("close-card"));
                frame++;
                yield return new WaitForSecondsRealtime(.1f);
            }
            Assert.IsTrue(read);
            Assert.IsFalse(stage.Playback.Holding);
            stage.AssertFinal(latest);
        }

        // The Shadow's Event die pays Orcs Multiplying Again; its recruits land in Dol Guldur, then in Mount Gundabad.
        internal static PlayerView Play(PresentedBoardStage stage, string focus)
        {
            var reserve = stage.Reserve("sauron", UnitKind.Regular, 6);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve.Take(3), "dol-guldur");
            PresentedBoardStage.Place(latest, reserve.Skip(3), "mount-gundabad");
            var played = stage.Step(1);
            played.Costs.Add(new ActionCost { Kind = ActionCostKind.Die, Id = PresentedBoardStage.Die, Owner = Side.Shadow, Face = DieFace.Muster, Use = DieFace.Muster });
            played.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Event, Id = Orcs, Deck = "strategy" });
            stage.Recruits(played, reserve.Take(3), "dol-guldur");
            stage.Recruits(played, reserve.Skip(3), "mount-gundabad");
            stage.Play(latest, focus, played);
            return latest;
        }
    }
}
