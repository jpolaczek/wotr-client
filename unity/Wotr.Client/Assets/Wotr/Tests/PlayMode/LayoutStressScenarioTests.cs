using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // TEST-15 (owner): each layout stress scenario of the server's Testing catalog, opened as the owner opens it, keeps its
    // decision window, console, tray and open panels on the screen in the four layouts (1366×768 and 1920×1080 at 100 and
    // 125 %), without a decision scroll; every layout leaves a screenshot, .artifacts/screenshots/test15-<scenario>-*.png.
    public sealed class LayoutStressScenarioTests
    {
        [UnityTest] public IEnumerator TheFullRosterFits() => Wait(Check("layout-full-roster", Side.FreePeoples, OpenCharacters));
        [UnityTest] public IEnumerator TheFiveKingsRosterFits() => Wait(Check("layout-full-roster-five-kings", Side.FreePeoples, OpenCharacters));
        [UnityTest] public IEnumerator ThreeChieftainsBattleFits() => Wait(Check("layout-chieftains-battle", Side.Shadow, OpenDecision));
        [UnityTest] public IEnumerator TheHuntResponsesFit() => Wait(Check("layout-hunt-responses", Side.FreePeoples, OpenDecision));
        [UnityTest] public IEnumerator TheCouncilTravelFits() => Wait(Check("layout-council-travel", Side.Shadow, OpenDecision));
        [UnityTest] public IEnumerator TheWhiteRiderFits() => Wait(Check("layout-white-rider", Side.FreePeoples, OpenDecision));
        [UnityTest] public IEnumerator EveryResourceFits() => Wait(Check("layout-all-resources", Side.FreePeoples, OpenDecision));
        [UnityTest] public IEnumerator TheFullestEffectsPanelFits() => Wait(Check("layout-effects-sources", Side.FreePeoples, OpenEffects));
        // UI-430 (owner): the scenario stressed The Nazgûl Strike!'s three console options; they are its objects now, so it
        // checks those instead — no option strip, the red table cards in the open Effects panel, the glowing Hunt Box, the lit ✓.
        [UnityTest] public IEnumerator TheNazgulStrikesObjectsFit() => Wait(Check("layout-console-options", Side.Shadow, OpenStrikeObjects));

        // What a scenario opens, in one layout, with that layout's screen for its own screenshots.
        private delegate Task Opening(TestScenarioRoom room, BoardScreen board, Shot shot);

        private sealed class Shot
        {
            public RenderTexture Texture;
            public string Layout;
        }

        private static async Task Check(string scenario, Side side, Opening open)
        {
            var room = await TestScenarioRoom.Start(scenario);
            try
            {
                var client = room.Client(side);
                foreach (var width in new[] { 1366, 1920 })
                    foreach (var scale in new[] { 1f, 1.25f })
                        await Layout(room, client, scenario, width, scale, open);
            }
            finally
            {
                await room.End();
            }
        }

        private static async Task Layout(TestScenarioRoom room, MountedMatchClient client, string scenario, int width, float scale,
            Opening open)
        {
            using var viewport = new BoardCompletionLayoutTests.LayoutViewport(client, width, scale);
            await room.Settle();
            await open(room, client.Board, new Shot { Texture = viewport.Texture, Layout = $"{width}-{scale:0.00}" });
            await room.Settle();
            // The screenshot first, so a layout that fails still leaves its picture for review.
            LayoutTests.Save(viewport.Texture, $"test15-{scenario}-{width}-{scale:0.00}.png");
            LayoutTests.AssertNoDecisionScroll(client.Board);
            ExpansionMatrixLayoutTests.ResourcesFit(client.Board);
            OnScreen(client.Board, client.Interface.Content.parent.worldBound, $"{scenario} {width} {scale:0.00}");
        }

        // The decision this side owns, opened as its notice opens it.
        private static async Task OpenDecision(TestScenarioRoom room, BoardScreen board, Shot shot)
        {
            if (board.Notice.Opens)
                PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
        }

        private static async Task OpenCharacters(TestScenarioRoom room, BoardScreen board, Shot shot)
        {
            PointerGestureEvents.ClickAt(board.Characters.Tab);
            await room.Settle();
            Assert.IsTrue(board.Characters.Pinned, "The Characters tab opens with a click.");
        }

        // UI-424: every source is a whole tile with its whole title in the panel, which ends above the bottom row; the card
        // with the longest text opens its details beside the panel, whose width and columns stay.
        private static async Task OpenEffects(TestScenarioRoom room, BoardScreen board, Shot shot)
        {
            PointerGestureEvents.ClickAt(board.TableEffects.Tab);
            await room.Settle();
            Assert.IsTrue(board.TableEffects.Pinned, "The Effects tab opens with a click.");
            // UI-429 (owner): with game-wide effects in play the header still holds only the close button.
            Assert.IsTrue(board.View.Effects.Any(effect => effect.Targets.Any(target => target.Kind == "Global")),
                "The scenario has game-wide effects.");
            CollectionAssert.AreEqual(new[] { "close-effects" }, board.TableEffects.Root.Q(className: "window-header")
                .Query<Button>().ToList().Select(button => button.name).ToArray(), "The Effects header holds only Close.");
            var sources = EffectsPanelChecks.Sources(board);
            Assert.GreaterOrEqual(sources.Count, 17, "Every source is in view · " + shot.Layout);
            EffectsPanelChecks.TitlesInsideTiles(board, shot.Layout);
            EffectsPanelChecks.PanelAboveBottomRow(board, shot.Layout);
            var width = board.TableEffects.Root.layout.width;
            Assert.That(width, Is.EqualTo(board.TableEffects.Arrangement.PanelWidth).Within(1f), "The arrangement's width · " + shot.Layout);
            var grid = EffectsPanelChecks.Grid(board);
            LayoutTests.Save(shot.Texture, $"ui424-effects-sources-{shot.Layout}.png");
            PointerGestureEvents.ClickAt(LongestText(sources));
            // The pointer leaves the tile for the panel's header, so the hovered text rolls back (UI-221).
            CardGestures.Move(board.TableEffects.Root.Q(className: "panel-header"));
            await room.Settle();
            Assert.That(board.TableEffects.Root.layout.width, Is.EqualTo(width).Within(.5f), "The panel keeps its width · " + shot.Layout);
            EffectsPanelChecks.SameGrid(grid, EffectsPanelChecks.Grid(board), shot.Layout);
            EffectsPanelChecks.DetailsBesideThePanel(board, shot.Layout);
            EffectsPanelChecks.PanelAboveBottomRow(board, shot.Layout);
            LayoutTests.Save(shot.Texture, $"ui424-effects-details-{shot.Layout}.png");
        }

        // The table card whose Event and Combat text is the longest.
        private static Button LongestText(System.Collections.Generic.IEnumerable<Button> sources) => sources
            .Where(button => CardLibrary.Cards.ContainsKey(button.name.Substring("effect-source-".Length)))
            .OrderByDescending(button => Length(CardLibrary.Cards[button.name.Substring("effect-source-".Length)])).First();

        private static int Length(CardText card) => card.Event.Length + card.Combat.Length;

        private static async Task OpenStrikeObjects(TestScenarioRoom room, BoardScreen board, Shot shot)
        {
            await NazgulStrikeObjectTests.RedTile(room, board, "base.fp.character.08");
            NazgulStrikeObjectTests.AssertObjectsLit(board);
        }

        // Every shown window, panel and bar stays inside the screen — an edge panel above the bottom bar — and every shown
        // button inside its own window (the innermost one holding it), with its whole text.
        private static void OnScreen(BoardScreen board, Rect screen, string layout)
        {
            var hosts = new[]
            {
                board.DecisionWindow.Root, board.Console.Root, board.Console.Status.RaisedOptions, board.Tray,
                board.Characters.Root, board.TableEffects.Root, board.TableEffects.Details, board.OwnDice, board.OpponentDice
            };
            foreach (var host in hosts.Where(Shown))
            {
                Inside(host.worldBound, screen, host.name + " · " + layout);
                foreach (var button in host.Query<Button>().ToList().Where(button => Shown(button) && Innermost(hosts, host, button)))
                {
                    Inside(button.worldBound, host.worldBound, button.name + " in " + host.name + " · " + layout);
                    BoardCompletionLayoutTests.FullText(button);
                }
            }
            // The edge panels open between the top bar and the bottom row (the console's row); UI-424: the Effects panel with
            // every source and its details card as well.
            var table = Rect.MinMaxRect(screen.xMin, board.TopBar.Root.worldBound.yMax, screen.xMax, EffectsPanelChecks.BottomRow(board));
            if (Shown(board.Characters.Root))
                Inside(board.Characters.Root.worldBound, table, "characters-panel above the bottom row · " + layout);
            if (Shown(board.TableEffects.Root))
                Inside(board.TableEffects.Root.worldBound, table, "effects-panel above the bottom row · " + layout);
            if (Shown(board.TableEffects.Details))
                Inside(board.TableEffects.Details.worldBound, table, "effects-source-details above the bottom row · " + layout);
        }

        // No other host inside this one holds the button.
        private static bool Innermost(VisualElement[] hosts, VisualElement host, VisualElement button) =>
            !hosts.Any(other => other != host && host.Contains(other) && other.Contains(button));

        private static void Inside(Rect element, Rect bounds, string name)
        {
            Assert.GreaterOrEqual(element.xMin + 1, bounds.xMin, name);
            Assert.GreaterOrEqual(element.yMin + 1, bounds.yMin, name);
            Assert.LessOrEqual(element.xMax - 1, bounds.xMax, name);
            Assert.LessOrEqual(element.yMax - 1, bounds.yMax, name);
        }

        private static bool Shown(VisualElement element)
        {
            for (var current = element; current != null; current = current.parent)
                if (current.resolvedStyle.display == DisplayStyle.None || current.resolvedStyle.visibility == Visibility.Hidden ||
                    current.resolvedStyle.opacity <= 0)
                    return false;
            return element.worldBound.width > 0 && element.worldBound.height > 0;
        }
    }
}
