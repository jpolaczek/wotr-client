using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Wotr.Contracts;
using static Wotr.PlayTests.MatchTestRoom;

namespace Wotr.PlayTests
{
    public sealed class MusterNetworkTests
    {
        [UnityTest] public IEnumerator EliteDropSpendsBothRecruitmentPoints() => Wait(Elite());
        [UnityTest] public IEnumerator LoneLeaderIsLegalWarnedRemovedAndFinishKeepsPaidStep() => Wait(Leader());
        [UnityTest] public IEnumerator HoverIsReadOnlyAndCrestDragAdvancesOnlyLegalTrack() => Wait(Politics());
        [UnityTest] public IEnumerator RecruitmentOverflowWaitsForFinishAndExactFigureReturn() => Wait(Excess());

        private static async Task Elite()
        {
            using var room = await Connect("board-elite", ScenarioKind.BaseAcceptance);
            var die = SelectDie(room.Fp);
            var elite = await Reserve(room, "pelargir", UnitKind.Elite);
            var id = ((Wotr.Client.DraftToken)elite.userData).Id;
            var map = room.Fp.Board.Map;
            var anchor = map.Data.Shapes["minas-tirith"].Anchor;
            var point = map.LocalToWorld(map.Camera.ToScreen(new Vector2(anchor[0], anchor[1])));
            await CardGestureNetworkTests.Drop(room, room.Fp, elite, map, dropPoint: point);
            foreach (var client in new[] { room.Fp, room.Sh })
            {
                Assert.AreEqual("minas-tirith", client.Board.View.Units.Single(u => u.Id == id).Location);
                Assert.IsNull(client.Board.View.ActionSequence);
                Assert.AreEqual(Side.Shadow, client.Board.View.DecisionOwner);
            }
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task Leader()
        {
            using var room = await Connect("board-leader", ScenarioKind.BaseAcceptance);
            var die = SelectDie(room.Fp);
            var leader = await Reserve(room, "pelargir", UnitKind.Leader);
            Assert.IsTrue(leader.enabledInHierarchy);
            // UI-135: the consequence is on the Reinforcements token; the settlement click commits the step.
            StringAssert.Contains(Wotr.Client.ReinforcementRows.OrphanWarning, leader.tooltip);
            var id = ((Wotr.Client.DraftToken)leader.userData).Id;
            CardNetworkTests.Click(leader);
            await room.Settle();
            Assert.IsTrue(room.Fp.Board.Map.DraftOverlay.Targets.Contains("pelargir"));
            Assert.AreEqual(0, room.Sh.Board.View.Revision);
            room.Fp.Board.Map.Select("pelargir");
            await room.At(1);
            Assert.AreEqual("casualties", room.Fp.Board.View.Units.Single(u => u.Id == id).Location);
            Assert.AreEqual(1, room.Fp.Board.View.ActionSequence.Remaining);
            Assert.IsFalse(room.Fp.Board.Reinforcements.Root.Q<Button>("reserve-token-gondor-Elite").enabledSelf);
            await ArmyGestures.Finish(room, room.Fp);
            Assert.IsNull(room.Fp.Board.View.ActionSequence);
            Assert.AreEqual("casualties", room.Sh.Board.View.Units.Single(u => u.Id == id).Location);
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task Politics()
        {
            using var room = await Connect("board-politics", ScenarioKind.BaseAcceptance);
            var die = SelectDie(room.Fp);
            var pan = room.Fp.Board.Map.Camera.Pan;
            var track = room.Fp.Board.Nations;
            Hover(room.Fp.Board.Nations.Root);
            await Task.Delay(240);
            Assert.AreEqual(DisplayStyle.Flex, track.Drawer.resolvedStyle.display);
            Assert.IsTrue(track.Drawer.ClassListContains(Wotr.Client.LeftRailPreview.PreviewClass));
            PointerGestureEvents.ClickAt(room.Fp.Board.Nations.Root);
            await room.Settle();
            var rohan = room.Fp.Board.Nations.Drawer.Q<Button>("nation-status-rohan");
            Hover(rohan);
            await Task.Delay(240);
            Assert.AreEqual(Visibility.Hidden, track.Drawer.Q<Button>("nation-activate-rohan").resolvedStyle.visibility,
                "The base game offers no activation button.");
            Assert.IsFalse(track.Drawer.Q<Button>("political-slot-rohan-0").enabledSelf, "Passive nation cannot enter war.");
            var crest = room.Fp.Board.Nations.Drawer.Q<Button>("nation-status-elves");
            Hover(crest);
            await Task.Delay(240);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.Flex, track.Drawer.resolvedStyle.display);
            Assert.AreEqual(0, room.Sh.Board.View.Revision);
            Assert.IsNull(room.Fp.Board.Tray.Q<Button>("confirm-decision"));
            Assert.IsEmpty(track.Drawer.Query<Button>(className: "destination-selected").ToList());
            // UI-144: every own nation the die can move shows its arrow before any click in the panel.
            // UI-410 (owner): the nation's token glows, never a station; the next station stays the drop target.
            Assert.IsTrue(track.Drawer.Q<Button>("nation-status-elves").ClassListContains("legal-target"));
            Assert.IsFalse(track.Drawer.Q<Button>("political-slot-elves-1").ClassListContains("legal-target"));
            Assert.IsTrue(track.Drawer.Q<Button>("political-slot-elves-1").enabledSelf);
            Assert.IsTrue(track.Drawer.Q<Button>("political-slot-north-2").enabledSelf, "A passive nation can advance before the war space.");
            var shadow = room.Fp.Board.View.Nations.Where(n => n.Id is "sauron" or "isengard" or "southrons").Select(n => n.Id).ToList();
            Assert.IsFalse(track.Drawer.Query<Button>(className: "nation-slot").ToList()
                .Any(slot => slot.enabledSelf && shadow.Any(id => slot.name.StartsWith("political-slot-" + id))),
                "An opponent's nation never offers a step.");
            Assert.IsFalse(shadow.Any(id => track.Drawer.Q<Button>("nation-status-" + id)?.ClassListContains("legal-target") == true),
                "An opponent's token never glows.");
            PoliticalRecoveryTests.AssertNoBottomPolitics(room.Fp.Board);
            CardNetworkTests.Click(track.ShowOpponent);
            await room.Settle();
            Assert.IsFalse(track.ShowOpponent.value);
            Assert.IsTrue(track.Drawer.Q<Button>("political-slot-north-2").enabledSelf);
            Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Information.Root.resolvedStyle.display);
            await CardGestureNetworkTests.Drop(room, room.Fp, crest, track.Drawer.Q<Button>("political-slot-north-2"), commits: false);
            Assert.AreEqual(0, room.Sh.Board.View.Revision, "A token dropped on another nation's arrow sends nothing.");
            Assert.IsTrue(track.Drawer.Q<Button>("political-slot-elves-1").enabledSelf, "An invalid drop keeps every arrow.");
            var destination = track.Drawer.Q<Button>("political-slot-elves-1");
            await CardGestureNetworkTests.Drop(room, room.Fp, crest, destination, holdMilliseconds: 350,
                beforeRelease: () =>
                {
                    Assert.AreEqual(DisplayStyle.Flex, track.Drawer.resolvedStyle.display,
                        "The interactive preview must keep its own drag source and destination visible.");
                    var hit = destination.panel.Pick(destination.worldBound.center);
                    Assert.IsTrue(hit == destination || destination.Contains(hit),
                        "The legal political slot must remain reachable throughout the held drag.");
                    Assert.AreEqual(0, room.Sh.Board.View.Revision, "The held drag sends no command before release.");
                });
            Assert.AreEqual(1, room.Sh.Board.View.Nations.Single(n => n.Id == "elves").StepsToWar);
            Assert.AreEqual(pan, room.Fp.Board.Map.Camera.Pan);
            Assert.AreEqual(DisplayStyle.None, room.Fp.Board.Information.Root.resolvedStyle.display);
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static async Task Excess()
        {
            using var room = await Connect("board-recruit-excess", ScenarioKind.BaseAcceptance);
            var die = SelectDie(room.Fp);
            var regular = await Reserve(room, "minas-tirith", UnitKind.Regular);
            var id = ((Wotr.Client.DraftToken)regular.userData).Id;
            CardNetworkTests.Click(regular);
            await room.Settle();
            room.Fp.Board.Hover.Close();
            room.Fp.Board.Map.Select("minas-tirith");
            await room.At(1);
            Assert.AreEqual(DecisionKind.ActionSequence, room.Fp.Board.View.Decision.Kind);
            await ArmyGestures.Finish(room, room.Fp);
            Assert.AreEqual(DecisionKind.RemoveExcess, room.Fp.Board.View.Decision.Kind);
            Assert.AreEqual(DieLocation.Resolving, room.Fp.Board.View.Dice.Single(d => d.Id == die).Location);
            // UI-215: the one excess figure is chosen in its region's panel and sent with that click.
            await room.ChooseFigure(room.Fp, id);
            await room.Confirm(room.Fp);
            Assert.AreEqual("reserve", room.Sh.Board.View.Units.Single(u => u.Id == id).Location);
            T22NetworkActions.Spent(room, Side.FreePeoples, die);
        }

        private static string SelectDie(MountedMatchClient client)
        {
            var die = client.Board.View.Dice.Single(d => d.Owner == Side.FreePeoples && d.Face == DieFace.Muster).Id;
            ArmyGestures.Die(client, die, DieFace.Muster);
            return die;
        }

        private static void Hover(VisualElement target)
        {
            using var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = target.worldBound.center });
            target.SendEvent(move);
        }

        private static async Task<Button> Reserve(MatchTestRoom room, string region, UnitKind kind)
        {
            var board = room.Fp.Board;
            if (region == "pelargir")
            {
                board.Map.Focus(region);
                await room.Settle();
                var point = RegionInspectionTests.Terrain(board, region);
                Assert.IsFalse(new Wotr.Client.RegionViewIndex(board.View).Occupied(region));
                RegionInspectionTests.Move(board, point);
                await Task.Delay(300, room.Token);
                Assert.AreEqual(DisplayStyle.None, board.Hover.Panel.Root.resolvedStyle.display,
                    "An empty settlement must wait for a click even with a Muster die selected.");
                var die = board.ActionDie.DieId;
                RegionInspectionTests.ClickTerrain(board, point);
                Assert.AreEqual(die, board.ActionDie.DieId);
            }
            else
                board.Hover.Open(region);
            await room.Settle();
            // UI-135: reserves are chosen only in the Reserve tab; UI-181: the player opens it, a Muster die only lights it.
            room.Fp.Board.Reinforcements.Open();
            await room.Settle();
            return room.Fp.Board.Reinforcements.Root.Q<Button>("reserve-token-gondor-" + kind);
        }
    }
}
