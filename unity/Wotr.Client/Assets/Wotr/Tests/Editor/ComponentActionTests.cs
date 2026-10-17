using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using UnityEngine.UIElements;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    public sealed class ComponentActionTests
    {
        [Test]
        public void AbilityIsAnchoredAtItsOwnerAndCannotSubstituteAnUnselectedCost()
        {
            var view = View();
            view.Characters.Add(new CharacterView { Id = "owner", Location = "moria" });
            var choice = new CharacterActionOption { CharacterId = "owner", AbilityId = "ability", Dice = { "cost" }, Targets = { "moria" }, TargetCount = 1 };
            view.Options.Add(new ActionOption { Kind = CommandKind.UseCharacterAbility, EligibleDice = { "cost" }, CharacterActions = { choice } });
            var draft = new TurnDraft();
            var unavailable = ComponentActions.For(view, draft).Single();
            Assert.AreEqual("moria", unavailable.Region);
            Assert.IsFalse(unavailable.Enabled);
            Assert.IsFalse(ComponentActions.Select(view, draft, unavailable));
            draft.Change(p => p.DieId = "cost");
            var selected = ComponentActions.For(view, draft).Single();
            choice.Targets[0] = "hollin";
            Assert.IsTrue(ComponentActions.Select(view, draft, selected));
            Assert.AreEqual("cost", draft.Payload.DieId);
            CollectionAssert.AreEqual(new[] { "hollin" }, draft.Payload.Ids);
        }

        [Test]
        public void PassAndDiscardWithoutAnOwnerShowAnActionIcon()
        {
            var view = View();
            view.Options.Add(new ActionOption { Kind = CommandKind.Pass });
            view.Options.Add(new ActionOption { Kind = CommandKind.DiscardDie, EligibleDice = { "cost" } });
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "cost");
            var actions = ComponentActions.For(view, draft).ToDictionary(a => a.Key);
            Assert.AreEqual("pass", actions["Pass"].Icon);
            Assert.AreEqual("discard", actions["DiscardDie"].Icon);
            var buttons = new ComponentActionView();
            buttons.Apply(actions.Values, true);
            foreach (var key in new[] { "Pass", "DiscardDie" })
            {
                var button = buttons.Root.Q<Button>("component-" + key);
                Assert.IsTrue(button.ClassListContains(IconButton.ClassName), key);
                Assert.AreEqual("", button.text, key);
                Assert.AreEqual(actions[key].Icon, IconButton.Icon(button), key);
                StringAssert.StartsWith(actions[key].Detail, button.tooltip, key);
            }
        }

        [Test]
        public void SpecialTravelBindsItsSymbolAndKeepsUnrelatedFiguresUnavailable()
        {
            var view = View();
            view.Dice.Add(new DieView { Id = "cost", Owner = Side.Shadow, Face = DieFace.UseBalrog, Location = DieLocation.Available });
            view.Characters.Add(new CharacterView { Id = "balrog", Location = "moria", Side = Side.Shadow });
            var option = new ActionOption { Kind = CommandKind.MoveShadowCharacters, EligibleDice = { "cost" }, EligibleIds = { "balrog", "nazgul" } };
            option.MovementCosts.Add(new MovementCostOption { DieId = "cost", Face = DieFace.Character, AbilityId = "use-balrog", RequiredCharacterId = "balrog" });
            option.Travel.Add(new TravelOption { Id = "balrog", Origin = "moria", Range = 2, Targets = { "moria", "hollin" } });
            option.Travel.Add(new TravelOption { Id = "nazgul", Origin = "moria", Flight = true, Targets = { "moria", "hollin" } });
            view.Options.Add(option);
            var draft = new TurnDraft();
            draft.Change(p => p.DieId = "cost");
            Assert.IsTrue(ComponentActions.Select(view, draft, ComponentActions.For(view, draft).Single()));
            var context = new ActionDieContext();
            context.Sync(view, draft.Payload.DieId);
            context.BindCost(view, draft);
            Assert.AreEqual(DieFace.Character, context.Face);
            Assert.IsFalse(TravelActions.CanSelect(view, draft, context, "nazgul"));
            Assert.IsTrue(TravelActions.Select(view, draft, context, "balrog"));
            Assert.AreEqual("use-balrog", draft.Payload.AbilityId);
            Assert.AreEqual("cost", draft.Payload.DieId);
            Assert.IsTrue(draft.Payload.Stepwise);
        }

        [Test]
        public void CouncilDropChecksThatPortraitsOwnHomelandsAndKeepsTheOtherPlacements()
        {
            var view = View();
            view.Decision.Kind = DecisionKind.CouncilHomelands;
            view.Options.Add(new ActionOption
            {
                Kind = CommandKind.ChooseCouncilHomelands,
                CharacterActions = { new CharacterActionOption { CharacterId = "gimli", Targets = { "ered-luin" } } }
            });
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseCouncilHomelands);
            var portrait = new DraftToken(DraftObject.CouncilPortrait, "gimli");
            Assert.IsFalse(DraftGesture.Drop(draft, view, portrait, new DraftToken(DraftObject.Region, "moria")));
            Assert.IsEmpty(draft.Payload.Placements);
            Assert.IsTrue(DraftGesture.Drop(draft, view, portrait, new DraftToken(DraftObject.Region, "ered-luin")));
            Assert.AreEqual("ered-luin", draft.Payload.Placements.Single().RegionId);
            Assert.IsFalse(ComponentActions.Immediate(view, draft), "Council setup is one reviewed set.");
        }

        [Test]
        public void FarSightedSeparationRetainsTheTokenCostWithoutARegularFellowshipOption()
        {
            var view = View();
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Dice.Add(new DieView { Id = "ruler", Owner = Side.FreePeoples, Source = "ruler", Face = DieFace.Muster, Location = DieLocation.Available });
            var option = new ActionOption
            {
                Kind = CommandKind.SeparateCompanions, EligibleIds = { "legolas" }, EligibleDice = { "ruler" },
                RulerTravel = new RulerTravelOption
                {
                    AbilityId = "far-sighted", Dice = { "ruler" },
                    Travel = { new TravelOption { Id = "legolas", Origin = "rivendell", Range = 3, Targets = { "rivendell", "hollin" } } }
                }
            };
            view.RulerTokenActions.Add(new RulerTokenActionOption { TokenId = "token", AbilityId = "far-sighted", Action = option });
            var draft = new TurnDraft();
            draft.Change(p => { p.DieId = "ruler"; p.RulerTokenId = "token"; });
            Assert.IsTrue(ComponentActions.Select(view, draft, ComponentActions.For(view, draft).Single()));
            var context = new ActionDieContext();
            context.Sync(view, draft.Payload.DieId);
            context.BindCost(view, draft);
            Assert.IsTrue(FellowshipActions.Portrait(view, draft, context, "legolas"));
            Assert.AreEqual("ruler", draft.Payload.DieId);
            Assert.AreEqual("token", draft.Payload.RulerTokenId);
            Assert.AreEqual("far-sighted", draft.Payload.AbilityId);
            CollectionAssert.AreEqual(new[] { "legolas" }, draft.Payload.Ids);
        }

        private static PlayerView View() => new PlayerView
        {
            MatchId = "component", You = Side.Shadow, DecisionOwner = Side.Shadow,
            Decision = new DecisionView { Id = "action", Kind = DecisionKind.Action }
        };
    }
}
