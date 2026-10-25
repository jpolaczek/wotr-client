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
            var middle = stage.Board.Map.LocalToWorld(stage.Presenters.Camera.SafeArea().center);
            var latest = Play(stage, "grey-havens");
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Orcs && cards.Root.style.display.value == DisplayStyle.Flex);
            yield return stage.Until(() => Standing(cards));
            Assert.IsFalse(cards.Pinned, "The reveal is the presentation's, not a pinned card.");
            Assert.AreEqual(DisplayStyle.None, cards.Root.Q(className: "card-toolbar").resolvedStyle.display, "Read-only: no Play bar.");
            Assert.IsTrue(cards.Root.ClassListContains(CardRevealPresenter.PresentedClass));
            Assert.IsTrue(cards.Root.Q("card-event-section").ClassListContains("focused-effect"), "The Event half is the used one.");
            var console = stage.Board.Console.Root.parent.worldBound;
            Assert.LessOrEqual(cards.Root.worldBound.yMax, console.yMin + 1, "The card stands clear of the console.");
            // UI-324 (owner): the card stands in the middle of the safe area (the old rule kept it away from its effect).
            Assert.AreEqual(middle.x, cards.Root.worldBound.center.x, 2f, "Centred across the safe area.");
            Assert.AreEqual(middle.y, cards.Root.worldBound.center.y, 2f, "Centred down the safe area.");
            LayoutTests.Save(stage.Texture, "ui308-reveal.png");
            yield return new WaitForSecondsRealtime(1.5f);
            Assert.AreEqual(Orcs, cards.CardId, "The reveal holds at least 3 s.");
            var chip = stage.Board.Console.Status.Trail.Q<Button>("console-presented-source");
            yield return stage.Until(() => (chip = stage.Board.Console.Status.Trail.Q<Button>("console-presented-source")) != null);
            Assert.AreEqual("✓ Orcs Multiplying Again", chip.text, "The card docks into the console trail.");
            // UI-324: its stand-in lands on the source, which shows once it is there.
            yield return stage.Until(() => cards.Root.style.display.value == DisplayStyle.None && chip.resolvedStyle.opacity > .99f);
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

        // UI-316 (owner review): the played card grows out of the opponent's hand count into its reading place.
        [UnityTest]
        public IEnumerator TheCardGrowsOutOfTheOpponentsHandCountIntoItsPlace()
        {
            using var stage = new PresentedBoardStage(1366, 1.25f, false);
            yield return stage.Ready();
            var latest = Play(stage, "grey-havens");
            var cards = stage.Board.Cards;
            var hand = stage.Board.TopBar.Opponent.Root.Q("opponent-cards").worldBound.center;
            yield return stage.Until(() => cards.CardId == Orcs && cards.Root.style.scale.keyword != StyleKeyword.Null);
            yield return null;
            var early = cards.Root.worldBound;
            LayoutTests.Save(stage.Texture, "ui316-arriving.png");
            yield return stage.Until(() => Standing(cards));
            var place = cards.Root.worldBound;
            Assert.Less(early.width, place.width, "It starts small.");
            Assert.Less(Vector2.Distance(early.center, hand), Vector2.Distance(place.center, hand), "It comes out of the hand count.");
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        // UI-316: after its reading time the Event shrinks into the console trail, where its docked source then glows.
        [UnityTest]
        public IEnumerator TheReadEventShrinksIntoTheConsoleTrailWhereItsSourceGlows()
        {
            using var stage = new PresentedBoardStage(1920, 1f, false);
            yield return stage.Ready();
            var latest = Play(stage, "grey-havens");
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Orcs && Standing(cards));
            var place = cards.Root.worldBound;
            // UI-324: a stand-in above the HUD lands on the docked source, which shows once it arrives.
            yield return stage.Until(() => StandIn(stage) is { } card && card.resolvedStyle.scale.value.x < .8f &&
                cards.Root.style.display.value == DisplayStyle.None);
            var chip = stage.Board.Console.Status.Trail.Q<Button>("console-presented-source");
            var late = StandIn(stage).worldBound;
            Assert.Less(late.width, place.width, "It shrinks.");
            Assert.Less(Vector2.Distance(late.center, chip.worldBound.center), Vector2.Distance(place.center, chip.worldBound.center),
                "onto its source in the console trail.");
            Assert.AreEqual(0f, chip.resolvedStyle.opacity, "The source waits for it.");
            Assert.AreEqual(DisplayStyle.None, cards.Root.style.display.value, "The card itself has handed over.");
            LayoutTests.Save(stage.Texture, "ui316-leaving.png");
            yield return stage.Until(() => PresentationGlow.IsLit(chip));
            Assert.IsNull(StandIn(stage), "The stand-in has landed.");
            Assert.AreEqual(1f, chip.resolvedStyle.opacity, "The source shows.");
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        // UI-316: a card that stays on the table drifts to the Effects tab after its reading time, and the tab lights up.
        [UnityTest]
        public IEnumerator ACardThatStaysOnTheTableDriftsToTheEffectsTab()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = stage.Latest();
            var entered = stage.Step(1);
            entered.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.Event, Id = Crebain, Deck = "character" });
            entered.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.TableEntered, Id = Crebain, Deck = "character" });
            stage.Play(latest, null, entered);
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Crebain && Standing(cards));
            var place = cards.Root.worldBound;
            var tab = stage.Board.TableEffects.Tab.worldBound;
            yield return stage.Until(() => StandIn(stage) is { } card && card.resolvedStyle.scale.value.x < .8f);
            Assert.Less(Vector2.Distance(StandIn(stage).worldBound.center, tab.center), Vector2.Distance(place.center, tab.center),
                "It drifts toward the Effects tab.");
            LayoutTests.Save(stage.Texture, "ui316-to-effects.png");
            yield return stage.Until(() => PresentationGlow.IsLit(stage.Board.TableEffects.Tab));
            yield return stage.Released();
        }

        // UI-321 (owner rule): under Skip animations the card stands at its place at once — no flight, no growth, no fade.
        [UnityTest]
        public IEnumerator UnderSkipAnimationsTheCardStandsAtOnce()
        {
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            var latest = Play(stage, null);
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Orcs && cards.Root.style.display.value == DisplayStyle.Flex);
            for (var frame = 0; frame < 10 && cards.Root.style.display.value == DisplayStyle.Flex; frame++)
            {
                Assert.AreEqual(StyleKeyword.Null, cards.Root.style.opacity.keyword, "No fade.");
                Assert.AreEqual(StyleKeyword.Null, cards.Root.style.translate.keyword, "No travel.");
                Assert.AreEqual(StyleKeyword.Null, cards.Root.style.scale.keyword, "No growth.");
                Assert.AreEqual(1f, cards.Root.resolvedStyle.opacity, "Shown at once.");
                yield return null;
            }
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator AClickOnTheRevealedCardHoldsThePresentationUntilItIsClosed()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var latest = Play(stage, null);
            var cards = stage.Board.Cards;
            yield return stage.Until(() => cards.CardId == Orcs && Standing(cards));
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
            yield return stage.Until(() => PresentationGlow.IsLit(stage.Board.TableEffects.Tab));
            Assert.IsTrue(PresentationGlow.IsLit(stage.Board.TableEffects.Tab), "The table card settles at the Effects tab.");
            yield return stage.Released();
            var removal = stage.Next(latest);
            var removed = stage.Step(1, 2);
            removed.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.TableRemoved, Id = Crebain, Deck = "character" });
            stage.Play(removal, null, removed);
            yield return stage.Until(() => stage.Lit.Count > 0);
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
        // UI-316: the card stands at its place — no flight is set on it.
        private static VisualElement StandIn(PresentedBoardStage stage) => stage.Board.Root.Q(CardRevealPresenter.StandInName);

        internal static bool Standing(CardInspector cards) => cards.Root.style.display.value == DisplayStyle.Flex &&
            cards.Root.style.opacity.keyword == StyleKeyword.Null && cards.Root.style.scale.keyword == StyleKeyword.Null;

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
