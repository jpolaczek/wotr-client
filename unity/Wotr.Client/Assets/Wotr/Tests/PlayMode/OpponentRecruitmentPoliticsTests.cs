using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    // UI-307 on a mounted board: the Shadow's recorded recruitment, upgrade, removal and political changes presented to the
    // Free Peoples. Region, unit type, nation and amount read from the stack and its delta badge; an upgrade is a
    // replacement; distant sites follow one source; the Politics panel opens read-only and shows activation, then the
    // token's steps, and war with its caption; the release and Skip end on the latest counts and tracks. TEST-20 (owner,
    // 2026-11-04): each scene also plays with the Shadow watching the Free Peoples.
    public sealed class OpponentRecruitmentPoliticsTests
    {
        [UnityTest]
        public IEnumerator TwoDistantRecruitmentSitesFollowOneSourceAndSettleToTheirCounts([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            yield return stage.Ready();
            var reserve = stage.Reserve(stage.Main, UnitKind.Regular, 6);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve.Take(3), "dol-guldur");
            PresentedBoardStage.Place(latest, reserve.Skip(3), "mount-gundabad");
            var before = stage.Buttons();
            stage.Play(latest, "grey-havens", stage.Recruits(stage.Paid(stage.Step(1), DieFace.Muster), reserve.Take(3), "dol-guldur"),
                stage.Recruits(stage.Step(2), reserve.Skip(3), "mount-gundabad"));
            yield return stage.Until(() => Delta(stage, "+3") != null && stage.Army("dol-guldur") == 3);
            var first = Delta(stage, "+3");
            Assert.IsNotNull(first.Q(className: "nation-emblem-" + stage.Main), "The badge carries the nation's crest.");
            Assert.IsNotNull(first.Q(className: "observed-delta-figure"), "and the unit's figure.");
            Assert.AreEqual(0, stage.Army("mount-gundabad"), "The second site waits for its own beat.");
            Assert.AreEqual(before + 1, stage.Buttons(), "Only the Skip target is added.");
            yield return new WaitForSecondsRealtime(.2f);
            stage.Save("ui307-recruit-1-dol-guldur.png");
            yield return stage.Until(() => stage.Army("mount-gundabad") == 3);
            Assert.IsTrue(stage.Presenters.Camera.SafeArea().Contains(stage.Board.Map.Camera.ToScreen(stage.Anchor("mount-gundabad"))),
                "The camera visits the second site.");
            Assert.AreEqual(1, stage.Board.Root.Query(CostEmphasis.MarkName).ToList().Count, "One charged source for both sites.");
            yield return new WaitForSecondsRealtime(.2f);
            stage.Save("ui307-recruit-2-mount-gundabad.png");
            yield return stage.Released();
            stage.AssertFinal(latest);
            Assert.AreEqual(before, stage.Buttons(), "No control remains.");
            yield return stage.NothingFlies();
        }

        // UI-314 (owner review): the recruited force glows around its own figures — no frame — and keeps glowing for 1.5 s
        // after its new figures land; nothing glows after the release.
        [UnityTest]
        public IEnumerator NewRecruitsGlowAroundTheirFiguresForAMomentAfterLanding([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            yield return stage.Ready();
            var reserve = stage.Reserve(stage.Main, UnitKind.Regular, 2);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve, "dol-guldur");
            stage.Play(latest, "between:dol-guldur:dol-guldur", stage.Recruits(stage.Step(1), reserve, "dol-guldur"));
            yield return stage.Until(() => stage.Army("dol-guldur") == 2 && stage.Presenters.Trace.Lit.Any());
            var started = Time.realtimeSinceStartup;
            Assert.IsEmpty(stage.Board.Map.Query(className: "observed-ring").ToList(), "No frame around the force.");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(stage.Presenters.Trace.Lit.Any(), "The recruits still glow after they have landed.");
            stage.Save("ui314-recruit-glow.png");
            yield return stage.Released();
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started,
                FigureChangePresenter.LandingSeconds + FigureChangePresenter.ArrivalGlowSeconds - .1f, "The glow holds 1.5 s after the landing.");
            Assert.IsEmpty(stage.Lit, "Nothing glows after the release.");
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator SeveralNationsArrivingInOneRegionShowOneEntryEach([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            yield return stage.Ready();
            var sauron = stage.Reserve(stage.Main, UnitKind.Regular, 2);
            var isengard = stage.Reserve(stage.Second, UnitKind.Elite, 1);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, sauron.Concat(isengard), "dol-guldur");
            stage.Play(latest, "between:dol-guldur:dol-guldur", stage.Recruits(stage.Step(1), sauron.Concat(isengard), "dol-guldur"));
            yield return stage.Until(() => stage.Board.Map.Q("observed-delta") != null);
            var entries = stage.Board.Map.Q("observed-delta").Query(className: "observed-delta-entry").ToList();
            Assert.AreEqual(2, entries.Count);
            CollectionAssert.AreEquivalent(new[] { "+2", "+1" }, entries.Select(e => e.Q<Label>("observed-delta-count").text));
            Assert.IsNotNull(entries.Single(e => e.Q<Label>("observed-delta-count").text == "+1").Q(className: "nation-emblem-" + stage.Second));
            Assert.AreEqual(3, stage.Army("dol-guldur"));
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator AnUpgradeReplacesItsRegularAndARemovalLeavesTheStack([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var regulars = stage.Put(stage.Main, UnitKind.Regular, "dol-guldur", 2);
            yield return stage.Ready();
            var elite = stage.Reserve(stage.Main, UnitKind.Elite, 1).Single();
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, new[] { regulars[0] }, "reserve");
            PresentedBoardStage.Place(latest, new[] { elite }, "dol-guldur");
            var upgrade = stage.Step(1);
            upgrade.Facts.Figures.Add(stage.Change(regulars[0], "dol-guldur", "reserve"));
            upgrade.Facts.Figures.Add(stage.Change(elite, "reserve", "dol-guldur"));
            stage.Play(latest, "between:dol-guldur:dol-guldur", upgrade);
            yield return stage.Until(() => stage.Board.Map.Q("observed-delta") != null);
            var marker = Marker(stage, "dol-guldur");
            Assert.AreEqual(1, marker.Regular);
            Assert.AreEqual(1, marker.Elite);
            Assert.AreEqual(2, marker.Total, "The upgrade replaces the Regular: no extra figure.");
            var entry = stage.Board.Map.Q("observed-delta").Q(className: "observed-delta-entry");
            Assert.AreEqual(2, entry.Query(className: "observed-delta-figure").ToList().Count, "Regular → Elite.");
            yield return new WaitForSecondsRealtime(.2f);
            stage.Save("ui307-upgrade.png");
            yield return stage.Released();
            stage.AssertFinal(latest);
            var removal = stage.Next(latest);
            PresentedBoardStage.Place(removal, new[] { regulars[1] }, "reserve");
            var step = stage.Step(1, 2);
            step.Facts.Figures.Add(stage.Change(regulars[1], "dol-guldur", "reserve"));
            stage.Play(removal, null, step);
            yield return stage.Until(() => Delta(stage, "−1") != null);
            Assert.AreEqual(1, stage.Army("dol-guldur"), "The returned figure leaves the stack after its commit.");
            yield return stage.Released();
            stage.AssertFinal(removal);
        }

        [UnityTest]
        public IEnumerator ActivationThenAdvancementShowBothChangesAndReachingWarIsCaptioned([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var sauron = stage.Before.Nations.Single(n => n.Id == stage.Main);
            sauron.Active = false;
            sauron.StepsToWar = 2;
            yield return stage.Ready();
            var latest = stage.Latest();
            Nation(latest, stage.Main, true, 0);
            var before = stage.Buttons();
            stage.Play(latest, null, Politics(stage.Step(1), stage.Main, false, 2, true, 1), Politics(stage.Step(2), stage.Main, true, 1, true, 0));
            var panel = stage.Board.Nations;
            var token = panel.Drawer.Q<Button>("nation-status-" + stage.Main);
            yield return stage.Until(() => panel.Drawer.style.display.value == DisplayStyle.Flex);
            Assert.IsTrue(token.ClassListContains("passive-nation"), "The token starts as it was presented.");
            yield return stage.Until(() => token.ClassListContains("active-nation"));
            Assert.IsTrue(panel.Drawer.ClassListContains(PoliticsPresenter.MovingClass), "UI-319: the crest crossfades instead of swapping.");
            Assert.IsFalse(panel.Drawer.Q("nation-track-" + stage.Main).ClassListContains("nation-at-war"), "Activation alone is not war.");
            stage.Save("ui307-politics-1-activation.png");
            yield return stage.Until(() => stage.Board.Root.Q(PoliticsPresenter.CaptionName) != null);
            Assert.AreEqual("At war", stage.Board.Root.Q<Label>(PoliticsPresenter.CaptionName).text);
            Assert.IsTrue(panel.Drawer.Q("nation-track-" + stage.Main).ClassListContains("nation-at-war"));
            Assert.AreEqual(before + 1, stage.Buttons(), "No Politics or Continue control is added, only Skip.");
            yield return new WaitForSecondsRealtime(.3f);
            stage.Save("ui307-politics-2-war.png");
            yield return stage.Released();
            Assert.IsTrue(Appearance.Closed(panel.Drawer), "The panel the presentation opened closes again.");
            Assert.IsNull(stage.Board.Root.Q(PoliticsPresenter.CaptionName), "The caption ends with its beat.");
            Assert.IsTrue(panel.Drawer.Q("nation-track-" + stage.Main).ClassListContains("nation-at-war"), "The released row is at war.");
        }

        // UI-535 (owner's game, 2026-11-12): the whole action's change (passive 2 to active 1) and a later step's activation
        // alone (passive 2 to active 2): the token slides once toward war and never back.
        [UnityTest]
        public IEnumerator AStaleActivationNeverMovesTheTokenBack()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, Side.FreePeoples);
            var nation = stage.Before.Nations.Single(n => n.Id == stage.Main);
            nation.Active = false;
            nation.StepsToWar = 2;
            yield return stage.Ready();
            var latest = stage.Latest();
            Nation(latest, stage.Main, true, 1);
            var token = stage.Board.Nations.Drawer.Q<Button>("nation-status-" + stage.Main);
            stage.Play(latest, null, Politics(stage.Step(1), stage.Main, false, 2, true, 1), Politics(stage.Step(2), stage.Main, false, 2, true, 2));
            yield return stage.Until(() => stage.Board.Nations.Drawer.style.display.value == DisplayStyle.Flex);
            var start = token.resolvedStyle.left;
            yield return stage.Until(() => token.resolvedStyle.left > start + 1 || token.resolvedStyle.left < start - 1);
            var moved = token.resolvedStyle.left;
            var direction = Mathf.Sign(moved - start);
            var furthest = moved;
            while (stage.Playback.Holding)
            {
                var left = token.resolvedStyle.left;
                Assert.GreaterOrEqual((left - furthest) * direction, -1f, "The token never slides back toward its old station.");
                if ((left - furthest) * direction > 0)
                    furthest = left;
                yield return null;
            }
            yield return stage.Released();
            Assert.IsTrue(token.ClassListContains("active-nation"));
        }

        // UI-315 (owner review): the panel the step opens settles, the nation's token glows before it moves, slides to its
        // new station while lit and stays lit there — the step is seen, not flashed.
        [UnityTest]
        public IEnumerator AStepToWarLightsTheTokenBeforeItSlidesAndHoldsItLit([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var sauron = stage.Before.Nations.Single(n => n.Id == stage.Main);
            sauron.Active = true;
            sauron.StepsToWar = 2;
            yield return stage.Ready();
            var latest = stage.Latest();
            Nation(latest, stage.Main, true, 1);
            var token = stage.Board.Nations.Drawer.Q<Button>("nation-status-" + stage.Main);
            var started = Time.realtimeSinceStartup;
            stage.Play(latest, null, Politics(stage.Step(1), stage.Main, true, 2, true, 1));
            yield return stage.Until(() => PresentationGlow.IsLit(token));
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started, PresentationPace.PanelSettleSeconds - .1f,
                "The panel the step opened settles before the token is marked.");
            var station = token.resolvedStyle.left;
            yield return new WaitForSecondsRealtime((float)PresentationPace.MarkSeconds - .2f);
            Assert.AreEqual(station, token.resolvedStyle.left, "The token is lit before it moves.");
            Assert.IsTrue(token.ClassListContains(PoliticsPresenter.FlareClass), "UI-319: it flares.");
            Assert.Greater(token.resolvedStyle.scale.value.x, 1.05f, "It swells with its glow.");
            yield return stage.Until(() => token.resolvedStyle.left != station);
            Assert.IsFalse(token.ClassListContains(PoliticsPresenter.FlareClass), "It settles back as it slides.");
            yield return new WaitForSecondsRealtime((float)PresentationPace.ChangeSeconds);
            Assert.IsTrue(PresentationGlow.IsLit(token), "It stays lit at its new station.");
            Assert.IsTrue(PresentationGlow.IsLit(stage.Board.Nations.Drawer.Q("nation-track-" + stage.Main)), "The chosen nation's row is lit.");
            Assert.IsTrue(stage.Playback.Holding, "The hold belongs to the beat.");
            stage.Save("ui315-politics-held.png");
            yield return stage.Released();
            Assert.IsEmpty(stage.Lit);
        }

        // UI-342 (owner 2026-10-26: "let's not wait for the end of the opponent's turn to close the politics track; close it
        // when the next animation begins"): the panel the presentation opened closes as the next step's scene begins.
        [UnityTest]
        public IEnumerator ThePanelThePresentationOpenedClosesAsTheNextSceneBegins([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var sauron = stage.Before.Nations.Single(n => n.Id == stage.Main);
            sauron.Active = true;
            sauron.StepsToWar = 2;
            yield return stage.Ready();
            var reserve = stage.Reserve(stage.Main, UnitKind.Regular, 2);
            var latest = stage.Latest();
            Nation(latest, stage.Main, true, 1);
            PresentedBoardStage.Place(latest, reserve, "dol-guldur");
            stage.Play(latest, null, Politics(stage.Step(1), stage.Main, true, 2, true, 1), stage.Recruits(stage.Step(2), reserve, "dol-guldur"));
            var drawer = stage.Board.Nations.Drawer;
            var token = drawer.Q<Button>("nation-status-" + stage.Main);
            yield return stage.Until(() => PresentationGlow.IsLit(token));
            Assert.IsFalse(Appearance.Closed(drawer), "The panel stands through its own step.");
            yield return stage.Until(() => Appearance.Closed(drawer));
            Assert.IsTrue(stage.Playback.Holding, "It closes as the recruitment begins, before the presentation ends.");
            yield return stage.Released();
            Assert.IsTrue(Appearance.Closed(drawer));
        }

        [UnityTest]
        public IEnumerator APanelThePlayerOpenedStaysAndOneTheyClosedIsNotReopened([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            foreach (var id in new[] { stage.Main, stage.Second })
            {
                var nation = stage.Before.Nations.Single(n => n.Id == id);
                nation.StepsToWar = 2;
            }
            yield return stage.Ready();
            var panel = stage.Board.Nations;
            panel.Open();
            yield return null;
            var latest = stage.Latest();
            Nation(latest, stage.Main, true, 1);
            stage.Play(latest, null, Politics(stage.Step(1), stage.Main, true, 2, true, 1));
            yield return stage.Released();
            Assert.AreEqual(DisplayStyle.Flex, panel.Drawer.style.display.value, "The player's open panel stays open.");
            panel.Close();
            yield return null;
            var next = stage.Next(latest);
            Nation(next, stage.Main, true, 0);
            Nation(next, stage.Second, true, 1);
            stage.Play(next, null, Politics(stage.Step(1, 2), stage.Main, true, 1, true, 0), Politics(stage.Step(2, 2), stage.Second, true, 2, true, 1));
            yield return stage.Until(() => panel.Drawer.style.display.value == DisplayStyle.Flex);
            panel.Close();
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual(DisplayStyle.None, panel.Drawer.style.display.value, "A panel closed during the presentation is not reopened.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator SkipEndsOnTheLatestCountsAndTracksWithoutAReplay([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var sauron = stage.Before.Nations.Single(n => n.Id == stage.Main);
            sauron.StepsToWar = 2;
            yield return stage.Ready();
            var reserve = stage.Reserve(stage.Main, UnitKind.Regular, 3);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve, "dol-guldur");
            Nation(latest, stage.Main, true, 1);
            stage.Play(latest, "grey-havens", stage.Recruits(stage.Paid(stage.Step(1), DieFace.Muster), reserve, "dol-guldur"), Politics(stage.Step(2), stage.Main, true, 2, true, 1));
            yield return stage.Until(() => stage.Presenters.Skip.Button.parent != null);
            PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            stage.AssertFinal(latest);
            Assert.AreEqual(1, stage.Board.View.Nations.Single(n => n.Id == stage.Main).StepsToWar);
            Assert.IsTrue(Appearance.Closed(stage.Board.Nations.Drawer));
            yield return stage.NothingFlies();
        }

        [UnityTest]
        public IEnumerator RecruitmentAndPoliticsAreRecordedAsMutedClips()
        {
            using var stage = new PresentedBoardStage(1920, 1.25f, false);
            var sauron = stage.Before.Nations.Single(n => n.Id == stage.Main);
            sauron.Active = false;
            sauron.StepsToWar = 1;
            yield return stage.Ready();
            var reserve = stage.Reserve(stage.Main, UnitKind.Regular, 6);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve.Take(3), "dol-guldur");
            PresentedBoardStage.Place(latest, reserve.Skip(3), "mount-gundabad");
            Nation(latest, stage.Main, true, 0);
            stage.Play(latest, "grey-havens", stage.Recruits(stage.Paid(stage.Step(1), DieFace.Muster), reserve.Take(3), "dol-guldur"),
                stage.Recruits(stage.Step(2), reserve.Skip(3), "mount-gundabad"), Politics(stage.Step(3), stage.Main, false, 1, true, 0));
            var frame = 0;
            while (stage.Playback.Holding && frame < 80)
            {
                LayoutTests.Save(stage.Texture, "ui307-clip-" + frame.ToString("00") + ".png");
                frame++;
                yield return new WaitForSecondsRealtime(.1f);
            }
            Assert.IsFalse(stage.Playback.Holding);
            stage.AssertFinal(latest);
        }

        // UI-485 (owner, 2026-11-04): a recruit as the engine records it, from nowhere and from the reserve, is one figure on
        // the badge and in the stack, for both viewers.
        [UnityTest]
        public IEnumerator ARecruitRecordedTwiceShowsItsRealCount([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            yield return stage.Ready();
            var (nation, region) = Place(viewer);
            var id = stage.Reserve(nation, UnitKind.Regular, 1).Single();
            var before = stage.Army(region);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, new[] { id }, region);
            var recruit = stage.Step(1);
            recruit.Facts.Figures.Add(new ActionFigureChange { After = stage.Unit(id), To = region });
            recruit.Facts.Figures.Add(stage.Change(id, "reserve", region));
            stage.Play(latest, region, recruit);
            yield return stage.Until(() => stage.Board.Map.Q("observed-delta") != null);
            Assert.IsNotNull(Delta(stage, "+1"), "One Regular, not two.");
            Assert.AreEqual(before + 1, stage.Army(region));
            yield return new WaitForSecondsRealtime(.2f);
            LayoutTests.Save(stage.Texture, "ui485-recruit-" + viewer + ".png");
            yield return stage.Released();
            Assert.AreEqual(before + 1, stage.Army(region));
            stage.AssertFinal(latest);
        }

        // UI-486 (owner, 2026-11-04): an event's casualties play their scene. The camera travels there, the force glows, the
        // badge counts them and the stack settles — recorded as TakeEventLosses records them, a choice and its EventLosses step.
        [UnityTest]
        public IEnumerator AnEventsRemovalPlaysItsSceneAtTheRegion([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var (nation, region) = Place(viewer);
            var ids = stage.Put(nation, UnitKind.Regular, region, 3);
            yield return stage.Ready();
            var before = stage.Army(region);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, ids.Take(2), "reserve");
            var choice = stage.Step(1);
            choice.Command = CommandKind.TakeEventLosses;
            foreach (var id in ids.Take(2))
                choice.Facts.Figures.Add(stage.Change(id, region, "reserve"));
            var losses = EventLosses(stage, 2, region, ids.Take(2).Select(id => Loss(stage, id, BattleLossKind.Remove, region)).ToArray());
            stage.Play(latest, "grey-havens", choice, losses);
            yield return stage.Until(() => Delta(stage, "−2") != null);
            Assert.AreEqual(before - 2, stage.Army(region), "The stack settles to its true count.");
            Assert.IsTrue(stage.Presenters.Trace.Lit.Any(), "The force glows.");
            Assert.IsTrue(stage.Presenters.Camera.SafeArea().Contains(stage.Board.Map.Camera.ToScreen(stage.Anchor(region))),
                "The camera's travel ends at a scene.");
            yield return new WaitForSecondsRealtime(.2f);
            LayoutTests.Save(stage.Texture, "ui486-removal-" + viewer + ".png");
            Assert.IsTrue(stage.Playback.Holding, "The scene plays before the release.");
            yield return stage.Released();
            stage.AssertFinal(latest);
            yield return stage.NothingFlies();
        }

        // A downgrade: the Elite leaves, then its Regular arrives.
        [UnityTest]
        public IEnumerator AnEventsDowngradeShowsItsEliteOutAndItsRegularIn([Values(Side.FreePeoples, Side.Shadow)] Side viewer)
        {
            using var stage = new PresentedBoardStage(1366, 1f, false, viewer);
            var (nation, region) = Place(viewer);
            var stays = stage.Put(nation, UnitKind.Regular, region, 1);
            var elite = stage.Put(nation, UnitKind.Elite, region, 1).Single();
            yield return stage.Ready();
            var before = stage.Army(region);
            var regular = stage.Reserve(nation, UnitKind.Regular, 1).Single();
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, new[] { elite }, "reserve");
            PresentedBoardStage.Place(latest, new[] { regular }, region);
            var choice = stage.Step(1);
            choice.Command = CommandKind.TakeEventLosses;
            // The board's difference lists the figures in id order: here the Regular comes first.
            choice.Facts.Figures.Add(stage.Change(regular, "reserve", region));
            choice.Facts.Figures.Add(stage.Change(elite, region, "reserve"));
            var loss = Loss(stage, elite, BattleLossKind.Downgrade, region);
            loss.ReplacementId = regular;
            stage.Play(latest, "grey-havens", choice, EventLosses(stage, 1, region, loss));
            yield return stage.Until(() => Delta(stage, "−1") != null);
            Assert.AreEqual(before - 1, stage.Army(region), "The Elite left first.");
            Assert.IsNull(Delta(stage, "+1"));
            yield return new WaitForSecondsRealtime(.2f);
            LayoutTests.Save(stage.Texture, "ui486-downgrade-out-" + viewer + ".png");
            yield return stage.Until(() => Delta(stage, "+1") != null);
            Assert.AreEqual(before, stage.Army(region), "Its Regular takes its place.");
            yield return new WaitForSecondsRealtime(.2f);
            LayoutTests.Save(stage.Texture, "ui486-downgrade-in-" + viewer + ".png");
            yield return stage.Released();
            Assert.AreEqual(stays.Count + 1, stage.Army(region));
            stage.AssertFinal(latest);
            yield return stage.NothingFlies();
        }

        private static (string Nation, string Region) Place(Side viewer) =>
            viewer == Side.FreePeoples ? ("sauron", "dol-guldur") : ("rohan", "edoras");

        private static ActionLossFact Loss(PresentedBoardStage stage, string id, BattleLossKind kind, string region) =>
            new ActionLossFact { Figure = stage.Unit(id), Operation = kind, RegionId = region, Hits = 1 };

        private static ActionLogStep EventLosses(PresentedBoardStage stage, int order, string region, params ActionLossFact[] losses)
        {
            var step = stage.Step(order);
            step.Actor = null;
            step.Automatic = true;
            step.Kind = "EventLosses";
            step.Context.Regions.Add(region);
            step.Facts.LossDetails.AddRange(losses);
            return step;
        }

        private static void Nation(PlayerView view, string id, bool active, int steps)
        {
            var nation = view.Nations.Single(n => n.Id == id);
            nation.Active = active;
            nation.StepsToWar = steps;
        }

        private static ActionLogStep Politics(ActionLogStep step, string id, bool wasActive, int previous, bool active, int steps)
        {
            step.Facts.Nations.Add(new ActionNationChange
            {
                Id = id, WasActive = wasActive, PreviousStepsToWar = previous, Active = active, StepsToWar = steps
            });
            return step;
        }

        private static VisualElement Delta(PresentedBoardStage stage, string count) => stage.Board.Map.Query("observed-delta").ToList()
            .FirstOrDefault(delta => delta.Query<Label>("observed-delta-count").ToList().Any(label => label.text == count));

        private static ArmyMarker Marker(PresentedBoardStage stage, string region) =>
            stage.Board.Presentation.Armies.Single(a => a.RegionId == region && a.Side == stage.Opponent && !a.Garrison);
    }
}
