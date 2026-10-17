using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public class ExpansionDraftTests
    {
        [Test]
        public void CouncilRequiresExplicitGandalfAndGuideAndKeepsPrivateChoicesAcrossReconnect()
        {
            var view = View(CommandKind.ChooseCompanionVersions);
            view.Decision.Kind = DecisionKind.CouncilVersions;
            var option = view.Options[0];
            var gandalf = new CompanionVersionOption { FigureId = "grey", BaseId = "original", AlternateId = "keeper", Required = true };
            var gimli = new CompanionVersionOption { FigureId = "gimli", BaseId = "son", AlternateId = "lord" };
            option.CompanionVersions.AddRange(new[] { gandalf, gimli });
            option.EligibleIds.AddRange(new[] { "original", "keeper", "son", "lord" });
            option.Targets.AddRange(new[] { "grey", "strider" });
            var session = new TurnDraftSession();
            session.Update(view);
            var draft = session.Draft;
            DraftGesture.Choose(draft, view, option.Kind);
            Assert.IsFalse(draft.Ready(option));
            CouncilDraft.Version(draft, gandalf, "keeper");
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.GuideId = "strider");
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual("son", CouncilDraft.Version(draft.Payload, gimli));
            CouncilDraft.Version(draft, gimli, "lord");
            session.Edited(view);
            view.Revision++;
            session.Update(view);
            Assert.IsTrue(draft.Ready(option));
            CollectionAssert.AreEquivalent(new[] { "keeper", "lord" }, draft.Payload.Ids);
            option.EligibleIds.Remove("lord");
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsFalse(draft.Payload.Ids.Contains("lord"));
        }

        [Test]
        public void HomelandSupportsExplicitStayAndTwoFateDestinationsWithoutChangingOtherCompanions()
        {
            var view = View(CommandKind.ChooseCouncilHomelands);
            var option = view.Options[0];
            option.EligibleIds.Add("gimli");
            option.CharacterActions.Add(new CharacterActionOption { CharacterId = "gimli", Targets = { "iron-hills", "ered-luin" } });
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            Assert.IsTrue(draft.Ready(option));
            draft.Change(p => p.CharacterId = "gimli");
            Assert.IsFalse(CouncilDraft.Region(draft, option, "erebor"));
            Assert.IsTrue(CouncilDraft.Region(draft, option, "iron-hills"));
            Assert.IsTrue(CouncilDraft.Region(draft, option, "ered-luin"));
            Assert.AreEqual("ered-luin", draft.Command(view).Payload.Placements.Single().RegionId);
            draft.Reconcile(view);
            Assert.AreEqual("ered-luin", draft.Payload.Placements.Single().RegionId);
            CouncilDraft.Homeland(draft, "gimli", "");
            Assert.IsEmpty(draft.Payload.Placements);
        }

        [Test]
        public void RulerTokenAndConvertedDieStayInOneCommandAndDisappearWhenTheirOptionExpires()
        {
            var view = View(CommandKind.DiscardDie);
            view.Dice.Add(new DieView { Id = "ruler", Source = "ruler", Owner = Side.FreePeoples, Face = DieFace.Muster, Location = DieLocation.Available });
            view.RulerTokenActions.Add(new RulerTokenActionOption { TokenId = "denethor", Action = new ActionOption
                { Kind = CommandKind.DrawCard, EligibleDice = { "ruler" }, Targets = { "character", "strategy" } } });
            var draft = new TurnDraft();
            DraftOptions.SelectToken(draft, view, "denethor");
            Assert.IsFalse(DraftGesture.Choose(draft, view, CommandKind.DiscardDie));
            Assert.IsTrue(DraftGesture.Choose(draft, view, CommandKind.DrawCard));
            draft.Change(p => p.Deck = "strategy");
            var option = DraftOptions.For(view, draft);
            Assert.IsTrue(draft.Ready(option));
            var command = draft.Command(view);
            Assert.AreEqual("denethor", command.Payload.RulerTokenId);
            Assert.AreEqual("ruler", command.Payload.DieId);
            Assert.AreEqual(DieFace.Muster, view.Dice[0].Face);
            Assert.AreEqual(CommandKind.DiscardDie, view.Options.Single().Kind);
            Assert.IsFalse(draft.Reconcile(view));
            view.RulerTokenActions.Clear();
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsNull(draft.Kind);
        }

        [Test]
        public void CouncilTravelUsesProjectedGroupsAndDoesNotSpendAnActionDie()
        {
            var view = View(CommandKind.UseCouncilToken);
            var source = view.Options[0];
            source.EligibleIds.Add(ExpansionDraft.CouncilTravel);
            source.Travel.Add(new TravelOption { Id = "nazgul", Flight = true, Origin = "barad-dur", Targets = { "hollin" } });
            view.Units.Add(new UnitView { Id = "nazgul", Side = Side.Shadow, Kind = UnitKind.Nazgul, Location = "barad-dur" });
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, source.Kind);
            Assert.IsFalse(draft.Ready(DraftOptions.For(view, draft)));
            ExpansionDraft.CouncilToken(draft, ExpansionDraft.CouncilTravel);
            var option = DraftOptions.For(view, draft);
            TravelDraft.ToggleCompanion(draft, view, option, "nazgul");
            Assert.IsTrue(TravelDraft.Step(draft, view, BoardPresentation.Load(), "hollin"));
            Assert.IsTrue(draft.Ready(option));
            Assert.IsEmpty(draft.Command(view).Payload.DieId);
            Assert.IsFalse(draft.Reconcile(view));
            CollectionAssert.AreEqual(new[] { ExpansionDraft.CouncilTravel }, source.EligibleIds);
        }

        [Test]
        public void DismayTokenCanBeDroppedWithoutChoosingBetweenTheTwoRulerDice()
        {
            var view = View(CommandKind.UseDismay);
            view.You = Side.Shadow;
            view.Options[0].EligibleIds.Add("dismay");
            foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
            {
                var id = side.ToString();
                view.Dice.Add(new DieView { Id = id, Source = "ruler", Owner = side, Face = DieFace.Muster, Location = DieLocation.Available });
                view.Options[0].EligibleDice.Add(id);
            }
            var draft = new TurnDraft();
            Assert.IsTrue(DraftGesture.Drop(draft, view, new DraftToken(DraftObject.RulerToken, "dismay"), new DraftToken(DraftObject.Tray)));
            Assert.AreEqual(CommandKind.UseDismay, draft.Kind);
            CollectionAssert.AreEqual(new[] { "dismay" }, draft.Payload.Ids);
            Assert.IsEmpty(draft.Payload.DieId, "Ruler dice of different sides are never equivalent costs.");
            Assert.IsEmpty(draft.Payload.RulerTokenId);
        }

        [Test]
        public void ExpiredCouncilTokenCannotReuseAnotherTokensTargets()
        {
            var view = View(CommandKind.UseCouncilToken);
            view.Options[0].EligibleIds.Add(ExpansionDraft.CouncilPolitics);
            view.Options[0].Targets.Add("sauron");
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.UseCouncilToken);
            ExpansionDraft.CouncilToken(draft, ExpansionDraft.CouncilPolitics);
            draft.Change(p => p.NationId = "sauron");
            Assert.IsTrue(draft.Ready(DraftOptions.For(view, draft)));
            view.Options[0].EligibleIds[0] = ExpansionDraft.CouncilTravel;
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsNull(draft.Kind);
        }

        [Test]
        public void EntryRequiresItsCouncilCostAndBalrogRecruitmentRemainsOptional()
        {
            var view = View(CommandKind.EnterCharacter);
            var option = view.Options[0];
            var choice = new CharacterActionOption { CharacterId = "balrog", RequiresCouncilToken = true,
                CouncilTokens = { "travel" }, Dice = { "die" }, Targets = { "moria" }, Recruitment =
                { new RecruitmentOption { Nation = "sauron", Kind = UnitKind.Elite, ReserveIds = { "elite" }, Targets = { "moria" } } } };
            option.CharacterActions.Add(choice);
            view.Dice.Add(new DieView { Id = "die", Source = "standard", Owner = Side.FreePeoples, Face = DieFace.Muster, Location = DieLocation.Available });
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            CharacterDraft.Select(draft, view, choice);
            Assert.IsFalse(draft.Ready(option));
            draft.Change(p => p.CouncilTokenId = "travel");
            Assert.IsTrue(draft.Ready(option));
            CharacterRecruitDraft.Select(draft, choice, "elite");
            Assert.IsTrue(draft.Ready(option));
            Assert.AreEqual("moria", draft.Payload.Placements.Single().RegionId);
            choice.CouncilTokens.Clear();
            Assert.IsTrue(draft.Reconcile(view));
            Assert.IsFalse(draft.Ready(option));
        }

        [Test]
        public void MultipleSovereignsAreSelectableAndEachAwakeningKeepsItsOwnRoute()
        {
            var view = View(CommandKind.AwakenSovereign);
            view.Options.Clear();
            foreach (var id in new[] { "brand", "dain" })
                view.Options.Add(new ActionOption { Kind = CommandKind.AwakenSovereign, EligibleIds = { id }, EligibleDice = { "die" },
                    CharacterActions = { new CharacterActionOption { CharacterId = id, Dice = { "die" } } },
                    Travel = { new TravelOption { Id = id, Origin = id, Range = 1, Targets = { id } } } });
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.AwakenSovereign);
            Assert.AreEqual(2, DraftOptions.For(view, draft).CharacterActions.Count);
            draft.Change(p => { p.CharacterId = "dain"; p.DieId = "die"; });
            var option = DraftOptions.For(view, draft);
            Assert.AreEqual("dain", option.Travel.Single().Origin);
            TravelDraft.Stay(draft, option);
            Assert.IsTrue(draft.Ready(option));
            draft.Reconcile(view);
            Assert.IsTrue(draft.Ready(DraftOptions.For(view, draft)));
        }

        private static PlayerView View(CommandKind kind) => new PlayerView
        {
            MatchId = "expansions", You = Side.FreePeoples, DecisionOwner = Side.FreePeoples, Phase = GamePhase.ActionResolution,
            Decision = new DecisionView { Id = "decision", Kind = DecisionKind.Action }, Options = { new ActionOption { Kind = kind } }
        };
    }
}
