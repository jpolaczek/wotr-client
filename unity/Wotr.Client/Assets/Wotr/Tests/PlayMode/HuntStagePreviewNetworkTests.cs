using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using Object = UnityEngine.Object;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-509 against the real server: a real Hunt's stages, clicked in the window; only the window looks back.
    public sealed class HuntStagePreviewNetworkTests
    {
        [UnityTest]
        public IEnumerator AnEarlierStageShowsInTheWindowOnlyAndTheLitStageReturns() => Wait(Look());

        [UnityTest]
        public IEnumerator TheOpponentsMoveLeavesThePreviewAndALaterStageShowsItsTile() => Wait(Moves());

        private static VisualElement Stage(BoardScreen board, string key) => board.Hunt.Root.Q("hunt-stage-" + key);

        private static bool IsCurrent(BoardScreen board, string key) => Stage(board, key)?.ClassListContains("current-stage") == true;

        private static async Task Look()
        {
            using var room = await Connect("ui509-stages", ScenarioKind.Interaction);
            var chooser = room.Fp.Board.View.Decision != null ? room.Fp : room.Sh;
            var board = chooser.Board;
            board.Hunt.Open();
            await room.Until(() => IsCurrent(board, "reroll"));
            await room.Settle();
            var revision = board.View.Revision;
            var decision = board.View.Decision.Id;
            var place = board.Hunt.Root.worldBound;
            var ribbon = Stage(board, "roll").worldBound;
            Assert.IsTrue(Stage(board, "roll").enabledInHierarchy, "A stage already reached takes a click.");
            Assert.IsFalse(Stage(board, "tile").enabledInHierarchy, "A stage not reached yet does not.");

            PointerGestureEvents.ClickAt(Stage(board, "roll"));
            await room.Settle();
            Assert.IsTrue(board.Hunt.ShowsStage);
            Assert.IsTrue(Stage(board, "roll").ClassListContains("previewed-stage"));
            Assert.IsTrue(IsCurrent(board, "reroll"), "The marker stays where the Hunt stands.");
            Assert.AreEqual(ribbon.y, Stage(board, "roll").worldBound.y, 1f, "The ribbon stays under the pointer.");
            Assert.AreEqual(place.y, board.Hunt.Root.worldBound.y, 1f, "The window stays where it was.");
            Assert.AreEqual(DisplayStyle.None, board.Hunt.DecisionHost.resolvedStyle.display, "The controls give way.");
            StringAssert.Contains("An earlier stage", board.Hunt.Root.Q<Label>("hunt-step").text);
            var die = board.Hunt.Dice.Root.Query<Button>().ToList().First(button => button.worldBound.width > 0);
            Assert.IsFalse(die.ClassListContains("legal-target"), "Nothing in it is a choice.");
            Assert.AreEqual("hunt-stage-blocker", die.panel.Pick(die.worldBound.center)?.name, "Nothing in it takes a click.");
            Assert.AreEqual(revision, board.View.Revision, "Nothing was sent.");
            Assert.AreEqual(decision, board.View.Decision.Id, "The decision still stands.");

            await Layouts(room, chooser);

            PointerGestureEvents.ClickAt(Stage(board, "reroll"));
            await room.Settle();
            Assert.IsFalse(board.Hunt.ShowsStage, "The lit stage returns to the present.");
            Assert.AreEqual(DisplayStyle.Flex, board.Hunt.DecisionHost.resolvedStyle.display, "The controls return.");
            Assert.IsNull(board.Hunt.Root.Q("hunt-stage-blocker"));

            PointerGestureEvents.ClickAt(Stage(board, "roll"));
            await room.Settle();
            Assert.IsTrue(board.Hunt.ShowsStage);
            using (var escape = KeyDownEvent.GetPooled('\0', KeyCode.Escape, EventModifiers.None))
                board.Hunt.Root.SendEvent(escape);
            await room.Settle();
            Assert.IsFalse(board.Hunt.ShowsStage, "Esc returns to the present, also in a window that holds the decision.");
            Assert.IsTrue(board.Hunt.IsOpen);
        }

        private static async Task Moves()
        {
            using var room = await Connect("ui509-moves", ScenarioKind.Interaction);
            var shadow = room.Sh;
            var board = shadow.Board;
            var free = room.Fp.Board;
            board.Hunt.Open();
            await room.Until(() => IsCurrent(board, "reroll"));
            await room.Confirm(shadow);
            await room.Until(() => !shadow.Playback.Holding && free.View.Decision != null);
            await room.Settle();
            Assert.IsNull(board.View.Decision, "Free Peoples answer the tile; Shadow waits.");
            Assert.IsTrue(Stage(board, "roll").enabledInHierarchy);

            PointerGestureEvents.ClickAt(Stage(board, "roll"));
            await room.Settle();
            Assert.IsTrue(board.Hunt.ShowsStage);
            var roll = board.Hunt.Dice.Root.Query<Button>().ToList().Count(button => button.worldBound.width > 0);
            Assert.Greater(roll, 0, "The roll is drawn, without the tile drawn since.");
            Assert.AreEqual(DisplayStyle.None, board.Hunt.Tile.Root.resolvedStyle.display);
            LayoutTests.Save(shadow.Viewport, "ui509-roll-after-tile.png");

            PointerGestureEvents.ClickAt(Stage(board, "tile"));
            await room.Settle();
            if (board.Hunt.ShowsStage)
            {
                Assert.AreEqual(DisplayStyle.Flex, board.Hunt.Tile.Root.resolvedStyle.display, "The tile stage shows its tile.");
                Assert.AreEqual(DisplayStyle.None, board.Hunt.Dice.Root.resolvedStyle.display);
                PointerGestureEvents.ClickAt(Stage(board, "roll"));
                await room.Settle();
            }
            else
            {
                PointerGestureEvents.ClickAt(Stage(board, "roll"));
                await room.Settle();
            }
            Assert.IsTrue(board.Hunt.ShowsStage);

            // The opponent's move changes nothing here.
            var revision = board.View.Revision;
            PointerGestureEvents.ClickAt(free.Hunt.Root.Q<Button>("hunt-response-Ring"));
            await room.At(revision + 1);
            await room.Until(() => !shadow.Playback.Holding);
            await room.Settle();
            Assert.IsTrue(board.Hunt.ShowsStage, "The opponent's move leaves the look back alone.");
            Assert.AreEqual(roll, board.Hunt.Dice.Root.Query<Button>().ToList().Count(button => button.worldBound.width > 0), "The roll still stands.");

            // One's own new decision ends it, so it is never missed.
            var view = board.View;
            view.Decision = free.View.Decision;
            view.DecisionOwner = view.You;
            board.Hunt.Apply(view);
            Assert.IsFalse(board.Hunt.ShowsStage, "A decision of one's own ends the look back.");
        }

        private static async Task Layouts(MatchTestRoom room, MountedMatchClient client)
        {
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    try
                    {
                        client.Panel.targetTexture = target;
                        client.Panel.scale = scale;
                        await room.Settle();
                        await room.Settle();
                        var board = client.Board;
                        var window = board.Hunt.Root.worldBound;
                        foreach (var key in new[] { "roll", "reroll", "tile", "damage", "result" })
                        {
                            var stage = Stage(board, key).worldBound;
                            Assert.GreaterOrEqual(stage.xMin, window.xMin, key);
                            Assert.LessOrEqual(stage.xMax, window.xMax, key);
                        }
                        Assert.LessOrEqual(window.yMax, board.Root.worldBound.yMax + 1, "The window stays on the screen.");
                        LayoutTests.Save(target, $"ui509-stage-{width}-{scale:0.00}.png");
                    }
                    finally
                    {
                        client.Panel.targetTexture = client.Viewport;
                        client.Panel.scale = 1;
                        target.Release();
                        Object.Destroy(target);
                    }
                }
            await room.Settle();
        }
    }
}
