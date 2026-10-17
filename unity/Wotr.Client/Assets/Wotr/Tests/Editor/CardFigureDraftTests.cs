using System.Linq;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-215: a card's figure choice is made in the region panel: lit regions and figures, a toggle per click, a single
    // figure sent with its click, and no decision window.
    public sealed class CardFigureDraftTests
    {
        private static UnitView Unit(string id, string region, Side side = Side.Shadow, UnitKind kind = UnitKind.Regular) =>
            new UnitView { Id = id, Nation = side == Side.Shadow ? "sauron" : "gondor", Kind = kind, Side = side, Location = region };

        private static PlayerView Card(string presentation, int minimum, int maximum, params EffectChoiceView[] choices)
        {
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = minimum, Maximum = maximum };
            foreach (var choice in choices)
            {
                option.EffectChoices.Add(choice);
                option.EligibleIds.Add(choice.Id);
            }
            return new PlayerView
            {
                You = Side.Shadow,
                DecisionOwner = Side.Shadow,
                Decision = new DecisionView { Id = presentation, Kind = DecisionKind.EffectChoice, Presentation = presentation },
                Units = { Unit("moria-1", "moria"), Unit("orthanc-1", "orthanc"), Unit("orthanc-2", "orthanc") },
                Options = { option }
            };
        }

        private static EffectChoiceView Replacement(string id, string region) => new EffectChoiceView
        {
            Id = id, TargetId = region, Title = "Regular → Elite", SelectionGroup = "Replacements", GroupMinimum = 2, GroupMaximum = 2
        };

        private static TurnDraft Draft(CommandKind kind)
        {
            var draft = new TurnDraft();
            draft.Choose(kind);
            return draft;
        }

        private static bool WindowChoices(PlayerView view, TurnDraft draft)
        {
            var tray = new DecisionTray(new VisualElement());
            tray.Render(view, draft, false, false);
            return tray.HasChoices;
        }

        [Test]
        public void HillTrollsLightTheirRegionsAndKeepTwoChoicesAcrossThem()
        {
            var view = Card("ArmyFigures", 2, 2, Replacement("moria-1", "moria"), Replacement("orthanc-1", "orthanc"), Replacement("orthanc-2", "orthanc"));
            var draft = Draft(CommandKind.ChooseEffect);
            Assert.IsTrue(CardFigureDraft.Active(view, draft));
            CollectionAssert.AreEquivalent(new[] { "moria", "orthanc" }, CardFigureDraft.Sources(view, draft).Select(s => s.RegionId));
            Assert.AreEqual(Side.Shadow, CardFigureDraft.FigureSide(view, draft));
            Assert.IsFalse(CardFigureDraft.CompletesOnClick(view, draft));
            Assert.IsTrue(CardFigureDraft.Toggle(view, draft, "moria-1"));
            Assert.IsTrue(CardFigureDraft.Toggle(view, draft, "orthanc-1"));
            CollectionAssert.AreEquivalent(new[] { "moria-1", "orthanc-1" }, draft.Payload.Ids);
            CollectionAssert.AreEquivalent(new[] { "moria-1", "orthanc-1" }, CardFigureDraft.Eligible(view, draft), "The limit is reached; chosen figures stay toggleable.");
            Assert.IsFalse(CardFigureDraft.Toggle(view, draft, "orthanc-2"));
            Assert.IsTrue(CardFigureDraft.Toggle(view, draft, "orthanc-1"), "A click on a chosen figure releases it.");
            Assert.AreEqual(1, draft.Payload.Ids.Count);
            Assert.IsFalse(WindowChoices(view, draft));
        }

        [Test]
        public void ASingleFigureChoiceIsCompleteWithItsClick()
        {
            var view = Card("ArmyFigure", 1, 1, new EffectChoiceView { Id = "moria-1", TargetId = "moria" });
            var draft = Draft(CommandKind.ChooseEffect);
            Assert.IsTrue(CardFigureDraft.CompletesOnClick(view, draft));
            Assert.IsTrue(CardFigureDraft.Toggle(view, draft, "moria-1"));
            Assert.AreEqual("moria", draft.Payload.RegionId);
            Assert.IsTrue(EffectChoiceDraft.Ready(draft, view.Options[0]));
        }

        [Test]
        public void ExcessOutsideABattleIsChosenUpToItsCountInTheRegionPanel()
        {
            var view = Card("", 0, 0);
            view.Decision = new DecisionView { Id = "excess", Kind = DecisionKind.RemoveExcess };
            view.Options[0] = new ActionOption { Kind = CommandKind.RemoveExcess, Enabled = true, Minimum = 1, Maximum = 1, EligibleIds = { "orthanc-1", "orthanc-2" } };
            var draft = Draft(CommandKind.RemoveExcess);
            Assert.IsTrue(CardFigureDraft.Active(view, draft));
            Assert.IsTrue(CardFigureDraft.CompletesOnClick(view, draft));
            Assert.IsTrue(CardFigureDraft.Toggle(view, draft, "orthanc-2"));
            CollectionAssert.AreEqual(new[] { "orthanc-2" }, CardFigureDraft.Eligible(view, draft));
            Assert.IsFalse(WindowChoices(view, draft));
            view.Battle = new BattleView();
            Assert.IsFalse(CardFigureDraft.Active(view, draft), "Excess after a battle stays in the battle window.");
        }

        [Test]
        public void DeadMenOfferTheirCompanionsAsFiguresAndTheirDestinationsOnTheMap()
        {
            var view = Card("CompanionDestination", 1, 3,
                new EffectChoiceView { Id = "erech", TargetId = "erech", SelectionGroup = "destination", GroupMinimum = 1, GroupMaximum = 1 },
                new EffectChoiceView { Id = "gimli", SourceId = "base.character.gimli", SelectionGroup = "companions", GroupMaximum = 2 },
                new EffectChoiceView { Id = "legolas", SourceId = "base.character.legolas", SelectionGroup = "companions", GroupMaximum = 2 });
            view.You = Side.FreePeoples;
            view.DecisionOwner = Side.FreePeoples;
            view.Characters.Add(new CharacterView { Id = "gimli", Side = Side.FreePeoples, Location = "edoras" });
            view.Characters.Add(new CharacterView { Id = "legolas", Side = Side.FreePeoples, Location = "edoras" });
            var draft = Draft(CommandKind.ChooseEffect);
            CollectionAssert.AreEquivalent(new[] { "gimli", "legolas" }, CardFigureDraft.Offered(view, draft));
            Assert.IsFalse(CardFigureDraft.CompletesOnClick(view, draft), "The journey still needs its destination.");
            Assert.IsTrue(CardFigureDraft.Toggle(view, draft, "gimli"));
            Assert.AreEqual("", draft.Payload.RegionId, "A companion is no region choice.");
            Assert.IsFalse(WindowChoices(view, draft));
        }
    }
}
