using System.Threading.Tasks;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal static class ComponentGestures
    {
        public static async Task Select(MountedMatchClient client, string key)
        {
            var button = await Show(client, key);
            if (key is not ("Pass" or "DiscardDie"))
            {
                CardNetworkTests.Click(button);
                return;
            }
            // UI-227: a console Pass or die discard is sent at once and never flashes the decision window.
            var opened = 0;
            void Opening() => opened++;
            client.Board.DecisionWindow.Opening += Opening;
            try
            {
                CardNetworkTests.Click(button);
                await Task.Delay(400);
            }
            finally
            {
                client.Board.DecisionWindow.Opening -= Opening;
            }
            Assert.AreEqual(0, opened, key + " opened the decision window while it was being sent.");
        }

        public static async Task<Button> Show(MountedMatchClient client, string key)
        {
            // UI-160: Pass and discarding a die are permanent actions of the turn console.
            if (key is "Pass" or "DiscardDie")
            {
                var action = key == "Pass" ? client.Board.Console.Actions.Pass : client.Board.Console.Actions.Discard;
                Assert.IsTrue(action.enabledInHierarchy, "Unavailable console action " + key);
                return action;
            }
            var region = client.Board.Hover.Panel.ActionRegion(key);
            if (region.Length > 0)
            {
                client.Board.Hover.Open(region);
                await Task.Delay(100);
            }
            var root = region.Length > 0 ? client.Board.Hover.Panel.Components.Root : client.Board.Components.Root;
            var button = root.Q<Button>("component-" + key);
            Assert.IsNotNull(button, "Missing component " + key);
            Assert.IsTrue(button.enabledInHierarchy, "Unavailable component " + key);
            return button;
        }

        public static async Task Remove(MountedMatchClient client, string card)
        {
            client.Board.TableEffects.Inspect(card);
            await Task.Delay(100);
            var button = client.Board.TableEffects.Root.Q<Button>("remove-table-" + card);
            Assert.IsNotNull(button, "Missing removal on table card " + card);
            Assert.IsTrue(button.enabledInHierarchy);
            CardNetworkTests.Click(button);
        }

        public static async Task Hover(VisualElement target)
        {
            using (var move = PointerMoveEvent.GetPooled(new Event { type = EventType.MouseMove, mousePosition = target.worldBound.center }))
                target.panel.visualTree.SendEvent(move);
            await Task.Delay(300);
        }

        public static string Character(CharacterActionOption choice) => "character-" + choice.CharacterId + "-" + choice.AbilityId;

        // UI-108/UI-142: a character enters by a click on its row in the Characters list; the row opens no inspector
        // while it can enter.
        public static async Task Enter(MatchTestRoom room, MountedMatchClient client, CharacterActionOption choice)
        {
            var board = client.Board;
            var figure = CharacterRosterActions.Figure(board.View, choice.CharacterId);
            board.Characters.Open();
            await room.Settle();
            var row = board.Characters.Root.Q<Button>("character-roster-" + figure);
            Assert.IsNotNull(row, figure);
            Assert.IsTrue(row.ClassListContains(CharacterRosterPanel.EntryClass), figure + " can enter for the chosen cost.");
            PointerGestureEvents.ClickAt(row);
            await room.Settle();
            Assert.AreEqual(DisplayStyle.None, board.Characters.Inspector.Root.resolvedStyle.display, "The entering row opens no window.");
        }

        public static async Task Cost(MatchTestRoom room, MountedMatchClient client, string id)
        {
            T22NetworkActions.Die(client, id);
            await room.Settle();
        }
    }
}
