using System.Linq;
using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    public sealed class BoardCompletionContextTests
    {
        private const string Grey = "base.character.gandalf-grey";
        private const string White = "base.character.gandalf-white";

        [Test]
        public void TransformationNamesBothRecordedVersionsWithoutPretendingTheFigureMoved()
        {
            var step = Transformation();
            var lines = ActionFactText.Lines(step, BoardPresentation.Load()).ToArray();
            CollectionAssert.Contains(lines, "Gandalf the Grey · Base → Gandalf the White · Base · Minas Tirith");
            Assert.IsFalse(lines.Any(line => line.Contains("Minas Tirith → Minas Tirith")));
            Assert.AreEqual(Grey, step.Facts.Figures[0].Before.DefinitionId);
            Assert.AreEqual(White, step.Facts.Figures[0].After.DefinitionId);
        }

        [Test]
        public void AnUnrecordedPreviousVersionIsNotReconstructedFromTheStableFigureId()
        {
            var step = Transformation();
            step.Facts.Figures[0].Before = null;
            var text = string.Join("\n", ActionFactText.Lines(step, BoardPresentation.Load()));
            StringAssert.Contains("Gandalf the White · Base", text);
            StringAssert.DoesNotContain("Gandalf the Grey", text);
        }

        [Test]
        public void PrivateSetupNamesExplicitVersionsEvenWhenFellowshipFactsAlreadyExist()
        {
            var step = new ActionLogStep { Command = CommandKind.ChooseCompanionVersions, Facts = new ActionFacts
            {
                Targets = { "lords.character.gandalf-grey", "fate.character.gimli-dwarven-lord" },
                FellowshipAfter = new HuntOutcomeView { GuideId = "base.character.strider", Hidden = true }
            } };
            var lines = ActionFactText.Lines(step, BoardPresentation.Load()).ToArray();
            CollectionAssert.Contains(lines, "Selected version · Gandalf the Grey · Lords");
            CollectionAssert.Contains(lines, "Selected version · Gimli · Fate · Alternate");
            CollectionAssert.Contains(lines, "Guide · Aragorn");
        }

        [TestCase(CommandKind.DrawCorruptionTile)]
        [TestCase(CommandKind.ChooseCorruptionTile)]
        [TestCase(CommandKind.RerollBattle)]
        public void OtherTargetsDoNotBecomeCharacterVersionLabels(CommandKind command)
        {
            var step = new ActionLogStep { Command = command, Facts = new ActionFacts { Targets = { "private-target" } } };
            Assert.IsEmpty(ActionFactText.Lines(step, BoardPresentation.Load()));
        }

        [Test]
        public void GuideUsesOnlyTheRecordedVersionAndOtherwiseOnlyTheIdentity()
        {
            var facts = new ActionFacts { FellowshipAfter = new HuntOutcomeView { GuideId = Grey } };
            Assert.AreEqual("Guide · Gandalf", CharacterActionFacts.Guide(facts));
            facts.Figures.Add(new ActionFigureChange { After = Figure("lords.character.gandalf-grey"), To = "fellowship" });
            Assert.AreEqual("Guide · Gandalf the Grey · Lords", CharacterActionFacts.Guide(facts));
            facts.FellowshipAfter.GuideId = "";
            Assert.AreEqual("", CharacterActionFacts.Guide(facts));
        }

        [TestCase(CommandKind.ChooseCompanionVersions)]
        [TestCase(CommandKind.ChooseCouncilHomelands)]
        [TestCase(CommandKind.SelectGuide)]
        public void SetupAndGuideUseFellowshipRatherThanTheObserversCharacterRoster(CommandKind command)
        {
            var step = new ActionLogStep { Id = "setup", Actor = Side.FreePeoples, Command = command,
                Context = new ActionContext { Kind = ActionContextKind.Characters } };
            Assert.AreEqual(InGameActionHost.Fellowship, ActionContextModel.For(Record(step), step.Id).InGame.Host);
        }

        [Test]
        public void PublicRevealUsesTheRecordedLocationAndDoesNotNeedAPrivateSetupStep()
        {
            var step = new ActionLogStep { Id = "reveal", Kind = "CompanionRevealed", Automatic = true,
                Context = new ActionContext { Kind = ActionContextKind.Characters },
                Facts = new ActionFacts { Figures = { new ActionFigureChange { After = Figure(Grey), To = "fellowship" } } } };
            Assert.AreEqual(InGameActionHost.Fellowship, ActionContextModel.For(Record(step), step.Id).InGame.Host);
            Assert.IsNull(step.Actor, "A public reveal is still an automatic consequence, not an enemy action.");
            step.Facts.Figures[0].To = "minas-tirith";
            Assert.AreEqual(InGameActionHost.Default, ActionContextModel.For(Record(step), step.Id).InGame.Host);
        }

        [Test]
        public void GuideInsideHuntOrBattleKeepsItsExistingHistoricalHost()
        {
            var step = new ActionLogStep { Id = "guide", Command = CommandKind.SelectGuide,
                Context = new ActionContext { Kind = ActionContextKind.Fellowship, HuntChainId = "hunt-1" } };
            Assert.AreEqual(InGameActionHost.Default, ActionContextModel.For(Record(step), step.Id).InGame.Host);
            Assert.AreEqual(ActionContextKind.Hunt, ActionContextModel.For(Record(step), step.Id).Kind);
            step.Context.HuntChainId = "";
            step.Context.BattleId = "battle-1";
            Assert.AreEqual(InGameActionHost.Default, ActionContextModel.For(Record(step), step.Id).InGame.Host);
            Assert.AreEqual(ActionContextKind.Battle, ActionContextModel.For(Record(step), step.Id).Kind);
        }

        [Test]
        public void ASelectedOldCharacterStepDoesNotBorrowANewerGuideOrVersion()
        {
            var step = Transformation();
            step.Id = "old";
            step.Revision = 8;
            var record = Record(step);
            record.Steps.Add(new ActionLogStep { Id = "new", Revision = 9, Command = CommandKind.SelectGuide,
                Actor = Side.FreePeoples, Facts = new ActionFacts { FellowshipAfter = new HuntOutcomeView { GuideId = Grey } } });
            Assert.AreEqual(InGameActionHost.Default, ActionContextModel.For(record, "old").InGame.Host);
            var through = ActionVersion.Through(record, "old");
            Assert.AreEqual(1, through.Steps.Count);
            StringAssert.DoesNotContain("Guide", string.Join("\n", ActionFactText.Lines(through.Steps[0], BoardPresentation.Load())));
        }

        private static ActionLogStep Transformation() => new ActionLogStep { Command = CommandKind.EnterCharacter,
            Context = new ActionContext { Kind = ActionContextKind.Characters }, Facts = new ActionFacts
            {
                Figures = { new ActionFigureChange { Before = Figure(Grey), After = Figure(White),
                    From = "minas-tirith", To = "minas-tirith" } }
            } };

        private static ActionParticipant Figure(string version) => new ActionParticipant
            { Id = Grey, DefinitionId = version, Side = Side.FreePeoples };

        private static ActionLogView Record(ActionLogStep step) => new ActionLogView
            { Action = new ActionLogSummary { Context = step.Context, Initiator = step.Actor }, Steps = { step } };
    }
}
