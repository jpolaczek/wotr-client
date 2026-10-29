using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-420 (owner): tooltips join only parts with text, read units and nations as printed, and a ✓ says what it confirms.
    public sealed class TooltipTextTests
    {
        private const string Card = "base.fp.strategy.08";

        [Test]
        public void PartsAndLinesLeaveOutEmptyOnes()
        {
            Assert.AreEqual("Pass · Keep your dice", TooltipText.Parts("Pass", "", " Keep your dice ", null));
            Assert.AreEqual("Title\nDetail", TooltipText.Lines("Title", "", "\nDetail\n"));
        }

        [Test]
        public void UnitsNationsAndCountsReadAsPrinted()
        {
            Assert.AreEqual("Nazgûl", TooltipText.Unit("sauron", UnitKind.Nazgul));
            Assert.AreEqual("Rohan Elite", TooltipText.Unit("rohan", UnitKind.Elite));
            Assert.AreEqual("Southrons & Easterlings", TooltipText.Nation("southrons"));
            Assert.AreEqual("1 card", TooltipText.Count(1, "card"));
            Assert.AreEqual("3 cards", TooltipText.Count(3, "card"));
        }

        [Test]
        public void ACardChoiceConfirmNamesTheCardAndTheChosenOption()
        {
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Enabled = true, Minimum = 1, Maximum = 1 };
            option.EffectChoices.Add(new EffectChoiceView { Id = "inspect", Title = "Inspect Shadow hand" });
            option.EligibleIds.Add("inspect");
            var view = new PlayerView
            {
                You = Side.FreePeoples,
                DecisionOwner = Side.FreePeoples,
                Decision = new DecisionView { Id = "card", Kind = DecisionKind.EffectChoice, SourceId = Card },
                Options = { option }
            };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.ChooseEffect);
            draft.Change(p => p.Ids.Add("inspect"));
            Assert.AreEqual("Confirm · " + CardLibrary.Title(Card) + ": Inspect Shadow hand",
                ConfirmSummary.Tooltip(view, draft, "Confirm choice."));
        }

        [Test]
        public void ArmyMovesAndRecruitsNameTheirFiguresAndRegions()
        {
            var view = new PlayerView { Units = { new UnitView { Id = "u1", Nation = "rohan", Kind = UnitKind.Elite } } };
            var draft = new TurnDraft();
            draft.Choose(CommandKind.MoveArmies);
            draft.Change(p => p.Moves.Add(new ArmyMoveChoice { From = "rohan", To = "fangorn", UnitIds = { "a", "b", "c" } }));
            draft.Change(p => p.Placements.Add(new UnitPlacementChoice { UnitId = "u1", RegionId = "edoras" }));
            Assert.AreEqual("Rohan → Fangorn (3 figures)", ConfirmSummary.Moves(draft, null));
            Assert.AreEqual("Rohan Elite → Edoras", ConfirmSummary.Recruits(view, draft, null));
        }
    }
}
