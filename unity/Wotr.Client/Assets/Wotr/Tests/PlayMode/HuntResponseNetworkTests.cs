using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed class HuntResponseNetworkTests
    {
        // UI-142: a response is a complete choice; one click applies it, and a drop on the tile still does.
        [UnityTest] public IEnumerator RingClickAndDrop() => MatchTestRoom.Wait(Responses(HuntResponse.Ring));
        [UnityTest] public IEnumerator GuideClickAndDrop() => MatchTestRoom.Wait(Responses(HuntResponse.Guide));
        [UnityTest] public IEnumerator RandomClickAndDropNeverSendAVictim() => MatchTestRoom.Wait(Responses(HuntResponse.RandomCompanion));

        private static async Task Responses(HuntResponse response)
        {
            foreach (var gesture in new[] { "click", "drop" })
            {
                using var room = await UI10TestRoom.Connect("ui34-response-" + response + "-" + gesture);
                var board = room.Fp.Board;
                room.Fp.Interface.Preferences.SetScale(1.25f);
                await room.Settle();
                var revision = board.View.Revision;
                var other = JsonConvert.SerializeObject(room.Sh.Board.View);
                var pan = board.Map.Camera.Pan;
                var option = board.Hunt.Root.Q<Button>("hunt-response-" + response);
                var consequence = option.Q<Label>("hunt-response-consequence");
                Assert.IsNotNull(consequence);
                Assert.AreEqual(response == HuntResponse.Ring ? "Corruption\n4/12 → 7/12" :
                    response == HuntResponse.Guide ? "Lose Guide\nDamage 3 → 0" : "Random casualty\nDamage left ?", consequence.text);
                Assert.Greater(consequence.worldBound.height, 0, "Known consequence is visible before hover or selection.");
                Assert.IsNull(board.Tray.Q<Button>("confirm-decision"));
                Assert.AreEqual(DisplayStyle.None, room.Sh.Board.Hunt.Responses.Root.style.display.value);
                using var mouse = new EngineMouseInput(board.Root);
                await Drop(room, mouse, option, board.Menu.Toggle);
                Assert.AreEqual(0, room.Boundary.Commands, "An unrelated target cannot consume the response.");
                Assert.IsFalse(option.ClassListContains("selected"));
                Assert.IsFalse(option.ClassListContains("selected"));
                Assert.LessOrEqual(board.Hunt.Root.worldBound.yMax, board.Map.worldBound.yMax + 1,
                    "The responses and their known consequences fit at 1366×768 and 125%, including the Guide order.");
                Assert.AreEqual(other, JsonConvert.SerializeObject(room.Sh.Board.View));
                var completionPoint = option.worldBound.center;
                if (gesture == "click")
                {
                    await HuntRoleScreenshots.Capture(room, "response-" + response);
                    Assert.IsTrue(board.Hunt.IsOpen, "The permanent Hunt keeps its pending response available.");
                    room.Boundary.HoldNextCommand = true;
                    mouse.Click(option);
                    await room.Until(() => room.Boundary.CommandHeld);
                    // A fast second click on the same spot cannot send another response.
                    mouse.ClickAtPoint(completionPoint);
                }
                else
                {
                    completionPoint = board.Hunt.Tile.Root.worldBound.center;
                    await Drop(room, mouse, option, board.Hunt.Tile.Root);
                    await room.Until(() => room.Boundary.Commands == 1);
                    mouse.ClickAtPoint(completionPoint);
                }
                await room.At(revision + 1);
                var command = room.Boundary.LastCommand;
                Assert.AreEqual(CommandKind.ResolveHuntDamage, command.Kind);
                Assert.AreEqual(response, command.Payload.HuntResponse);
                Assert.IsEmpty(command.Payload.Ids);
                Assert.IsEmpty(command.Payload.GuideId);
                Assert.IsEmpty(command.Payload.CharacterId);
                Assert.IsEmpty(command.Payload.DieId);
                if (gesture == "click")
                    await Recover(room);
                var publicReport = JsonConvert.SerializeObject(board.View.HuntReport);
                var connected = room.Fp.Connected;
                // Hold a press in the original completed gesture's position across
                // reconnect. Do not teleport to a newly offered Guide control.
                mouse.PressAtPoint(completionPoint);
                room.Fp.Connection.Reconnect();
                await room.Until(() => room.Fp.Connected > connected);
                mouse.Release();
                Assert.AreEqual(publicReport, JsonConvert.SerializeObject(board.View.HuntReport));
                Assert.AreEqual(publicReport, JsonConvert.SerializeObject(room.Sh.Board.View.HuntReport));
                await room.Settle();
                Assert.AreEqual(revision + 1, board.View.Revision);
                Assert.AreEqual(1, room.Boundary.Commands);
                Assert.AreEqual(pan, board.Map.Camera.Pan);
                if (response == HuntResponse.Ring)
                    Assert.AreEqual(7, board.View.Fellowship.Corruption);
                if (response == HuntResponse.Guide)
                    Assert.AreEqual(DecisionKind.Guide, board.View.Decision.Kind);
            }
        }

        private static async Task Drop(UI10TestRoom room, EngineMouseInput mouse, VisualElement source, VisualElement target)
        {
            mouse.Press(source);
            await room.Settle();
            mouse.Move(target.worldBound.center);
            await room.Settle();
            mouse.Release();
        }

        private static async Task Recover(UI10TestRoom room)
        {
            room.Boundary.ReleaseCommand();
            await room.Until(() => room.Fp.Board.Tray.Query<Button>().ToList().Any(b => b.text == "Check result"));
            await room.Settle();
            PointerGestureEvents.ClickAt(room.Fp.Board.Tray.Query<Button>().ToList().Single(b => b.text == "Check result"));
            await room.Until(() => !room.Fp.Board.Tray.Query<Button>().ToList().Any(b => b.text == "Check result"));
            Assert.Greater(room.Boundary.ReceiptLookups, 0);
        }
    }
}
