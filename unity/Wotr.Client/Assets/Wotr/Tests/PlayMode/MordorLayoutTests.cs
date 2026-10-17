using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed class MordorLayoutTests
    {
        [UnityTest]
        public IEnumerator GuideAndForcedRemovalKeepOneVisibleHostBesideTheTrackInFourLayouts()
        {
            using var client = new MountedMatchClient("http://localhost", "mordor-followups-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    client.Interface.Windows.Clear();
                    foreach (var guide in new[] { true, false })
                    {
                        client.Interface.Windows.Clear();
                        var view = FollowupScreen(guide);
                        view.Decision.Id = "mordor-followup-" + guide + width + scale;
                        client.Apply(view);
                        client.SetOnline();
                        yield return Settle();
                        var board = client.Board;
                        board.Fellowship.Open();
                        yield return Settle();
                        board.Hunt.Open();
                        yield return Settle();
                        Assert.AreSame(board.Hunt.DecisionHost, board.Tray.parent);
                        Assert.IsFalse(Visible(board.Hunt.Dice.Root));
                        Assert.IsEmpty(board.Map.DraftOverlay.Targets);
                        Assert.IsTrue(board.Fellowship.Root.Q<Label>("mordor-step-4").ClassListContains("selected"));
                        if (guide)
                        {
                            Assert.IsTrue(Visible(board.Hunt.Guides.Root));
                            Assert.IsFalse(Visible(board.Hunt.Tile.Root));
                            Assert.IsFalse(Visible(board.Fellowship.Root.Q<Label>("fellowship-guide-hint")));
                        }
                        else
                        {
                            Assert.AreEqual("Remove from play", board.Tray.Q<Button>("confirm-decision").text);
                            Assert.AreEqual("Remove companions from play", board.Hunt.Root.Q<Label>("hunt-step").text);
                            Assert.AreEqual("Separate 2", board.Hunt.Root.Q<Label>("hunt-damage").text);
                        }
                        LayoutTests.Save(target, $"ui52-mordor-followup-{guide}-{width}-{scale:0.00}.png");
                        Check(board, board.Hunt.Root);
                        Check(board, board.Fellowship.Root);
                        board.Hunt.Close();
                        board.Fellowship.Close();
                    }
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        // UI-154: the track is drawn on the map; the Fellowship stands on its step with its Corruption, the missing
        // attempt and the drawn tile beside it, zoomed in with numbers and on the whole map without them.
        [UnityTest]
        public IEnumerator TrackOnTheMapShowsTheFellowshipOnItsStepInFourLayouts()
        {
            using var client = new MountedMatchClient("http://localhost", "mordor-track-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    client.Interface.Windows.Clear();
                    var view = HuntLayoutTests.Screen(CommandKind.ResolveHuntDamage);
                    view.MatchId = "mordor-track-layout";
                    view.Decision.Id = "mordor-track-" + width + scale;
                    view.Fellowship.MordorStep = 3;
                    view.Fellowship.RegionId = "mordor-track";
                    view.Fellowship.Corruption = 7;
                    view.Fellowship.MordorAttemptedThisTurn = false;
                    view.Hunt.Mordor = true;
                    view.Hunt.Face = new HuntTileFaceView { Pool = "standard", Value = "2", Stop = true, Count = 1 };
                    client.Apply(view);
                    yield return Settle();
                    var map = client.Board.Map;
                    var track = map.Data.MordorTrack;
                    map.Camera.Zoom = map.FitZoom * 1.8f;
                    map.Camera.Pan = map.contentRect.center - track.Bounds.center * map.Camera.Zoom;
                    map.Refresh();
                    yield return Settle();
                    var figure = map.FellowshipMarker.Root;
                    Assert.AreEqual(DisplayStyle.Flex, figure.resolvedStyle.display, "The Fellowship stays on the map in Mordor.");
                    var step = map.Q<Button>("mordor-track-step-3");
                    Assert.IsTrue(step.ClassListContains(MapMordorTrack.CurrentClass));
                    Assert.IsTrue(map.Q<Button>("mordor-track-step-1").ClassListContains(MapMordorTrack.PassedClass));
                    Assert.IsFalse(map.Q<Button>("mordor-track-step-4").ClassListContains(MapMordorTrack.PassedClass));
                    Assert.That(figure.worldBound.center.x, Is.EqualTo(step.worldBound.center.x).Within(2f), "The figure stands on its step.");
                    Assert.That(figure.worldBound.yMax, Is.EqualTo(step.worldBound.center.y + 4).Within(3f));
                    Assert.AreEqual(DisplayStyle.Flex, step.Q<Label>("mordor-step-number").resolvedStyle.display, "Zoomed in, steps show numbers.");
                    Assert.AreEqual("7/12", map.Q<Label>("fellowship-mordor-corruption").text);
                    Assert.AreEqual(DisplayStyle.Flex, map.Q("fellowship-mordor-idle").resolvedStyle.display);
                    Assert.AreEqual(DisplayStyle.Flex, map.FellowshipMarker.Tile.resolvedStyle.display, "The drawn tile stands beside the figure.");
                    foreach (var gate in new[] { "morannon", "minas-morgul" })
                        Assert.AreEqual(DisplayStyle.Flex, map.Q<Button>("mordor-track-gate-" + gate).resolvedStyle.display);
                    StringAssert.Contains("Mordor step 3 of 5", figure.tooltip);
                    Assert.IsNull(map.Q("mordor-crack-caption"), "UI-157: the Crack of Doom has no caption on the map.");
                    LayoutTests.Save(target, $"ui154-mordor-zoom-{width}-{scale:0.00}.png");
                    var repaints = map.Renderer.Repaints;
                    map.Camera.Pan += new Vector2(-40, 25);
                    map.Refresh();
                    yield return Settle();
                    Assert.AreEqual(repaints, map.Renderer.Repaints, "Panning moves the track without repainting the map layers.");
                    map.Fit();
                    yield return Settle();
                    Assert.AreEqual(DisplayStyle.None, step.Q<Label>("mordor-step-number").resolvedStyle.display,
                        "On the whole map the stones carry no numbers.");
                    LayoutTests.Save(target, $"ui154-mordor-fit-{width}-{scale:0.00}.png");
                    view.Fellowship.MordorStep = null;
                    view.Fellowship.RegionId = "minas-morgul";
                    view.Hunt = null;
                    view.Revision++;
                    client.Apply(view);
                    yield return Settle();
                    Assert.IsTrue(map.MordorTrack.Root.ClassListContains(MapMordorTrack.InactiveClass), "Without the Fellowship the track is faint.");
                    Assert.IsFalse(map.Q<Button>("mordor-track-step-3").ClassListContains(MapMordorTrack.CurrentClass));
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        // UI-154: a new step walks the figure along the drawn path instead of jumping to it.
        [UnityTest]
        public IEnumerator TheFellowshipWalksThePathToItsNextStep()
        {
            using var client = new MountedMatchClient("http://localhost", "mordor-track-walk");
            var view = HuntLayoutTests.Screen(CommandKind.ResolveHuntDamage);
            view.MatchId = "mordor-track-walk";
            view.Fellowship.MordorStep = 3;
            view.Fellowship.RegionId = "mordor-track";
            client.Apply(view);
            yield return Settle();
            var map = client.Board.Map;
            map.Camera.Zoom = map.FitZoom * 1.8f;
            map.Camera.Pan = map.contentRect.center - map.Data.MordorTrack.Bounds.center * map.Camera.Zoom;
            map.Refresh();
            yield return Settle();
            Assert.AreEqual(3f, map.FellowshipMarker.TrackStep, "The first sight places the figure.");
            view.Fellowship.MordorStep = 4;
            view.Revision++;
            client.Apply(view);
            yield return new WaitForSecondsRealtime(MapFellowshipMarker.MotionSeconds / 2);
            Assert.That(map.FellowshipMarker.TrackStep, Is.GreaterThan(3f).And.LessThan(4f), "The figure is on its way.");
            yield return new WaitForSecondsRealtime(MapFellowshipMarker.MotionSeconds);
            Assert.AreEqual(4f, map.FellowshipMarker.TrackStep);
            map.Refresh();
            yield return Settle();
            var step = map.Q<Button>("mordor-track-step-4");
            Assert.That(map.FellowshipMarker.Root.worldBound.center.x, Is.EqualTo(step.worldBound.center.x).Within(2f));
        }

        // UI-156: acceptance screens of the track for both sides at the first, a middle and the last step, hidden and
        // revealed, in four layouts, zoomed in on the track.
        [UnityTest]
        public IEnumerator AcceptanceScreensForBothSidesAndSteps()
        {
            using var client = new MountedMatchClient("http://localhost", "mordor-track-acceptance");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    foreach (var side in new[] { Side.FreePeoples, Side.Shadow })
                        foreach (var (step, hidden) in new[] { (0, true), (3, false), (5, true) })
                        {
                            client.Interface.Windows.Clear();
                            var view = HuntLayoutTests.Screen(CommandKind.ResolveHuntDamage);
                            view.MatchId = "mordor-track-acceptance";
                            view.You = side;
                            view.Hunt = null;
                            view.Decision = null;
                            view.Revision++;
                            view.Fellowship.MordorStep = step;
                            view.Fellowship.RegionId = "mordor-track";
                            view.Fellowship.Hidden = hidden;
                            view.Fellowship.Corruption = 4 + step;
                            view.Fellowship.MordorAttemptedThisTurn = step != 3;
                            client.Apply(view);
                            // A later step walks the figure along the path first; AUDIO-06: with the Corruption changed too,
                            // the step follows the Corruption's stage.
                            yield return new WaitForSecondsRealtime((float)TrackSequence.SecondStageSeconds + MapFellowshipMarker.MotionSeconds + .05f);
                            var map = client.Board.Map;
                            map.FocusMordorTrack();
                            yield return Settle();
                            Assert.IsTrue(TrackInView(map), $"{side} {step} {width}/{scale}");
                            var figure = map.FellowshipMarker.Root;
                            var stone = map.MordorTrack.StepButton(step);
                            Assert.That(figure.worldBound.center.x, Is.EqualTo(stone.worldBound.center.x).Within(2f));
                            Assert.AreEqual(hidden ? "◇" : "◆", ((Button)figure).text);
                            for (var passed = 0; passed < step; passed++)
                                Assert.IsTrue(map.MordorTrack.StepButton(passed).ClassListContains(MapMordorTrack.PassedClass));
                            Assert.AreEqual(step == 3 ? DisplayStyle.Flex : DisplayStyle.None, map.Q("fellowship-mordor-idle").resolvedStyle.display);
                            LayoutTests.Save(target, $"ui156-mordor-{side}-{step}-{width}-{scale:0.00}.png");
                        }
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        internal static bool TrackInView(MapCanvas map) => Enumerable.Range(0, MordorTrackGeometry.LastStep + 1)
            .All(step => map.MordorTrack.StepButton(step).style.display.value == DisplayStyle.Flex);

        private static PlayerView FollowupScreen(bool guide)
        {
            var view = HuntLayoutTests.Screen(guide ? CommandKind.SelectGuide : CommandKind.ResolveHuntDamage);
            view.MatchId = "mordor-followups-layout";
            view.Fellowship.MordorStep = 4;
            view.Fellowship.RegionId = "mordor-track";
            view.Fellowship.MordorAttemptedThisTurn = true;
            view.Hunt.Mordor = true;
            view.Hunt.Dice.Clear();
            view.Hunt.DamagePending = false;
            if (guide)
            {
                UseMordorReport(view);
                return view;
            }
            view.Decision.Kind = DecisionKind.EffectChoice;
            view.Decision.Presentation = "FellowshipCompanions";
            view.Hunt.SourceId = "base.sh.character.14";
            view.Hunt.SeparatesCompanions = true;
            view.Hunt.Damage = 2;
            view.Hunt.Reveal = false;
            view.Hunt.Stop = false;
            view.Options.Clear();
            var option = new ActionOption { Kind = CommandKind.ChooseEffect, Minimum = 2, Maximum = 2 };
            foreach (var id in view.OwnCompanions.Select(c => c.Id))
            {
                option.EligibleIds.Add(id);
                option.EffectChoices.Add(new EffectChoiceView { Id = id, SourceId = id,
                    Title = CharacterLibrary.Name(view, id), Detail = "Remove from play on the Mordor Track.",
                    SelectionGroup = "Companions", GroupMinimum = 2, GroupMaximum = 2 });
            }
            view.Options.Add(option);
            UseMordorReport(view);
            return view;
        }

        [UnityTest]
        public IEnumerator TrackActivityAndTileFitFourLayoutsWithoutScrollingOrMovingTheCamera()
        {
            using var client = new MountedMatchClient("http://localhost", "mordor-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    foreach (var attempted in new bool?[] { false, true, null })
                    {
                        var view = Screen(attempted);
                        client.Apply(view);
                        client.SetOnline();
                        client.Board.Hunt.Close();
                        client.Board.Fellowship.Open();
                        yield return Settle();
                        var board = client.Board;
                        var activity = board.Fellowship.Root.Q<Label>("mordor-activity");
                        Assert.AreEqual(DisplayStyle.Flex, activity.resolvedStyle.display);
                        Check(board, board.Fellowship.Root);
                        var locate = board.Fellowship.Root.Q<Button>("focus-fellowship");
                        locate.Focus();
                        client.Refresh(view);
                        yield return Settle();
                        Assert.AreSame(activity, board.Fellowship.Root.Q<Label>("mordor-activity"));
                        Assert.AreSame(locate, locate.focusController.focusedElement);
                        PointerGestureEvents.ClickAt(locate);
                        // UI-155: in Mordor the Fellowship's button brings the whole track into view.
                        Assert.IsTrue(TrackInView(board.Map));
                        LayoutTests.Save(target, $"ui34-mordor-activity-{attempted}-{width}-{scale:0.00}.png");
                        if (attempted != true)
                            continue;
                        board.Hunt.Open();
                        yield return Settle();
                        Check(board, board.Hunt.Root);
                        StringAssert.Contains("No Hunt roll", board.Hunt.Root.Q<Label>("hunt-source").text);
                        Assert.IsTrue(board.Fellowship.Root.Q<Label>("mordor-step-4").ClassListContains("selected"));
                        Assert.IsFalse(board.Fellowship.Root.Q<Label>("mordor-step-5").ClassListContains("selected"));
                        Assert.AreEqual(DisplayStyle.None, board.Victory.Banner.style.display.value);
                        LayoutTests.Save(target, $"ui34-mordor-tile-{width}-{scale:0.00}.png");
                    }
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        private static PlayerView Screen(bool? attempted)
        {
            var view = HuntLayoutTests.Screen(CommandKind.ResolveHuntDamage);
            view.MatchId = "mordor-layout";
            view.Decision.Id = "mordor-" + attempted;
            view.Fellowship.MordorStep = 4;
            view.Fellowship.RegionId = "mordor-track";
            view.Fellowship.MordorAttemptedThisTurn = attempted;
            view.Hunt.Mordor = true;
            view.Hunt.Dice.Clear();
            view.Hunt.EyeDamage = 0;
            UseMordorReport(view);
            return view;
        }

        private static void UseMordorReport(PlayerView view)
        {
            view.HuntReport.Entries.Clear();
            foreach (var kind in new[] { HuntReportKind.Started, HuntReportKind.TileDrawn, HuntReportKind.DamageDetermined })
                view.HuntReport.Entries.Add(new HuntReportEntryView
                {
                    Kind = kind,
                    SourceId = view.Hunt.SourceId,
                    Hunt = view.Hunt
                });
        }

        private static void Check(BoardScreen board, VisualElement root)
        {
            Assert.IsFalse(board.View.HuntReport.Entries.Any(e => e.Kind is HuntReportKind.Rolled or HuntReportKind.Rerolled));
            Assert.LessOrEqual(root.worldBound.yMax, board.Map.worldBound.yMax + 1);
            Assert.GreaterOrEqual(root.worldBound.yMin, board.Map.worldBound.yMin);
            Assert.IsEmpty(root.Query<ScrollView>().ToList());
            foreach (var element in root.Query<VisualElement>().ToList().Where(Visible))
            {
                if (element is Button button)
                {
                    var picked = button.panel.Pick(button.worldBound.center);
                    Assert.IsTrue(picked == button || button.Contains(picked), "Covered: " + button.name);
                    Assert.GreaterOrEqual(button.worldBound.height + 1, 24, button.name);
                }
                if (element is not TextElement text || text.text.Length == 0 || text.ClassListContains("hunt-report-event"))
                    continue;
                var size = text.MeasureTextSize(text.text, text.contentRect.width, VisualElement.MeasureMode.Exactly,
                    0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(size.y, text.contentRect.height + 1, "Clipped: " + text.name + " / " + text.text);
                Assert.LessOrEqual(text.worldBound.xMax, root.worldBound.xMax + 1, text.name);
            }
        }

        private static bool Visible(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return true;
        }

        private static IEnumerator Settle()
        {
            yield return null;
            yield return new WaitForSecondsRealtime(.12f);
            yield return null;
        }
    }
}
