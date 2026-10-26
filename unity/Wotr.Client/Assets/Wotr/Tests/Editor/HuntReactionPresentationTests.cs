using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.Tests
{
    public sealed class HuntReactionPresentationTests
    {
        [Test]
        public void ReactionShowsItsSourceEffectAndCostBeforeSelectionAndKeepsTheSameNodes()
        {
            var choice = new EffectChoiceView { Id = "card.mithril-replace", SourceId = "base.fp.character.05",
                Cost = "DiscardTableCard", Title = "Replace tile", Detail = "Draw a replacement before determining damage." };
            var view = View(choice);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RespondEffect);
            var root = new VisualElement();
            var choices = new CardChoices(root, view, draft, change => change(), BoardPresentation.Load());
            var button = root.Q<Button>("effect-choice-" + choice.Id);
            var detail = button.Q<Label>("hunt-reaction-detail");
            Assert.AreEqual(CardLibrary.Title(choice.SourceId), button.Q<Label>("hunt-reaction-source").text);
            Assert.AreEqual("Replace tile", button.Q<Label>("hunt-reaction-title").text);
            Assert.AreEqual(choice.Detail, detail.text);
            Assert.AreEqual("Discard this card", button.Q<Label>("hunt-reaction-cost").text);
            Assert.IsEmpty(draft.Payload.ReactionId);
            draft.Change(p => p.ReactionId = choice.Id);
            choices.Refresh();
            Assert.AreSame(detail, root.Q<Button>(button.name).Q<Label>(detail.name));
            Assert.IsTrue(button.ClassListContains("selected"));
        }

        [Test]
        public void AbilityInspectionUsesTheProjectedFateVersionAndNeverInventsAnUnrevealedVersion()
        {
            var choice = new EffectChoiceView { Id = "fate.gimli-guide", SourceId = "base.character.gimli",
                Cost = "None", Title = "Separate · Reduce damage", Detail = "Separate this Companion to reduce damage by one." };
            var view = View(choice);
            view.OwnCompanions.Add(new CharacterView { Id = choice.SourceId, DefinitionId = "fate.character.gimli-dwarven-lord", Level = 1 });
            var inspection = HuntReactionPresentation.Inspect(view, choice);
            StringAssert.Contains("Level 1", inspection);
            StringAssert.Contains("Guide: you may separate", inspection);
            StringAssert.Contains("Use ability · No die", inspection);
            view.You = Side.Shadow;
            view.OwnCompanions.Clear();
            inspection = HuntReactionPresentation.Inspect(view, choice);
            StringAssert.DoesNotContain("Level", inspection);
            StringAssert.DoesNotContain("Dwarven Lord", inspection);
            StringAssert.DoesNotContain("Guide:", inspection);
        }

        [Test]
        public void CompactReactionFooterStillShowsErrorsAndPreview()
        {
            var choice = new EffectChoiceView { Id = "card.mithril-replace", SourceId = "base.fp.character.05" };
            var view = View(choice);
            var draft = new TurnDraft();
            draft.Choose(CommandKind.RespondEffect);
            var root = new VisualElement();
            var tray = new DecisionTray(root) { ContextualHunt = true };
            tray.Render(view, draft, false, false);
            var status = root.Q<Label>(className: "draft-status");
            Assert.AreEqual(DisplayStyle.None, status.style.display.value);
            tray.Preview(false, "This decision has changed.");
            Assert.AreEqual(DisplayStyle.Flex, status.style.display.value);
            Assert.AreEqual("This decision has changed.", status.text);
            tray.Preview(true, "Discard this card.");
            Assert.AreEqual(DisplayStyle.Flex, status.style.display.value);
            Assert.AreEqual("Discard this card.", status.text);
        }

        [Test]
        public void NenyaAndReusableTableReactionsKeepTheirActualCosts()
        {
            var nenya = new EffectChoiceView { Cost = "TransferNenya", SourceId = "lords.character.galadriel" };
            Assert.AreEqual("Give Nenya to Shadow", HuntReactionPresentation.Cost(nenya));
            var worn = new EffectChoiceView { Cost = "None", SourceId = "base.sh.character.15" };
            Assert.AreEqual("Keep this card · No die", HuntReactionPresentation.Cost(worn));
        }

        private static PlayerView View(EffectChoiceView choice) => new PlayerView
        {
            You = Side.FreePeoples,
            DecisionOwner = Side.FreePeoples,
            Decision = new DecisionView { Id = "reaction", Kind = DecisionKind.Reaction },
            Hunt = new HuntView { TileId = "base.hunt.standard.3.0.1", DamagePending = true },
            Options = { new ActionOption { Kind = CommandKind.RespondEffect, EligibleIds = { choice.Id }, EffectChoices = { choice } } }
        };
    }
}
