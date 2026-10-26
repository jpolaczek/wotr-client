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
    public sealed class MultiUseDiceNetworkTests
    {
        [UnityTest] public IEnumerator OneDieClickExposesArmiesRecruitmentAndPolitics() => Wait(Alternatives());
        [UnityTest] public IEnumerator WillDrawsOrMovesACompanionWithoutChoosingASymbol() => Wait(Will());

        private static async Task Alternatives()
        {
            foreach (var face in new[] { DieFace.ArmyMuster, DieFace.WillOfTheWest })
                foreach (var use in new[] { "march", "recruit", "politics", "card", "card-alternate" })
                    if (face == DieFace.WillOfTheWest || use != "card-alternate")
                        await Alternative(face, use);
        }

        private static async Task Alternative(DieFace face, string use)
        {
            using var room = await Connect("ui28-" + face + "-" + use, ScenarioKind.BaseAcceptance);
            var client = room.Fp;
            var board = client.Board;
            var die = await Select(room, face);
            Assert.IsTrue(board.Map.Q<Button>("army-minas-tirith-FreePeoples").ClassListContains("eligible-army"));
            board.Nations.Open();
            await room.Settle();
            Assert.IsTrue(board.Nations.Drawer.Q<Button>("nation-status-elves").ClassListContains("legal-target"));
            Assert.IsFalse(board.Nations.Drawer.Q<Button>("nation-activate-rohan").enabledSelf,
                "Ordinary die uses do not grant an activation effect.");
            board.Hover.Open("minas-tirith");
            await room.Settle();
            var elite = board.Reinforcements.Root.Q<Button>("reserve-token-gondor-Elite");
            Assert.IsTrue(elite.enabledInHierarchy, "Recruitment is ready alongside army movement.");
            Assert.AreEqual(0, board.View.Revision, "Hover and selection do not spend the die.");
            if (use == "politics")
            {
                board.Nations.Open();
                await room.Settle();
                LayoutTests.Save(client.Viewport, "ui28-politics-" + face + ".png");
                Assert.IsTrue(board.Nations.Drawer.Q<Button>("political-slot-elves-1").enabledInHierarchy);
                // UI-144: a click on the token of a nation the die can move is the step itself.
                PointerGestureEvents.ClickAt(board.Nations.Drawer.Q<Button>("nation-status-elves"));
                await room.At(1);
                Assert.AreEqual(1, room.Sh.Board.View.Nations.Single(n => n.Id == "elves").StepsToWar);
            }
            else if (use.StartsWith("card"))
            {
                board.Nations.Close();
                await CardGestures.Preview(client, "base.fp.strategy.08");
                // UI-194: one way to pay plays the card with that click; two ways open the preview, whose buttons
                // choose the payment explicitly.
                var inspected = board.Cards.CardId == "base.fp.strategy.08";
                if (use == "card-alternate")
                    Assert.IsTrue(inspected, "Two payments open the preview with both Play buttons.");
                if (inspected)
                {
                    var play = use == "card-alternate" ? "play-card-alternate" : "play-card";
                    var button = board.Cards.Root.Q<Button>(play);
                    CardGestures.Move(button);
                    await Task.Delay(250, room.Token);
                    Assert.IsTrue(board.HandDrawer.IsOpen, "Hovering Play retains the hand and card.");
                    Assert.IsTrue(button.enabledInHierarchy);
                    Assert.LessOrEqual(button.Q<ActionSymbol>().worldBound.xMax, button.Q<Label>().worldBound.xMin,
                        "The cost icon and Play caption must not overlap.");
                    LayoutTests.Save(client.Viewport, "ui29-play-" + face + "-" + use + ".png");
                    PointerGestureEvents.ClickAt(button);
                }
                await room.Settle();
                // UI-148: the card waits for a nation, so Politics opens by itself with the nations highlighted.
                Assert.AreEqual(DisplayStyle.Flex, board.Nations.Drawer.resolvedStyle.display, "Politics opens for the card's nation.");
                Assert.IsTrue(board.Nations.Drawer.Q<Button>("nation-status-north").ClassListContains("legal-target"));
                Assert.IsFalse(board.Nations.Drawer.Query<Button>(className: "nation-slot").ToList().Any(slot => slot.ClassListContains("legal-target")),
                    "While the card picks its nation, the die's own political steps step aside.");
                LayoutTests.Save(client.Viewport, "ui148-politics-card-" + face + "-" + use + ".png");
                PointerGestureEvents.ClickAt(board.Nations.Drawer.Q<Button>("nation-status-north"));
                await room.At(1);
                var north = room.Sh.Board.View.Nations.Single(n => n.Id == "north");
                Assert.IsTrue(north.Active, "The chosen card supplies an actual activation effect.");
                Assert.AreEqual(2, north.StepsToWar);
                Assert.IsFalse(board.View.Hand.Contains("base.fp.strategy.08"));
                if (face == DieFace.WillOfTheWest && use == "card")
                {
                    Assert.AreEqual(DecisionKind.Reaction, board.View.Decision.Kind,
                        "Using Will as Event retains Gandalf's optional draw window.");
                    Assert.AreEqual(DieLocation.Resolving, board.View.Dice.Single(d => d.Id == die).Location);
                    // UI-291: no Decline button anywhere; the glowing Pass skips Gandalf's draw and says so.
                    await room.Settle();
                    Assert.IsEmpty(board.Root.Query<Button>().ToList().Where(b => b.text.Contains("Decline")).Select(b => b.name).ToList());
                    var pass = board.Console.Actions.Pass;
                    Assert.IsTrue(pass.enabledInHierarchy);
                    Assert.IsTrue(pass.ClassListContains(ChoiceGlow.ClassName));
                    StringAssert.Contains("Skip Gandalf the Grey", pass.tooltip);
                    LayoutTests.Save(client.Viewport, "ui291-gandalf-draw-pass.png");
                    Assert.AreEqual("Click the Guide's glowing portrait in the Fellowship panel to use Draw matching card · or Pass to skip it.",
                        board.Console.Status.Hint.text, "The console's hint says where the draw is and that Pass skips it.");
                    Assert.IsTrue(board.Edges.Corner.ClassListContains(BoardEdgeLayout.UsableClass), "UI-290: the Fellowship tab lights up.");
                    var rim = board.Edges.Corner.Q("edge-corner-usable");
                    Assert.AreEqual(DisplayStyle.Flex, rim.resolvedStyle.display);
                    Assert.AreEqual(board.Edges.Corner.worldBound, rim.worldBound, "The rim covers the whole tab.");
                    await room.Direct(client, "action-DeclineReaction");
                }
            }
            else if (use == "recruit")
            {
                await RecruitAtTheSamePoint(room, elite, face);
                await room.At(1);
                Assert.IsNull(board.View.ActionSequence, "An elite spends both points in one placement.");
            }
            else
            {
                var unit = board.View.Units.First(u => u.Location == "minas-tirith" && u.Kind == UnitKind.Regular);
                PointerGestureEvents.ClickAt(board.Hover.Panel.Root.Q<Button>("hover-figure-" + unit.Id));
                await room.Settle();
                Assert.Contains("osgiliath", board.Map.DraftOverlay.Targets.ToList());
                await ArmyGestures.Target(room, client, "osgiliath");
                Assert.AreEqual("osgiliath", room.Sh.Board.View.Units.Single(u => u.Id == unit.Id).Location);
                Assert.AreEqual(DieFace.Army, board.View.ActionSequence.Face);
                Assert.AreEqual(1, board.View.ActionSequence.Remaining);
                Assert.IsFalse(board.Nations.Drawer.Q<Button>("nation-status-elves").ClassListContains("legal-target"),
                    "A paid march cannot change into a political action.");
                await ArmyGestures.Finish(room, client);
            }
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
            Assert.AreEqual(1, board.View.Dice.Count(d => d.Owner == Side.FreePeoples && d.Location == DieLocation.Spent));
        }

        private static async Task RecruitAtTheSamePoint(MatchTestRoom room, Button elite, DieFace face)
        {
            var board = room.Fp.Board;
            // UI-135: the reserve is chosen in Reinforcements; its first click must not move it before the settlement click.
            var panel = board.Reinforcements.Root;
            // UI-181: the player opens the Reserve tab the die lit; it unrolls with a short slide (LeftRailPreview,
            // about 130 ms), so the source is measured once it has ended.
            board.Reinforcements.Open();
            await Task.Delay(300);
            await room.Settle();
            var before = elite.worldBound;
            var beforePanel = panel.worldBound;
            var beforeStage = panel.parent.worldBound;
            var point = before.center;
            LayoutTests.Save(room.Fp.Viewport, "ui74-recruit-before-" + face + ".png");
            PointerGestureEvents.ClickAt(elite);
            await room.Settle();
            var after = elite.worldBound;
            var picked = elite.panel.Pick(point);
            LayoutTests.Save(room.Fp.Viewport, "ui74-recruit-after-" + face + ".png");
            TestContext.Progress.WriteLine("Recruit click " + face + ": source " + before + " -> " + after +
                "; panel " + beforePanel + " -> " + panel.worldBound + "; stage " + beforeStage + " -> " +
                panel.parent.worldBound + "; first point " + point + "; actual pick " + picked?.name);
            Assert.AreEqual(0, board.View.Revision, "The first click selects a reserve without spending the die.");
            Assert.AreSame(elite, panel.Q<Button>(elite.name), "Selection keeps the same reserve control.");
            // A sub-point drift at the end of the press transition is not a move.
            Assert.That(Vector2.Distance(before.position, after.position), Is.LessThan(.5f),
                "Selecting a die use must not move the source between double-click presses.");
            Assert.That(Vector2.Distance(before.size, after.size), Is.LessThan(.5f));
            Assert.IsTrue(picked == elite || elite.Contains(picked),
                "The original first-click point must still reach the reserve; actual pick " + picked?.name);
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains("minas-tirith"));
            board.Hover.Close();
            board.Map.Select("minas-tirith");
        }

        private static async Task Will()
        {
            foreach (var use in new[] { "draw", "travel" })
            {
                using var room = await Connect("ui28-WillOfTheWest-" + use, ScenarioKind.BaseAcceptance);
                var client = room.Fp;
                var die = await Select(room, DieFace.WillOfTheWest);
                if (use == "draw")
                    await CardGestures.Draw(room, client, "strategy");
                else
                {
                    client.Board.Hover.Open("minas-tirith");
                    await room.Settle();
                    var figure = client.Board.Hover.Panel.Root.Q<Button>("hover-figure-base.character.strider");
                    Assert.IsTrue(figure.ClassListContains("eligible-figure"));
                    PointerGestureEvents.ClickAt(figure);
                    await room.Settle();
                    // UI-105: the destination click moves Strider at once.
                    Assert.IsTrue(client.Board.Map.DraftOverlay.Targets.Contains("osgiliath"));
                    MapContourProbe.ClickRegion(client.Board.Map, "osgiliath");
                    await room.At(1);
                    if (client.Board.View.ActionSequence != null)
                        await ArmyGestures.Finish(room, client);
                    Assert.AreEqual("osgiliath", room.Sh.Board.View.Characters.Single(c => c.Id == "base.character.strider").Location);
                }
                T22NetworkActions.Spent(room, Side.FreePeoples, die);
            }
        }

        private static async Task<string> Select(MatchTestRoom room, DieFace face)
        {
            var board = room.Fp.Board;
            var die = board.View.Dice.First(d => d.Owner == Side.FreePeoples && d.Face == face);
            PointerGestureEvents.ClickAt(board.OwnDice.Q<Button>(die.Id));
            await room.Settle();
            Assert.AreEqual(die.Id, board.ActionDie.DieId);
            Assert.IsNull(board.OwnDice.Q("action-face-picker"));
            var hint = board.OwnDice.Q<Button>(die.Id).tooltip;
            StringAssert.DoesNotContain("Action die", hint);
            StringAssert.DoesNotContain("Available", hint);
            StringAssert.Contains("Politics", hint);
            StringAssert.Contains("Recruit", hint);
            return die.Id;
        }
    }
}
