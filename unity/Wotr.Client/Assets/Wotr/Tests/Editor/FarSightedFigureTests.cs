using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-465 (owner, 2026-11-03, kings-far-sighted): "both". While Awakened Thranduil is the only companion outside the
    // Fellowship, Far-sighted's Move companions stays greyed (UI-456) and Thranduil's own figure is Far-sighted's mover with
    // the die that pays it (the Ruler die showing a Character): his figure's move takes Far-sighted and its range (his plain
    // move plus one region). With another Character die, or with another companion outside, his move stays plain.
    public sealed class FarSightedFigureTests
    {
        private const string Thranduil = FarSightedMoves.Owner;
        private const string Legolas = "base.character.legolas";
        private const string Ruler = "kings.die.fp.ruler";
        private const string Character = "character";

        [Test]
        public void ThranduilsFigureStartsFarSightedWithTheRulerDie()
        {
            var view = View();
            var draft = Chosen(view, Ruler, out var context);
            Assert.IsTrue(FarSightedFigure.Moves(view, draft, context));
            CollectionAssert.AreEqual(new[] { "woodland-realm", "dale", "erebor" }, Targets(TravelActions.Option(view, draft, context)),
                "His figure lights Far-sighted's range: one region beyond his plain move.");
            Assert.AreEqual(FarSightedFigure.Offer, FarSightedFigure.Offers(view, draft, context)[Thranduil], "His tooltip names it.");
            Assert.IsTrue(TravelActions.Select(view, draft, context, Thranduil));
            Assert.AreEqual(CommandKind.MoveCompanions, draft.Kind);
            Assert.AreEqual(FarSightedMoves.Ability, draft.Payload.AbilityId);
            Assert.AreEqual(Ruler, draft.Payload.DieId);
            Assert.IsTrue(draft.Payload.Stepwise);
            CollectionAssert.AreEqual(new[] { Thranduil }, TravelActions.Selected(draft));
            context.BindCost(view, draft);
            Assert.AreEqual(FarSightedMoves.Ability, context.AbilityId, "The die is bound to Far-sighted.");
            CollectionAssert.AreEqual(new[] { "woodland-realm", "dale", "erebor" }, Targets(DraftOptions.For(view, draft)),
                "The draft's destinations are Far-sighted's.");
            Assert.IsFalse(FarSightedFigure.Moves(view, draft, context), "Started: the draft's own ability rules.");
            CollectionAssert.AreEqual(new[] { Thranduil }, TravelActions.Option(view, draft, context).EligibleIds);
            Assert.AreEqual(FarSightedFigure.Offer, FarSightedFigure.Offers(view, draft, context)[Thranduil],
                "Still Far-sighted's move (the board drops the line while he is chosen).");
            draft.Back();
            Assert.IsEmpty(TravelActions.Selected(draft), "Back leaves him behind; the Far-sighted move waits for him.");
            Assert.AreEqual(FarSightedMoves.Ability, draft.Payload.AbilityId);
            Assert.IsTrue(TravelActions.Select(view, draft, context, Thranduil), "His figure puts him back into it.");
            CollectionAssert.AreEqual(new[] { Thranduil }, TravelActions.Selected(draft));
            Assert.AreEqual(FarSightedMoves.Ability, draft.Payload.AbilityId);
        }

        [Test]
        public void AnotherCharacterDieMovesThranduilPlainly()
        {
            var view = View();
            var draft = Chosen(view, Character, out var context);
            Assert.IsFalse(FarSightedFigure.Moves(view, draft, context), "Far-sighted is paid only by the Ruler die.");
            CollectionAssert.AreEqual(new[] { "woodland-realm", "dale" }, Targets(TravelActions.Option(view, draft, context)));
            Assert.IsEmpty(FarSightedFigure.Offers(view, draft, context));
            Assert.IsTrue(TravelActions.Select(view, draft, context, Thranduil));
            Assert.AreEqual("", draft.Payload.AbilityId);
            Assert.AreEqual(Character, draft.Payload.DieId);
        }

        [Test]
        public void MoveCompanionsStaysGreyedAndPointsAtHisFigure()
        {
            var view = View();
            var draft = Chosen(view, Ruler, out _);
            var move = ComponentActions.For(view, draft).Single(c => c.Key == "far-sighted-MoveCompanions");
            Assert.IsFalse(move.Enabled, "UI-456: only Thranduil stands outside the Fellowship.");
            Assert.AreEqual(FarSightedMoves.NoCompanion, move.Reason);
            StringAssert.EndsWith("Thranduil moves from his figure", move.Reason);
            Assert.IsFalse(ComponentActions.Select(view, draft, move));
            Assert.IsNull(draft.Kind);
        }

        [Test]
        public void WithAnotherCompanionOutsideTheVariantWorksAndHisFigureMovesPlainly()
        {
            var view = View();
            var option = view.Options[0];
            option.EligibleIds.Add(Legolas);
            option.Travel.Add(new TravelOption { Id = Legolas, Origin = "lorien", Range = 3, Targets = { "lorien" } });
            option.RulerTravel.Travel.Add(new TravelOption { Id = Legolas, Origin = "lorien", Range = 4, Targets = { "lorien" } });
            view.Characters.Add(new CharacterView { Id = Legolas, Location = "lorien", Side = Side.FreePeoples });
            var draft = Chosen(view, Ruler, out var context);
            Assert.IsFalse(FarSightedFigure.Moves(view, draft, context));
            Assert.IsTrue(ComponentActions.For(view, draft).Single(c => c.Key == "far-sighted-MoveCompanions").Enabled);
            Assert.IsTrue(TravelActions.Select(view, draft, context, Thranduil));
            Assert.AreEqual("", draft.Payload.AbilityId, "As before UI-465: the figure's move is plain.");
        }

        [Test]
        public void ARulerTokenKeepsItsOwnTravel()
        {
            var view = View();
            var draft = Chosen(view, Ruler, out var context);
            draft.Change(p => p.RulerTokenId = "kings.token.thranduil");
            Assert.IsFalse(FarSightedFigure.Moves(view, draft, context), "UI-389: the token's actions carry its ability.");
        }

        private static TurnDraft Chosen(PlayerView view, string die, out ActionDieContext context)
        {
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = die);
            context = ActionDieContext.Of(view, draft);
            return draft;
        }

        private static string[] Targets(ActionOption option) =>
            option.Travel.Single(t => t.Id == Thranduil).Targets.ToArray();

        // kings-far-sighted: Thranduil Awakened in the Woodland Realm, every companion in the Fellowship, the Ruler die and a
        // standard die showing a Character.
        private static PlayerView View() => new PlayerView
        {
            MatchId = "far-sighted-figure", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action },
            Dice =
            {
                new DieView { Id = Ruler, Owner = Side.FreePeoples, Face = DieFace.Character, Source = "ruler", Location = DieLocation.Available },
                new DieView { Id = Character, Owner = Side.FreePeoples, Face = DieFace.Character, Location = DieLocation.Available }
            },
            Characters = { new CharacterView { Id = Thranduil, Location = "woodland-realm", Side = Side.FreePeoples } },
            Options =
            {
                new ActionOption
                {
                    Kind = CommandKind.MoveCompanions, EligibleIds = { Thranduil }, EligibleDice = { Ruler, Character },
                    Travel = { new TravelOption { Id = Thranduil, Origin = "woodland-realm", Range = 2, Targets = { "woodland-realm", "dale" } } },
                    RulerTravel = new RulerTravelOption
                    {
                        AbilityId = FarSightedMoves.Ability, Dice = { Ruler }, RangeBonus = 1,
                        Travel = { new TravelOption { Id = Thranduil, Origin = "woodland-realm", Range = 3, Targets = { "woodland-realm", "dale", "erebor" } } }
                    }
                }
            }
        };
    }
}
