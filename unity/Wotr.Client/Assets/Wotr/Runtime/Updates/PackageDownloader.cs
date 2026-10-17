using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wotr.Contracts;

namespace Wotr.Client.Updates
{
    public readonly struct DownloadProgress
    {
        public DownloadProgress(long received, long total)
        {
            Received = received;
            Total = total;
        }

        public long Received { get; }
        public long Total { get; }
    }

    // D13: the game downloads and checks the package itself; Velopack only installs the checked file. A separate
    // HttpClient carries no session token or cookies and follows no redirect. A download resumes with Range and
    // If-Range from <folder>/<version>/<file>.partial, so a broken connection, Cancel or a restart lose nothing; the
    // finished file must have the manifest's size and SHA-256. A partial file of another release is discarded:
    // consent for one release never carries over to another.
    public sealed class PackageDownloader : IDisposable
    {
        public const int SmallFileLimit = 64 * 1024;
        private const int BufferSize = 81920;
        // Velopack copies the package once more into its own folder before applying it.
        private const long SpaceMargin = 64L * 1024 * 1024;
        private static readonly TimeSpan StallTimeout = TimeSpan.FromSeconds(60);

        private readonly HttpClient http;
        private readonly string folder;
        private readonly Func<string, long> freeSpace;

        private sealed class PartialState
        {
            public string Url { get; set; } = "";
            public long Size { get; set; }
            public string Sha256 { get; set; } = "";
            public string Validator { get; set; } = "";
        }

        public PackageDownloader(string folder, string userAgent, HttpMessageHandler handler = null, Func<string, long> freeSpace = null)
        {
            this.folder = folder;
            this.freeSpace = freeSpace ?? FreeSpace;
            http = new HttpClient(handler ?? new HttpClientHandler { AllowAutoRedirect = false, UseCookies = false })
            {
                Timeout = Timeout.InfiniteTimeSpan
            };
            http.DefaultRequestHeaders.UserAgent.ParseAdd(userAgent);
        }

        public static string FinalPath(string folder, string version, ClientReleaseFile file) =>
            Path.Combine(folder, version, file.FileName);

        // Manifests and signatures: small files from the approved origin only.
        public async Task<byte[]> FetchSmall(string url, CancellationToken cancellation)
        {
            using (var response = await Send(new HttpRequestMessage(HttpMethod.Get, url), cancellation))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new UpdateRejected($"{url} answered {(int)response.StatusCode}.");
                var body = await response.Content.ReadAsByteArrayAsync();
                if (body.Length > SmallFileLimit)
                    throw new UpdateRejected($"{url} is larger than a release manifest.");
                return body;
            }
        }

        public async Task<string> Download(string version, ClientReleaseFile file, IProgress<DownloadProgress> progress,
            CancellationToken cancellation)
        {
            var target = FinalPath(folder, version, file);
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            if (File.Exists(target) && await HashAsync(target) == file.Sha256)
                return target;
            File.Delete(target);
            var partial = target + ".partial";
            var state = ReadState(partial, file);
            var existing = File.Exists(partial) ? new FileInfo(partial).Length : 0;
            RequireSpace(target, file.Size - existing + file.Size + SpaceMargin);
            await Transfer(file, partial, state, existing, progress, cancellation);
            if (await HashAsync(partial) != file.Sha256)
            {
                Discard(partial);
                throw new UpdateRejected("The downloaded package does not match the SHA-256 of its signed manifest.");
            }
            File.Move(partial, target);
            File.Delete(StatePath(partial));
            return target;
        }

        public void Dispose() => http.Dispose();

        private async Task Transfer(ClientReleaseFile file, string partial, PartialState state, long existing,
            IProgress<DownloadProgress> progress, CancellationToken cancellation)
        {
            if (existing == file.Size)
                return;
            var request = new HttpRequestMessage(HttpMethod.Get, file.Url);
            if (existing > 0)
            {
                request.Headers.Range = new RangeHeaderValue(existing, null);
                if (state.Validator.Length > 0)
                    request.Headers.TryAddWithoutValidation("If-Range", state.Validator);
            }
            using (var response = await Send(request, cancellation))
            {
                var resumed = response.StatusCode == HttpStatusCode.PartialContent;
                if (!resumed && response.StatusCode != HttpStatusCode.OK)
                    throw new UpdateRejected($"The package download answered {(int)response.StatusCode}.");
                var start = resumed ? existing : 0;
                CheckLength(response, file.Size, start, resumed);
                state.Validator = Validator(response);
                WriteState(partial, state);
                using (var source = await response.Content.ReadAsStreamAsync())
                using (var output = new FileStream(partial, resumed ? FileMode.Append : FileMode.Create, FileAccess.Write))
                    await Copy(source, output, start, file.Size, progress, cancellation);
            }
        }

