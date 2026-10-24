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
    // token's steps, and war with its caption; the release and Skip end on the latest counts and tracks.
    public sealed class OpponentRecruitmentPoliticsTests
    {
        [UnityTest]
        public IEnumerator TwoDistantRecruitmentSitesFollowOneSourceAndSettleToTheirCounts()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var reserve = stage.Reserve("sauron", UnitKind.Regular, 6);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve.Take(3), "dol-guldur");
            PresentedBoardStage.Place(latest, reserve.Skip(3), "mount-gundabad");
            var before = stage.Buttons();
            stage.Play(latest, "grey-havens", stage.Recruits(Cost(stage.Step(1)), reserve.Take(3), "dol-guldur"),
                stage.Recruits(stage.Step(2), reserve.Skip(3), "mount-gundabad"));
            yield return stage.Until(() => Delta(stage, "+3") != null && stage.Army("dol-guldur") == 3);
            var first = Delta(stage, "+3");
            Assert.IsNotNull(first.Q(className: "nation-emblem-sauron"), "The badge carries the nation's crest.");
            Assert.IsNotNull(first.Q(className: "observed-delta-figure"), "and the unit's figure.");
            Assert.AreEqual(0, stage.Army("mount-gundabad"), "The second site waits for its own beat.");
            Assert.AreEqual(before + 1, stage.Buttons(), "Only the Skip target is added.");
            yield return new WaitForSecondsRealtime(.2f);
            LayoutTests.Save(stage.Texture, "ui307-recruit-1-dol-guldur.png");
            yield return stage.Until(() => stage.Army("mount-gundabad") == 3);
            Assert.IsTrue(stage.Presenters.Camera.SafeArea().Contains(stage.Board.Map.Camera.ToScreen(stage.Anchor("mount-gundabad"))),
                "The camera visits the second site.");
            Assert.AreEqual(1, stage.Board.Root.Query(CostEmphasis.MarkName).ToList().Count, "One charged source for both sites.");
            yield return new WaitForSecondsRealtime(.2f);
            LayoutTests.Save(stage.Texture, "ui307-recruit-2-mount-gundabad.png");
            yield return stage.Released();
            stage.AssertFinal(latest);
            Assert.AreEqual(before, stage.Buttons(), "No control remains.");
            yield return stage.NothingFlies();
        }

        // UI-314 (owner review): the recruited force glows around its own figures — no frame — and keeps glowing for 1.5 s
        // after its new figures land; nothing glows after the release.
        [UnityTest]
        public IEnumerator NewRecruitsGlowAroundTheirFiguresForAMomentAfterLanding()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var reserve = stage.Reserve("sauron", UnitKind.Regular, 2);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve, "dol-guldur");
            stage.Play(latest, "between:dol-guldur:dol-guldur", stage.Recruits(stage.Step(1), reserve, "dol-guldur"));
            yield return stage.Until(() => stage.Army("dol-guldur") == 2 && stage.Presenters.Trace.Lit.Any());
            var started = Time.realtimeSinceStartup;
            Assert.IsEmpty(stage.Board.Map.Query(className: "observed-ring").ToList(), "No frame around the force.");
            yield return new WaitForSecondsRealtime(1.2f);
            Assert.IsTrue(stage.Presenters.Trace.Lit.Any(), "The recruits still glow after they have landed.");
            LayoutTests.Save(stage.Texture, "ui314-recruit-glow.png");
            yield return stage.Released();
            Assert.GreaterOrEqual(Time.realtimeSinceStartup - started,
                FigureChangePresenter.LandingSeconds + FigureChangePresenter.ArrivalGlowSeconds - .1f, "The glow holds 1.5 s after the landing.");
            Assert.IsEmpty(stage.Lit, "Nothing glows after the release.");
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator SeveralNationsArrivingInOneRegionShowOneEntryEach()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            yield return stage.Ready();
            var sauron = stage.Reserve("sauron", UnitKind.Regular, 2);
            var isengard = stage.Reserve("isengard", UnitKind.Elite, 1);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, sauron.Concat(isengard), "dol-guldur");
            stage.Play(latest, "between:dol-guldur:dol-guldur", stage.Recruits(stage.Step(1), sauron.Concat(isengard), "dol-guldur"));
            yield return stage.Until(() => stage.Board.Map.Q("observed-delta") != null);
            var entries = stage.Board.Map.Q("observed-delta").Query(className: "observed-delta-entry").ToList();
            Assert.AreEqual(2, entries.Count);
            CollectionAssert.AreEquivalent(new[] { "+2", "+1" }, entries.Select(e => e.Q<Label>("observed-delta-count").text));
            Assert.IsNotNull(entries.Single(e => e.Q<Label>("observed-delta-count").text == "+1").Q(className: "nation-emblem-isengard"));
            Assert.AreEqual(3, stage.Army("dol-guldur"));
            yield return stage.Released();
            stage.AssertFinal(latest);
        }

        [UnityTest]
        public IEnumerator AnUpgradeReplacesItsRegularAndARemovalLeavesTheStack()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var regulars = stage.Put("sauron", UnitKind.Regular, "dol-guldur", 2);
            yield return stage.Ready();
            var elite = stage.Reserve("sauron", UnitKind.Elite, 1).Single();
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
            LayoutTests.Save(stage.Texture, "ui307-upgrade.png");
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
        public IEnumerator ActivationThenAdvancementShowBothChangesAndReachingWarIsCaptioned()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var sauron = stage.Before.Nations.Single(n => n.Id == "sauron");
            sauron.Active = false;
            sauron.StepsToWar = 2;
            yield return stage.Ready();
            var latest = stage.Latest();
            Nation(latest, "sauron", true, 0);
            var before = stage.Buttons();
            stage.Play(latest, null, Politics(stage.Step(1), "sauron", false, 2, true, 1), Politics(stage.Step(2), "sauron", true, 1, true, 0));
            var panel = stage.Board.Nations;
            var token = panel.Drawer.Q<Button>("nation-status-sauron");
            yield return stage.Until(() => panel.Drawer.style.display.value == DisplayStyle.Flex);
            Assert.IsTrue(token.ClassListContains("passive-nation"), "The token starts as it was presented.");
            yield return stage.Until(() => token.ClassListContains("active-nation"));
            Assert.IsTrue(panel.Drawer.ClassListContains(PoliticsPresenter.MovingClass), "UI-319: the crest crossfades instead of swapping.");
            Assert.IsFalse(panel.Drawer.Q("nation-track-sauron").ClassListContains("nation-at-war"), "Activation alone is not war.");
            LayoutTests.Save(stage.Texture, "ui307-politics-1-activation.png");
            yield return stage.Until(() => stage.Board.Root.Q(PoliticsPresenter.CaptionName) != null);
            Assert.AreEqual("At war", stage.Board.Root.Q<Label>(PoliticsPresenter.CaptionName).text);
            Assert.IsTrue(panel.Drawer.Q("nation-track-sauron").ClassListContains("nation-at-war"));
            Assert.AreEqual(before + 1, stage.Buttons(), "No Politics or Continue control is added, only Skip.");
            yield return new WaitForSecondsRealtime(.3f);
            LayoutTests.Save(stage.Texture, "ui307-politics-2-war.png");
            yield return stage.Released();
            Assert.AreEqual(DisplayStyle.None, panel.Drawer.style.display.value, "The panel the presentation opened closes again.");
            Assert.IsNull(stage.Board.Root.Q(PoliticsPresenter.CaptionName), "The caption ends with its beat.");
            Assert.IsTrue(panel.Drawer.Q("nation-track-sauron").ClassListContains("nation-at-war"), "The released row is at war.");
        }

        // UI-315 (owner review): the panel the step opens settles, the nation's token glows before it moves, slides to its
        // new station while lit and stays lit there — the step is seen, not flashed.
        [UnityTest]
        public IEnumerator AStepToWarLightsTheTokenBeforeItSlidesAndHoldsItLit()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var sauron = stage.Before.Nations.Single(n => n.Id == "sauron");
            sauron.Active = true;
            sauron.StepsToWar = 2;
            yield return stage.Ready();
            var latest = stage.Latest();
            Nation(latest, "sauron", true, 1);
            var token = stage.Board.Nations.Drawer.Q<Button>("nation-status-sauron");
            var started = Time.realtimeSinceStartup;
            stage.Play(latest, null, Politics(stage.Step(1), "sauron", true, 2, true, 1));
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
            Assert.IsTrue(PresentationGlow.IsLit(stage.Board.Nations.Drawer.Q("nation-track-sauron")), "The chosen nation's row is lit.");
            Assert.IsTrue(stage.Playback.Holding, "The hold belongs to the beat.");
            LayoutTests.Save(stage.Texture, "ui315-politics-held.png");
            yield return stage.Released();
            Assert.IsEmpty(stage.Lit);
        }

        [UnityTest]
        public IEnumerator APanelThePlayerOpenedStaysAndOneTheyClosedIsNotReopened()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            foreach (var id in new[] { "sauron", "isengard" })
            {
                var nation = stage.Before.Nations.Single(n => n.Id == id);
                nation.StepsToWar = 2;
            }
            yield return stage.Ready();
            var panel = stage.Board.Nations;
            panel.Open();
            yield return null;
            var latest = stage.Latest();
            Nation(latest, "sauron", true, 1);
            stage.Play(latest, null, Politics(stage.Step(1), "sauron", true, 2, true, 1));
            yield return stage.Released();
            Assert.AreEqual(DisplayStyle.Flex, panel.Drawer.style.display.value, "The player's open panel stays open.");
            panel.Close();
            yield return null;
            var next = stage.Next(latest);
            Nation(next, "sauron", true, 0);
            Nation(next, "isengard", true, 1);
            stage.Play(next, null, Politics(stage.Step(1, 2), "sauron", true, 1, true, 0), Politics(stage.Step(2, 2), "isengard", true, 2, true, 1));
            yield return stage.Until(() => panel.Drawer.style.display.value == DisplayStyle.Flex);
            panel.Close();
            yield return new WaitForSecondsRealtime(2f);
            Assert.AreEqual(DisplayStyle.None, panel.Drawer.style.display.value, "A panel closed during the presentation is not reopened.");
            yield return stage.Released();
        }

        [UnityTest]
        public IEnumerator SkipEndsOnTheLatestCountsAndTracksWithoutAReplay()
        {
            using var stage = new PresentedBoardStage(1366, 1f, false);
            var sauron = stage.Before.Nations.Single(n => n.Id == "sauron");
            sauron.StepsToWar = 2;
            yield return stage.Ready();
            var reserve = stage.Reserve("sauron", UnitKind.Regular, 3);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve, "dol-guldur");
            Nation(latest, "sauron", true, 1);
            stage.Play(latest, "grey-havens", stage.Recruits(Cost(stage.Step(1)), reserve, "dol-guldur"), Politics(stage.Step(2), "sauron", true, 2, true, 1));
            yield return stage.Until(() => stage.Presenters.Skip.Button.parent != null);
            PointerGestureEvents.ClickAt(stage.Presenters.Skip.Button);
            yield return stage.Released();
            stage.AssertFinal(latest);
            Assert.AreEqual(1, stage.Board.View.Nations.Single(n => n.Id == "sauron").StepsToWar);
            Assert.AreEqual(DisplayStyle.None, stage.Board.Nations.Drawer.style.display.value);
            yield return stage.NothingFlies();
        }

        [UnityTest]
        public IEnumerator RecruitmentAndPoliticsAreRecordedAsMutedClips()
        {
            using var stage = new PresentedBoardStage(1920, 1.25f, false);
            var sauron = stage.Before.Nations.Single(n => n.Id == "sauron");
            sauron.Active = false;
            sauron.StepsToWar = 1;
            yield return stage.Ready();
            var reserve = stage.Reserve("sauron", UnitKind.Regular, 6);
            var latest = stage.Latest();
            PresentedBoardStage.Place(latest, reserve.Take(3), "dol-guldur");
            PresentedBoardStage.Place(latest, reserve.Skip(3), "mount-gundabad");
            Nation(latest, "sauron", true, 0);
            stage.Play(latest, "grey-havens", stage.Recruits(Cost(stage.Step(1)), reserve.Take(3), "dol-guldur"),
                stage.Recruits(stage.Step(2), reserve.Skip(3), "mount-gundabad"), Politics(stage.Step(3), "sauron", false, 1, true, 0));
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

        private static void Nation(PlayerView view, string id, bool active, int steps)
        {
            var nation = view.Nations.Single(n => n.Id == id);
            nation.Active = active;
            nation.StepsToWar = steps;
        }

        private static ActionLogStep Cost(ActionLogStep step)
        {
            step.Costs.Add(new ActionCost { Kind = ActionCostKind.Die, Id = PresentedBoardStage.Die, Owner = Side.Shadow, Face = DieFace.Muster, Use = DieFace.Muster });
            return step;
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
            stage.Board.Presentation.Armies.Single(a => a.RegionId == region && a.Side == Side.Shadow && !a.Garrison);
    }
}
