using System.Collections;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Wotr.Fixtures;

namespace Wotr.PlayTests
{
    public sealed class HuntLayoutTests
    {
        [UnityTest]
        public IEnumerator ClosedHuntWindowStaysClosedWithinItsHuntAndKeepsChoicesUntilANewOwnDecision()
        {
            using var client = new MountedMatchClient("http://localhost", "hunt-window");
            var view = Screen(CommandKind.RerollHunt);
            view.Decision.Id = "first";
            client.Apply(view);
            client.SetOnline();
            yield return Settle();
            var board = client.Board;
            var pan = board.Map.Camera.Pan;
            // UI-162: a new Hunt opens its window; one the player closes stays closed during that Hunt.
            Assert.IsTrue(board.Hunt.IsOpen, "A new Hunt opens its window.");
            PointerGestureEvents.ClickAt(board.Hunt.Root.Q<Button>("close-hunt"));
            client.Refresh(view);
            yield return Settle();
            Assert.IsFalse(board.Hunt.IsOpen, "A closed Hunt window stays closed within its Hunt.");
            PointerGestureEvents.ClickAt(board.Hunt.Tab);
            yield return Settle();
            Assert.IsTrue(board.Hunt.IsOpen, "The console's Hunt plaque opens it again.");
            Move(board.Hunt.Root.Q<Button>("hunt-die-roll-0"));
            yield return new WaitForSecondsRealtime(.2f);
            Move(board.Phase);
            yield return new WaitForSecondsRealtime(.3f);
            Assert.IsTrue(board.Hunt.IsOpen, "The window remains after the pointer leaves.");
            var die = board.Hunt.Root.Q<Button>("hunt-die-roll-0");
            PointerGestureEvents.ClickAt(die);
            Assert.IsTrue(die.ClassListContains("selected"));
            board.Hunt.Close();
            client.Refresh(view);
            yield return Settle();
            Assert.IsFalse(board.Hunt.IsOpen);
            board.Hunt.Open();
            yield return Settle();
            Assert.IsTrue(die.ClassListContains("selected"), "Closing the window keeps the unsent choice.");
            view = Screen(CommandKind.RerollHunt);
            view.Revision++;
            view.Decision.Id = "second";
            client.Refresh(view);
            yield return Settle();
            Assert.IsTrue(board.Hunt.IsOpen);
            Assert.IsFalse(die.ClassListContains("selected"));
            Assert.AreEqual(pan, board.Map.Camera.Pan);
        }