        private async Task<HttpResponseMessage> Send(HttpRequestMessage request, CancellationToken cancellation)
        {
            HttpResponseMessage response;
            try
            {
                response = await http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellation);
            }
            catch (HttpRequestException error)
            {
                throw new UpdateRejected("The download host is not reachable: " + error.Message);
            }
            if ((int)response.StatusCode >= 300 && (int)response.StatusCode < 400)
            {
                response.Dispose();
                throw new UpdateRejected("The download host redirected the request; updates come only from the approved origin.");
            }
            return response;
        }

        private static void CheckLength(HttpResponseMessage response, long size, long start, bool resumed)
        {
            var headers = response.Content.Headers;
            if (resumed && (headers.ContentRange?.From != start || headers.ContentRange?.Length != size))
                throw new UpdateRejected("The download host resumed at another position or size.");
            if (headers.ContentLength.HasValue && headers.ContentLength.Value != size - start)
                throw new UpdateRejected("The download host announces another package size than the signed manifest.");
        }

        // A stalled connection fails after a minute without bytes instead of hanging the download.
        private static async Task Copy(Stream source, Stream output, long received, long size,
            IProgress<DownloadProgress> progress, CancellationToken cancellation)
        {
            var buffer = new byte[BufferSize];
            while (true)
            {
                int read;
                using (var stall = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
                {
                    stall.CancelAfter(StallTimeout);
                    try
                    {
                        read = await source.ReadAsync(buffer, 0, buffer.Length, stall.Token);
                    }
                    catch (OperationCanceledException) when (!cancellation.IsCancellationRequested)
                    {
                        throw new UpdateRejected("The download stalled; it resumes on the next try.");
                    }
                    catch (IOException)
                    {
                        throw new UpdateRejected("The download broke off; it resumes on the next try.");
                    }
                }
                if (read == 0)
                    break;
                received += read;
                if (received > size)
                    throw new UpdateRejected("The download host sent more bytes than the signed manifest names.");
                await output.WriteAsync(buffer, 0, read, cancellation);
                progress?.Report(new DownloadProgress(received, size));
            }
            if (received != size)
                throw new UpdateRejected("The download ended early; it resumes on the next try.");
        }

        private static string Validator(HttpResponseMessage response) =>
            response.Headers.ETag?.ToString() ?? response.Content.Headers.LastModified?.ToString("R") ?? "";

        private PartialState ReadState(string partial, ClientReleaseFile file)
        {
            var path = StatePath(partial);
            PartialState state = null;
            try
            {
                if (File.Exists(path))
                    state = JsonConvert.DeserializeObject<PartialState>(File.ReadAllText(path));
            }
            catch (JsonException)
            {
            }
            if (state == null || state.Url != file.Url || state.Size != file.Size || state.Sha256 != file.Sha256 ||
                (File.Exists(partial) && new FileInfo(partial).Length > file.Size))
            {
                Discard(partial);
                state = new PartialState { Url = file.Url, Size = file.Size, Sha256 = file.Sha256 };
            }
            return state;
        }

        private static void WriteState(string partial, PartialState state) =>
            File.WriteAllText(StatePath(partial), JsonConvert.SerializeObject(state));

        private static string StatePath(string partial) => partial + ".json";

        private static void Discard(string partial)
        {
            File.Delete(partial);
            File.Delete(StatePath(partial));
        }

        private void RequireSpace(string target, long needed)
        {
            var available = freeSpace(target);
            if (available >= 0 && available < needed)
                throw new UpdateRejected($"Not enough disk space: the update needs {needed / 1048576 + 1} MB free.");
        }

        private static long FreeSpace(string path)
        {
            try
            {
                return new DriveInfo(Path.GetPathRoot(Path.GetFullPath(path))).AvailableFreeSpace;
            }
            catch (Exception error) when (error is ArgumentException || error is IOException)
            {
                return -1;
            }
        }

        // Hashing a package takes about a second; off the main thread the interface keeps drawing.
        public static Task<string> HashAsync(string path) => Task.Run(() => Hash(path));

        public static string Hash(string path)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(path))
                return BitConverter.ToString(sha.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
        }
    }
}
