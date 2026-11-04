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
    // WEB-01 (owner, 2026-11-04): the website's pictures, .artifacts/screenshots/site-*.png: regions crowded with characters
    // and armies, battles and effects, at 1920×1080 and 100 %. Each scene comes from the server's Testing catalog, so anyone
    // can open it in the game; the picture is taken once its windows have settled. The infra/site pictures come from these.
    public sealed class WebsiteScreenshotTests
    {
        private const int Width = 1920;
        private const int SettledSteps = 14;
        private const int PresentationSteps = 70;
        private const float CloseUp = 1.75f;
        private const float Closest = 2.5f;
        private const string ShadowCard = "base.sh.strategy.09";
        private const string FreePeoplesCard = "base.fp.strategy.01";

        private delegate Task Pictures(TestScenarioRoom room, BoardScreen board, RenderTexture target);

        [UnityTest] public IEnumerator TheWarAtItsHeight() => Wait(Capture("showcase-war", Side.FreePeoples, War));
        [UnityTest] public IEnumerator TheGreatSiege() => Wait(Capture("showcase-siege", Side.Shadow, Siege));
        [UnityTest] public IEnumerator TheEffectsPanel() => Wait(Capture("layout-effects-sources", Side.FreePeoples, Effects));
        [UnityTest] public IEnumerator EveryCharacter() => Wait(Capture("layout-full-roster", Side.FreePeoples, Characters));
        [UnityTest] public IEnumerator TheFiveKings() => Wait(Capture("layout-full-roster-five-kings", Side.FreePeoples, Kings));
        [UnityTest] public IEnumerator TheHunt() => Wait(Capture("layout-hunt-responses", Side.FreePeoples, Hunt));

        private static async Task Capture(string scenario, Side side, Pictures pictures)
        {
            var room = await TestScenarioRoom.Start(scenario);
            try
            {
                var client = room.Client(side);
                using var viewport = new BoardCompletionLayoutTests.LayoutViewport(client, Width, 1f);
                await Rest(room);
                await pictures(room, client.Board, viewport.Texture);
            }
            finally
            {
                await room.End();
            }
        }

        // The whole front, then each crowded region up close.
        private static async Task War(TestScenarioRoom room, BoardScreen board, RenderTexture target)
        {
            LayoutTests.Save(target, "site-war.png");
            await Region(room, board, target, "osgiliath", Closest, "site-gondor.png");
            await Region(room, board, target, "helms-deep", CloseUp, "site-rohan.png");
            await Region(room, board, target, "dale", CloseUp, "site-north.png");
            Assert.IsTrue(board.Map.MarkerVisibility.Detail, "Close up the figures stand as miniatures.");
        }

        private static async Task Region(TestScenarioRoom room, BoardScreen board, RenderTexture target, string region, float zoom,
            string file)
        {
            var anchor = board.Map.Data.Shapes[region].Anchor;
            board.Map.FocusOn(new Vector2(anchor[0], anchor[1]), board.Map.FitZoom * zoom);
            await Rest(room);
            LayoutTests.Save(target, file);
        }

        // The battle window at the Combat cards, then the dice both sides roll after them.
        private static async Task Siege(TestScenarioRoom room, BoardScreen board, RenderTexture target)
        {
            await OpenDecision(room, board);
            LayoutTests.Save(target, "site-siege.png");
            TitlesStayInTheirCards(board);
            await room.Send(Side.Shadow, CommandKind.CommitCombatCard, new CommandPayload { CardId = ShadowCard });
            await room.Until(() => room.Client(Side.FreePeoples).Board.View.Decision?.Kind == DecisionKind.CombatCard);
            await room.Send(Side.FreePeoples, CommandKind.CommitCombatCard, new CommandPayload { CardId = FreePeoplesCard });
            await PresentationEnds(room);
            await OpenDecision(room, board);
            LayoutTests.Save(target, "site-dice.png");
        }

        // UI-495: with a full hand every Combat title wraps inside its narrow card rather than running out of it.
        private static void TitlesStayInTheirCards(BoardScreen board)
        {
            var cards = board.Root.Query<Button>(className: "combat-choice").ToList();
            Assert.GreaterOrEqual(cards.Count, 4, "The scene offers a full hand.");
            foreach (var card in cards)
            {
                var title = card.Q<Label>("combat-choice-title");
                Assert.LessOrEqual(title.worldBound.xMax, card.worldBound.xMax + 1, title.text + " stays inside its card.");
                var measured = title.MeasureTextSize(title.text, title.resolvedStyle.width, VisualElement.MeasureMode.AtMost, 0,
                    VisualElement.MeasureMode.Undefined);
                Assert.LessOrEqual(measured.x, title.resolvedStyle.width + 1, title.text + " wraps at its card's width.");
                Assert.LessOrEqual(measured.y, title.resolvedStyle.height + 1, title.text + " shows whole, without an ellipsis.");
            }
        }

        // The panel at its fullest.
        private static async Task Effects(TestScenarioRoom room, BoardScreen board, RenderTexture target)
        {
            PointerGestureEvents.ClickAt(board.TableEffects.Tab);
            await Rest(room);
            Assert.IsTrue(board.TableEffects.Pinned, "The Effects tab opens with a click.");
            Assert.GreaterOrEqual(EffectsPanelChecks.Sources(board).Count, 17, "Every source is in view.");
            LayoutTests.Save(target, "site-effects.png");
        }

        private static async Task Characters(TestScenarioRoom room, BoardScreen board, RenderTexture target)
        {
            PointerGestureEvents.ClickAt(board.Characters.Tab);
            await Rest(room);
            PointerGestureEvents.ClickAt(Row(board, "Galadriel"));
            await Rest(room);
            LayoutTests.Save(target, "site-characters.png");
        }

        private static async Task Kings(TestScenarioRoom room, BoardScreen board, RenderTexture target)
        {
            PointerGestureEvents.ClickAt(board.Characters.Tab);
            await Rest(room);
            var portraits = board.Sovereigns.Root.Query<Button>(className: "sovereign-portrait").ToList();
            Assert.AreEqual(5, portraits.Count, "All five kings stand in the panel.");
            PointerGestureEvents.ClickAt(portraits[0]);
            await Rest(room);
            LayoutTests.Save(target, "site-kings.png");
        }

        private static async Task Hunt(TestScenarioRoom room, BoardScreen board, RenderTexture target)
        {
            await OpenDecision(room, board);
            LayoutTests.Save(target, "site-hunt.png");
        }

        private static Button Row(BoardScreen board, string name)
        {
            var rows = board.Characters.Root.Query<Button>(className: "character-roster-row").ToList();
            Assert.IsNotEmpty(rows, "The Characters panel lists its characters.");
            return rows.FirstOrDefault(row => row.Query<Label>().ToList().Any(label => label.text == name)) ?? rows[0];
        }

        // The decision this side owns, opened as its notice opens it.
        private static async Task OpenDecision(TestScenarioRoom room, BoardScreen board)
        {
            if (board.Notice.Opens)
                PointerGestureEvents.ClickAt(board.Notice.Button);
            await Rest(room);
        }

        // Past the opponent's presentation of a step: its cues play one after another.
        private static async Task PresentationEnds(TestScenarioRoom room)
        {
            for (var step = 0; step < PresentationSteps; step++)
                await room.Settle();
        }

        // Past the longest transition, so a picture never catches a window or a figure on its way.
        private static async Task Rest(TestScenarioRoom room)
        {
            for (var step = 0; step < SettledSteps; step++)
                await room.Settle();
        }
    }
}
