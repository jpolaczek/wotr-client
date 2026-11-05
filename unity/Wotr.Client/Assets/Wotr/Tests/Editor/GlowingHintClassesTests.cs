using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.EditorTests
{
    // UI-434 (owner, after UI-433): every object a hint asks the player to click wears the shared gold ChoiceGlow where it
    // stands. These are the classes of the fixed cases, including those no seeded match reaches (the Council's token
    // offer, a deck choice's plain draw, a Ring the die merely may use, Gandalf the Grey's draw on its deck);
    // GlowingHintAuditTests checks them on real seeds.
    public sealed class GlowingHintClassesTests
    {
        // Two interactions offer dice in one pool; the pool lights their union, and one's refresh never puts out another's.
        [Test]
        public void APoolLightsTheUnionOfTheDiceItsInteractionsOffer()
        {
            var pool = new VisualElement();
            foreach (var id in new[] { "a", "b", "c" })
                pool.Add(new Button { name = id, userData = new DraftToken(DraftObject.Die, id) });
            var glow = new PoolChoiceGlow(pool);
            var ring = new object();
            var resource = new object();
            glow.Offer(ring, new[] { "a" });
            glow.Offer(resource, new[] { "b" });
            CollectionAssert.AreEqual(new[] { "a", "b" }, Glowing(pool));
            glow.Offer(ring, new string[0]);
            CollectionAssert.AreEqual(new[] { "b" }, Glowing(pool), "The Ring's refresh keeps the resource decision's die lit.");
            glow.Withdraw(resource);
            CollectionAssert.IsEmpty(Glowing(pool));
        }

        // Dismay's hint names where the offered Ruler die stands: the Free Peoples' in the top bar, the Shadow's own in its pool.
        [TestCase(Side.FreePeoples, "Click the glowing Ruler die in the top bar to reroll it with Dismay.")]
        [TestCase(Side.Shadow, "Click your glowing Ruler die to reroll it with Dismay.")]
        public void DismaysHintNamesThePoolOfItsRulerDie(Side owner, string hint)
        {
            var view = Dismay();
            view.Dice.Add(new DieView { Id = "ruler", Owner = owner });
            view.Options.Add(new ActionOption { Kind = CommandKind.UseDismay, Enabled = true, EligibleIds = { "kings.token.theoden" }, EligibleDice = { "ruler" } });
            Assert.AreEqual(hint, ResourcePresentation.PoolHint(view));
        }

        [Test]
        public void DismaysHintNamesBothPoolsWhenBothRulerDiceGlow()
        {
            var view = Dismay();
            view.Dice.Add(new DieView { Id = "free", Owner = Side.FreePeoples });
            view.Dice.Add(new DieView { Id = "shadow", Owner = Side.Shadow });
            view.Options.Add(new ActionOption
                { Kind = CommandKind.UseDismay, Enabled = true, EligibleIds = { "kings.token.theoden" }, EligibleDice = { "free", "shadow" } });
            Assert.AreEqual("Click a glowing Ruler die, yours or the top bar's, to reroll it with Dismay.", ResourcePresentation.PoolHint(view));
        }

        // Vilya is a socket in the console; Lords also has a Vilya die, so the hint says where.
        [Test]
        public void VilyasHintNamesTheConsole()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "keep", Kind = DecisionKind.KeepActionDie }
            };
            StringAssert.StartsWith("Click the glowing Vilya in the console", ResourcePresentation.PoolHint(view));
        }

        // A socket a hint asks for glows: a Keeper Ring offered now, a card's recovery, a removal's payment; a Ring the chosen
        // die may merely use keeps its bright art, and a Ring already picked is selected rather than glowing.
        [Test]
        public void TheSocketsAHintAsksForGlowAndARingTheDieMayUseDoesNot()
        {
            var view = Lords();
            var rings = new ConsoleRings();
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, EligibleIds = { NamedRings.Vilya, NamedRings.Narya } };
            rings.Apply(view, "", true, option, CommandKind.UseRing);
            Assert.IsFalse(rings.For(NamedRings.Vilya).ClassListContains(ChoiceGlow.ClassName), "A Ring the die may use keeps its art.");
            Assert.IsTrue(rings.For(NamedRings.Vilya).ClassListContains("legal-target"));
            rings.Apply(view, "", true, option, CommandKind.ChooseEffect, new[] { NamedRings.Narya });
            Assert.IsTrue(rings.For(NamedRings.Vilya).ClassListContains(ChoiceGlow.ClassName), "Three Rings' Ring glows.");
            Assert.IsFalse(rings.For(NamedRings.Narya).ClassListContains(ChoiceGlow.ClassName), "A picked Ring is selected.");
            Assert.IsTrue(rings.For(NamedRings.Narya).ClassListContains("selected"));
            Assert.IsFalse(rings.For(NamedRings.Nenya).ClassListContains(ChoiceGlow.ClassName));
            rings.Apply(view, "", true, option, CommandKind.DiscardLore);
            Assert.IsTrue(rings.For(NamedRings.Narya).ClassListContains(ChoiceGlow.ClassName), "A removal's Ring payment glows.");
            rings.Apply(view, "", false, option, CommandKind.DiscardLore);
            Assert.IsFalse(rings.For(NamedRings.Narya).ClassListContains(ChoiceGlow.ClassName), "A step being sent lights nothing.");
        }

        [Test]
        public void AKeeperRingOfferedNowGlowsOnItsSocket()
        {
            var view = Lords();
            view.Hunt = new HuntView();
            view.Decision = new DecisionView { Id = "d", Kind = DecisionKind.Reaction };
            view.Options.Add(new ActionOption { Kind = CommandKind.RespondEffect, Enabled = true, EligibleIds = { NamedRings.NenyaReaction } });
            var rings = new ConsoleRings();
            rings.Apply(view, "", true, null, CommandKind.UseRing);
            var nenya = rings.For(NamedRings.Nenya);
            Assert.IsTrue(nenya.ClassListContains(ConsoleRings.KeeperReadyClass) && nenya.ClassListContains(ChoiceGlow.ClassName));
            Assert.IsFalse(rings.For(NamedRings.Vilya).ClassListContains(ChoiceGlow.ClassName));
        }

        // A deck choice's decks glow beside the hand; with no decision they are plain decks again.
        [Test]
        public void TheDecksADeckChoiceOffersGlow()
        {
            var view = new PlayerView
            {
                You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
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

        // The Council's token offer (no seeded match reaches it in one step) and a Chieftain's entry cost glow.
        [Test]
        public void TheCouncilTokenAHintAsksForGlows()
        {
            const string token = "council.advance-politics";
            var view = new PlayerView
            {
                You = Side.Shadow, DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = "council", Kind = DecisionKind.CouncilToken },
                Options = { new ActionOption { Kind = CommandKind.ChooseCouncilToken, Enabled = true, EligibleIds = { token } } }
            };
            Assert.IsTrue(ResourcePresentation.CouncilAsked(view, new TurnDraft(), token), "The Council's offer glows.");
            Assert.IsFalse(ResourcePresentation.CouncilAsked(view, new TurnDraft(), "council.move-shadow-characters"));
            var entry = new CharacterActionOption { CharacterId = "kings.character.black-serpent", AbilityId = "enter", CouncilTokens = { token } };
            view.Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action };
            view.Options.Clear();
            view.Options.Add(new ActionOption { Kind = CommandKind.EnterCharacter, Enabled = true, CharacterActions = { entry } });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.EnterCharacter);
            draft.Change(p =>
            {
                p.CharacterId = entry.CharacterId;
                p.AbilityId = entry.AbilityId;
            });
            Assert.IsTrue(ResourcePresentation.CouncilAsked(view, draft, token), "A Chieftain's entry may pay with it.");
            draft.Change(p => p.CouncilTokenId = token);
            Assert.IsFalse(ResourcePresentation.CouncilAsked(view, draft, token), "Paid, it is chosen rather than asked for.");
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

        private static PlayerView Dismay() => new PlayerView
        {
            You = Side.Shadow, DecisionOwner = Side.Shadow,
            Decision = new DecisionView { Id = "dismay", Kind = DecisionKind.Dismay }
        };

        private static PlayerView Lords() => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Configuration = new MatchConfiguration { Lords = true },
            Rings =
            {
                new RingView { Id = NamedRings.Narya, Owner = Side.FreePeoples },
                new RingView { Id = NamedRings.Nenya, Owner = Side.FreePeoples },
                new RingView { Id = NamedRings.Vilya, Owner = Side.FreePeoples }
            }
        };
    }
}
