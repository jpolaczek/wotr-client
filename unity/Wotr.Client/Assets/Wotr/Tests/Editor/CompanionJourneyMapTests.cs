using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-294 (owner's decision, after UI-284): a card's journey of companions outside the Fellowship is made on the
    // map; the window lists nothing and the console's ✓ finishes it.
    public sealed class CompanionJourneyMapTests
    {
        private const string Merry = "base.character.merry";
        private const string Strider = "base.character.strider";

        [Test]
        public void TheJourneyIsMadeOnTheMapAndTheWindowListsNothing()
        {
            var view = Journey(out var option);
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);

            Assert.IsTrue(CompanionJourneyDraft.Active(view, draft));
            Assert.IsTrue(CompanionJourneyDraft.OnMap(view));
            Assert.AreEqual(DecisionHost.Map, DecisionRoutes.Of(view));
            Assert.IsFalse(FellowshipActions.UsesPanel(view, draft), "The Fellowship panel does not host it.");
            Assert.IsTrue(CardDecisions.Owns(view, draft), "The console's ✓ finishes it.");
            var root = new VisualElement();
            var tray = new DecisionTray(root);
            tray.Render(view, draft, false, false);
            Assert.IsFalse(tray.HasChoices, "No companions, groups, Stay all or Stay here in a window.");
            foreach (var name in new[] { "companion-" + Merry, "travel-stay-all", "travel-stay" })
                Assert.IsNull(root.Q<Button>(name), name);
            StringAssert.StartsWith("Click a lit companion", CompanionJourneyDraft.Hint(view, draft));
            StringAssert.EndsWith("moving none is allowed.", CompanionJourneyDraft.Hint(view, draft));
        }

        [Test]
        public void ARoutedCompanionStandsFaintlyAtItsDestination()
        {
            var view = Journey(out var option);
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, option.Kind);
            TravelDraft.ToggleCompanion(draft, view, option, Merry);
            StringAssert.StartsWith("Click a highlighted destination", CompanionJourneyDraft.Hint(view, draft));
            Assert.IsTrue(TravelDraft.RouteTo(draft, view, BoardPresentation.Load(), "lorien"));

            var planned = CompanionJourneyDraft.Planned(view, draft);
            CollectionAssert.AreEqual(new[] { "lorien" }, planned.Keys.ToArray());
            Assert.AreEqual(1, planned["lorien"].Characters);
            Assert.AreEqual(CharacterLibrary.Name(Merry), planned["lorien"].CharacterNames.Single());

            TravelDraft.Stay(draft, option);
            Assert.IsEmpty(CompanionJourneyDraft.Planned(view, draft), "A companion that stays is not drawn elsewhere.");
        }

        [Test]
        public void GwaihirsSeparationStaysWithTheFellowshipPanel()
        {
            var view = Journey(out _);
            view.Options.Insert(0, new ActionOption { Kind = CommandKind.SeparateCompanions, Enabled = true, EligibleIds = { "base.character.legolas" } });
            var draft = new TurnDraft();
            DraftGesture.Choose(draft, view, CommandKind.SeparateCompanions);
            Assert.IsFalse(CompanionJourneyDraft.OnMap(view));
            Assert.IsFalse(CompanionJourneyDraft.Active(view, draft));
            Assert.AreEqual(DecisionHost.Fellowship, DecisionRoutes.Of(view));
            Assert.IsTrue(FellowshipActions.UsesPanel(view, draft));
        }

        private static PlayerView Journey(out ActionOption option)
        {
            var view = new PlayerView
            {
                MatchId = "journey",
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "book", Kind = DecisionKind.CompanionSeparation, SourceId = "base.fp.strategy.04" },
                Fellowship = new FellowshipView { RegionId = "rivendell", Guide = "base.character.gandalf-grey" }
            };
            option = new ActionOption { Kind = CommandKind.MoveCompanions, Enabled = true, MaximumGroups = 0, AllowEmptySelection = true };
            foreach (var id in new[] { Merry, Strider })
            {
                option.EligibleIds.Add(id);
                option.Travel.Add(new TravelOption { Id = id, Origin = "moria", Range = 3, Targets = { "moria", "lorien" } });
                view.Characters.Add(new CharacterView { Id = id, DefinitionId = id, Side = Side.FreePeoples, Location = "moria" });
            }
            view.Options.Add(option);
            return view;
        }
    }
}
