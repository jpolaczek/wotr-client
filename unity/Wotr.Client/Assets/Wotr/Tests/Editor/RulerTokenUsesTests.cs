using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-389 (owner's report, 2026-10-28): a Ruler token's click lights every action the server offers for it, judged by
    // the token's face, and none of the Ruler die's own uses; its actions carry the token's ability.
    public sealed class RulerTokenUsesTests
    {
        private const string Ruler = "kings.die.fp.ruler";

        [Test]
        public void ThranduilsTokenLightsTheFellowshipNotTheMusterDiesPolitics()
        {
            var view = View(DieFace.Character, "kings.token.thranduil");
            Token(view, "kings.token.thranduil", "kings.far-sighted", new ActionOption { Kind = CommandKind.MoveFellowship, EligibleDice = { Ruler } });
            Token(view, "kings.token.thranduil", "kings.far-sighted",
                new ActionOption { Kind = CommandKind.SeparateCompanions, EligibleIds = { "base.character.legolas" }, EligibleDice = { Ruler } });
            var draft = Draft("kings.token.thranduil");
            var options = DieOptions.For(view, draft, Die(view, draft));
            Assert.IsTrue(options.Fellowship, "The token's Fellowship move and separation light the Fellowship.");
            Assert.IsFalse(options.Politics, "The Muster die's Politics is not the token's use.");
            Assert.IsFalse(options.Recruit, "The Muster die's recruitment is not the token's use.");
            var plain = Draft("");
            Assert.IsTrue(DieOptions.For(view, plain, Die(view, plain)).Politics, "Without the token the Muster die lights Politics.");
        }

        [Test]
        public void DenethorsTokenLightsTheDecksAndNothingOfMuster()
        {
            var view = View(DieFace.Event, "kings.token.denethor");
            Token(view, "kings.token.denethor", "",
                new ActionOption { Kind = CommandKind.DrawCard, EligibleDice = { Ruler }, Targets = { "character", "strategy" } });
            var draft = Draft("kings.token.denethor");
            var options = DieOptions.For(view, draft, Die(view, draft));
            Assert.IsTrue(options.Draw, "The token's Event face draws a card.");
            Assert.IsFalse(options.Politics);
            Assert.IsFalse(options.Recruit);
            Assert.IsFalse(options.Fellowship);
        }

        [Test]
        public void TheTokensFellowshipMoveCarriesFarSighted()
        {
            var view = View(DieFace.Character, "kings.token.thranduil");
            Token(view, "kings.token.thranduil", "kings.far-sighted", new ActionOption { Kind = CommandKind.MoveFellowship, EligibleDice = { Ruler } });
            var draft = Draft("kings.token.thranduil");
            Assert.IsTrue(FellowshipActions.Choose(view, draft, Die(view, draft), CommandKind.MoveFellowship));
            Assert.AreEqual(CommandKind.MoveFellowship, draft.Kind);
            Assert.AreEqual(Ruler, draft.Payload.DieId);
            Assert.AreEqual("kings.token.thranduil", draft.Payload.RulerTokenId);
            Assert.AreEqual("kings.far-sighted", draft.Payload.AbilityId, "The server takes the token's travel only with Far-sighted.");
        }

        // The Ruler die shows Muster; the server offers its own Politics and recruitment beside the token.
        private static PlayerView View(DieFace tokenFace, string token)
        {
            var view = DieOptionsTests.Fixture();
            view.Dice.Add(new DieView { Id = Ruler, Owner = Side.FreePeoples, Face = DieFace.Muster, Source = "ruler", Location = DieLocation.Available });
            view.Options.Add(new ActionOption { Kind = CommandKind.AdvancePolitics, EligibleDice = { Ruler } });
            view.Options.Add(new ActionOption { Kind = CommandKind.Recruit, EligibleDice = { Ruler } });
            view.RulerTokens.Add(new RulerTokenView { Id = token, Face = tokenFace, Location = "free-peoples" });
            return view;
        }

        private static void Token(PlayerView view, string token, string ability, ActionOption action) =>
            view.RulerTokenActions.Add(new RulerTokenActionOption { TokenId = token, AbilityId = ability, Action = action });

        private static TurnDraft Draft(string token)
        {
            var draft = new TurnDraft();
            draft.Change(p => { p.DieId = Ruler; p.RulerTokenId = token; });
            return draft;
        }

        private static ActionDieContext Die(PlayerView view, TurnDraft draft)
        {
            var context = new ActionDieContext();
            context.Sync(view, draft.Payload.DieId, draft.Payload.RulerTokenId);
            return context;
        }
    }
}
