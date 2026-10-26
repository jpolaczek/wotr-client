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

        [UnityTest]
        public IEnumerator EmptyAndStayKeepTheSamePrivateWaitingViewAndGrantNoTokens() => MatchTestRoom.Wait(Empty());

        [UnityTest]
        public IEnumerator OneDepartureHandsEitherTokenToTheShadowResourcePanel() => MatchTestRoom.Wait(One());

        [UnityTest]
        public IEnumerator TwoAndFiveDeparturesShareClickDropUndoAndGrantBothTokensOnce() => MatchTestRoom.Wait(Many());

        private static async Task Empty()
        {
            using var empty = await UI10TestRoom.Connect("ui69-empty");
            using var stay = await UI10TestRoom.Connect("ui69-stay");
            Assert.IsEmpty(empty.Fp.Board.View.Options.Single().CharacterActions);
            Assert.AreEqual(5, stay.Fp.Board.View.Options.Single().CharacterActions.Count);
            Assert.AreEqual(Public(stay.Sh.Board.View), Public(empty.Sh.Board.View));
            foreach (var room in new[] { empty, stay })
            {
                AssertPrivate(room.Sh.Board);
                Assert.AreSame(room.Fp.Board.Fellowship.Root, room.Fp.Board.Tray.parent, "UI-270: the homelands are on the Fellowship panel.");
                Assert.AreEqual("No tokens", Effect(room.Fp.Board));
                var revision = room.Fp.Board.View.Revision;
                await Confirm(room);
                Assert.AreEqual(revision + 1, room.Fp.Board.View.Revision);
                Assert.AreEqual(DecisionKind.FellowshipPhase, room.Fp.Board.View.Decision.Kind);
                Assert.IsEmpty(room.Sh.Board.View.CouncilTokens);
                Assert.AreEqual(7, room.Fp.Board.View.Fellowship.CompanionIdentities.Count);
                Assert.AreEqual(1, room.Boundary.Commands);
            }
        }

        private static async Task One()
        {
            foreach (var token in new[] { ExpansionDraft.CouncilPolitics, ExpansionDraft.CouncilTravel })
            {
                using var room = await UI10TestRoom.Connect(token == ExpansionDraft.CouncilPolitics ? "ui69-one-politics" : "ui69-one-travel");
                var before = JsonConvert.SerializeObject(room.Sh.Board.View);
                await Place(room, Gimli, "erebor", false);
                Assert.AreEqual("Shadow chooses one", Effect(room.Fp.Board));
                Assert.AreEqual(before, JsonConvert.SerializeObject(await room.Sh.Api.Get<PlayerView>(
                    "api/matches/" + room.Sh.Board.View.MatchId, room.Token)));
                Assert.AreEqual(0, room.Boundary.Commands);
                await Confirm(room);
                Assert.AreEqual(DecisionKind.CouncilToken, room.Sh.Board.View.Decision.Kind);
                // UI-270: the Shadow takes its token on the lit medallion in its pool; no panel or window opens.
                await room.Settle();
                Assert.AreEqual(DecisionHost.Dice, DecisionRoutes.Of(room.Sh.Board.View));
                Assert.IsFalse(room.Sh.Board.ResourceDecision.IsOpen);
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
                Assert.AreEqual(count == 2 ? "Shadow chooses one" : "Shadow receives both", Effect(board));
                await Place(room, choices[0].CharacterId, choices[0].Targets.Single(), false);
                Assert.AreEqual("Shadow receives both", Effect(board));
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
        private static string Effect(BoardScreen board) => board.Tray.Q<Label>("homeland-token-effect").text;
        private static Button Portrait(BoardScreen board, string id) => board.Fellowship.Root.Q<Button>("fellowship-portrait-" + id);

        private static void AssertPrivate(BoardScreen board)
        {
            Assert.IsNull(board.View.Decision);
            Assert.IsEmpty(board.View.OwnCompanions);
            Assert.IsEmpty(board.View.OwnCompanionVersions);
            Assert.IsFalse(CompanionSetupPresentation.Setup(board.View));
            Assert.IsTrue(board.Root.Query<Label>("homeland-location").ToList().All(l => l.style.display.value == DisplayStyle.None));
            Assert.IsNull(board.Root.Q<VisualElement>("council-homeland-choices"));
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
            await room.Until(() => room.Fp.Board.Tray.Q<Button>("confirm-decision")?.enabledInHierarchy == true);
            await Press(room, room.Fp.Board, "confirm-decision");
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
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains(region));
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
