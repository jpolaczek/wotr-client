using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    public sealed partial class BoardCompletionLayoutTests
    {
        private static async Task InspectVersions(MatchTestRoom room, MountedMatchClient client)
        {
            var board = client.Board;
            if (board.View.You == Side.Shadow)
            {
                AssertPrivateSetup(board);
                return;
            }
            var option = board.View.Options.Single(value => value.Kind == CommandKind.ChooseCompanionVersions);
            var camera = board.Map.Camera.Pan;
            // UI-269: the versions are chosen on the Fellowship panel's portraits.
            Assert.IsTrue(board.Fellowship.IsOpen);
            Assert.AreSame(board.Fellowship.Root, board.Tray.parent);
            Assert.AreEqual(board.View.Configuration.Council ? 7 : 1, option.CompanionVersions.Count);
            foreach (var group in option.CompanionVersions)
            {
                await Press(room, board.Root.Q<Button>("fellowship-portrait-" + group.FigureId));
                Assert.IsNotNull(board.Tray.Q<Button>("version-" + group.BaseId));
                Assert.IsNotNull(board.Tray.Q<Button>("version-" + group.AlternateId));
                CheckPanel(board.Fellowship.Root, board.Map.parent.worldBound);
                var abilities = board.Tray.Query<Label>(className: "setup-version-ability").ToList();
                Assert.IsNotEmpty(abilities);
                foreach (var ability in abilities)
                {
                    Assert.IsTrue(ability.focusable);
                    Assert.Greater(ability.tooltip.Length, ability.text.Length);
                }
                await ReadTooltip(room, client, abilities.OrderByDescending(label => label.tooltip.Length).First());
            }
            await Press(room, board.Root.Q<Button>("fellowship-portrait-" + Grey));
            await Press(room, board.Root.Q<Button>("version-" + option.CompanionVersions.Single(group => group.FigureId == Grey).BaseId));
            await Press(room, board.Root.Q<Button>("starting-guide-" + Strider));
            Assert.IsTrue(board.Tray.Q<Button>("confirm-decision").enabledInHierarchy);
            if (board.View.Configuration.Council)
                await Press(room, board.Root.Q<Button>("fellowship-portrait-" + Gimli));
            Assert.IsTrue(board.Root.Q<Button>("starting-guide-" + Strider).ClassListContains("selected"));
            CheckPanel(board.Fellowship.Root, board.Map.parent.worldBound);
            Assert.AreEqual(camera, board.Map.Camera.Pan);
        }

        private static async Task InspectHomelands(MatchTestRoom room, MountedMatchClient client)
        {
            var board = client.Board;
            if (board.View.You == Side.Shadow)
            {
                AssertPrivateSetup(board);
                return;
            }
            Assert.AreEqual(DecisionKind.CouncilHomelands, board.View.Decision.Kind);
            Assert.AreSame(board.CompanionSetup.DecisionHost, board.Tray.parent);
            Assert.AreEqual(5, board.CompanionSetup.Root.Query<Button>(className: "homeland-portrait").ToList().Count);
            var camera = board.Map.Camera.Pan;
            await Press(room, board.Root.Q<Button>("homeland-character-" + Gimli));
            Assert.AreEqual(camera, board.Map.Camera.Pan);
            await PlaceHomeland(room, board, "iron-hills", false);
            await PlaceHomeland(room, board, "ered-luin", true);
            Assert.AreEqual("Shadow chooses one", board.Root.Q<Label>("homeland-token-effect").text);
            await Press(room, board.Root.Q<Button>("homeland-stay"));
            Assert.AreEqual("Fellowship", HomelandLocation(board));
            Assert.AreEqual("No tokens", board.Root.Q<Label>("homeland-token-effect").text);
            await PlaceHomeland(room, board, "ered-luin", false);
            await EscapeAndReopenSetup(room, board);
            Assert.AreEqual("→ Ered Luin", HomelandLocation(board));
            CheckPanel(board.CompanionSetup.Root, board.Map.parent.worldBound);
        }

        private static string HomelandLocation(BoardScreen board) =>
            board.Root.Q<Button>("homeland-character-" + Gimli).Q<Label>("homeland-location").text;

        private static async Task PlaceHomeland(MatchTestRoom room, BoardScreen board, string region, bool drag)
        {
            var portrait = board.Root.Q<Button>("homeland-character-" + Gimli);
            await Press(room, portrait);
            Assert.IsTrue(board.Map.DraftOverlay.Targets.Contains(region));
            // Deliberate camera navigation exposes terrain beside the private setup panel.
            MapContourProbe.ReachRegion(board.Map, region);
            await room.Settle();
            var camera = board.Map.Camera.Pan;
            var zoom = board.Map.Camera.Zoom;
            var point = MapContourProbe.Terrain(board.Map, region);
            if (drag)
            {
                Pickable(portrait);
                MapContourProbe.BeginDrag(board, portrait);
                await room.Settle();
                Assert.AreSame(board.Map, board.Map.panel.Pick(point));
                MapContourProbe.Drop(board, point);
            }
            else
                MapContourProbe.Click(board.Map, point);
            await room.Settle();
            Assert.AreEqual("→ " + board.Presentation.Regions[region].Name, HomelandLocation(board));
            Assert.AreEqual(camera, board.Map.Camera.Pan, "Placing a homeland must preserve the explicitly navigated camera.");
            Assert.AreEqual(zoom, board.Map.Camera.Zoom);
        }

        private static async Task EscapeAndReopenSetup(MatchTestRoom room, BoardScreen board)
        {
            board.CompanionSetup.Root.Focus();
            BoardMenuLayoutTests.Key(board.CompanionSetup.Root, true);
            BoardMenuLayoutTests.Key(board.Root, false);
            await room.Settle();
            Assert.IsFalse(board.CompanionSetup.IsOpen);
            Assert.AreSame(board.CompanionSetup.Tab, board.Root.panel.focusController.focusedElement);
            await Press(room, board.CompanionSetup.Tab);
            Assert.IsTrue(board.CompanionSetup.IsOpen);
        }

        private static void AssertPrivateSetup(BoardScreen board)
        {
            Assert.IsNull(board.View.Decision);
            Assert.IsEmpty(board.View.OwnCompanionVersions);
            Assert.IsEmpty(board.View.OwnCompanions);
            Assert.IsFalse(board.CompanionSetup.Active);
            Assert.IsFalse(board.CompanionSetup.IsOpen);
            Assert.AreEqual(DisplayStyle.None, board.CompanionSetup.Tab.resolvedStyle.display);
            Assert.IsEmpty(board.CompanionSetup.DecisionHost.Children());
            Assert.IsNull(board.Root.Q("companion-setup-choices"));
            Assert.IsNull(board.Root.Q("council-homeland-choices"));
        }
    }
}
