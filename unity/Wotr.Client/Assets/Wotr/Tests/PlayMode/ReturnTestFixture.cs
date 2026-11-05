using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal sealed class ReturnTestFixture
    {
        public string Address, ReturnFreePeoplesUser, ReturnShadowUser, Password;
        public Dictionary<string, string> ReturnMatches;

        public static ReturnTestFixture Load()
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real server integration.");
            return JsonConvert.DeserializeObject<ReturnTestFixture>(File.ReadAllText(path));
        }

        public async Task<GameApi> Login(Side side, CancellationToken token, HttpMessageHandler handler = null, string address = null)
        {
            var api = new GameApi(address ?? Address, handler);
            try
            {
                await api.Login(side == Side.FreePeoples ? ReturnFreePeoplesUser : ReturnShadowUser, Password, token);
                return api;
            }
            catch { api.Dispose(); throw; }
        }

        public static async Task Open(MountedFlowClient client, string id, CancellationToken token)
        {
            while (client.Root.Q<Button>("resume-match-" + id) == null)
            {
                token.ThrowIfCancellationRequested();
                await ShowList(client.Root, "resume-match-" + id, token);
                var scroll = client.Root.Q<ScrollView>("my-games-scroll");
                if (scroll != null && scroll.verticalScroller.highValue > 0)
                    scroll.verticalScroller.value = scroll.verticalScroller.highValue;
                await Task.Delay(100, token);
            }
            await ShowList(client.Root, "resume-match-" + id, token);
            var button = client.Root.Q<Button>("resume-match-" + id);
            await Reveal(button, token);
            Click(button);
            await Until(() => client.Board?.View?.MatchId == id && client.Board.Connection.text == "Live", token);
            await Task.Delay(80, token);
        }

        public static async Task Until(Func<bool> condition, CancellationToken token)
        {
            while (!condition())
                await Task.Delay(25, token);
        }

        // A test-menu scenario, opened as the owner opens it (UI-380, UI-412).
        public static async Task OpenScenario(MountedFlowClient client, string scenario, CancellationToken token)
        {
            await ClickReady(client.Root, "open-test-scenarios", token);
            client.Root.Q<TextField>("test-scenario-search").value = scenario;
            await Task.Delay(150, token);
            await ClickReady(client.Root, "test-scenario-" + scenario, token);
            await Until(() => client.Board?.View != null && client.Board.Connection.text == "Live", token);
        }

        public static async Task ClickReady(VisualElement root, string name, CancellationToken token)
        {
            if (name is "back-to-games" or "open-interface")
            {
                // UI-169: a finished game shows its end screen over the board; View board uncovers the menu.
                var end = root.Q<Button>("game-end-view-board");
                if (end != null && end.panel != null && end.worldBound.height > 0)
                {
                    Click(end);
                    await Task.Delay(80, token);
                }
                var menu = root.Q<VisualElement>("board-menu");
                if (menu != null && menu.resolvedStyle.display == DisplayStyle.None)
                {
                    PointerGestureEvents.ClickAt(root.Q<Button>("open-board-menu"));
                    await Task.Delay(80, token);
                }
            }
            await ShowList(root, name, token);
            await Until(() => root.Q<Button>(name)?.enabledInHierarchy == true && root.Q<Button>(name).worldBound.height > 0, token);
            var button = root.Q<Button>(name);
            await Reveal(button, token);
            Click(button);
            await Task.Delay(60, token);
        }

        // MENU-11: the menu window shows one list at a time; a row of the other list waits behind its action.
        public static async Task ShowList(VisualElement root, string row, CancellationToken token)
        {
            var list = row.StartsWith("join-public-") || row.StartsWith("observe-public-") ? "show-public-games"
                : row.StartsWith("resume-match-") || row.StartsWith("open-room-") ? "show-my-games"
                : null;
            var action = list == null ? null : root.Q<Button>(list);
            if (action == null || action.ClassListContains("selected"))
                return;
            await Until(() => action.enabledInHierarchy && action.worldBound.height > 0, token);
            Click(action);
            await Task.Delay(60, token);
        }

        private static async Task Reveal(VisualElement element, CancellationToken token)
        {
            await Until(() => element.panel != null && element.worldBound.height > 0, token);
            element.GetFirstAncestorOfType<ScrollView>()?.ScrollTo(element);
            await Task.Delay(80, token);
        }

        public static void Click(Button button) => CardNetworkTests.Click(button);
    }
}
