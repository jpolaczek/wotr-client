using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NUnit.Framework;
using Wotr.Client;
using Wotr.Contracts;

namespace Wotr.PlayTests
{
    internal sealed class PveTestFixture
    {
        public string Address, FreePeoplesUser, ShadowUser, Password;
        public Dictionary<string, string> PveMatches;
        public static PveTestFixture Load()
        {
            var file = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG");
            if (string.IsNullOrEmpty(file) || Environment.GetEnvironmentVariable("WOTR_CLIENT_PVE_FIXTURE") != "1")
                Assert.Ignore("Run WOTR_CLIENT_PVE_FIXTURE=1 scripts/test-client.sh with the public embedded ABI fixture.");
            return JsonConvert.DeserializeObject<PveTestFixture>(File.ReadAllText(file));
        }
        public async Task<GameApi> Human(CancellationToken token, System.Net.Http.HttpMessageHandler handler = null)
        {
            var api = new GameApi(Address, handler);
            await TestAccounts.Register(api, "pve_" + Guid.NewGuid().ToString("N").Substring(0, 16), token);
            return api;
        }
        public async Task<GameApi> Login(Side human, CancellationToken token, string address = null)
        {
            var api = new GameApi(address ?? Address);
            await api.Login(human == Side.FreePeoples ? FreePeoplesUser : ShadowUser, Password, token);
            return api;
        }
        public static string Path(RoomView room) => "api/rooms/" + room.Id;
        public static AddRoomBotRequest Request(RoomView room) => new AddRoomBotRequest
            { ExpectedVersion = room.Version, RequestId = Guid.NewGuid().ToString("N") };
        public static async Task<RoomView> CpuReady(GameApi api, RoomView room, CancellationToken token)
        {
            while (room.Bot?.Status != BotSeatStatus.Ready)
            {
                await Task.Delay(50, token);
                room = await api.Get<RoomView>(Path(room), token);
            }
            return room;
        }
        public static async Task<RoomView> Create(GameApi api, Side human, CancellationToken token)
        {
            var room = await api.Post<RoomView>("api/rooms", new CreateRoomRequest { Name = "CPU test", Side = human }, token);
            return await api.Put<RoomView>(Path(room) + "/bot", Request(room), token);
        }
        public static async Task Release(string match, CancellationToken token)
        {
            var repository = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_REPOSITORY");
            var local = System.IO.Path.Combine(repository, ".artifacts/dotnet/dotnet");
            var seeder = System.IO.Path.Combine(repository, ".deps/test/wotr-seeder/Wotr.Scenarios.dll");
            var start = new ProcessStartInfo(File.Exists(local) ? local : "dotnet", "\"" + seeder + "\" --pve-release " + match)
            {
                WorkingDirectory = repository, UseShellExecute = false, RedirectStandardOutput = true,
                RedirectStandardError = true, CreateNoWindow = true
            };
            // The normal harness may reset its disposable store before a batch. This command only releases one fixture.
            start.EnvironmentVariables.Remove("WOTR_CLIENT_TEST_RESET_DATABASE");
            using var process = Process.Start(start);
            var output = process.StandardOutput.ReadToEndAsync();
            var errors = process.StandardError.ReadToEndAsync();
            try { while (!process.HasExited) await Task.Delay(20, token); }
            finally { if (!process.HasExited) process.Kill(); }
            Assert.AreEqual(0, process.ExitCode, await output + await errors);
        }
    }
}
