using System;
using System.Collections;
using System.IO;
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
    public sealed partial class BoardCompletionLayoutTests
    {
        [UnityTest]
        public IEnumerator MenuAndSettingsPreserveBattleAndHuntDraftsAcrossFourLayouts() => Wait(MenuLayouts());

        private static async Task MenuLayouts()
        {
            foreach (var battle in new[] { true, false })
            {
                var boundary = new UI10HttpBoundary();
                var room = await Connect(battle ? "no-regular" : "ui34-hunt-reroll",
                    battle ? ScenarioKind.Battle : ScenarioKind.Interaction,
                    fpHandler: battle ? boundary : null, shHandler: battle ? null : boundary, openPanels: false);
                var directory = Path.Combine(Path.GetTempPath(), "wotr-ui72-layout-" + Guid.NewGuid().ToString("N"));
                LocalMatchPersistence persistence = null;
                try
                {
                    persistence = await AttachPersistence(room, battle, directory);
                    var owner = battle ? room.Fp : room.Sh;
                    boundary.MatchPath = "/api/matches/" + owner.Board.View.MatchId;
                    var selected = await PrepareDecision(room, owner, battle);
                    await persistence.FlushStable();
                    Assert.IsEmpty(persistence.RecoveryError);
                    Assert.IsNotNull(persistence.Draft);
                    AssertDraftSelection(persistence.Draft, battle, selected);
                    var expected = Json(persistence.Draft);
                    await FourLayouts(room, battle ? "menu-battle" : "menu-hunt", async (current, client) =>
                    {
                        await InspectMenu(current, client, battle);
                        await persistence.FlushStable();
                        Assert.AreEqual(expected, Json(persistence.Draft));
                        AssertDraftSelection(persistence.Draft, battle, selected);
                        Assert.IsNull(persistence.Actions.Pending);
                        Assert.IsNull(persistence.Policy.Pending);
                        Assert.AreEqual(0, boundary.Commands);
                    }, captureFinal: false);
                    Assert.AreSame(battle ? owner.Board.Battles.Root : owner.Board.Hunt.DecisionHost,
                        owner.Board.Tray.parent);
                    Assert.IsTrue(owner.Board.Root.Q<Button>((battle ? "loss-" : "hunt-die-") + selected)
                        .ClassListContains("selected"));
                }
                finally
                {
                    room.Dispose();
                    if (persistence != null)
                        await persistence.FlushStable();
                    if (Directory.Exists(directory))
                        Directory.Delete(directory, true);
                }
            }
        }

        private static async Task<LocalMatchPersistence> AttachPersistence(MatchTestRoom room, bool battle, string directory)
        {
            var original = battle ? room.Fp : room.Sh;
            var api = original.Api;
            var match = original.Board.View.MatchId;
            var persistence = await LocalMatchPersistence.Open(directory, api.Address.AbsoluteUri, api.AccountId, match);
            var replacement = new MountedMatchClient(api.Address.AbsoluteUri, match, api: api, persistence: persistence);
            original.Dispose();
            if (battle)
                room.Fp = replacement;
            else
                room.Sh = replacement;
            replacement.Connection.Start();
            await room.Until(() => replacement.Connected > 0);
            await room.Settle();
            return persistence;
        }

        private static async Task<string> PrepareDecision(MatchTestRoom room, MountedMatchClient owner, bool battle)
        {
            var board = owner.Board;
            string selected;
            if (battle)
            {
                Assert.AreEqual(DecisionKind.Losses, board.View.Decision.Kind);
                board.Battles.Open();
                selected = board.View.Battle.Attacker.Units.Single(unit => unit.Kind == UnitKind.Elite).Id;
            }
            else
            {
                Assert.AreEqual(DecisionKind.HuntReroll, board.View.Decision.Kind);
                board.Hunt.Open();
                selected = board.View.Options.Single(option => option.Kind == CommandKind.RerollHunt).EligibleIds.First();
            }
            await room.Settle();
            await Press(room, board.Root.Q<Button>((battle ? "loss-" : "hunt-die-") + selected));
            return selected;
        }

        private static void AssertDraftSelection(SavedTurnDraft saved, bool battle, string selected)
        {
            Assert.IsNotNull(saved.Draft);
            if (battle)
            {
                Assert.AreEqual(1, saved.Draft.Payload.Losses.Count);
                Assert.AreEqual(selected, saved.Draft.Payload.Losses.Single().UnitId);
            }
            else
                CollectionAssert.AreEqual(new[] { selected }, saved.Draft.Payload.Ids);
        }

        private static async Task InspectMenu(MatchTestRoom room, MountedMatchClient client, bool battle)
        {
            var board = client.Board;
            var camera = board.Map.Camera.Pan;
            await Press(room, board.Menu.Toggle);
            CheckPanel(board.Menu.Panel, board.Root.worldBound);
            await Press(room, board.Menu.Panel.Q<Button>("open-interface"));
            var settings = client.Interface.Settings;
            Assert.IsTrue(settings.IsOpen);
            var panel = settings.Root.Q(className: "interface-settings-panel");
            CheckPanel(panel, settings.Root.worldBound);
            await Press(room, settings.Root.Q<Button>(client.Interface.Preferences.Scale == 1f ? "scale-100" : "scale-125"));
            var motion = settings.Root.Q<Toggle>("reduced-motion");
            Assert.IsTrue(motion.value);
            await Press(room, motion);
            Assert.IsFalse(client.Interface.Preferences.ReducedMotion);
            await Press(room, motion);
            Assert.IsTrue(client.Interface.Preferences.ReducedMotion);
            BoardMenuLayoutTests.Key(settings.Root.Q<Button>("close-interface"), true);
            BoardMenuLayoutTests.Key(board.Root, false);
            await room.Settle();
            Assert.IsFalse(settings.IsOpen);
            Assert.AreSame(board.Menu.Toggle, board.Root.panel.focusController.focusedElement);
            await Press(room, board.Menu.Toggle);
            Assert.IsNull(board.Menu.Panel.Q<Button>("board-menu-help"), "UI-173: the board menu has no Help.");
            var texture = client.Panel.targetTexture;
            LayoutTests.Save(texture, $"ui72-menu-{(battle ? "battle" : "hunt")}-{board.View.You}-{texture.width}-{client.Interface.Preferences.Scale:0.00}.png");
            await Press(room, board.Menu.Panel.Q<Button>("board-menu-resume"));
            await room.Settle();
            Assert.IsFalse(board.Menu.IsOpen);
            Assert.AreEqual(camera, board.Map.Camera.Pan);
            if (battle)
                board.Battles.Open();
            else
                board.Hunt.Open();
            await room.Settle();
            Bounds(battle ? board.Battles.Root : board.Hunt.Root, board.Map.parent.worldBound);
        }
    }
}