        private static void Move(VisualElement target)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = target.worldBound.center });
            target.SendEvent(move);
        }

        [UnityTest]
        public IEnumerator RerollsTileAndGuideFitTheHuntWindowInFourLayouts()
        {
            using var client = new MountedMatchClient("http://localhost", "hunt-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    // UI-162: the allocation belongs to the turn console (HuntNetworkTests); the window shows the Hunt.
                    foreach (var kind in new[] { CommandKind.RerollHunt, CommandKind.ResolveHuntDamage, CommandKind.SelectGuide, CommandKind.RespondEffect })
                    {
                        var view = Screen(kind);
                        var cardSide = view.You == Side.FreePeoples ? "fp" : "sh";
                        view.Hand.AddRange(Enumerable.Range(1, 6).Select(i => $"base.{cardSide}.character.{i:00}"));
                        view.Decision.Id = kind + "/" + width + "/" + scale;
                        client.Apply(view);
                        client.SetOnline();
                        client.Board.Fellowship.Open();
                        client.Board.Hunt.Open();
                        yield return Settle();
                        var board = client.Board;
                        LayoutTests.Save(target, $"ui34-hunt-{kind}-{width}-{scale:0.00}.png");
                        Assert.AreSame(board.Hunt.DecisionHost, board.Tray.parent);
                        Check(board);
                        var cards = board.Root.Q("edge-cards");
                        Assert.IsFalse(board.Hunt.Root.worldBound.Overlaps(cards.worldBound),
                            $"The Hunt window stands clear of the hand · {kind} {width} {scale:0.00} · {board.Hunt.Root.worldBound} / {cards.worldBound}");
                        Assert.IsNotNull(board.Hunt.Root.Q("hunt-stages"), "The stage ribbon heads the Hunt.");
                        Assert.That(board.Hand.worldBound.yMax, Is.LessThanOrEqualTo(board.Root.worldBound.yMax + 1));
                        if (board.Hand.ClassListContains("edge-hand-overflow"))
                        {
                            Assert.That(board.Hand.worldBound.xMax, Is.LessThan(board.Edges.Right.worldBound.xMin));
                            Assert.That(board.Hand.worldBound.yMax, Is.LessThan(board.Root.Q("board-stage").worldBound.yMax));
                        }
                        var pan = board.Map.Camera.Pan;
                        var tile = board.Hunt.Tile.Root;
                        var die = board.Hunt.Root.Q<Button>("hunt-die-roll-0");
                        client.Apply(view);
                        yield return Settle();
                        Assert.AreSame(tile, board.Hunt.Tile.Root);
                        Assert.AreSame(die, board.Hunt.Root.Q<Button>("hunt-die-roll-0"));
                        Assert.That(Vector2.Distance(pan, board.Map.Camera.Pan), Is.LessThan(.001f),
                            "Repeated projection may round cover bounds below a pixel, but must not navigate the camera.");
                        if (kind == CommandKind.RerollHunt)
                        {
                            PointerGestureEvents.ClickAt(die);
                            yield return Settle();
                            Assert.IsTrue(die.ClassListContains("selected"));
                            Assert.AreEqual("Reroll selected", board.Tray.Q<Button>("confirm-decision").text);
                        }
                        if (kind == CommandKind.ResolveHuntDamage)
                        {
                            Assert.AreEqual("Damage 3", board.Hunt.Root.Q<Label>("hunt-damage").text);
                            Assert.AreEqual("Lose Guide\nDamage 3 → 0", board.Hunt.Root.Q<Button>("hunt-response-Guide").Q<Label>("hunt-response-consequence").text);
                            Assert.IsTrue(Visible(board.Hunt.Root.Q<Label>("hunt-reveal")));
                            Assert.IsTrue(Visible(board.Hunt.Root.Q<Label>("hunt-stop")));
                            yield return CheckResponseTargets(board);
                            board.Fellowship.Close();
                            yield return Settle();
                            yield return CheckResponseTargets(board);
                        }
                        if (kind == CommandKind.SelectGuide)
                        {
                            Assert.IsTrue(Visible(board.Hunt.Guides.Root));
                            Assert.IsFalse(Visible(board.Hunt.Tile.Root), "The current portraits dominate the earlier tile.");
                            Assert.IsFalse(Visible(board.Hunt.Dice.Root));
                        }
                        if (kind == CommandKind.RespondEffect)
                        {
                            // UI-291: the console's Pass declines the window, glows and names what it skips.
                            Assert.IsNull(board.Tray.Q<Button>("action-DeclineReaction"));
                            var pass = board.Console.Actions.Pass;
                            Assert.IsTrue(pass.enabledSelf);
                            Assert.IsTrue(pass.ClassListContains(ChoiceGlow.ClassName));
                            StringAssert.Contains("Mithril Coat and Sting · Replace tile", pass.tooltip);
                            // UI-285: Mithril is a glowing card object in the response row, not a tray choice.
                            var mithril = board.Hunt.Root.Q<Button>("effect-choice-card.mithril-replace");
                            Assert.IsTrue(mithril.ClassListContains("legal-target"));
                            Assert.IsTrue(mithril.ClassListContains(ChoiceGlow.ClassName));
                            Assert.IsNull(board.Tray.Q<Button>("effect-choice-card.mithril-replace"));
                            Assert.AreEqual(DisplayStyle.Flex, board.Hunt.Responses.Root.style.display.value);
                            // The object fills its row, so its pulse must not swell it past the window's frame.
                            for (var sample = 0; sample < 8; sample++)
                            {
                                var frame = board.Hunt.Root.worldBound;
                                Assert.GreaterOrEqual(mithril.worldBound.xMin, frame.xMin, "The pulsing card stays inside the Hunt window");
                                Assert.LessOrEqual(mithril.worldBound.xMax, frame.xMax, "The pulsing card stays inside the Hunt window");
                                yield return new WaitForSecondsRealtime(.25f);
                            }
                        }
                        // UI-141: the pool list opens from the header icon and fits beside every decision. UI-184: under
                        // way it drops over the window's content, so the window neither moves nor grows.
                        var window = board.Hunt.Root.worldBound;
                        PointerGestureEvents.ClickAt(board.Hunt.PoolToggle);
                        yield return Settle();
                        Assert.IsTrue(Visible(board.Hunt.Pool.Root));
                        Assert.AreEqual(view.HuntPool.Tiles.Count, board.Hunt.Pool.Root.Q("hunt-pool-entries").childCount);
                        Assert.IsTrue(board.Hunt.Pool.Root.Q("hunt-pool-entries").Children().All(entry => entry.tooltip.Length > 0));
                        Assert.AreEqual(window, board.Hunt.Root.worldBound, "The open pool keeps the window in place · " + kind);
                        Assert.AreEqual(board.Hunt.Root.IndexOf(board.Hunt.DecisionHost) + 1, board.Hunt.Root.IndexOf(board.Hunt.Pool.Root),
                            "The dropped list is drawn over the window's content.");
                        var list = board.Hunt.Pool.Root.worldBound;
                        Assert.GreaterOrEqual(list.xMin, board.Map.worldBound.xMin, "The list stays on the map · " + kind);
                        Assert.LessOrEqual(list.xMax, board.Map.worldBound.xMax + 1, "The list stays on the map · " + kind);
                        Assert.LessOrEqual(list.yMax, board.Console.Root.worldBound.yMin + 1, "The list stays above the console · " + kind);
                        CheckPool(board, $"{kind} {width} {scale:0.00}");
                        LayoutTests.Save(target, $"ui141-hunt-pool-{kind}-{width}-{scale:0.00}.png");
                        PointerGestureEvents.ClickAt(board.Hunt.PoolToggle);
                        yield return Settle();
                        Assert.IsFalse(Visible(board.Hunt.Pool.Root));
                        LayoutTests.Save(target, $"ui34-hunt-{kind}-{width}-{scale:0.00}.png");
                        board.Fellowship.Close();
                        board.Hunt.Close();
                    }
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        // UI-184 (the user's screenshot): the Hunt window at rest with its pool open; the tiles used to ride over the
        // heading there.
        [UnityTest]
        public IEnumerator ThePoolAtRestReadsAsAGridUnderItsHeadingInFourLayouts()
        {
            using var client = new MountedMatchClient("http://localhost", "hunt-pool-layout");
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Interface.Preferences.SetScale(scale);
                    var view = new BoardFixture().View;
                    view.MatchId = "hunt-pool-layout";
                    view.HuntPool = Pool();
                    client.Apply(view);
                    client.SetOnline();
                    var board = client.Board;
                    board.Hunt.Open();
                    yield return Settle();
                    if (!Visible(board.Hunt.Pool.Root))
                        PointerGestureEvents.ClickAt(board.Hunt.PoolToggle);
                    yield return Settle();
                    Assert.IsTrue(board.Hunt.Root.ClassListContains("hunt-idle"), "The window is at rest.");
                    Assert.AreEqual(board.Hunt.Root.IndexOf(board.Hunt.Box.Root) + 1, board.Hunt.Root.IndexOf(board.Hunt.Pool.Root),
                        "At rest the list stands in the window under the Hunt Box.");
                    Check(board);
                    CheckPool(board, $"rest {width} {scale:0.00}");
                    LayoutTests.Save(target, $"ui184-hunt-pool-rest-{width}-{scale:0.00}.png");
                    board.Hunt.Close();
                    yield return Settle();
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
        }

        // UI-184: the heading stands whole above the tiles, the entries keep one grid of equal cells, the Mordor tiles
        // follow on a line of their own, and the counts read at a glance.
        private static void CheckPool(BoardScreen board, string label)
        {
            var pool = board.Hunt.Pool.Root;
            Assert.IsTrue(Visible(pool), label);
            var heading = pool.Q<Label>("hunt-pool-heading");
            var measured = heading.MeasureTextSize(heading.text, heading.contentRect.width, VisualElement.MeasureMode.Exactly, 0,
                VisualElement.MeasureMode.Undefined);
            Assert.LessOrEqual(measured.y, heading.contentRect.height + 1, "The heading keeps its height · " + label);
            var entries = pool.Q("hunt-pool-entries").Children().ToList();
            var prepared = pool.Q("hunt-pool-prepared").Children().ToList();
            var top = entries.Min(entry => entry.worldBound.yMin);
            Assert.LessOrEqual(heading.worldBound.yMax, top + 0.5f, "The heading stands above the tiles · " + label);
            // A scaled panel snaps each edge to a device pixel, so equal cells may differ by less than one point.
            var cell = entries[0].layout.width;
            var columns = entries.Where(entry => Mathf.Abs(entry.worldBound.yMin - top) < 1).Select(entry => entry.worldBound.xMin).ToList();
            foreach (var entry in entries.Concat(prepared))
                Assert.AreEqual(cell, entry.layout.width, 1f, "Every entry has the same cell · " + label);
            foreach (var entry in entries)
                Assert.IsTrue(columns.Any(x => Mathf.Abs(x - entry.worldBound.xMin) < 1.5f), "The entries keep their columns · " + label);
            var bottom = entries.Max(entry => entry.worldBound.yMax);
            foreach (var entry in prepared)
                Assert.GreaterOrEqual(entry.worldBound.yMin, bottom - 0.5f, "The Mordor tiles have their own line · " + label);
            foreach (var entry in entries.Concat(prepared))
            {
                Assert.GreaterOrEqual(entry.worldBound.xMin, pool.worldBound.xMin - 0.5f, label);
                Assert.LessOrEqual(entry.worldBound.xMax, pool.worldBound.xMax + 0.5f, label);
                Assert.LessOrEqual(entry.worldBound.yMax, pool.worldBound.yMax + 0.5f, label);
                Assert.GreaterOrEqual(entry.Q(className: "hunt-face").layout.height, 27, "Tiles stay large (28 points less pixel snapping) · " + label);
                Assert.GreaterOrEqual(entry.Q<Label>(className: "hunt-pool-count").resolvedStyle.fontSize, 13, "Counts stay legible · " + label);
            }
        }

        [UnityTest]
        public IEnumerator ThreeKnownStepsCanBeInspectedWithoutLosingTheLiveChoice()
        {
            using var client = new MountedMatchClient("http://localhost", "hunt-timeline");
            var view = Screen(CommandKind.RerollHunt);
            view.Decision.Id = "timeline-reroll";
            view.HuntReport.Entries.Clear();
            for (var i = 0; i < 6; i++)
                view.HuntReport.Entries.Add(new HuntReportEntryView { Kind = HuntReportKind.Corruption,
                    Outcome = new HuntOutcomeView { Corruption = i } });
            client.Apply(view);
            client.SetOnline();
            yield return Settle();
            var board = client.Board;
            var pan = board.Map.Camera.Pan;
            var die = board.Hunt.Root.Q<Button>("hunt-die-roll-0");
            PointerGestureEvents.ClickAt(die);
            yield return Settle();
            var labels = board.Hunt.Root.Query<Label>(className: "hunt-report-event").ToList();
            Assert.AreEqual(3, labels.Count(Visible));
            StringAssert.Contains("Corruption 3/12", labels[0].text);
            StringAssert.Contains("Corruption 5/12", labels[2].text);
            PointerGestureEvents.ClickAt(board.Hunt.Root.Q<Button>("hunt-report-previous"));
            yield return Settle();
            StringAssert.Contains("Corruption 4/12", labels[2].text);
            labels[1].Focus();
            yield return Settle();
            Assert.AreSame(labels[1], board.Root.panel.focusController.focusedElement);
            view.Revision++;
            view.HuntReport.Entries.Add(new HuntReportEntryView { Kind = HuntReportKind.Corruption,
                Outcome = new HuntOutcomeView { Corruption = 6 } });
            client.Refresh(view);
            yield return Settle();
            Assert.AreSame(labels[2], board.Hunt.Root.Q<Label>("hunt-report-entry"));
            StringAssert.Contains("Corruption 4/12", labels[2].text);
            Assert.IsTrue(die.ClassListContains("selected"));
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreSame(board.Hunt.DecisionHost, board.Tray.parent);
        }

        [UnityTest]
        public IEnumerator MissingAndAutomaticReportsDoNotOpenOrAdvertiseAnUnrelatedChoice()
        {
            using var client = new MountedMatchClient("http://localhost", "hunt-automatic");
            var view = Screen(CommandKind.RerollHunt);
            var report = view.HuntReport;
            view.Hunt = null;
            view.HuntReport = null;
            view.Decision = null;
            view.Options.Clear();
            client.Apply(view);
            yield return Settle();
            var board = client.Board;
            Assert.IsFalse(board.Hunt.IsOpen, "Without a Hunt the window waits under its plaque.");
            board.Hunt.Open();
            yield return Settle();
            Assert.AreEqual("No Hunt report yet", board.Hunt.Root.Q<Label>("hunt-step").text);
            Assert.IsFalse(Visible(board.Hunt.Tile.Root));
            Assert.IsFalse(Visible(board.Hunt.Dice.Root));
            board.Hunt.Close();
            report.Completed = true;
            view.HuntReport = report;
            view.Revision++;
            client.Refresh(view);
            yield return Settle();
            Assert.IsFalse(board.Hunt.IsOpen, "A completed Hunt does not open the window.");
            Assert.AreEqual("Hunt", board.Hunt.Tab.text);
            board.Hunt.Open();
            yield return Settle();
            Assert.AreEqual("Complete", board.Hunt.Root.Q<Label>("hunt-step").text);
            Assert.IsFalse(Visible(board.Hunt.Responses.Root));
            Assert.IsEmpty(board.Hunt.Root.Q<Label>("hunt-owner").text);
            view = JsonConvert.DeserializeObject<PlayerView>(JsonConvert.SerializeObject(view));
            view.MatchId = "other-match";
            view.HuntReport = null;
            client.Apply(view);
            yield return Settle();
            Assert.IsFalse(board.Hunt.IsOpen, "Another match starts with the window closed.");
            board.Hunt.Open();
            yield return Settle();
            Assert.AreEqual("No Hunt report yet", board.Hunt.Root.Q<Label>("hunt-step").text);
        }

        // UI-142: a response's click applies it at once, so only the first one is clicked; every response stays
        // reachable under the pointer, and the applied one keeps its place.
        private static IEnumerator CheckResponseTargets(BoardScreen board)
        {
            using var mouse = new EngineMouseInput(board.Root);
            foreach (var response in new[] { HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion })
            {
                var option = board.Hunt.Root.Q<Button>("hunt-response-" + response);
                var picked = option.panel.Pick(option.worldBound.center);
                Assert.IsTrue(picked == option || option.Contains(picked), response + " is reachable. Picked: " + picked?.name);
            }
            var ring = board.Hunt.Root.Q<Button>("hunt-response-" + HuntResponse.Ring);
            var point = ring.worldBound.center;
            mouse.Click(ring);
            yield return Settle();
            ring = board.Hunt.Root.Q<Button>("hunt-response-" + HuntResponse.Ring);
            Assert.IsTrue(ring.ClassListContains("selected"), "The clicked response is applied.");
            var applied = ring.panel.Pick(point);
            Assert.IsTrue(applied == ring || ring.Contains(applied),
                "Applying a response must preserve its pointer target with Fellowship " +
                (board.Fellowship.IsOpen ? "open" : "closed") + ". Picked: " + applied?.name);
            Check(board);
        }

        private static void Check(BoardScreen board)
        {
            Assert.Greater(board.Map.worldBound.height, board.Root.worldBound.height / 2);
            Assert.LessOrEqual(board.Hunt.Root.worldBound.yMax, board.Map.worldBound.yMax + 1);
            Assert.GreaterOrEqual(board.Hunt.Root.worldBound.yMin, board.Map.worldBound.yMin);
            Assert.LessOrEqual(board.Hunt.Root.worldBound.yMax, board.Console.Root.worldBound.yMin + 1, "UI-162: the Hunt window stays above the console.");
            Assert.IsEmpty(board.Hunt.Root.Query<ScrollView>().ToList());
            Assert.IsEmpty(board.Hunt.Root.Query<TextField>().ToList());
            foreach (var button in board.Hunt.Root.Query<Button>().ToList().Where(Visible))
            {
                Assert.GreaterOrEqual(button.worldBound.xMin, board.Map.worldBound.xMin, button.name);
                Assert.LessOrEqual(button.worldBound.xMax, board.Map.worldBound.xMax + 1, button.name);
                Assert.LessOrEqual(button.worldBound.yMax, board.Map.worldBound.yMax + 1, button.name);
                Assert.GreaterOrEqual(button.worldBound.height + 1, 24, button.name + " (one pixel rounding tolerance)");
                var picked = button.panel.Pick(button.worldBound.center);
                Assert.IsTrue(picked == button || button.Contains(picked), "Covered: " + Path(button) + " '" + button.text + "' by " + Path(picked) + " / " + picked?.GetType().Name);
            }
            foreach (var label in board.Hunt.Root.Query<Label>().ToList().Where(Visible).Where(l => !l.ClassListContains("hunt-report-event")))
            {
                var measured = label.MeasureTextSize(label.text, label.contentRect.width, VisualElement.MeasureMode.Exactly, 0, VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(measured.y, label.contentRect.height + 1, "Clipped: " + label.name + " / " + label.text);
            }
        }

        private static string Path(VisualElement element)
        {
            var names = new System.Collections.Generic.List<string>();
            for (var item = element; item != null && names.Count < 4; item = item.parent)
                names.Add(item.name.Length > 0 ? item.name : item.GetType().Name + "." + string.Join(".", item.GetClasses()));
            return string.Join(" < ", names);
        }

        private static bool Visible(VisualElement element)
        {
            for (var item = element; item != null; item = item.parent)
                if (item.resolvedStyle.display == DisplayStyle.None || item.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            return true;
        }

        internal static PlayerView Screen(CommandKind kind)
        {
            var view = new BoardFixture().View;
            view.MatchId = "hunt-layout";
            view.You = kind is CommandKind.AllocateHunt or CommandKind.RerollHunt ? Side.Shadow : Side.FreePeoples;
            view.DecisionOwner = view.You;
            view.Phase = kind == CommandKind.AllocateHunt ? GamePhase.HuntAllocation : GamePhase.ActionResolution;
            view.Fellowship.Corruption = 11;
            view.Fellowship.Guide = "base.character.gandalf-grey";
            view.OwnCompanions = new[] { "gandalf-grey", "strider", "gimli", "boromir", "legolas", "merry", "pippin" }
                .Select(id => new CharacterView { Id = "base.character." + id, Location = "fellowship", Level = 3 }).ToList();
            view.Fellowship.CompanionIdentities = view.OwnCompanions.Select(c => CharacterLibrary.Identity(c.Id)).ToList();
            view.Fellowship.CompanionCount = 7;
            view.HuntPool = Pool();
            var option = new ActionOption { Kind = kind };
            view.Options.Clear();
            view.Options.Add(option);
            view.Decision = new DecisionView { Kind = kind switch
                { CommandKind.AllocateHunt => DecisionKind.AllocateHunt, CommandKind.RerollHunt => DecisionKind.HuntReroll,
                  CommandKind.SelectGuide => DecisionKind.Guide, CommandKind.RespondEffect => DecisionKind.Reaction,
                  _ => DecisionKind.HuntDamage } };
            if (kind == CommandKind.AllocateHunt)
            {
                view.Dice.Clear();
                for (var i = 0; i < 7; i++)
                    view.Dice.Add(new DieView { Id = "allocate-" + i, Owner = Side.Shadow, Location = DieLocation.Unrolled });
                option.EligibleIds = view.Dice.Where(d => d.Owner == view.You).Select(d => d.Id).ToList();
                option.Maximum = option.EligibleIds.Count;
                return view;
            }
            view.Hunt = new HuntView { SourceId = "fellowship-move", Bonus = 3, MovementBonus = 2, EffectBonus = 1, RerollBonus = 1, PoolCount = 12,
                RerollSources = { "stronghold", "army", "nazgul" } };
            for (var i = 0; i < 5; i++)
                view.Hunt.Dice.Add(new HuntDieView { Id = "roll-" + i, First = i + 1, Result = i + 1, Success = i >= 2 });
            if (kind == CommandKind.RerollHunt)
            {
                option.EligibleIds.AddRange(new[] { "roll-0", "roll-1" });
                option.Maximum = 2;
            }
            else
            {
                view.Hunt.TileId = "base.hunt.special-sh.die.0.1";
                view.Hunt.Face = new HuntTileFaceView { Pool = "special-sh", Value = "die", Stop = true, Source = "base" };
                view.Hunt.DamagePending = true;
                view.Hunt.Reveal = true;
                view.Hunt.Stop = true;
                if (kind == CommandKind.SelectGuide)
                    option.EligibleIds = view.OwnCompanions.Select(c => c.Id).Take(2).ToList();
                else if (kind == CommandKind.RespondEffect)
                {
                    view.Decision.Window = "hunt.replace";
                    view.Players.Add(new PlayerSummary { Side = Side.FreePeoples, TableCards = { "base.fp.character.05" } });
                    option.EffectChoices.Add(new EffectChoiceView { Id = "card.mithril-replace", SourceId = "base.fp.character.05",
                        Title = "Replace tile", Detail = "Draw a replacement before determining damage. The new tile is unknown.", Cost = "DiscardTableCard" });
                    view.Characters.Add(new CharacterView { Id = "lords.character.galadriel", DefinitionId = "lords.character.galadriel" });
                    option.EffectChoices.Add(new EffectChoiceView { Id = "lords.nenya.replace", SourceId = "lords.character.galadriel",
                        Title = "Nenya · Replace Eye", Detail = "Give Nenya to Shadow, remove this Eye from the game and draw another tile.", Cost = "TransferNenya" });
                    option.EligibleIds = option.EffectChoices.Select(c => c.Id).ToList();
                    view.Options.Add(new ActionOption { Kind = CommandKind.DeclineReaction });
                }
                else
                {
                    view.Hunt.DamagePending = false;
                    view.Hunt.Damage = 3;
                    option.HuntResponses.AddRange(new[] { HuntResponse.Ring, HuntResponse.Guide, HuntResponse.RandomCompanion });
                }
            }
            view.HuntReport = new HuntReportView { Id = "layout-report", Entries =
                { new HuntReportEntryView { Kind = HuntReportKind.Rolled, Hunt = view.Hunt },
                  new HuntReportEntryView { Kind = HuntReportKind.TileReplaced, Hunt = view.Hunt } } };
            return view;
        }

        // UI-141: a full pool — every standard face, a Kings tile, Sméagol, an unseen tile and two prepared specials.
        internal static HuntPoolView Pool()
        {
            HuntTileFaceView Face(string pool, string value, int count, bool reveal = false, string source = "base") =>
                new HuntTileFaceView { Pool = pool, Value = value, Count = count, Reveal = reveal, Source = source };
            return new HuntPoolView
            {
                Count = 17, Unseen = 1,
                Tiles = { Face("standard", "eye", 4, true), Face("standard", "3", 3), Face("standard", "2", 2), Face("standard", "2", 1, true),
                    Face("standard", "1", 2), Face("standard", "1", 2, true), Face("standard", "0", 2, true), Face("standard", "2", 1, source: "kings"),
                    Face(HuntTileText.Smeagol, "0", 1, source: "lords") },
                Prepared = { new HuntTileFaceView { Pool = "special-sh", Value = "die", Stop = true, Source = "base" },
                    new HuntTileFaceView { Pool = "special-fp", Value = "-1", Source = "base" } }
            };
        }

        private static IEnumerator Settle() { for (var i = 0; i < 4; i++) yield return null; }
    }
}
