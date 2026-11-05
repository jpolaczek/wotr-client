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
    // UI-461 (owner, 2026-11-03): "We need the previous small menu at the bottom (console) that let us choose the new face
    // WotW turns into, while the additional effects coming directly from WotW (like deactivating the Balrog) light on the
    // table; if the player picks one of them the die is spent and the player needn't choose anything."
    // lords-fp-balrog-deactivate: Gandalf the Grey stands with the active Balrog in Dimrill Dale and the Free Peoples hold
    // a Will of the West die.
    public sealed class WillOfTheWestFaceNetworkTests
    {
        private const string Scenario = "lords-fp-balrog-deactivate";
        private const string Balrog = "lords.character.balrog";

        [UnityTest] public IEnumerator TheBalrogsDeactivationSpendsWillOfTheWestItselfWithOneClick() => Wait(Deactivate());
        [UnityTest] public IEnumerator ACharacterResultMovesALedArmyOnce() => Wait(March(DieFace.Character));
        [UnityTest] public IEnumerator AnArmyResultMovesArmiesTwice() => Wait(March(DieFace.Army));
        [UnityTest] public IEnumerator BackMakesTheDieWillOfTheWestItselfAgain() => Wait(Back());

        private static async Task Deactivate()
        {
            using var room = await ConnectCatalog(Scenario);
            var client = room.Fp;
            var board = client.Board;
            var die = await Choose(room);
            var faces = board.Console.Status.Faces;
            Assert.IsTrue(faces.Shown, "The chosen Will die opens the console's face menu.");
            foreach (var face in WillOfTheWestUses.Faces)
                Assert.IsFalse(faces[face].ClassListContains(ConsoleFaceMenu.ChosenClass), face + " is not chosen yet.");
            StringAssert.Contains(WillFaceChoices.Hint, board.Console.Status.Hint.text);
            Assert.IsEmpty(board.Map.Query<Button>(className: "eligible-army").ToList(), "No army moves with Will of the West itself.");
            board.Hover.Open("dimrill-dale");
            await room.Settle();
            var balrog = board.Hover.Panel.Root.Q<Button>("hover-figure-" + Balrog);
            Assert.IsTrue(balrog.ClassListContains("eligible-figure"), "The Balrog's deactivation takes Will of the West itself.");
            StringAssert.Contains("Click to deactivate the Balrog", balrog.tooltip);
            var gandalf = board.Hover.Panel.Root.Q<Button>("hover-figure-base.character.gandalf-grey");
            Assert.IsFalse(gandalf.ClassListContains("eligible-figure"), "Gandalf moves only once the die is a Character result.");
            await Layouts(room, client, "as-itself");
            board.Hover.Open("dimrill-dale");
            await room.Settle();
            var revision = board.View.Revision;
            CardNetworkTests.Click(board.Hover.Panel.Root.Q<Button>("hover-figure-" + Balrog));
            await room.At(revision + 1);
            Assert.AreEqual(DieLocation.Spent, room.Sh.Board.View.Dice.Single(d => d.Id == die).Location);
            Assert.IsFalse(room.Sh.Board.View.Characters.Any(c => c.Id == Balrog && c.Location == "dimrill-dale"),
                "Outside Moria the deactivation eliminates the Balrog.");
            Assert.IsFalse(faces.Shown);
        }

        // A force with a leader: a Character result moves it once, an Army result moves armies twice (the server records
        // the result it paid as the die's face).
        private static async Task March(DieFace face)
        {
            using var room = await ConnectCatalog(Scenario);
            var client = room.Fp;
            var board = client.Board;
            var view = board.View;
            var die = await Choose(room);
            await Turn(room, face);
            Assert.IsTrue(board.Console.Status.Faces[face].ClassListContains(ConsoleFaceMenu.ChosenClass));
            LayoutTests.Save(client.Viewport, "ui461-will-" + face.ToString().ToLowerInvariant() + ".png");
            var option = view.Options.Single(o => o.Kind == CommandKind.MoveArmies);
            var army = option.Armies.First(a => !a.Garrison && view.Units.Any(u => a.UnitIds.Contains(u.Id) && u.Kind == UnitKind.Leader) &&
                a.Targets.Any(t => a.UnitIds.All(t.EligibleIds.Contains)));
            var target = army.Targets.First(t => army.UnitIds.All(t.EligibleIds.Contains)).RegionId;
            await ArmyGestures.All(client, army.RegionId);
            await room.Settle();
            await ArmyGestures.Target(room, client, target);
            if (face == DieFace.Army)
            {
                Assert.AreEqual(DieFace.Army, board.View.ActionSequence.Face);
                Assert.AreEqual(1, board.View.ActionSequence.Remaining, "An Army result moves two armies.");
                await ArmyGestures.Finish(room, client);
            }
            Assert.IsNull(board.View.ActionSequence);
            Assert.AreEqual(target, room.Sh.Board.View.Units.Single(u => u.Id == army.UnitIds[0]).Location);
            var paid = room.Sh.Board.View.Dice.Single(d => d.Id == die);
            Assert.AreEqual(DieLocation.Spent, paid.Location);
            Assert.AreEqual(face, paid.Face, "The die is paid as the result the console chose.");
        }

        private static async Task Back()
        {
            using var room = await ConnectCatalog(Scenario);
            var client = room.Fp;
            var board = client.Board;
            var start = board.View.Revision;
            var die = await Choose(room);
            await Turn(room, DieFace.Army);
            Assert.IsNotEmpty(board.Map.Query<Button>(className: "eligible-army").ToList(), "An Army result lights the armies.");
            board.Hover.Open("dimrill-dale");
            await room.Settle();
            Assert.IsFalse(board.Hover.Panel.Root.Q<Button>("hover-figure-" + Balrog).ClassListContains("eligible-figure"),
                "A turned die no longer deactivates the Balrog.");
            board.Hover.Close();
            PointerGestureEvents.ClickAt(board.Console.Actions.Back);
            await room.Settle();
            Assert.IsTrue(board.Console.Status.Faces.Shown);
            Assert.IsFalse(board.Console.Status.Faces[DieFace.Army].ClassListContains(ConsoleFaceMenu.ChosenClass));
            Assert.IsEmpty(board.Map.Query<Button>(className: "eligible-army").ToList(), "Back makes it Will of the West itself.");
            Assert.AreEqual(die, board.ActionDie.DieId, "Back keeps the die.");
            await Turn(room, DieFace.Muster);
            await Turn(room, DieFace.Muster);
            Assert.IsFalse(board.Console.Status.Faces[DieFace.Muster].ClassListContains(ConsoleFaceMenu.ChosenClass),
                "A second click on the chosen result makes it Will of the West itself.");
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await room.Settle();
            Assert.IsEmpty(board.ActionDie.DieId, "UI-386: a second click on the die releases it.");
            Assert.IsFalse(board.Console.Status.Faces.Shown);
            Assert.AreEqual(start, board.View.Revision, "Nothing was sent.");
        }

        private static async Task<string> Choose(MatchTestRoom room)
        {
            var board = room.Fp.Board;
            var die = board.View.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.WillOfTheWest).Id;
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die));
            await room.Settle();
            Assert.AreEqual(die, board.ActionDie.DieId);
            return die;
        }

        private static async Task Turn(MatchTestRoom room, DieFace face)
        {
            WillFaceGestures.Click(room.Fp, face);
            await room.Settle();
        }

        // At every size the four slots stand in one row, the bottom row keeps its height, and the menu stands either in the
        // console's line beside the hint or, where that line is too narrow (Lords at 1366 × 768 and 125 %), in the strip
        // just above the console.
        private static async Task Layouts(MatchTestRoom room, MountedMatchClient client, string name)
        {
            var board = client.Board;
            foreach (var width in new[] { 1366, 1920 })
                foreach (var scale in new[] { 1f, 1.25f })
                {
                    var target = new RenderTexture(width, width == 1366 ? 768 : 1080, 24);
                    target.Create();
                    client.Panel.targetTexture = target;
                    client.Panel.scale = scale;
                    await room.Settle();
                    await room.Settle();
                    LayoutTests.Save(target, $"ui461-{name}-{width}-{scale:0.00}.png");
                    var status = board.Console.Status;
                    var at = width + " × " + scale;
                    var slots = WillOfTheWestUses.Faces.Select(face => status.Faces[face].worldBound).ToList();
                    for (var i = 1; i < slots.Count; i++)
                    {
                        Assert.AreEqual(slots[0].yMin, slots[i].yMin, .5f, "One row at " + at);
                        Assert.GreaterOrEqual(slots[i].xMin, slots[i - 1].xMax - .5f, "No slot covers another at " + at);
                    }
                    var console = board.Console.Root.worldBound;
                    if (status.Faces.Root.parent == status.RaisedOptions)
                    {
                        Assert.LessOrEqual(slots[0].yMax, console.yMin + .5f, "The raised menu stands above the console at " + at);
                        Assert.LessOrEqual(slots.Last().xMax, board.Root.worldBound.xMax, "The raised menu stays on the screen at " + at);
                    }
                    else
                    {
                        Assert.LessOrEqual(slots.Last().xMax, status.Hint.worldBound.xMin + .5f, "The hint stands beside the menu at " + at);
                        Assert.LessOrEqual(status.Hint.worldBound.xMax, status.Root.worldBound.xMax + .5f, "The hint fits at " + at);
                        Assert.GreaterOrEqual(slots[0].xMin, status.Root.worldBound.xMin - .5f);
                        Assert.LessOrEqual(slots[0].yMax, console.yMax + .5f, "The menu stands inside the console at " + at);
                    }
                    Assert.AreEqual(132f, board.Console.Root.resolvedStyle.height, .5f, "The console keeps its height.");
                    client.Panel.targetTexture = client.Viewport;
                    target.Release();
                    Object.Destroy(target);
                }
            client.Panel.scale = 1f;
            await room.Settle();
        }
    }
}
