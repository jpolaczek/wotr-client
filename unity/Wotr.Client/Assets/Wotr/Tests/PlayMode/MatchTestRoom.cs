using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal enum ScenarioKind { Battle, Character, CardPack, BaseAcceptance, Interaction }
    internal sealed class MatchTestRoom : IDisposable
    {
        public MountedMatchClient Fp, Sh;
        public CancellationToken Token => lifetime.Token;
        private readonly CancellationTokenSource lifetime;
        private readonly List<GameApi> privateSessions = new List<GameApi>();
        private bool openPanels = true;
        public MountedMatchClient Actor => Fp.Board.View.Decision != null ? Fp : Sh;

        private MatchTestRoom(int seconds = 60)
        {
            lifetime = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        }

        public static async Task<MatchTestRoom> CreateNormalGame(MatchConfiguration configuration = null,
            Func<GameApi, GameApi, RoomView, CancellationToken, Task<RoomView>> startFromLobby = null)
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real server integration.");
            var fixture = JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(path));
            var room = new MatchTestRoom(600);
            try
            {
                var fp = await BattleTestSessions.Login(fixture.Address, fixture.FreePeoplesUser, fixture.Password, room.Token);
                var sh = await BattleTestSessions.Login(fixture.Address, fixture.ShadowUser, fixture.Password, room.Token);
                var lobby = await fp.Post<RoomView>("api/rooms", new CreateRoomRequest { Side = Side.FreePeoples }, room.Token);
                await sh.Post<RoomView>("api/rooms/join", new JoinRoomRequest { Code = lobby.JoinCode }, room.Token);
                if (configuration != null)
                    lobby = await fp.Put<RoomView>($"api/rooms/{lobby.Id}/configuration", new ConfigureRoomRequest
                        { ExpectedVersion = lobby.Version, Configuration = configuration }, room.Token);
                var started = await (startFromLobby == null ? StartAccepted(fp, sh, lobby, room.Token) : startFromLobby(fp, sh, lobby, room.Token));
                room.Fp = new MountedMatchClient(fixture.Address, started.MatchId, api: fp);
                room.Sh = new MountedMatchClient(fixture.Address, started.MatchId, api: sh);
                room.Fp.Connection.Start();
                room.Sh.Connection.Start();
                await room.Until(() => room.Fp.Connected > 0 && room.Sh.Connected > 0);
                await room.Settle();
                await room.OpenBattles();
                return room;
            }
            catch { room.Dispose(); throw; }
        }

        private static async Task<RoomView> StartAccepted(GameApi fp, GameApi sh, RoomView lobby, CancellationToken token)
        {
            var version = new RoomRevisionRequest { ExpectedVersion = lobby.Version };
            await fp.Post<RoomView>($"api/rooms/{lobby.Id}/confirm", version, token);
            await sh.Post<RoomView>($"api/rooms/{lobby.Id}/confirm", version, token);
            return await fp.Post<RoomView>($"api/rooms/{lobby.Id}/start", version, token);
        }

        public static async Task<MatchTestRoom> Connect(string scenario, ScenarioKind kind = ScenarioKind.Battle,
            string readRoot = null, HttpMessageHandler fpHandler = null, HttpMessageHandler shHandler = null, bool openPanels = true)
        {
            var path = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(path))
                Assert.Ignore("Run scripts/test-client.sh for real server integration.");
            var fixture = JsonConvert.DeserializeObject<Fixture>(File.ReadAllText(path));
            var matches = kind switch
            {
                ScenarioKind.Character => fixture.CharacterMatches,
                ScenarioKind.CardPack => fixture.CardPackMatches,
                ScenarioKind.BaseAcceptance => fixture.BaseAcceptanceMatches,
                ScenarioKind.Interaction => fixture.InteractionMatches,
                _ => fixture.BattleMatches
            };
            var match = matches[scenario];
            var room = new MatchTestRoom();
            room.openPanels = openPanels;
            try
            {
                var fp = await room.Session(fixture.Address, fixture.FreePeoplesUser, fixture.Password, fpHandler);
                var sh = await room.Session(fixture.Address, fixture.ShadowUser, fixture.Password, shHandler);
                room.Fp = await room.Mount(fp, match, readRoot);
                room.Sh = await room.Mount(sh, match, readRoot);
                room.Fp.Connection.Start();
                room.Sh.Connection.Start();
                await room.Until(() => room.Fp.Connected > 0 && room.Sh.Connected > 0);
                await room.Settle();
                await room.OpenBattles();
                return room;
            }
            catch { room.Dispose(); throw; }
        }

        private async Task<GameApi> Session(string address, string user, string password, HttpMessageHandler handler)
        {
            if (handler == null) return await BattleTestSessions.Login(address, user, password, Token);
            var api = new GameApi(address, handler);
            privateSessions.Add(api);
            await api.Login(user, password, Token);
            return api;
        }

        private async Task<MountedMatchClient> Mount(GameApi api, string match, string readRoot)
        {
            var reads = readRoot == null ? null : await NotificationReadStore.Open(
                new LocalMatchFiles(readRoot, api.Address.AbsoluteUri, api.AccountId, match));
            return new MountedMatchClient(api.Address.AbsoluteUri, match, api: api, reads: reads);
        }

        public async Task<MountedMatchClient> AnotherDevice(MountedMatchClient original, string readRoot)
        {
            var client = await Mount(original.Api, original.Board.View.MatchId, readRoot);
            client.Connection.Start();
            await Until(() => client.Connected > 0 && client.Activity.Loaded);
            await Settle();
            return client;
        }

        public async Task RestartFreePeoples(string readRoot)
        {
            var previous = Fp;
            await previous.Activity.LocalWrites;
            Fp = await AnotherDevice(previous, readRoot);
            previous.Dispose();
        }

        private sealed class SubmittedChoice { public long Revision; }
        private static readonly ConditionalWeakTable<MountedMatchClient, SubmittedChoice> submitted = new();

        public static void Click(MountedMatchClient client, string name)
        {
            if (name.StartsWith("hunt-response-"))
            {
                client.Board.Hunt.Open();
                // UI-142: a response's click applies it; no tile click follows.
                CardNetworkTests.Click(client.Board.Hunt.Root.Q<Button>(name));
                return;
            }
            var view = client.Board.View;
            if (name.StartsWith("companion-") && view.Decision?.Kind == DecisionKind.Guide ||
                name.StartsWith("effect-choice-") && view.Options.Any(o => o.Kind == CommandKind.RespondEffect ||
                o.Kind == CommandKind.ChooseEffect && o.Minimum == 1 && o.Maximum == 1 && o.EventRecruitment == null) ||
                name.StartsWith("figure-") && view.Battle == null && view.Options.Any(o => o.Kind == CommandKind.RemoveExcess && o.Maximum == 1))
            {
                submitted.Remove(client);
                submitted.Add(client, new SubmittedChoice { Revision = view.Revision });
            }
            VisualElement button = TrayOrConsole(client, name) ?? Reinforcement(client, name) ?? PanelFigure(client, name) ??
                ObjectChoice(client, name) ?? FellowshipControl(client, name);
            if (button == null && name.StartsWith("companion-"))
            {
                if (client.Board.Tray.parent == client.Board.Hunt.DecisionHost)
                {
                    client.Board.Hunt.Open();
                    button = client.Board.Hunt.Root.Q<Button>("fellowship-portrait-" + name.Substring("companion-".Length));
                }
                else
                {
                    client.Board.Fellowship.Open();
                    button = client.Board.Fellowship.Root.Q<Button>("fellowship-portrait-" + name.Substring("companion-".Length));
                }
            }
            CardNetworkTests.Click(button);
        }
        // UI-269/UI-270: the Council's setup choices on the Fellowship panel's portraits (a portrait, its crown).
        internal static Button FellowshipControl(MountedMatchClient client, string name)
        {
            if (!name.StartsWith("fellowship-portrait-") && !name.StartsWith("starting-guide-"))
                return null;
            client.Board.Fellowship.Open();
            return client.Board.Fellowship.Root.Q<Button>(name);
        }

        // UI-245/UI-246/UI-249: a card's choice made on its object — a battle die, portrait, figure stack, Leadership
        // or hit pip, a Companion on the Hunt panel's roster, or There Is Another Way's track and Ring.
        internal static VisualElement ObjectChoice(MountedMatchClient client, string name)
        {
            if (!name.StartsWith("effect-choice-"))
                return null;
            var id = name.Substring("effect-choice-".Length);
            var view = client.Board.View;
            var tray = client.Board.Tray;
            // UI-270: an alternate companion's Guide response is a response object in the Hunt's response row.
            if (view.Hunt != null && client.Board.Hunt.Root.Q<Button>(name) is { } response)
            {
                client.Board.Hunt.Open();
                return response;
            }
            if (view.Battle != null)
            {
                // UI-246: a Leadership point is clicked on the figure that has it (its choice's source).
                var source = view.Options.FirstOrDefault(o => o.Kind == CommandKind.ChooseEffect)?.EffectChoices
                    .FirstOrDefault(c => c.Id == id && id.Contains(":leadership:"))?.SourceId;
                if (!string.IsNullOrEmpty(source))
                    id = source;
                var unit = view.Units.FirstOrDefault(u => u.Id == id);
                var hits = id.Contains(".hits.") ? id.Substring(id.LastIndexOf('.') + 1) : "";
                return tray.Q<Button>("battle-die-" + id) ??
                    (hits.Length > 0 ? tray.Q<Button>("battle-hit-cancel-" + hits) : null) ?? tray.Q("portrait-" + id) ??
                    (unit == null ? null : tray.Q("battle-figure-" + unit.Nation + "-" + unit.Kind));
            }
            if (FellowshipActions.RosterChoice(view, id))
            {
                client.Board.Hunt.Open();
                return client.Board.Hunt.Guides.Root.Q<Button>("fellowship-portrait-" + id);
            }
            if (id == FellowshipCardActions.Move)
            {
                client.Board.Fellowship.Open();
                return client.Board.Fellowship.Root.Q<Button>("fellowship-progress");
            }
            if (id == FellowshipCardActions.Hide)
                return client.Board.Fellowship.IsOpen ? client.Board.Fellowship.PanelRing : client.Board.Fellowship.Ring;
            return null;
        }

        // A choice by its tray name, wherever it is made now: the tray, the console or its object.
        internal static VisualElement Choice(MountedMatchClient client, string name) =>
            (VisualElement)TrayOrConsole(client, name) ?? ObjectChoice(client, name);

        // UI-214: a card's object-less options and a reaction's responses are buttons in the console, named as in the tray.
        internal static Button TrayOrConsole(MountedMatchClient client, string name) =>
            client.Board.Tray.Q<Button>(name) ?? client.Board.Console.Status.Options.Q<Button>(name);

        // UI-215: a card's figure choice (and excess outside a battle) is made in the open region panel.
        internal static Button PanelFigure(MountedMatchClient client, string name)
        {
            var id = name.StartsWith("figure-") ? name.Substring("figure-".Length)
                : name.StartsWith("effect-choice-") ? name.Substring("effect-choice-".Length) : "";
            var panel = client.Board.Hover.Panel.Root;
            return id.Length == 0 || panel.resolvedStyle.display == DisplayStyle.None ? null : panel.Q<Button>("hover-figure-" + id);
        }

        // UI-215: a click on the figure's region opens its panel, where the figure is clicked.
        public async Task ChooseFigure(MountedMatchClient client, string id)
        {
            var view = client.Board.View;
            var name = (view.Options.Any(o => o.Kind == CommandKind.RemoveExcess) ? "figure-" : "effect-choice-") + id;
            if (TrayOrConsole(client, name) == null && PanelFigure(client, name) == null)
            {
                var location = view.Units.FirstOrDefault(u => u.Id == id)?.Location ?? view.Characters.Single(c => c.Id == id).Location;
                client.Board.Map.Select(location);
                await Settle();
            }
            Click(client, name);
            await Settle();
        }

        // UI-214: the console holds exactly these option buttons and the decision window stays closed.
        internal static void AssertConsoleOptions(MountedMatchClient client, params string[] names)
        {
            CollectionAssert.AreEquivalent(names, client.Board.Console.Status.Options.Query<Button>().ToList().Select(b => b.name));
            Assert.IsFalse(client.Board.DecisionWindow.IsOpen, "A card's options in the console open no window.");
        }

        // UI-131: card and ability recruitment left the tray for the Reinforcements panel. A ReserveFigures choice
        // named by its figure is the planned chip of that figure, or else the token of its nation and kind.
        internal static Button Reinforcement(MountedMatchClient client, string name)
        {
            var panel = client.Board.Reinforcements.Root;
            var button = panel.Q<Button>(name);
            if (button != null || !name.StartsWith("effect-choice-") || client.Board.View.Decision?.Presentation != "ReserveFigures")
                return button;
            var id = name.Substring("effect-choice-".Length);
            var planned = panel.Query<Button>(className: "planned-reinforcement").ToList().FirstOrDefault(b => b.userData as string == id);
            var unit = client.Board.View.Units.FirstOrDefault(u => u.Id == id);
            return planned ?? (unit == null ? null : panel.Q<Button>("effect-reserve-" + unit.Nation + "-" + unit.Kind));
        }

        public Task Settle() => Task.Delay(80, Token);
        public async Task Until(Func<bool> condition)
        {
            while (!condition())
                await Task.Delay(30, Token);
        }
        public async Task At(long revision)
        {
            await Until(() => Fp.Board.View?.Revision == revision && Sh.Board.View?.Revision == revision);
            await Settle();
            await OpenBattles();
        }
        private async Task OpenBattles()
        {
            if (!openPanels) return;
            foreach (var client in new[] { Fp, Sh })
            {
                if ((client.Board.View.Battle != null || client.Board.View.EventLoss != null) && !client.Board.Battles.IsOpen)
                {
                    CardNetworkTests.Click(client.Board.Console.Status.Plaque);
                    await Settle();
                }
                if (client.Board.Tray.parent == client.Board.Fellowship.Root && !client.Board.Fellowship.IsOpen)
                {
                    await ComponentGestures.Hover(client.Board.Edges.Corner);
                    await Settle();
                }
                // UI-162: a Hunt window the test closed comes back from the console's plaque.
                if (client.Board.Tray.parent == client.Board.Hunt.DecisionHost && !client.Board.Hunt.IsOpen)
                {
                    CardNetworkTests.Click(client.Board.Hunt.Tab);
                    await Settle();
                }
            }
        }
        public async Task Confirm(MountedMatchClient client)
        {
            if (submitted.TryGetValue(client, out var choice))
            {
                submitted.Remove(client);
                await At(choice.Revision + 1);
                return;
            }
            var revision = client.Board.View.Revision;
            // UI-172: the Shadow's own Hunt allocation is submitted with the Hunt plaque.
            if (T22NetworkActions.OwnAllocation(client))
            {
                await Until(() => T22NetworkActions.PlaqueReady(client));
                await Settle();
                CardNetworkTests.Click(client.Board.Hunt.Tab);
                await At(revision + 1);
                return;
            }
            // Complete routes with their actual endpoint gesture. Grouped event journeys
            // keep their shared confirmation; ordinary journeys are server-saved steps.
            var overlay = client.Board.Map.DraftOverlay;
            var endpoint = overlay.Targets.SingleOrDefault(overlay.IsRouteEnd);
            if (client.Board.Tray.Q<Button>("confirm-decision") == null && endpoint != null)
            {
                await Settle();
                FellowshipGestures.FinishRoute(client, endpoint);
                await At(revision + 1);
                return;
            }
            if (client.Board.Tray.Q<Button>("confirm-decision") == null && client.Board.View.Options.Any(o =>
                o.Kind == CommandKind.ChooseEffect && o.Minimum == 1 && o.Maximum == 1))
            {
                await At(revision + 1);
                return;
            }
            // UI-160: a card or ability recruitment plan keeps the decision window closed and is confirmed with the
            // Reinforcements panel's own ✓.
            await Settle();
            var board = client.Board;
            var planned = board.Reinforcements.Root.Q<Button>("reinforcements-confirm");
            if (board.Tray.parent == board.DecisionWindow.Dock && !board.DecisionWindow.IsOpen &&
                board.Tray.Q<Button>("confirm-decision") != null && planned?.style.display.value == DisplayStyle.Flex)
            {
                if (board.Reinforcements.Root.style.display.value == DisplayStyle.None)
                    CardNetworkTests.Click(board.Console.Status.Plaque);
                await Until(() => planned.enabledInHierarchy && planned.worldBound.width > 0);
                await Settle();
                CardNetworkTests.Click(planned);
                await At(revision + 1);
                return;
            }
            // UI-211: a card's decision whose choices are on the board is confirmed with the console's ✓.
            var console = board.Console.Actions.Confirm;
            if (board.Tray.parent == board.DecisionWindow.Dock && !board.DecisionWindow.IsOpen && board.Tray.Q<Button>("confirm-decision") != null)
            {
                await Until(() => console.enabledSelf);
                await Settle();
                CardNetworkTests.Click(console);
                await At(revision + 1);
                return;
            }
            await ButtonReady(client, "confirm-decision");
            Click(client, "confirm-decision");
            await At(revision + 1);
        }
        // UI-105: a click on a highlighted destination moves the selected characters at once.
        public async Task MoveTo(MountedMatchClient client, string region)
        {
            Assert.IsTrue(client.Board.Map.DraftOverlay.Targets.Contains(region), region + " must be a highlighted destination.");
            var revision = client.Board.View.Revision;
            await Settle();
            FellowshipGestures.FinishRoute(client, region);
            await At(revision + 1);
        }

        // UI-151: without a legal Combat card the client answers No card itself; with one the player clicks it.
        public async Task NoCard(MountedMatchClient client)
        {
            var view = client.Board.View;
            var option = view.Options.FirstOrDefault(o => o.Kind == CommandKind.CommitCombatCard);
            if (view.Decision?.Kind == DecisionKind.CombatCard && view.DecisionOwner == view.You && option?.EligibleIds.Count > 0)
            {
                await Direct(client, "battle-no-card");
                return;
            }
            await Until(() => client.Board.View.Decision?.Kind != DecisionKind.CombatCard &&
                Fp.Board.View?.Revision == Sh.Board.View?.Revision);
            await Settle();
            await OpenBattles();
        }
        public async Task Direct(MountedMatchClient client, string button)
        {
            var revision = client.Board.View.Revision;
            await ButtonReady(client, button);
            Click(client, button);
            await At(revision + 1);
        }
        private async Task ButtonReady(MountedMatchClient client, string name)
        {
            await Until(() =>
            {
                var button = TrayOrConsole(client, name);
                return button?.enabledInHierarchy == true && button.worldBound.width > 0 && button.worldBound.height > 0;
            });
            await Settle();
        }
        public async Task Attack(MountedMatchClient client, string from, string to)
        {
            if (client.Board.View.Decision.Kind == DecisionKind.Action)
            {
                ArmyGestures.Die(client, ArmyGestures.ArmyDie(client));
                var force = client.Board.View.Options.First(o => o.Kind == CommandKind.AttackArmy).Armies.First(a => a.RegionId == from);
                await ArmyGestures.All(client, from, force.Garrison);
                await ArmyGestures.Target(this, client, to);
                return;
            }
            Click(client, "action-AttackArmy");
            client.Board.Map.Select(from);
            await Settle();
            MapContourProbe.ClickRegion(client.Board.Map, to);
            await Confirm(client);
        }
        public async Task SelectAllAdvancing(MountedMatchClient client)
        {
            // UI-146: one click on the toggle selects every figure that can advance; a second click clears them.
            var toggle = client.Board.Root.Q<Button>("battle-select-all");
            Assert.IsNotNull(toggle, "The advance offers a select-all toggle.");
            Click(client, "battle-select-all");
            Assert.IsTrue(toggle.ClassListContains("all-selected"));
            Click(client, "battle-select-all");
            Assert.IsFalse(toggle.ClassListContains("all-selected"));
            Click(client, "battle-select-all");
            Assert.IsTrue(toggle.ClassListContains("all-selected"));
            // UI-105: figures move by clicks only; a drag of an advancing figure is no longer a shortcut.
            var destination = client.Board.View.Battle.RegionId;
            await ArmyGestures.Target(this, client, destination);
        }
        public async Task RemoveExcess(MountedMatchClient client)
        {
            var option = client.Board.View.Options.Single(o => o.Kind == CommandKind.RemoveExcess);
            foreach (var id in option.EligibleIds.Take(option.Minimum))
                await ChooseFigure(client, id);
            await Confirm(client);
        }
        public static IEnumerator Wait(Task task)
        {
            var deadline = Time.realtimeSinceStartup + 170;
            while (!task.IsCompleted && Time.realtimeSinceStartup < deadline)
                yield return null;
            Assert.IsTrue(task.IsCompleted, "Match integration timed out.");
            task.GetAwaiter().GetResult();
        }
        public void Dispose()
        {
            lifetime.Cancel();
            Fp?.Dispose();
            Sh?.Dispose();
            foreach (var api in privateSessions) api.Dispose();
            lifetime.Dispose();
        }
        private sealed class Fixture
        {
            public string Address, FreePeoplesUser, ShadowUser, Password;
            public Dictionary<string, string> BattleMatches;
            public Dictionary<string, string> CharacterMatches;
            public Dictionary<string, string> CardPackMatches;
            public Dictionary<string, string> BaseAcceptanceMatches;
            public Dictionary<string, string> InteractionMatches;
        }
    }
}
