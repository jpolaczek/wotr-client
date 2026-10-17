using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;
using Object = UnityEngine.Object;

namespace Wotr.PlayTests
{
    public class CardInteractionTests
    {
        [UnityTest]
        public IEnumerator PressingAReactionKeepsItsSingleCommitWhileHoverAndFocusStaySilent()
        {
            const string cardId = "base.fp.character.05";
            var view = new BoardFixture().View;
            view.Decision = new DecisionView { Id = "reaction", Kind = DecisionKind.Reaction, Window = "fixture-window" };
            view.Options.Clear();
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.RespondEffect,
                EligibleIds = { "fixture-reply" },
                EffectChoices = { new EffectChoiceView { Id = "fixture-reply", SourceId = cardId } }
            });
            var backend = new FrozenBackend(view);
            using (var client = new MountedMatchClient("http://localhost", view.MatchId, backend))
            {
                client.Apply(view);
                client.SetOnline();
                yield return Settle();
                // UI-214: a reaction outside the Hunt and battles answers in the console.
                var button = MatchTestRoom.TrayOrConsole(client, "effect-choice-fixture-reply");
                button.Focus();
                yield return Settle();
                Assert.IsEmpty(client.Board.Cards.CardId, "Focus does not open rules before the reaction click.");
                client.Board.Root.Focus();
                client.Board.Cards.Close();
                PointerGestureEvents.Press(button);
                yield return new WaitForSeconds(.3f);
                Assert.IsEmpty(client.Board.Cards.CardId, "Pointer focus must not open an overlay during a press.");
                var picked = button.panel.Pick(button.worldBound.center);
                Assert.IsTrue(picked == button || button.Contains(picked));
                PointerGestureEvents.Release(button);
                yield return Commit(client, backend, 1);
                Assert.AreEqual("fixture-reply", backend.Last.Payload.ReactionId);
            }
        }

        [UnityTest]
        public IEnumerator AllCardTextsAndExcessHandFitWithoutScrollingAtBothSizesAndScales()
        {
            var view = new BoardFixture().View;
            view.Hand = new[] { "base.fp.character.19", "base.fp.character.22", "base.fp.character.04",
                "base.fp.character.18", "base.fp.character.17", "base.fp.strategy.17", "base.fp.strategy.15",
                "base.fp.strategy.08", "base.fp.character.12" }.ToList();
            view.Decision.Kind = DecisionKind.DiscardCards;
            view.Options.Clear();
            view.Options.Add(new ActionOption { Kind = CommandKind.DiscardCards, Minimum = 1, Maximum = 3, EligibleIds = view.Hand.ToList() });
            using (var client = new MountedMatchClient("http://localhost", view.MatchId, new FrozenBackend(view)))
            {
                client.Apply(view);
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                    {
                        var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                        target.Create();
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        yield return Settle();
                        client.Board.HandDrawer.Open();
                        yield return Settle();
                        var first = client.Board.Hand.Q<Button>(view.Hand[0]);
                        var before = first.worldBound;
                        client.Board.Root.Focus();
                        client.Board.Cards.Close();
                        first.Focus();
                        yield return Settle();
                        Assert.IsEmpty(client.Board.Cards.CardId);
                        Assert.AreEqual(view.Hand[0], client.Board.ExpandedCard, "UI-91: focus unrolls the card for reading.");
                        using (var submit = NavigationSubmitEvent.GetPooled())
                            first.SendEvent(submit);
                        yield return Settle();
                        Assert.IsEmpty(client.Board.Cards.CardId, "UI-91: Enter on a hand card does not open the separate preview.");
                        Assert.AreEqual(before, first.worldBound, "Explicit inspection must not reflow the hand.");
                        client.Board.Root.Focus();
                        yield return Settle();
                        foreach (var card in CardLibrary.Cards.Values)
                        {
                            client.Board.Cards.Show(card.Id, true);
                            yield return Settle();
                            try
                            {
                                CheckText(client.Board);
                                foreach (var button in client.Board.Hand.Query<Button>(className: "hand-card").ToList())
                                {
                                    var picked = button.panel.Pick(button.worldBound.center);
                                    Assert.IsTrue(picked == button || button.Contains(picked),
                                        "Preview " + card.Id + " must not cover hand card " + button.name);
                                }
                            }
                            catch (AssertionException)
                            {
                                LayoutTests.Save(target, $"ui29-clipped-{card.Id}-{width}-{scale:0.00}.png");
                                throw;
                            }
                        }
                        client.Board.Cards.Show(view.Hand[0], true);
                        yield return Settle();
                        var camera = client.Board.Map.Camera;
                        camera.Pan += new Vector2(100, 35);
                        client.Board.Map.Refresh();
                        client.Board.Map.Select("lorien");
                        Assert.IsTrue(client.Board.Cards.Pinned);
                        Assert.AreEqual(view.Hand[0], client.Board.Cards.CardId);
                        client.Board.HandDrawer.Open();
                        yield return Settle();
                        Assert.LessOrEqual(client.Board.Hand.worldBound.yMax, client.Board.Map.worldBound.yMax + 1,
                            "The decision dock must not cover the hand.");
                        foreach (var card in client.Board.Hand.Query<Button>(className: "hand-card").ToList())
                        {
                            Assert.LessOrEqual(card.worldBound.xMax, client.Board.Hand.worldBound.xMax, card.text);
                            Assert.LessOrEqual(card.worldBound.yMax, client.Board.Root.worldBound.yMax, card.text);
                            // UI-217: the name sits in the tile's title label (the slot button has no text of its own).
                            var title = card.Q<Label>("hand-card-title");
                            var required = title.MeasureTextSize(title.text, title.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                            Assert.LessOrEqual(required.y, title.contentRect.height + 1, "Title clipped: " + title.text);
                            Assert.GreaterOrEqual(title.resolvedStyle.fontSize, CardTitleFit.MinimumSize, title.text);
                        }
                        Assert.IsNull(client.Board.Tray.Q<Button>("confirm-decision"));
                        LayoutTests.AssertNoDecisionScroll(client.Board);
                        LayoutTests.Save(target, $"cards-{width}-{scale:0.00}.png");
                        client.Panel.targetTexture = client.Viewport;
                        target.Release();
                        Object.Destroy(target);
                    }
            }
        }

        [UnityTest]
        public IEnumerator TwoMapChoicesKeepThePinnedCardAndCommittedStepsDuringAReaction()
        {
            // UI protocol fixture: exercises generic multi-step effects; game-card rules use the real server test.
            var view = new BoardFixture().View;
            view.Options.Clear();
            var backend = new FrozenBackend(view);
            using (var client = new MountedMatchClient("http://localhost", view.MatchId, backend))
            {
                RegionDecision(view, "first", "lorien", "rivendell");
                view.EffectTrail.Add(new EffectTrailView { SourceId = "base.fp.strategy.20" });
                client.Apply(view);
                client.SetOnline();
                client.Board.Cards.Show("base.fp.strategy.20", true);
                yield return Settle();
                client.Board.Map.Focus("lorien");
                yield return Settle();
                FellowshipGestures.Region(client, "lorien");
                yield return Commit(client, backend, 1);
                CollectionAssert.AreEqual(new[] { "lorien" }, backend.Last.Payload.Ids);
                RegionDecision(view, "second", "ered-luin", "erebor");
                view.EffectTrail.Add(new EffectTrailView { SourceId = "base.fp.strategy.20", ChoiceId = "lorien" });
                client.Apply(view);
                yield return Settle();
                Assert.AreEqual(2, client.Board.Tray.Query<Button>(className: "committed-choice").ToList().Count);
                client.Board.Map.Focus("ered-luin");
                yield return Settle();
                FellowshipGestures.Region(client, "ered-luin");
                yield return Commit(client, backend, 2);
                CollectionAssert.AreEqual(new[] { "ered-luin" }, backend.Last.Payload.Ids);
                view.Revision++;
                view.Decision = new DecisionView { Id = "reply", Kind = DecisionKind.Reaction, SourceId = "base.fp.strategy.20", Window = "fixture-window" };
                view.Options.Clear();
                view.Options.Add(new ActionOption
                {
                    Kind = CommandKind.RespondEffect,
                    EligibleIds = { "fixture-reply" },
                    EffectChoices = {
                    new EffectChoiceView { Id = "fixture-reply", SourceId = "base.fp.character.05", Cost = "DiscardTableCard" } }
                });
                view.Options.Add(new ActionOption { Kind = CommandKind.DeclineReaction });
                client.Apply(view);
                yield return Settle();
                Assert.IsTrue(client.Board.Cards.Pinned);
                yield return Settle();
                CardNetworkTests.Click(MatchTestRoom.TrayOrConsole(client, "effect-choice-fixture-reply"));
                yield return Commit(client, backend, 3);
                Assert.AreEqual("fixture-reply", backend.Last.Payload.ReactionId);
                Assert.IsEmpty(client.Board.Tray.Query<TextField>().ToList());
            }
        }

        [UnityTest]
        public IEnumerator ClickInspectionPreservesSelectionAndPlayNeedsTheChosenDie()
        {
            var view = new BoardFixture().View;
            const string id = "base.fp.strategy.01";
            view.Hand.Add(id);
            view.Options.Add(new ActionOption { Kind = CommandKind.PlayEvent, Events = {
                new EventPlayOption { CardId = id, Costs = { new EventCostOption { DieId = "army", Face = DieFace.Army } } } } });
            var backend = new FrozenBackend(view);
            using (var client = new MountedMatchClient("http://localhost", view.MatchId, backend))
            {
                client.Apply(view);
                client.SetOnline();
                yield return Settle();
                Assert.IsTrue(client.Board.HandDrawer.IsOpen, "The hand is permanent.");
                Assert.AreEqual(DisplayStyle.None, client.Board.HandDrawer.Tab.resolvedStyle.display);
                var card = client.Board.Hand.Q<Button>(id);
                var bounds = card.worldBound;
                MovePointer(card);
                yield return new WaitForSeconds(.25f);
                Assert.IsEmpty(client.Board.Cards.CardId);
                Assert.AreEqual(id, client.Board.ExpandedCard, "UI-91 unrolls the card in its hand slot.");
                Assert.IsFalse(client.Board.Cards.Pinned);
                PointerGestureEvents.ClickAt(card);
                yield return Settle();
                Assert.IsEmpty(client.Board.Cards.CardId, "An ordinary hand click reads inline.");
                Assert.AreEqual(0, backend.Commands);
                MovePointer(client.Board.Phase);
                yield return new WaitForSeconds(.2f);
                Assert.IsEmpty(client.Board.Cards.CardId);
                Assert.IsTrue(client.Board.HandDrawer.IsOpen);
                MovePointer(client.Board.Hand);
                yield return new WaitForSeconds(.28f);
                CardGestures.PlayClick(client, id);
                yield return Settle();
                Assert.AreEqual(0, backend.Commands, "No die was selected; a click cannot choose one.");
                Assert.IsEmpty(client.Board.Cards.CardId);
                Assert.IsEmpty(client.Board.ActionDie.DieId);
                CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("army"));
                yield return Settle();
                Assert.IsTrue(card.ClassListContains("playable-card"));
                MovePointer(client.Board.Hand);
                yield return new WaitForSeconds(.28f);
                // UI-194: with the die chosen, one click plays the card: it sinks on the way down and plays on the release.
                PointerGestureEvents.Press(card);
                yield return null;
                Assert.IsTrue(card.ClassListContains(PressFeedback.PressedClass), "The held card sinks.");
                Assert.AreEqual(0, backend.Commands, "Nothing is played before the release.");
                PointerGestureEvents.Release(card);
                yield return Commit(client, backend, 1);
                Assert.IsFalse(card.ClassListContains(PressFeedback.PressedClass));
                Assert.AreEqual("army", backend.Last.Payload.DieId);
                Assert.AreEqual(id, backend.Last.Payload.CardId);
                Assert.AreSame(card, client.Board.Hand.Q<Button>(id));
                Assert.AreEqual(bounds, card.worldBound);
                view.Revision++;
                view.Decision.Id = "next-card-window";
                view.Decision.Kind = DecisionKind.CombatCard;
                view.Options.Clear();
                view.Options.Add(new ActionOption { Kind = CommandKind.CommitCombatCard, EligibleIds = { id } });
                client.Apply(view);
                yield return Settle();
                Assert.IsFalse(card.ClassListContains("playable-card"), "Combat choices belong to the battle window; the regular hand remains read-only.");
                ArmyGestures.Click(card, 2);
                yield return Settle();
                Assert.AreEqual(1, backend.Commands, "The second click cannot spill into the next decision.");
            }
        }

        [UnityTest]
        public IEnumerator UI124ChosenDieLightsItsCardsAndDimsTheRest()
        {
            var view = new BoardFixture().View;
            const string muster = "base.fp.strategy.01";
            const string army = "base.fp.strategy.02";
            view.Hand.Add(muster);
            view.Hand.Add(army);
            view.Options.Add(new ActionOption { Kind = CommandKind.PlayEvent, Events = {
                new EventPlayOption { CardId = muster, Costs = { new EventCostOption { DieId = "muster", Face = DieFace.Muster } } },
                new EventPlayOption { CardId = army, Costs = { new EventCostOption { DieId = "army", Face = DieFace.Army } } } } });
            var backend = new FrozenBackend(view);
            using var client = new MountedMatchClient("http://localhost", view.MatchId, backend);
            client.Apply(view);
            client.SetOnline();
            yield return Settle();
            var musterCard = client.Board.Hand.Q<Button>(muster);
            var armyCard = client.Board.Hand.Q<Button>(army);
            foreach (var card in new[] { musterCard, armyCard })
            {
                Assert.IsFalse(card.ClassListContains("playable-card"), "Without a die nothing is highlighted.");
                Assert.IsFalse(card.ClassListContains(HandCard.UnplayableClass), "Without a die nothing is dimmed.");
            }
            var bounds = musterCard.worldBound;

            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("muster"));
            yield return Settle();
            Assert.AreEqual("muster", client.Board.ActionDie.DieId);
            Assert.IsTrue(musterCard.ClassListContains("playable-card"), "The Muster die lights the card it can play.");
            Assert.IsFalse(musterCard.ClassListContains(HandCard.UnplayableClass));
            Assert.IsTrue(armyCard.ClassListContains(HandCard.UnplayableClass), "A card the die cannot play steps back.");
            Assert.Less(armyCard.resolvedStyle.opacity, .6f);
            Assert.AreEqual(1f, musterCard.resolvedStyle.opacity);
            Assert.AreEqual(bounds, musterCard.worldBound, "Highlighting never moves a card.");
            LayoutTests.Save(client.Viewport, "ui124-muster-cards.png");

            CardNetworkTests.Click(client.Board.OwnDice.Q<Button>("army"));
            yield return Settle();
            Assert.AreEqual("army", client.Board.ActionDie.DieId);
            Assert.IsTrue(armyCard.ClassListContains("playable-card"), "Another die lights its own cards.");
            Assert.IsTrue(musterCard.ClassListContains(HandCard.UnplayableClass));
            Assert.AreSame(musterCard, client.Board.Hand.Q<Button>(muster), "Changing the die does not rebuild the hand.");
            Assert.AreEqual(0, backend.Commands, "Choosing a die never sends a command.");
        }

        private static void MovePointer(VisualElement target)
        {
            CardGestures.Move(target);
        }

        private static void RegionDecision(PlayerView view, string id, params string[] regions)
        {
            view.Revision++;
            view.Decision = new DecisionView { Id = id, Kind = DecisionKind.EffectChoice, SourceId = "base.fp.strategy.20" };
            view.Options.Clear();
            view.Options.Add(new ActionOption { Kind = CommandKind.ChooseEffect, EligibleIds = regions.ToList(), Minimum = 1, Maximum = 1 });
        }

        private static void CheckText(BoardScreen board)
        {
            var card = CardLibrary.Cards[board.Cards.CardId];
            var root = board.Cards.Root;
            Assert.AreEqual(card.Event, root.Q<Label>("card-text").text, card.Id);
            Assert.AreEqual(card.Combat, root.Q<Label>("card-combat-text").text, card.Id);
            foreach (var text in root.Query<Label>(className: "card-rules").ToList())
            {
                if (text.name == "card-combat-text" && !card.HasCombat)
                    continue;
                Assert.GreaterOrEqual(text.resolvedStyle.fontSize, 14);
                Assert.LessOrEqual(text.worldBound.yMax, root.worldBound.yMax - 8, card.Id);
                Assert.GreaterOrEqual(text.worldBound.yMin, root.Q<Label>("card-window").worldBound.yMax);
                var required = text.MeasureTextSize(text.text, text.contentRect.width,
                    VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(required.y, text.contentRect.height + 1,
                    $"Rules clipped: {card.Id}/{text.name}, content {text.contentRect}, world {text.worldBound}, preview {root.worldBound}");
            }
            Assert.LessOrEqual(root.worldBound.yMax, board.Root.worldBound.yMax, "Preview outside screen: " + card.Id);
            Assert.LessOrEqual(root.worldBound.yMax, board.Map.worldBound.yMax + 1,
                "The decision dock must not cover card rules: " + card.Id);
            Assert.GreaterOrEqual(root.worldBound.xMin, board.Root.worldBound.xMin, card.Id);
            if (card.HasCombat)
                Assert.GreaterOrEqual(root.Q("card-combat-section").worldBound.yMin, root.Q("card-event-section").worldBound.yMax);
            foreach (var button in root.Query<Button>().ToList().Where(b => b.resolvedStyle.display != DisplayStyle.None && b.worldBound.height > 0))
                Assert.LessOrEqual(button.worldBound.yMax, board.Cards.Root.Q<Label>("card-window").worldBound.yMin, "Card controls overlap the rules.");
            Assert.Greater(board.Map.worldBound.width - board.Cards.Root.worldBound.width, board.Map.worldBound.width / 2);
        }
        private static IEnumerator Settle()
        {
            for (var i = 0; i < 3; i++)
                yield return null;
        }
        private static IEnumerator Commit(MountedMatchClient client, FrozenBackend backend, int expected)
        {
            var deadline = Time.realtimeSinceStartup + 3;
            while (backend.Commands < expected && Time.realtimeSinceStartup < deadline)
                yield return null;
            yield return new WaitForSeconds(.1f);
            Assert.AreEqual(expected, backend.Commands);
        }

        private sealed class FrozenBackend : HttpMessageHandler
        {
            private readonly PlayerView view;
            public int Commands;
            public GameCommand Last;
            public FrozenBackend(PlayerView view) { this.view = view; }
            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                object response = view;
                if (request.RequestUri.AbsolutePath.EndsWith("/preview"))
                    response = new ActionPreview { Legal = true, Revision = view.Revision };
                if (request.RequestUri.AbsolutePath.EndsWith("/commands"))
                {
                    Last = JsonConvert.DeserializeObject<GameCommand>(await request.Content.ReadAsStringAsync());
                    Commands++;
                    response = new CommandReceipt { Accepted = true, Revision = view.Revision, CommandId = Last.CommandId };
                }
                return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(JsonConvert.SerializeObject(response)) };
            }
        }
    }
}
