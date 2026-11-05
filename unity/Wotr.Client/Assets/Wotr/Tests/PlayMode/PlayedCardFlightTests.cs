using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-463 (owner, 2026-11-03: "let's add an animation when playing a card makes it go to Effects"), on UI-326's
    // mark-and-settle pattern: the player's own card that stays on the table leaves its place as its stand-in and shrinks
    // into the Effects tab, which then glows; a card the server kept, or one that left for anything else, is forgotten; a
    // quiet view plays nothing; under Skip animations only the tab glows. Frames: .artifacts/screenshots/ui463-*.png.
    public sealed class PlayedCardFlightTests
    {
        private const string Mithril = "base.fp.character.05";

        [UnityTest]
        public IEnumerator AStayingCardFliesFromItsPlaceIntoTheEffectsTabWhichThenGlows()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var flight = stage.Board.PlayedCards;
            var tab = stage.Board.TableEffects.Tab;
            var place = HandPlace(stage);
            yield return null;
            flight.Mark(Mithril, place, View(10, new[] { Mithril }));
            flight.Settle(View(10, new[] { Mithril }), false);
            Assert.IsFalse(flight.Flying, "Nothing left the hand yet.");
            flight.Settle(View(11, new string[0], Mithril), false);
            Assert.IsTrue(flight.Flying, "It left the hand for its own table.");
            var card = StandIn(stage);
            Assert.IsNotNull(card, "Its stand-in stands where it was played.");
            Assert.AreEqual(CardLibrary.Title(Mithril), card.Q<Label>().text);
            yield return null;
            var start = card.worldBound;
            yield return Until(() => !flight.Flying || card.resolvedStyle.scale.value.x < .7f);
            Assert.IsTrue(flight.Flying, "It is on its way.");
            var target = tab.worldBound.center;
            Assert.Less(Vector2.Distance(card.worldBound.center, target), Vector2.Distance(start.center, target),
                "It drifts toward the Effects tab.");
            Assert.Less(card.worldBound.width, start.width, "It shrinks.");
            Assert.AreEqual(PickingMode.Ignore, card.pickingMode, "It never takes the pointer.");
            LayoutTests.Save(stage.Texture, "ui463-flight.png");
            yield return Until(() => !flight.Flying);
            Assert.IsNull(StandIn(stage), "It has landed.");
            Assert.IsTrue(PresentationGlow.IsLit(tab), "The Effects tab glows.");
            yield return new WaitForSecondsRealtime(.2f);
            LayoutTests.Save(stage.Texture, "ui463-landed.png");
            yield return new WaitForSecondsRealtime((float)PlayedCardFlight.GlowSeconds + .3f);
            Assert.IsFalse(PresentationGlow.IsLit(tab), "Its glow goes out.");
        }

        [UnityTest]
        public IEnumerator ACardTheServerKeptOrThatDidNotStayIsForgottenAndAQuietViewPlaysNothing()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var flight = stage.Board.PlayedCards;
            var place = stage.Board.Phase;
            flight.Mark("kept", place, View(10, new[] { "kept" }));
            flight.Settle(View(11, new[] { "kept" }), false);
            Assert.AreEqual(0, flight.Waiting, "The card the server kept at a newer revision is forgotten.");
            flight.Mark(Mithril, place, View(11, new[] { Mithril }));
            var resolving = View(12, new string[0]);
            resolving.EffectTrail.Add(new EffectTrailView { SourceId = Mithril });
            flight.Settle(resolving, false);
            Assert.AreEqual(1, flight.Waiting, "Its resolution goes on: it may still stay on the table.");
            flight.Settle(View(13, new string[0]), false);
            Assert.AreEqual(0, flight.Waiting, "Resolved without staying: forgotten.");
            Assert.IsFalse(flight.Flying);
            Assert.IsFalse(PresentationGlow.IsLit(stage.Board.TableEffects.Tab), "Nothing went to the Effects tab.");
            flight.Mark(Mithril, place, View(13, new[] { Mithril }));
            var opponents = View(14, new string[0]);
            opponents.Players.Add(new PlayerSummary { Side = Side.Shadow, TableCards = new List<string> { Mithril } });
            flight.Settle(opponents, false);
            Assert.IsFalse(flight.Flying, "Only the player's own table counts.");
            flight.Mark("quiet", place, View(14, new[] { "quiet" }));
            flight.Settle(View(15, new string[0], "quiet"), true);
            Assert.AreEqual(0, flight.Waiting, "A quiet view forgets them all.");
            Assert.IsFalse(flight.Flying, "A quiet view plays nothing.");
        }

        [UnityTest]
        public IEnumerator UnderSkipAnimationsOnlyTheEffectsTabGlows()
        {
            using var stage = new PresentedBoardStage(1366, 1f, true);
            yield return stage.Ready();
            var flight = stage.Board.PlayedCards;
            flight.Mark(Mithril, stage.Board.Phase, View(10, new[] { Mithril }));
            flight.Settle(View(11, new string[0], Mithril), false);
            Assert.IsFalse(flight.Flying, "No flight.");
            Assert.IsNull(StandIn(stage), "No stand-in.");
            Assert.IsTrue(PresentationGlow.IsLit(stage.Board.TableEffects.Tab), "The tab glows at once.");
        }

        // A hand card's place: a card-sized slot at the bottom of the board.
        private static VisualElement HandPlace(PresentedBoardStage stage)
        {
            var place = new VisualElement { pickingMode = PickingMode.Ignore };
            place.style.position = Position.Absolute;
            place.style.left = 600;
            place.style.bottom = 40;
            place.style.width = 96;
            place.style.height = 134;
            stage.Board.Root.Add(place);
            return place;
        }

        private static VisualElement StandIn(PresentedBoardStage stage) => stage.Board.PlayedCards.Root.Q(PlayedCardFlight.StandInName);

        private static IEnumerator Until(Func<bool> condition)
        {
            var deadline = Time.realtimeSinceStartup + 5;
            while (!condition() && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(condition(), "The flight did not reach the expected state.");
        }

        private static PlayerView View(long revision, IEnumerable<string> hand, params string[] table)
        {
            var view = new PlayerView { Revision = revision, You = Side.FreePeoples, Hand = new List<string>(hand) };
            view.Players.Add(new PlayerSummary { Side = Side.FreePeoples, TableCards = new List<string>(table) });
            return view;
        }
    }
}
