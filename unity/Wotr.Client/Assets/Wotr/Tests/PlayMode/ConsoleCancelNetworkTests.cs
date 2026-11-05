using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    // UI-455 and UI-456 (owner's playtest, 2026-11-03) on the Testing catalog's scenarios. fp-character-separate-companions:
    // no long Cancel option in the console's line, but its small fixed Cancel, greyed with nothing chosen; the Fellowship
    // panel shows no instruction, the console every one. kings-far-sighted: with no companion outside the Fellowship,
    // Far-sighted's Move companions is greyed and says why.
    public sealed class ConsoleCancelNetworkTests
    {
        private const string Legolas = "base.character.legolas";

        [UnityTest] public IEnumerator ASeparationIsCancelledFromTheConsolesFixedCancel() => Wait(Separation());
        [UnityTest] public IEnumerator FarSightedMovesNoCompanionWhileNoneIsOutsideTheFellowship() => Wait(FarSighted());

        private static async Task Separation()
        {
            var session = await TestScenarioRoom.Start("fp-character-separate-companions");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var board = fp.Board;
                var cancel = board.Console.Actions.Cancel;
                var place = cancel.worldBound;
                Assert.IsFalse(cancel.enabledSelf, "Nothing chosen: the Cancel is greyed.");
                Assert.AreEqual(DisplayStyle.Flex, cancel.resolvedStyle.display, "…but in its place.");
                var die = board.View.Options.First(o => o.Kind == CommandKind.SeparateCompanions).EligibleDice.First();
                T22NetworkActions.Die(fp, die);
                await session.Settle();
                Assert.IsTrue(cancel.enabledSelf, "A chosen die can be cancelled.");
                board.Fellowship.Open();
                await session.Settle();
                CardNetworkTests.Click(board.Fellowship.Root.Q<Button>("fellowship-portrait-" + Legolas));
                await session.Until(() => board.Tray.parent == board.Fellowship.Root && board.Tray.Q<Label>("travel-summary") != null);
                await session.Settle();
                LayoutTests.Save(fp.Viewport, "ui455-after-separation.png");
                // UI-455: no long Cancel in the console's line; its hint stays in view.
                Assert.IsNull(board.Console.Status.Options.Q<Button>("console-cancel-route"), "No Cancel option in the console's line.");
                Assert.AreEqual(DisplayStyle.Flex, board.Console.Status.Hint.resolvedStyle.display, "The console's hint stays.");
                Assert.AreEqual(place, cancel.worldBound, "The Cancel keeps its place.");
                Assert.IsTrue(cancel.enabledSelf);
                Assert.AreEqual("Cancel · Clear the chosen companions and their route", cancel.tooltip);
                // UI-456: every instruction is the console's; the panel states only where the move stands.
                StringAssert.EndsWith("Legolas · Click a highlighted region to move there.", board.Console.Status.Hint.text);
                Assert.IsNull(board.Tray.Q<Label>("fellowship-separation-hint"), "No 'Separated companions cannot rejoin'.");
                Assert.AreEqual(board.Map.Data.Regions[board.View.Fellowship.RegionId].Name, board.Tray.Q<Label>("travel-summary").text,
                    "No 'Choose a destination'.");
                foreach (var label in board.Tray.Query<Label>().ToList().Where(label => label.resolvedStyle.display == DisplayStyle.Flex))
                    StringAssert.DoesNotContain("Click", label.text, label.name + " instructs in the panel.");
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(cancel);
                await session.Settle();
                Assert.AreEqual(revision, board.View.Revision, "Cancel sends nothing.");
                Assert.IsFalse(cancel.enabledSelf, "The die and the companion are dropped: nothing left to cancel.");
                Assert.IsFalse(board.OwnDice.Q<Button>(die).ClassListContains("selected"), "The die is dropped too.");
                LayoutTests.Save(fp.Viewport, "ui455-after-cancelled.png");
            }
            finally
            {
                await session.End();
            }
        }

        private static async Task FarSighted()
        {
            var session = await TestScenarioRoom.Start("kings-far-sighted");
            try
            {
                var fp = session.Client(Side.FreePeoples);
                var board = fp.Board;
                Assert.IsFalse(board.View.OwnCompanions.Any(c => c.Location != "fellowship"), "Every companion is in the Fellowship.");
                var die = board.View.Dice.Single(d => d.Source == "ruler" && d.Owner == Side.FreePeoples).Id;
                T22NetworkActions.Die(fp, die);
                await session.Settle();
                var region = board.Hover.Panel.ActionRegion("far-sighted-MoveCompanions");
                Assert.AreEqual("woodland-realm", region, "Thranduil's Far-sighted stands in his region's window.");
                board.Hover.Open(region);
                await session.Settle();
                var components = board.Hover.Panel.Components.Root;
                var move = components.Q<Button>("component-far-sighted-MoveCompanions");
                Assert.IsNotNull(move);
                Assert.IsFalse(move.enabledSelf, "Move companions is greyed: no companion is outside the Fellowship.");
                StringAssert.EndsWith(FarSightedMoves.NoCompanion, SkillTooltip.Plain(move.tooltip));
                Assert.IsTrue(components.Q<Button>("component-far-sighted-SeparateCompanions").enabledSelf, "Separate companions stays.");
                Assert.IsTrue(components.Q<Button>("component-far-sighted-MoveFellowship").enabledSelf, "Move the Fellowship stays.");
                LayoutTests.Save(fp.Viewport, "ui456-after-far-sighted.png");
                var hint = board.Console.Status.Hint.text;
                CardNetworkTests.Click(move);
                await session.Settle();
                Assert.AreEqual(hint, board.Console.Status.Hint.text, "Its click starts no move.");
                Assert.IsTrue(board.OwnDice.Q<Button>(die).ClassListContains("selected"), "The die stays chosen.");
            }
            finally
            {
                await session.End();
            }
        }
    }
}
