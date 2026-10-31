using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-434 (owner, after UI-433): every object a hint asks the player to click wears the shared gold ChoiceGlow where it
    // stands. These are the classes of the fixed cases, including one no seeded match reaches (Gandalf the Grey's draw on
    // its deck); GlowingHintAuditTests checks them on real seeds.
    public sealed class GlowingHintClassesTests
    {
        // Two offers of dice in one pool; the pool lights their union, and one's refresh never puts out the other's.
        [Test]
        public void APoolLightsTheUnionOfTheDiceItsInteractionsOffer()
        {
            var pool = new VisualElement();
            foreach (var id in new[] { "a", "b", "c" })
                pool.Add(new Button { name = id, userData = new DraftToken(DraftObject.Die, id) });
            var glow = new PoolChoiceGlow(pool);
            var ring = new object();
            var payment = new object();
            glow.Offer(ring, new[] { "a" });
            glow.Offer(payment, new[] { "b" });
            CollectionAssert.AreEqual(new[] { "a", "b" }, Glowing(pool));
            glow.Offer(ring, new string[0]);
            CollectionAssert.AreEqual(new[] { "b" }, Glowing(pool), "The Ring's refresh keeps the payment's die lit.");
            glow.Withdraw(payment);
            CollectionAssert.IsEmpty(Glowing(pool));
        }

        // A deck choice's decks (the Palantír of Orthanc's draw) glow beside the hand; with no decision they are plain decks.
        [Test]
        public void TheDecksADeckChoiceOffersGlow()
        {
            var view = new PlayerView
            {
                You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "deck", Kind = DecisionKind.EffectChoice, Presentation = DeckChoice.Presentation },
                Options = { new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, EligibleIds = { "character", "strategy" } } }
            };
            var decks = new DeckView();
            decks.Refresh(view, new TurnDraft(), new ActionDieContext(), false);
            foreach (var deck in new[] { "character", "strategy" })
                Assert.IsTrue(decks.Root.Q<Button>("deck-" + deck).ClassListContains(ChoiceGlow.ClassName), deck + " glows.");
            view.Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action };
            view.Options.Clear();
            decks.Refresh(view, new TurnDraft(), new ActionDieContext(), false);
            Assert.IsFalse(decks.Root.Q<Button>("deck-character").ClassListContains(ChoiceGlow.ClassName));
        }

        // Gandalf the Grey's draw, whenever his portrait cannot hold it (UI-431), is the deck of the Event card just resolved:
        // that deck glows, the other does not. No seeded match reaches it (the server offers it only while he is the Guide).
        [Test]
        public void TheDeckGandalfTheGreysDrawTakesGlows()
        {
            var respond = new ActionOption { Kind = CommandKind.RespondEffect, Enabled = true, EligibleIds = { ReactionDecks.GreyDraw } };
            respond.EffectChoices.Add(new EffectChoiceView { Id = ReactionDecks.GreyDraw, SourceId = "base.character.gandalf-grey", Title = "Draw matching card" });
            var view = new PlayerView
            {
                You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "grey", Kind = DecisionKind.Reaction, Window = "event.completed", SourceId = "base.fp.strategy.12" },
                Options = { respond, new ActionOption { Kind = CommandKind.DeclineReaction, Enabled = true } }
            };
            var deck = CardLibrary.Cards["base.fp.strategy.12"].Deck;
            var decks = new DeckView();
            decks.Refresh(view, new TurnDraft(), new ActionDieContext(), false);
            Assert.IsTrue(decks.Root.Q<Button>("deck-" + deck).ClassListContains(ChoiceGlow.ClassName), "The drawn deck glows.");
            Assert.IsFalse(decks.Root.Q<Button>("deck-" + (deck == "strategy" ? "character" : "strategy")).ClassListContains(ChoiceGlow.ClassName));
        }

        // The gate the Fellowship may enter by glows on the map's Mordor track; the other gate does not.
        [Test]
        public void TheOfferedMordorGateGlows()
        {
            var map = new MapCanvas(BoardPresentation.Load());
            map.MordorTrack.Offer(0, "morannon");
            Assert.IsTrue(map.MordorTrack.GateButton("morannon").ClassListContains(ChoiceGlow.ClassName));
            Assert.IsFalse(map.MordorTrack.GateButton("minas-morgul").ClassListContains(ChoiceGlow.ClassName));
            map.MordorTrack.Offer(-1, "");
            Assert.IsFalse(map.MordorTrack.GateButton("morannon").ClassListContains(ChoiceGlow.ClassName));
        }

        // A siege extension takes one figure with one click: each figure glows instead of standing dimmed as left behind.
        [Test]
        public void TheFiguresASiegeExtensionMayTakeGlow()
        {
            var view = BattleScreenFixture.Create(CommandKind.ExtendSiege, mode: BattleMode.Siege);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ExtendSiege);
            var root = new VisualElement();
            var choices = new BattleChoices(root, new VisualElement(), view, draft, action => action(), action => action(), _ => { },
                BoardPresentation.Load());
            choices.Refresh();
            var figures = root.Query<Button>(className: "figure").ToList();
            Assert.IsNotEmpty(figures);
            foreach (var figure in figures)
            {
                Assert.IsTrue(figure.ClassListContains(ChoiceGlow.ClassName), figure.name + " glows.");
                Assert.IsFalse(figure.ClassListContains("left-behind"), figure.name + " is not dimmed.");
            }
        }

        private static string[] Glowing(VisualElement pool) =>
            pool.Query<Button>(className: ChoiceGlow.ClassName).ToList().Select(button => button.name).ToArray();
    }
}
