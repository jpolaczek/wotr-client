using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.EditorTests
{
    // UI-303: the opponent presentation model on history recorded from the real engine (wotr-server
    // OpponentActionFixtureRecorder: OpponentActions/*.json, the authorized All feed of each viewer, one page per revision).
    public sealed class OpponentPresentationModelTests
    {
        private static readonly BoardPresentation Data = BoardPresentation.Load();

        [Test]
        public void TwoMarchesShareOneCostAndKeepTheirRealCompositions()
        {
            var cues = Replay(Fixture("march-free-peoples"), out var model);
            Assert.AreEqual(1, cues.Count(cue => cue.Kind == PresentationCueKind.Cost));
            Assert.AreEqual(DieFace.Army, cues.Single(cue => cue.Kind == PresentationCueKind.Cost).Cost.Use);
            var marches = cues.Where(cue => cue.Kind == PresentationCueKind.March).ToList();
            Assert.AreEqual(2, marches.Count);
            Assert.AreEqual(("dol-guldur", "eastern-brown-lands", 4), (marches[0].Movement.From, marches[0].Movement.To, marches[0].Movement.Participants.Count));
            Assert.AreEqual(3, marches[0].Movement.Participants.Count(p => p.UnitKind == UnitKind.Regular), "The rear guard stays behind.");
            Assert.AreEqual(("moria", "hollin", 2), (marches[1].Movement.From, marches[1].Movement.To, marches[1].Movement.Participants.Count));
            Assert.IsFalse(cues.Any(cue => cue.Kind is PresentationCueKind.Relocation or PresentationCueKind.Recruit or PresentationCueKind.Removal),
                "The board difference of a march is the march itself.");
            Assert.AreEqual(1, model.Actions.Count, "Both marches belong to one action and one source.");
            Assert.AreEqual(Side.Shadow, model.Actions.Values.Single().Initiator);
        }

        [Test]
        public void ACardRecruitingInTwoRegionsShowsTheCardThenEachRegionOnce()
        {
            var cues = Replay(Fixture("card-recruit-free-peoples"), out var model);
            CollectionAssert.AreEqual(new[] { PresentationCueKind.Cost, PresentationCueKind.Card, PresentationCueKind.Recruit, PresentationCueKind.Recruit },
                cues.Select(cue => cue.Kind));
            Assert.AreEqual("base.sh.strategy.20", cues[1].Card.Id);
            Assert.AreEqual(ActionCardUse.Event, cues[1].Card.Use);
            CollectionAssert.AreEquivalent(new[] { "dol-guldur", "mount-gundabad" }, cues.Skip(2).Select(cue => cue.Regions.Single()));
            Assert.IsTrue(cues.Skip(2).All(cue => cue.Figures.Count == 3 && cue.Figures.All(f => f.After.UnitKind == UnitKind.Regular)));
            Assert.AreEqual("base.sh.strategy.20", model.Actions.Values.Single().CardId);
        }

        [Test]
        public void TheAttackKeepsItsAutomaticConsequencesOnceAndHidesCommittedCards()
        {
            var cues = Replay(Fixture("attack-free-peoples"), out _);
            Assert.AreEqual(PresentationCueKind.Cost, cues[0].Kind);
            Assert.AreEqual(PresentationCueKind.Attack, cues[1].Kind);
            CollectionAssert.AreEqual(new[] { "orthanc", "fords-of-isen" }, cues[1].Regions);
            var commits = cues.Where(cue => cue.Kind == PresentationCueKind.Card && cue.Card.Use == ActionCardUse.CombatCommit).ToList();
            Assert.IsTrue(commits.Count > 0 && commits.All(cue => cue.Card.Owner == Side.Shadow && cue.Card.Id.Length == 0),
                "Only the opponent's commit, as a back.");
            var reveals = cues.Where(cue => cue.Kind == PresentationCueKind.Card && cue.Card.Use == ActionCardUse.CombatReveal).Select(cue => cue.Card.Id).ToList();
            CollectionAssert.IsSubsetOf(new[] { "base.sh.strategy.13", "base.fp.strategy.14" }, reveals, "Both reveals are shared results.");
            Assert.AreEqual(reveals.Count, reveals.Distinct().Count());
            Assert.IsTrue(cues.Any(cue => cue.Kind == PresentationCueKind.Battle), "Rolls and hits are battle beats.");
            // A Regular lost in the first round can return from the reserve as a downgraded Elite and be lost again: each
            // loss is its own fact, and none is shown twice.
            var lost = cues.Where(cue => cue.Kind == PresentationCueKind.Losses)
                .SelectMany(cue => cue.Losses.Select(loss => cue.Revision + ":" + loss.Figure.Id)).ToList();
            Assert.AreEqual(6, lost.Count);
            Assert.AreEqual(lost.Count, lost.Distinct().Count(), "A casualty is shown once.");
            var removed = cues.Where(cue => cue.Kind == PresentationCueKind.Removal).SelectMany(cue => cue.Figures).Select(f => f.Before.Id);
            CollectionAssert.IsEmpty(removed.Intersect(lost.Select(id => id.Substring(id.IndexOf(':') + 1))),
                "A battle loss is not also a removal.");
            Assert.AreEqual(cues.Count, cues.Select(cue => cue.Id).Distinct().Count());
        }

        [Test]
        public void OurOwnAttackIsNoAttackSceneButItsSharedResolutionIs()
        {
            var cues = Replay(Fixture("attack-shadow"), out _);
            Assert.IsFalse(cues.Any(cue => cue.Actor == Side.Shadow), "None of our own decisions is replayed.");
            Assert.IsFalse(cues.Any(cue => cue.Kind is PresentationCueKind.Attack or PresentationCueKind.Cost),
                "Our own attack and its die are known to us, even when an automatic step repeats the battle.");
            Assert.IsTrue(cues.Any(cue => cue.Kind == PresentationCueKind.Battle && !cue.Actor.HasValue), "The shared rolls are shown.");
            var commits = cues.Where(cue => cue.Kind == PresentationCueKind.Card && cue.Card.Use == ActionCardUse.CombatCommit).ToList();
            Assert.IsTrue(commits.Count > 0 && commits.All(cue => cue.Card.Owner == Side.FreePeoples && cue.Card.Id.Length == 0));
            Assert.IsTrue(cues.Any(cue => cue.Kind == PresentationCueKind.Losses && !cue.Actor.HasValue &&
                cue.Losses.Any(loss => loss.Figure.Side == Side.FreePeoples)), "The opponent's casualties are a shared result.");
        }

        [Test]
        public void SharedResultsOfOurOwnClickAreMarkedSoTheCameraStaysOurs()
        {
            var shadow = Replay(Fixture("attack-shadow"), out _);
            var free = Replay(Fixture("attack-free-peoples"), out _);
            var advance = shadow.Single(cue => cue.Kind == PresentationCueKind.Advance);
            Assert.IsTrue(advance.OwnCommand, "The Shadow's own advance, recorded on an automatic step.");
            Assert.IsTrue(shadow.Where(cue => cue.Kind == PresentationCueKind.Losses && cue.Revision == 5).All(cue => cue.OwnCommand),
                "The Shadow chose those casualties.");
            Assert.IsTrue(shadow.Where(cue => cue.Revision == 3).All(cue => !cue.OwnCommand), "The Free Peoples' commit caused revision 3.");
            Assert.IsFalse(free.Single(cue => cue.Kind == PresentationCueKind.Advance).OwnCommand, "For the Free Peoples it is the opponent's advance.");
        }

        [Test]
        public void ABattleWithoutItsOpeningSnapshotIsABeatNotALateAttack()
        {
            var fixture = Fixture("attack-free-peoples");
            var model = new OpponentPresentationModel(fixture.Viewer, Data);
            // The battle opened in history recorded before facts existed: its first steps are legacy, without a snapshot.
            var legacy = fixture.Updates.SelectMany(update => update.Page.Steps).Where(step => step.Revision <= 4).ToList();
            foreach (var step in legacy)
            {
                step.Legacy = true;
                step.Facts = new ActionFacts();
            }
            model.Baseline(new[] { new ActionLogPage { Steps = legacy } }, 4);
            var cues = new List<PresentationCue>();
            foreach (var update in fixture.Updates.Where(update => update.ViewRevision > 4))
            {
                cues.AddRange(model.Advance(update.ViewRevision));
                cues.AddRange(model.Merge(new[] { Newer(update.Page, 4) }));
            }
            Assert.IsFalse(cues.Any(cue => cue.Kind == PresentationCueKind.Attack), "The attack happened before this client was there.");
            Assert.IsTrue(cues.Any(cue => cue.Kind == PresentationCueKind.Battle));
            Assert.IsTrue(cues.Any(cue => cue.Kind == PresentationCueKind.Losses));
        }

        private static ActionLogPage Newer(ActionLogPage page, long revision) => new ActionLogPage
        {
            Steps = page.Steps.Where(step => step.Revision > revision).ToList(),
            Actions = page.Actions
        };

        // The two clients of one match: a step both of them show makes the same cues, so a battle beat or a reveal is one
        // public event whichever player watches it, even while one of them already knows a committed card.
        [TestCase("attack")]
        [TestCase("hunt")]
        public void BothPlayersGiveEverySharedStepTheSameCues(string scenario)
        {
            var free = BySource(Replay(Fixture(scenario + "-free-peoples"), out _));
            var shadow = BySource(Replay(Fixture(scenario + "-shadow"), out _));
            var shared = free.Keys.Intersect(shadow.Keys).ToList();
            Assert.Greater(shared.Count, 3);
            foreach (var source in shared)
                CollectionAssert.AreEqual(free[source], shadow[source], source);
            if (scenario == "attack")
                Assert.IsTrue(shared.Any(source => source.EndsWith("/" + PresentationCueKind.Battle)));
        }

        private static Dictionary<string, List<string>> BySource(IEnumerable<PresentationCue> cues) => cues
            .GroupBy(cue => cue.StepId + "/" + cue.Kind)
            .ToDictionary(group => group.Key, group => group.Select(cue => cue.Id).ToList());

        [Test]
        public void TheOpponentsReactionDuringOurMoveIsPresentedOnce()
        {
            var cues = Replay(Fixture("reaction-free-peoples"), out _);
            var removed = cues.Where(cue => cue.Kind == PresentationCueKind.Card && cue.Card.Use == ActionCardUse.TableRemoved).ToList();
            Assert.AreEqual(1, removed.Count, "Flocks of Crebain leaves the table once, though two steps record it.");
            Assert.AreEqual("base.sh.character.16", removed[0].Card.Id);
            Assert.AreEqual(Side.Shadow, removed[0].Actor);
            Assert.IsTrue(cues.Any(cue => cue.Kind == PresentationCueKind.Hunt && cue.Hunt.Kind == HuntReportKind.Rolled));
            Assert.IsFalse(cues.Any(cue => cue.Kind == PresentationCueKind.Cost && cue.Actor == Side.FreePeoples), "Our own die is ours to see.");
        }

        [Test]
        public void ASharedHuntResolvedByOurOwnMoveIsPresented()
        {
            var cues = Replay(Fixture("hunt-free-peoples"), out _);
            var hunt = cues.Where(cue => cue.Kind == PresentationCueKind.Hunt).Select(cue => cue.Hunt.Kind).ToList();
            CollectionAssert.IsSubsetOf(new[] { HuntReportKind.Rolled, HuntReportKind.TileDrawn, HuntReportKind.DamageDetermined }, hunt);
            Assert.AreEqual(hunt.Count, cues.Count(cue => cue.Kind == PresentationCueKind.Hunt));
            Assert.AreEqual(1, hunt.Count(kind => kind == HuntReportKind.HuntCompleted), "The chain's end is recorded twice, shown once.");
            Assert.IsFalse(cues.Any(cue => cue.Actor == Side.FreePeoples), "None of our own decisions is replayed.");
            Assert.IsFalse(cues.Any(cue => cue.Kind is PresentationCueKind.Draw or PresentationCueKind.Card), "Our own card and draws are ours.");
        }

        [Test]
        public void TheOpponentsDrawsAreCountsWithoutIdentities()
        {
            var cues = Replay(Fixture("hunt-shadow"), out _);
            var draws = cues.Where(cue => cue.Kind == PresentationCueKind.Draw).ToList();
            Assert.IsTrue(draws.Count > 0);
            Assert.IsTrue(draws.All(cue => cue.Card.Owner == Side.FreePeoples && cue.Card.Id.Length == 0 && cue.Card.Count > 0));
            Assert.IsFalse(cues.Any(cue => cue.Card != null && cue.Card.Owner == Side.Shadow && cue.Card.Use == ActionCardUse.Draw),
                "Our own draws make no cue.");
            Assert.AreEqual("base.fp.character.06", cues.First(cue => cue.Kind == PresentationCueKind.Card).Card.Id, "The played public card.");
        }

        [Test]
        public void DuplicatedReorderedAndSplitPagesMakeTheSameCues()
        {
            var fixture = Fixture("attack-free-peoples");
            var expected = Replay(fixture, out _).Select(cue => cue.Id).ToList();
            var model = Baseline(fixture);
            var actual = new List<string>();
            foreach (var update in fixture.Updates)
            {
                actual.AddRange(model.Advance(update.ViewRevision).Select(cue => cue.Id));
                var steps = update.Page.Steps.AsEnumerable().Reverse().ToList();
                var first = new ActionLogPage { Steps = steps.Take(steps.Count / 2).ToList(), Actions = update.Page.Actions };
                var second = new ActionLogPage { Steps = steps.Skip(steps.Count / 2).ToList() };
                actual.AddRange(model.Merge(new[] { second, first, update.Page }).Select(cue => cue.Id));
                actual.AddRange(model.Merge(new[] { update.Page }).Select(cue => cue.Id));
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void StepsAheadOfTheShownViewWaitForIt()
        {
            var fixture = Fixture("march-free-peoples");
            var model = Baseline(fixture);
            Assert.IsEmpty(model.Merge(fixture.Updates.Select(update => update.Page)));
            Assert.AreEqual(fixture.Updates.Sum(update => update.Page.Steps.Count), model.Held);
            var first = model.Advance(fixture.Updates[0].ViewRevision);
            Assert.IsTrue(first.Any(cue => cue.Kind == PresentationCueKind.Cost) && first.Count(cue => cue.Kind == PresentationCueKind.March) == 1);
            Assert.AreEqual(1, model.Advance(fixture.Updates[1].ViewRevision).Count(cue => cue.Kind == PresentationCueKind.March));
            Assert.AreEqual(0, model.Held);
        }

        [Test]
        public void AMissingIntermediateViewMakesTheSameCues()
        {
            var fixture = Fixture("attack-free-peoples");
            var expected = Replay(fixture, out _).Select(cue => cue.Id).ToList();
            var model = Baseline(fixture);
            model.Advance(fixture.Updates.Last().ViewRevision);
            CollectionAssert.AreEqual(expected, model.Merge(fixture.Updates.Select(update => update.Page)).Select(cue => cue.Id));
        }

        [Test]
        public void TheBaselineIsRememberedButNeverPlayed()
        {
            var fixture = Fixture("march-free-peoples");
            var model = new OpponentPresentationModel(fixture.Viewer, Data);
            model.Baseline(new[] { fixture.Baseline, fixture.Updates[0].Page }, fixture.Updates[0].ViewRevision);
            model.Advance(fixture.Updates[1].ViewRevision);
            var cues = model.Merge(new[] { fixture.Updates[0].Page, fixture.Updates[1].Page });
            CollectionAssert.AreEqual(new[] { PresentationCueKind.March }, cues.Select(cue => cue.Kind),
                "A reconnect after the first march shows only the second, without the cost again.");
            Assert.AreEqual("hollin", cues[0].Movement.To);
        }

        [Test]
        public void ADeclineCausingAPublicRevealShowsOnlyTheReveal()
        {
            var model = new OpponentPresentationModel(Side.FreePeoples, Data);
            model.Baseline(new ActionLogPage[0], 1);
            var decline = Step("decline", 2, 1, Side.Shadow, "CombatCardCommitted");
            decline.Context.BattleId = "battle";
            decline.Facts.Cards.Add(new ActionCardFact { Owner = Side.Shadow, Use = ActionCardUse.CombatCommit, Deck = "strategy", Count = 0 });
            var reveal = Step("reveal", 2, 2, null, "CombatCardRevealed");
            reveal.Context.BattleId = "battle";
            reveal.Facts.Cards.Add(new ActionCardFact { Owner = Side.FreePeoples, Use = ActionCardUse.CombatReveal, Id = "base.fp.strategy.14" });
            model.Advance(2);
            var cues = model.Merge(new[] { Page(decline, reveal) });
            Assert.AreEqual(1, cues.Count);
            Assert.AreEqual(("reveal", "base.fp.strategy.14"), (cues[0].StepId, cues[0].Card.Id));
        }

        [Test]
        public void AnUpgradeIsOneReplacementAndATransformationReplacesTheFigure()
        {
            var model = new OpponentPresentationModel(Side.FreePeoples, Data);
            model.Baseline(new ActionLogPage[0], 1);
            var step = Step("trolls", 2, 1, Side.Shadow, "EventPlayed");
            step.Facts.Figures.Add(new ActionFigureChange { Before = Unit("r", UnitKind.Regular), From = "gorgoroth", To = "reserve" });
            step.Facts.Figures.Add(new ActionFigureChange { After = Unit("e", UnitKind.Elite), From = "reserve", To = "gorgoroth" });
            step.Facts.Figures.Add(new ActionFigureChange
            {
                Before = new ActionParticipant { Id = "witch", Side = Side.Shadow, DefinitionId = "base.character.witch-king" },
                After = new ActionParticipant { Id = "witch", Side = Side.Shadow, DefinitionId = "base.character.mouth-of-sauron" },
                From = "minas-morgul", To = "minas-morgul"
            });
            model.Advance(2);
            var cues = model.Merge(new[] { Page(step) });
            CollectionAssert.AreEquivalent(new[] { PresentationCueKind.Upgrade, PresentationCueKind.Transformation }, cues.Select(cue => cue.Kind));
            Assert.AreEqual(2, cues.Single(cue => cue.Kind == PresentationCueKind.Upgrade).Figures.Count);
        }

        [Test]
        public void AMinionNewToTheGameEntersAndACompanionRecordedFromTheFellowshipLeavesIt()
        {
            var model = new OpponentPresentationModel(Side.FreePeoples, Data);
            model.Baseline(new ActionLogPage[0], 1);
            var step = Step("entry", 2, 1, Side.Shadow, "CharacterEntered");
            step.Facts.Figures.Add(new ActionFigureChange
            {
                After = new ActionParticipant { Id = "witch", Side = Side.Shadow, DefinitionId = "base.character.witch-king" },
                From = "fellowship", To = "minas-morgul"
            });
            step.Facts.Figures.Add(new ActionFigureChange
            {
                After = new ActionParticipant { Id = "gimli", Side = Side.FreePeoples, DefinitionId = "base.character.gimli" },
                From = "fellowship", To = "moria"
            });
            model.Advance(2);
            var cues = model.Merge(new[] { Page(step) });
            Assert.AreEqual("minas-morgul", cues.Single(cue => cue.Kind == PresentationCueKind.Entry).Regions.Single());
            Assert.AreEqual("moria", cues.Single(cue => cue.Kind == PresentationCueKind.Relocation).Regions.Single());
        }

        [Test]
        public void TheAllFeedGivesThePennantsTheOpponentFeedGave()
        {
            var fixture = Fixture("attack-free-peoples");
            var pages = new[] { fixture.Baseline }.Concat(fixture.Updates.Select(update => update.Page)).ToList();
            var all = Pennants(pages, Side.FreePeoples);
            var opponentOnly = Pennants(pages.Select(page => new ActionLogPage
            {
                Steps = page.Steps.Where(step => step.Actor == Side.Shadow).ToList(),
                Actions = page.Actions
            }), Side.FreePeoples);
            CollectionAssert.AreEqual(opponentOnly, all);
        }

        [Test]
        public void CuesAreCopiesThatLaterPagesCannotChange()
        {
            var fixture = Fixture("march-free-peoples");
            var cues = Replay(fixture, out _);
            fixture.Updates[0].Page.Steps[0].Movements[0].To = "changed";
            Assert.AreEqual("eastern-brown-lands", cues.First(cue => cue.Kind == PresentationCueKind.March).Movement.To);
        }

        private static List<string> Pennants(IEnumerable<ActionLogPage> pages, Side you)
        {
            var model = new ActionNoticeModel(new NotificationReadStore(), Data) { You = you };
            foreach (var page in pages)
                model.Merge(page);
            return model.Groups.Select(group => group.Latest.StepId + "/" + group.Count + "/" + group.Result.Tooltip).ToList();
        }

        private static List<PresentationCue> Replay(Recording fixture, out OpponentPresentationModel model)
        {
            model = Baseline(fixture);
            var cues = new List<PresentationCue>();
            foreach (var update in fixture.Updates)
            {
                cues.AddRange(model.Advance(update.ViewRevision));
                cues.AddRange(model.Merge(new[] { update.Page }));
            }
            return cues;
        }

        private static OpponentPresentationModel Baseline(Recording fixture)
        {
            var model = new OpponentPresentationModel(fixture.Viewer, Data);
            model.Baseline(new[] { fixture.Baseline }, fixture.BaselineRevision);
            return model;
        }

        private static Recording Fixture(string name) => JsonConvert.DeserializeObject<Recording>(File.ReadAllText(
            Path.Combine(Application.dataPath, "Wotr/Tests/Editor/OpponentActions", name + ".json")));

        private static ActionLogPage Page(params ActionLogStep[] steps) => new ActionLogPage { Steps = steps.ToList() };

        private static ActionLogStep Step(string id, long revision, int order, Side? actor, string kind) => new ActionLogStep
        {
            Id = id, ActionId = "action-" + revision, Revision = revision, Order = order, Actor = actor, Kind = kind, Automatic = !actor.HasValue
        };

        private static ActionParticipant Unit(string id, UnitKind kind) =>
            new ActionParticipant { Id = id, Side = Side.Shadow, NationId = "sauron", UnitKind = kind };

        private sealed class Recording
        {
            public string Scenario { get; set; } = "";
            public Side Viewer { get; set; }
            public long BaselineRevision { get; set; }
            public ActionLogPage Baseline { get; set; } = new ActionLogPage();
            public List<Update> Updates { get; set; } = new List<Update>();
        }

        private sealed class Update
        {
            public long ViewRevision { get; set; }
            public ActionLogPage Page { get; set; } = new ActionLogPage();
        }
    }
}
