using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Wotr.PlayTests
{
    // Owns only its child server. Production never exposes a restart/fixture endpoint.
    internal sealed class RestartTestServer : IDisposable
    {
        private Process process;
        private readonly string repository;
        private readonly string server;
        public string Address { get; }

        public RestartTestServer()
        {
            repository = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_REPOSITORY") ??
                Path.GetDirectoryName(Path.GetDirectoryName(Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_CONFIG")));
            // The pinned test backend from scripts/test-client.sh; the client has no server sources.
            server = Environment.GetEnvironmentVariable("WOTR_CLIENT_TEST_SERVER") ??
                Path.Combine(repository, ".deps/test/wotr-server/Wotr.Server.dll");
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            Address = "http://127.0.0.1:" + ((IPEndPoint)listener.LocalEndpoint).Port;
            listener.Stop();
        }

        public async Task Start(CancellationToken token)
        {
            var start = new ProcessStartInfo
            {
                FileName = Executable(),
                Arguments = "\"" + server + "\" --urls " + Address,
                WorkingDirectory = repository,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };
            process = Process.Start(start);
            process.OutputDataReceived += (_, _) => { };
            process.ErrorDataReceived += (_, _) => { };
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(1) })
                while (true)
                {
                    if (process.HasExited)
                        throw new InvalidOperationException("Restart test server exited before becoming healthy.");
                    try
                    {
                        using (var response = await http.GetAsync(Address + "/health", token))
                            if (response.IsSuccessStatusCode)
                                return;
                    }
                    catch (HttpRequestException) { }
                    await Task.Delay(100, token);
                }
        }

        public void Stop()
        {
            if (process == null)
                return;
            if (!process.HasExited)
                process.Kill();
            process.WaitForExit(5000);
            process.Dispose();
            process = null;
        }

        public void Dispose() => Stop();

        private string Executable()
        {
            var name = Environment.OSVersion.Platform == PlatformID.Win32NT ? "dotnet.exe" : "dotnet";
            var local = Path.Combine(repository, ".artifacts", "dotnet", name);
            return File.Exists(local) ? local : name;
        }
    }
}
