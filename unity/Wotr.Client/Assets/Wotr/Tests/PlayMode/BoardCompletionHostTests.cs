using System;
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
    public sealed partial class BoardCompletionContextNetworkTests
    {
        [UnityTest]
        public IEnumerator NewHobbitDecisionsDuringMenuAndSettingsKeepOneHostCostAndFocus() => Wait(HobbitWindows());

        [UnityTest]
        public IEnumerator CharacterReactionLinkAndAwakenedPortraitReuseTheExistingOwners() => Wait(Owners());

        private static async Task HobbitWindows()
        {
            using var room = await Connect("ui71-hobbits", ScenarioKind.Interaction, openPanels: false);
            await ActionNoticeNetworkTests.Loaded(room);
            var client = room.Fp;
            var board = client.Board;
            var die = board.View.Dice.Single(value => value.Location == DieLocation.Resolving).Id;
            var pan = board.Map.Camera.Pan;
            var first = board.View.Decision.Id;
            var redBefore = client.Activity.Model.StepCount;
            Assert.AreSame(board.Hunt.DecisionHost, board.Tray.parent);
            PointerGestureEvents.ClickAt(board.Menu.Toggle);
            await room.Settle();
            await ReplyWhileOverlayIsOpen(room, CommandKind.RespondEffect, "character.merry-guide");
            Assert.AreNotEqual(first, board.View.Decision.Id);
            Assert.IsTrue(board.Menu.IsOpen, "A new nested decision must not dismiss Menu.");
            Assert.AreSame(board.Hunt.DecisionHost, board.Tray.parent);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreEqual(DieLocation.Resolving, board.View.Dice.Single(value => value.Id == die).Location);
            PointerGestureEvents.ClickAt(board.Menu.Panel.Q<Button>("board-menu-resume"));
            await room.Settle();
            Assert.AreSame(board.Menu.Toggle, board.Root.panel.focusController.focusedElement);
            board.Hunt.Close();
            await KingsCardNetworkChecks.Reconnect(room, client);
            Assert.IsFalse(board.Hunt.Pinned, "The same decision ID must respect Close after refresh.");
            PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
            Assert.IsTrue(board.Hunt.IsOpen);

            PointerGestureEvents.ClickAt(board.Menu.Toggle);
            await room.Settle();
            PointerGestureEvents.ClickAt(board.Menu.Panel.Q<Button>("open-interface"));
            await room.Until(() => client.Interface.Settings.IsOpen);
            await room.Settle();
            var second = board.View.Decision.Id;
            await ReplyWhileOverlayIsOpen(room, CommandKind.RespondEffect, "character.pippin-guide");
            Assert.IsTrue(client.Interface.Settings.IsOpen);
            Assert.AreNotEqual(second, board.View.Decision.Id);
            Assert.AreEqual(pan, board.Map.Camera.Pan);
            Assert.AreSame(board.Hunt.DecisionHost, board.Tray.parent);
            PointerGestureEvents.ClickAt(client.Interface.Settings.Root.Q<Button>("close-interface"));
            await room.Settle();
            Assert.AreSame(board.Menu.Toggle, board.Root.panel.focusController.focusedElement);
            Assert.AreEqual(redBefore, client.Activity.Model.StepCount,
                "Own automatic consequences and new own decisions are not enemy actions.");
            board.Notice.Open();
            await room.Settle();
            var skip = board.Tray.Q<Button>("action-DeclineReaction");
            Assert.IsNotNull(skip, "Gollum's voluntary response must preserve an explicit refusal.");
            var revision = board.View.Revision;
            PointerGestureEvents.ClickAt(skip);
            await room.At(revision + 1);
            Assert.AreEqual(DecisionKind.CompanionSeparation, board.View.Decision.Kind);
            Assert.AreSame(board.Fellowship.Root, board.Tray.parent);
            board.Fellowship.Close();
            PointerGestureEvents.ClickAt(board.Notice.Button);
            await room.Settle();
            Assert.IsTrue(board.Fellowship.IsOpen);
            Assert.AreEqual(2, board.Tray.Query<Button>(className: "companion-portrait").ToList()
                .Count(button => button.ClassListContains("selected")));
            Assert.AreSame(board.Fellowship.Root, board.Tray.parent);
            revision = board.View.Revision;
            FellowshipGestures.FinishRoute(client, "north-dunland");
            await room.At(revision + 1);
            Assert.IsTrue(board.View.Characters.Where(c => c.Id is "base.character.merry" or "base.character.pippin")
                .All(c => c.Location == "north-dunland"));
            Assert.AreEqual(2, board.View.Characters.Count(c => c.Id is "base.character.merry" or "base.character.pippin"));
            Assert.AreEqual(DieLocation.Hunt, board.View.Dice.Single(value => value.Id == die).Location);
            Assert.AreEqual(1, board.Root.Query<VisualElement>("decision-tray").ToList().Count);
            LayoutTests.Save(client.Viewport, "ui71-hobbit-return.png");
        }

        private static async Task ReplyWhileOverlayIsOpen(MatchTestRoom room, CommandKind kind, string reaction)
        {
            var view = room.Fp.Board.View;
            var receipt = await room.Fp.Api.Post<CommandReceipt>("api/matches/" + view.MatchId + "/commands", new GameCommand
            {
                MatchId = view.MatchId, ExpectedRevision = view.Revision, DecisionId = view.Decision.Id,
                CommandId = Guid.NewGuid().ToString("N"), Kind = kind,
                Payload = new CommandPayload { ReactionId = reaction }
            }, room.Token);
            Assert.IsTrue(receipt.Accepted);
            await room.At(receipt.Revision);
        }

        private static async Task Owners()
        {
            using (var room = await Connect("ui71-white-rider", ScenarioKind.Interaction, openPanels: false))
            {
                var board = room.Fp.Board;
                board.Characters.Show(Grey, true);
                await room.Settle();
                var link = board.Characters.Inspector.Actions.Q<Button>("character-reaction-link");
                Assert.IsNotNull(link);
                PointerGestureEvents.ClickAt(link);
                await room.Settle();
                Assert.IsTrue(board.Battles.IsOpen);
                Assert.AreSame(board.Battles.Root, board.Tray.parent);
                Assert.AreEqual(1, board.Root.Query<VisualElement>("decision-tray").ToList().Count);
                Assert.AreEqual(0, board.Characters.DecisionHost.childCount);
                var revision = board.View.Revision;
                PointerGestureEvents.ClickAt(board.Tray.Q<Button>("action-DeclineReaction"));
                await room.At(revision + 1);
                Assert.AreEqual(DecisionKind.CombatCard, board.View.Decision.Kind);
                Assert.AreSame(board.Battles.Root, board.Tray.parent);
                Assert.IsFalse(board.View.Battle.WhiteRiderActive);
                LayoutTests.Save(room.Fp.Viewport, "ui71-battle-reaction-owner.png");
            }
            using (var room = await Connect("ui71-awakened", ScenarioKind.Interaction, openPanels: false))
            {
                var board = room.Fp.Board;
                const string dain = "kings.sovereign.dain";
                await room.Settle();
                PointerGestureEvents.ClickAt(await BoardNavigation.Sovereign(board, dain));
                await room.Settle();
                Assert.AreEqual(dain, board.Characters.Inspector.FigureId);
                Assert.AreEqual(1, board.Root.Query<VisualElement>("character-inspector").ToList().Count);
                Assert.AreEqual(1, board.View.OwnCharacterRoster.Count(row => row.FigureId == dain));
                Assert.AreEqual(DisplayStyle.None, board.Sovereigns.Inspector.resolvedStyle.display);
                var revision = board.View.Revision;
                var pan = board.Map.Camera.Pan;
                await room.Settle();
                Assert.AreEqual(dain, board.Characters.Inspector.FigureId);
                Assert.AreEqual(revision, board.View.Revision);
                Assert.AreEqual(pan, board.Map.Camera.Pan);
                LayoutTests.Save(room.Fp.Viewport, "ui71-awakened-shared-inspector.png");
            }
        }
    }
}
