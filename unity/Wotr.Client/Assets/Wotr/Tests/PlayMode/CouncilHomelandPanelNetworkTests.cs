using System;
using System.Collections;
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
    public sealed partial class CouncilHomelandPanelNetworkTests
    {
        private const string Gimli = "base.character.gimli";
        private const string Legolas = "base.character.legolas";

        // UI-482 (owner, 2026-11-04): the Shadow's view is the same in shape; with no candidate the client answers the step
        // itself and a refused answer leaves the ✓ to the player; with a candidate, choosing nobody is the player's ✓.
        [UnityTest]
        public IEnumerator EmptyAndStayKeepTheSamePrivateWaitingViewAndGrantNoTokens() => MatchTestRoom.Wait(Empty());

        [UnityTest]
        public IEnumerator OneDepartureHandsEitherTokenToTheShadowsPool() => MatchTestRoom.Wait(One());

        [UnityTest]
        public IEnumerator TwoAndFiveDeparturesShareClickDropUndoAndGrantBothTokensOnce() => MatchTestRoom.Wait(Many());

        private static async Task Empty()
        {
            using var empty = await UI10TestRoom.Connect("ui69-empty", holdFreePeoples: true);
            using var stay = await UI10TestRoom.Connect("ui69-stay");
            Assert.IsEmpty(empty.Fp.Board.View.Options.Single().CharacterActions);
            Assert.AreEqual(5, stay.Fp.Board.View.Options.Single().CharacterActions.Count);
            Assert.AreEqual(Public(stay.Sh.Board.View), Public(empty.Sh.Board.View));
            foreach (var room in new[] { empty, stay })
            {
                AssertPrivate(room.Sh.Board);
                Assert.AreSame(room.Fp.Board.Fellowship.Root, room.Fp.Board.Tray.parent, "UI-270: the homelands are on the Fellowship panel.");
            }
            await ACandidateWaitsForTheConfirm(stay);
            await ARefusedAnswerLeavesTheConfirm(empty);
            foreach (var room in new[] { empty, stay })
            {
                Assert.AreEqual(DecisionKind.FellowshipPhase, room.Fp.Board.View.Decision.Kind);
                Assert.IsEmpty(room.Sh.Board.View.CouncilTokens);
                Assert.AreEqual(7, room.Fp.Board.View.Fellowship.CompanionIdentities.Count);
            }
        }

        private const int PastThePause = CouncilHomelandSkip.LongestPause + 300;

        private static async Task ACandidateWaitsForTheConfirm(UI10TestRoom room)
        {
            var revision = room.Fp.Board.View.Revision;
            await Task.Delay(PastThePause, room.Token);
            Assert.AreEqual(0, room.Boundary.Commands, "Choosing nobody with a candidate available is a real decision.");
            Assert.AreEqual(DecisionKind.CouncilHomelands, room.Fp.Board.View.Decision.Kind);
            StringAssert.EndsWith("· ✓ confirms.", Effect(room.Fp.Board));
            StringAssert.DoesNotContain("Council token", Effect(room.Fp.Board));
            await Confirm(room);
            Assert.AreEqual(revision + 1, room.Fp.Board.View.Revision);
            Assert.AreEqual(1, room.Boundary.Commands);
        }

        // The client waits its short pause and sends once, without a click; refused, it leaves the ✓ and does not repeat.
        private static async Task ARefusedAnswerLeavesTheConfirm(UI10TestRoom room)
        {
            var board = room.Fp.Board;
            var revision = board.View.Revision;
            room.Boundary.RefuseNextCommand = true;
            var pause = System.Diagnostics.Stopwatch.StartNew();
            room.Fp.Interaction.Hold(false);
            await room.Until(() => Effect(board) == CouncilHomelandPresentation.AllStay);
            await room.Settle();
            Assert.AreEqual(0, room.Boundary.Commands, "The answer waits for its pause.");
            Assert.AreEqual(DisplayStyle.None, board.Notice.Button.style.display.value, "No notice asks to place a companion while none can be.");
            LayoutTests.Save(room.Fp.Viewport, "ui482-empty-homelands-pause.png");
            await room.Until(() => room.Boundary.Commands == 1);
            Assert.GreaterOrEqual(pause.ElapsedMilliseconds, CouncilHomelandSkip.ShortestPause - 50);
            Assert.LessOrEqual(pause.ElapsedMilliseconds, CouncilHomelandSkip.LongestPause + 1000);
            Assert.AreEqual(CommandKind.ChooseCouncilHomelands, room.Boundary.LastCommand.Kind);
            Assert.IsEmpty(room.Boundary.LastCommand.Payload.Placements);
            await room.Until(() => Effect(board) == CouncilHomelandPresentation.AllStayByHand && board.Console.Actions.Confirm.enabledInHierarchy);
            await Task.Delay(PastThePause, room.Token);
            Assert.AreEqual(1, room.Boundary.Commands, "No retry loop.");
            Assert.AreEqual(revision, board.View.Revision);
            await Confirm(room);
            Assert.AreEqual(2, room.Boundary.Commands);
        }

        private static async Task One()
        {
            foreach (var token in new[] { ExpansionDraft.CouncilPolitics, ExpansionDraft.CouncilTravel })
            {
                using var room = await UI10TestRoom.Connect(token == ExpansionDraft.CouncilPolitics ? "ui69-one-politics" : "ui69-one-travel");
                var before = JsonConvert.SerializeObject(room.Sh.Board.View);
                await Place(room, Gimli, "erebor", false);
                StringAssert.Contains("Shadow chooses one Council token", Effect(room.Fp.Board));
                Assert.AreEqual(before, JsonConvert.SerializeObject(await room.Sh.Api.Get<PlayerView>(
                    "api/matches/" + room.Sh.Board.View.MatchId, room.Token)));
                Assert.AreEqual(0, room.Boundary.Commands);
                await Confirm(room);
                Assert.AreEqual(DecisionKind.CouncilToken, room.Sh.Board.View.Decision.Kind);
                // UI-270: the Shadow takes its token on the lit medallion in its pool; no panel or window opens.
                await room.Settle();
                Assert.AreEqual(DecisionHost.Dice, DecisionRoutes.Of(room.Sh.Board.View));
                Assert.IsFalse(room.Sh.Board.DecisionWindow.IsOpen);
                foreach (var id in CouncilTokenSlots.Ids)
                {
                    var slot = room.Sh.Board.OwnDice.Q<Button>("pool-" + id);
                    Assert.IsTrue(slot.ClassListContains("legal-target"), id + " is offered.");
                    StringAssert.StartsWith("Click to take", slot.tooltip);
                }
                Assert.IsFalse(CompanionSetupPresentation.Setup(room.Fp.Board.View));
                Assert.IsEmpty(room.Sh.Board.View.CouncilTokens);
                Assert.AreEqual("erebor", room.Sh.Board.View.Characters.Single(c => c.Id == Gimli).Location);
                Assert.IsFalse(room.Sh.Board.View.Nations.Single(n => n.Id == "dwarves").Active);
                Assert.IsTrue(room.Sh.Board.View.Fellowship.RevealedCompanionVersions.ContainsKey(Gimli));
                var revision = room.Sh.Board.View.Revision;
                await Press(room, room.Sh.Board, "pool-" + token);
                await room.At(revision + 1);
                Assert.AreEqual(token, room.Sh.Board.View.CouncilTokens.Single().Id);
                var other = CouncilTokenSlots.Ids.Single(id => id != token);
                Assert.IsTrue(room.Sh.Board.OwnDice.Q<Button>("pool-" + other).ClassListContains("council-spent"), "The token not taken stays dim.");
                Assert.IsFalse(room.Sh.Board.View.CouncilTokens.Single().Spent);
                Assert.AreEqual(DecisionKind.FellowshipPhase, room.Fp.Board.View.Decision.Kind);
            }
        }

        private static async Task Many()
        {
            foreach (var count in new[] { 2, 5 })
            {
                using var room = await UI10TestRoom.Connect(count == 2 ? "ui69-two" : "ui69-max");
                var board = room.Fp.Board;
                var before = JsonConvert.SerializeObject(room.Sh.Board.View);
                var politics = JsonConvert.SerializeObject(board.View.Nations);
                var choices = board.View.Options.Single().CharacterActions.ToArray();
                Assert.AreEqual(count, choices.Length);
                for (var index = 0; index < choices.Length; index++)
                {
                    var choice = choices[index];
                    await Place(room, choice.CharacterId, choice.Targets.Single(), index % 2 == 0);
                }
                // UI-347 (D14): the first click selects the placed companion, the second keeps it with the Fellowship.
                await Press(room, board, "fellowship-portrait-" + choices[0].CharacterId);
                await Press(room, board, "fellowship-portrait-" + choices[0].CharacterId);
                Assert.AreEqual("", Portrait(board, choices[0].CharacterId).Q<Label>("homeland-location").text);
                StringAssert.Contains(count == 2 ? "Shadow chooses one Council token" : "Shadow receives both Council tokens", Effect(board));
                await Place(room, choices[0].CharacterId, choices[0].Targets.Single(), false);
                StringAssert.Contains("Shadow receives both Council tokens", Effect(board));
                Assert.AreEqual(before, JsonConvert.SerializeObject(await room.Sh.Api.Get<PlayerView>(
                    "api/matches/" + room.Sh.Board.View.MatchId, room.Token)));
                Assert.AreEqual(0, room.Boundary.Commands);
                await Confirm(room);
                Assert.AreEqual(DecisionKind.FellowshipPhase, board.View.Decision.Kind);
                Assert.AreEqual(politics, JsonConvert.SerializeObject(board.View.Nations),
                    "Figures without nation icons cannot activate their homeland; Elves were already active.");
                CollectionAssert.AreEquivalent(new[] { ExpansionDraft.CouncilPolitics, ExpansionDraft.CouncilTravel },
                    room.Sh.Board.View.CouncilTokens.Select(t => t.Id));
                foreach (var choice in choices)
                {
                    var character = room.Sh.Board.View.Characters.Single(c => c.Id == choice.CharacterId);
                    Assert.AreEqual(choice.Targets.Single(), character.Location);
                    Assert.IsFalse(board.View.Fellowship.CompanionIdentities.Contains(character.Id));
                    Assert.AreEqual(CharacterRosterLifecycle.OnBoard, board.View.OwnCharacterRoster.Single(c => c.FigureId == character.Id).Lifecycle);
                }
                Assert.AreEqual(1, room.Boundary.Commands);
            }
        }

        private static string Public(PlayerView view) => JsonConvert.SerializeObject(view).Replace(view.MatchId, "same-match");
        // UI-482 (owner, 2026-11-04): the console's hint names the Shadow's Council tokens.
        private static string Effect(BoardScreen board) => board.Console.Status.Hint.text;
        private static Button Portrait(BoardScreen board, string id) => board.Fellowship.Root.Q<Button>("fellowship-portrait-" + id);

        private static void AssertPrivate(BoardScreen board)
        {
            Assert.IsNull(board.View.Decision);
            Assert.IsEmpty(board.View.OwnCompanions);
            Assert.IsEmpty(board.View.OwnCompanionVersions);
            Assert.IsFalse(CompanionSetupPresentation.Setup(board.View));
            Assert.IsTrue(board.Root.Query<Label>("homeland-location").ToList().All(l => l.style.display.value == DisplayStyle.None));
        }

        private static async Task Press(UI10TestRoom room, BoardScreen board, string name)
        {
            var button = board.Root.Q<Button>(name);
            PointerGestureEvents.ClickAt(button);
            await room.Settle();
        }

        private static async Task Confirm(UI10TestRoom room)
        {
            var revision = room.Fp.Board.View.Revision;
            await room.Until(() => room.Fp.Board.Console.Actions.Confirm.enabledInHierarchy);
            await Press(room, room.Fp.Board, "console-confirm");
            await room.At(revision + 1);
        }

        private static Task Place(UI10TestRoom room, string id, string region, bool drag) =>
            Place(room.Fp.Board, room.Settle, id, region, drag);

        private static async Task Place(BoardScreen board, Func<Task> settle, string id, string region, bool drag)
        {
            var camera = board.Map.Camera.Pan;
            // UI-347 (D14): a click on the selected companion already placed keeps it with the Fellowship, so a move to
            // another homeland starts from the lit homelands.
            if (!Portrait(board, id).ClassListContains("selected"))
                PointerGestureEvents.ClickAt(Portrait(board, id));
            await settle();
            Assert.AreEqual(camera, board.Map.Camera.Pan, "Selecting a portrait never moves the camera.");
            // UI-451: a homeland lights while a companion may start there; the one he already stands in keeps the gold edge.
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains(region) || board.Map.DraftOverlay.ChosenRegions.Contains(region),
                region + " is lit as a homeland.");
            // Reach the actual contour using the same bounded camera navigation as a player.
            MapContourProbe.ReachRegion(board.Map, region);
            await settle();
            var point = MapContourProbe.Terrain(board.Map, region);
            if (drag)
            {
                var portrait = Portrait(board, id);
                var picked = portrait.panel.Pick(portrait.worldBound.center);
                Assert.IsTrue(picked == portrait || portrait.Contains(picked));
                MapContourProbe.BeginDrag(board, portrait);
                await settle();
                Assert.AreSame(board.Map, board.Map.panel.Pick(point), "The drop must still reach exposed terrain.");
                MapContourProbe.Drop(board, point);
            }
            else
                MapContourProbe.Click(board.Map, point);
            await settle();
            Assert.AreEqual("→ " + board.Presentation.Regions[region].Name, Portrait(board, id).Q<Label>("homeland-location").text);
        }
    }
}
