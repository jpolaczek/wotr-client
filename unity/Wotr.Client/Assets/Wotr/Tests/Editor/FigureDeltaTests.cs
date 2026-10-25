using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-307: what a delta badge says about a region's recorded figure changes — unit, nation and signed amount; an
    // upgrade or a new version as one replacement, never an extra figure.
    public sealed class FigureDeltaTests
    {
        private static readonly BoardPresentation Data = BoardPresentation.Load();

        [Test]
        public void RecruitsOfTwoNationsInOneRegionAreOneEntryEach()
        {
            var cue = Single(PresentationCueKind.Recruit,
                Recruit("s1", "sauron", UnitKind.Regular), Recruit("s2", "sauron", UnitKind.Regular), Recruit("s3", "sauron", UnitKind.Regular),
                Recruit("i1", "isengard", UnitKind.Elite));
            var deltas = FigureChangePresenter.Deltas(cue).ToList();
            CollectionAssert.AreEquivalent(new[] { "sauron:+3", "isengard:+1" }, deltas.Select(d => d.Nation + ":" + d.Text));
            Assert.IsTrue(deltas.All(d => !d.From.HasValue));
            Assert.AreNotEqual(deltas[0].Art, deltas[1].Art, "A Regular and an Elite show different figures.");
        }

        [Test]
        public void AnUpgradeIsOneReplacementAndEachRecordedPairIsItsOwn()
        {
            var one = Single(PresentationCueKind.Upgrade, Leave("r1", "dol-guldur", "reserve", UnitKind.Regular), Enter("e1", UnitKind.Elite));
            var upgrade = FigureChangePresenter.Deltas(one).Single();
            Assert.IsTrue(upgrade.From.HasValue, "Regular → Elite.");
            Assert.AreEqual("", upgrade.Text, "A single replacement needs no number.");
            Assert.AreEqual("sauron", upgrade.Nation);
            var two = Cues(Leave("r1", "dol-guldur", "reserve", UnitKind.Regular), Enter("e1", UnitKind.Elite),
                Leave("r2", "dol-guldur", "reserve", UnitKind.Regular), Enter("e2", UnitKind.Elite))
                .Where(cue => cue.Kind == PresentationCueKind.Upgrade).ToList();
            Assert.AreEqual(2, two.SelectMany(cue => cue.Figures).Count(change => change.After?.UnitKind == UnitKind.Elite),
                "Both pairs are upgrades, none an extra Elite.");
        }

        [Test]
        public void ARemovalCountsDownAndATransformationReplacesTheVersion()
        {
            var removal = Single(PresentationCueKind.Removal, Leave("r1", "dol-guldur", "reserve", UnitKind.Regular));
            Assert.AreEqual("−1", FigureChangePresenter.Deltas(removal).Single().Text);
            var white = new ActionFigureChange
            {
                Before = new ActionParticipant { Id = "gandalf", Side = Side.FreePeoples, DefinitionId = "base.character.gandalf-grey" },
                After = new ActionParticipant { Id = "gandalf", Side = Side.FreePeoples, DefinitionId = "base.character.gandalf-white" },
                From = "fangorn", To = "fangorn"
            };
            var transformation = Single(PresentationCueKind.Transformation, white);
            var delta = FigureChangePresenter.Deltas(transformation).Single();
            Assert.IsTrue(delta.From.HasValue);
            Assert.AreNotEqual(delta.From.Value, delta.Art);
        }

        private static ActionFigureChange Recruit(string id, string nation, UnitKind kind)
        {
            var figure = new ActionParticipant { Id = id, Side = Side.Shadow, NationId = nation, UnitKind = kind };
            return new ActionFigureChange { Before = figure, After = figure, From = "reserve", To = "dol-guldur" };
        }

        private static ActionFigureChange Leave(string id, string from, string to, UnitKind kind)
        {
            var figure = new ActionParticipant { Id = id, Side = Side.Shadow, NationId = "sauron", UnitKind = kind };
            return new ActionFigureChange { Before = figure, After = figure, From = from, To = to };
        }

        private static ActionFigureChange Enter(string id, UnitKind kind)
        {
            var figure = new ActionParticipant { Id = id, Side = Side.Shadow, NationId = "sauron", UnitKind = kind };
            return new ActionFigureChange { Before = null, After = figure, From = "reserve", To = "dol-guldur" };
        }

        private static PresentationCue Single(PresentationCueKind kind, params ActionFigureChange[] changes) =>
            Cues(changes).Single(cue => cue.Kind == kind);

        // The cues the UI-303 model makes of one recorded step of the Shadow.
        private static IReadOnlyList<PresentationCue> Cues(params ActionFigureChange[] changes)
        {
            var step = new ActionLogStep { Id = "step", ActionId = "action", Revision = 1, Order = 1, Actor = Side.Shadow, Kind = "Recruited" };
            step.Facts.Figures.AddRange(changes);
            var model = new OpponentPresentationModel(Side.FreePeoples, Data);
            model.Baseline(new ActionLogPage[0], 0);
            model.Advance(1);
            return model.Merge(new[] { new ActionLogPage { Steps = { step } } });
        }
    }
}
