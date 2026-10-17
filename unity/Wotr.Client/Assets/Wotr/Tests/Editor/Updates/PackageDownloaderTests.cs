using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Wotr.Client.Updates;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // DS15/DS17: the package comes only from its signed URL, without the session token or redirects, resumes with
    // Range and is accepted only with the manifest's size and SHA-256.
    public class PackageDownloaderTests
    {
        private string folder;
        private byte[] package;
        private ClientReleaseFile file;

        [SetUp]
        public void Create()
        {
            folder = Path.Combine(Path.GetTempPath(), "wotr-updates-" + Guid.NewGuid().ToString("N"));
            package = new byte[300000];
            new Random(7).NextBytes(package);
            file = TestReleases.Manifest(package: package).Package;
        }

        [TearDown]
        public void Remove()
        {
            if (Directory.Exists(folder))
                Directory.Delete(folder, true);
        }

        [Test]
        public async Task PackageIsDownloadedWithoutTheSessionAndChecked()
        {
            var host = new RangeHost(package);
            using (var downloader = new PackageDownloader(folder, "WotR test", host))
            {
                var path = await downloader.Download("0.9.1", file, null, CancellationToken.None);
                CollectionAssert.AreEqual(package, File.ReadAllBytes(path));
                Assert.AreEqual(Path.Combine(folder, "0.9.1", file.FileName), path);
            }
            Assert.AreEqual(1, host.Requests.Count);
            Assert.IsNull(host.Requests[0].Headers.Authorization);
            Assert.IsFalse(host.Requests[0].Headers.Contains("Cookie"));
            Assert.AreEqual(package.Length, host.BytesSent);
        }

        [Test]
        public async Task BrokenTransferResumesWithRangeAndIfRange()
        {
            var host = new RangeHost(package) { BreakAfter = 120000 };
            using (var downloader = new PackageDownloader(folder, "WotR test", host))
            {
                await Expect.Throws<UpdateRejected>(() => downloader.Download("0.9.1", file, null, CancellationToken.None));
                host.BreakAfter = -1;
                var path = await downloader.Download("0.9.1", file, null, CancellationToken.None);
                CollectionAssert.AreEqual(package, File.ReadAllBytes(path));
            }
            var resumed = host.Requests[1];
            Assert.AreEqual(120000, resumed.Headers.Range.Ranges.Single().From);
            Assert.AreEqual(RangeHost.ETag, resumed.Headers.GetValues("If-Range").Single());
            Assert.AreEqual(package.Length, host.BytesSent);
        }

        [Test]
        public async Task CancelKeepsThePartialFileForResume()
        {
            var host = new RangeHost(package) { BreakAfter = 50000, CancelInsteadOfBreak = true };
            using (var cancel = new CancellationTokenSource())
            using (var downloader = new PackageDownloader(folder, "WotR test", host))
            {
                host.Cancel = cancel;
                await Expect.Throws<OperationCanceledException>(() => downloader.Download("0.9.1", file, null, cancel.Token));
                host.BreakAfter = -1;
                await downloader.Download("0.9.1", file, null, CancellationToken.None);
            }
            Assert.AreEqual(2, host.Requests.Count);
            Assert.IsNotNull(host.Requests[1].Headers.Range);
        }

        [Test]
        public async Task FinishedPackageIsNotDownloadedAgain()
        {
            var host = new RangeHost(package);
            using (var downloader = new PackageDownloader(folder, "WotR test", host))
            {
                await downloader.Download("0.9.1", file, null, CancellationToken.None);
                await downloader.Download("0.9.1", file, null, CancellationToken.None);
            }
            Assert.AreEqual(1, host.Requests.Count);
        }

        [Test]
        public async Task RedirectIsRefused()
        {
            var host = new RangeHost(package) { Redirect = true };
            using (var downloader = new PackageDownloader(folder, "WotR test", host))
                await Expect.Throws<UpdateRejected>(() => downloader.Download("0.9.1", file, null, CancellationToken.None));
        }

        [Test]
        public async Task WrongContentIsRefusedAndDiscarded()
        {
            var other = (byte[])package.Clone();
            other[10] ^= 1;
            var host = new RangeHost(other);
            using (var downloader = new PackageDownloader(folder, "WotR test", host))
                await Expect.Throws<UpdateRejected>(() => downloader.Download("0.9.1", file, null, CancellationToken.None));
            Assert.IsEmpty(Directory.GetFiles(Path.Combine(folder, "0.9.1")));
        }

        [Test]
        public async Task HostAnnouncingAnotherSizeIsRefused()
        {
            var host = new RangeHost(package.Take(1000).ToArray());
            using (var downloader = new PackageDownloader(folder, "WotR test", host))
                await Expect.Throws<UpdateRejected>(() => downloader.Download("0.9.1", file, null, CancellationToken.None));
        }

        [Test]
        public async Task MissingDiskSpaceStopsBeforeAnyByte()
        {
            var host = new RangeHost(package);
            using (var downloader = new PackageDownloader(folder, "WotR test", host, _ => package.Length))
                await Expect.Throws<UpdateRejected>(() => downloader.Download("0.9.1", file, null, CancellationToken.None));
            Assert.AreEqual(0, host.Requests.Count);
        }

        [Test]
        public async Task PartialFileOfAnotherReleaseIsDiscarded()
        {
            var host = new RangeHost(package) { BreakAfter = 100000 };
            using (var downloader = new PackageDownloader(folder, "WotR test", host))
            {
                await Expect.Throws<UpdateRejected>(() => downloader.Download("0.9.1", file, null, CancellationToken.None));
                host.BreakAfter = -1;
                var changed = TestReleases.Manifest(package: package).Package;
                changed.Url += "?v2";
                await downloader.Download("0.9.1", changed, null, CancellationToken.None);
            }
            Assert.IsNull(host.Requests[1].Headers.Range);
        }

        [Test]
        public async Task ProgressReportsReceivedBytes()
        {
            var reports = new List<DownloadProgress>();
            using (var downloader = new PackageDownloader(folder, "WotR test", new RangeHost(package)))
                await downloader.Download("0.9.1", file, new SynchronousProgress(reports), CancellationToken.None);
            Assert.AreEqual(package.Length, reports.Last().Received);
            Assert.AreEqual(package.Length, reports.Last().Total);
        }

        private sealed class SynchronousProgress : IProgress<DownloadProgress>
        {
            private readonly List<DownloadProgress> reports;

            public SynchronousProgress(List<DownloadProgress> reports) => this.reports = reports;

            public void Report(DownloadProgress value) => reports.Add(value);
        }

        // nginx on the downloads host: Range with If-Range on a strong ETag, 206 with Content-Range.
        internal sealed class RangeHost : HttpMessageHandler
        {
            public const string ETag = "\"6526e1-49e40\"";
            private readonly byte[] content;

            public RangeHost(byte[] content) => this.content = content;

            public List<HttpRequestMessage> Requests { get; } = new List<HttpRequestMessage>();
            public long BytesSent { get; private set; }
            public int BreakAfter { get; set; } = -1;
            public bool CancelInsteadOfBreak { get; set; }
            public CancellationTokenSource Cancel { get; set; }
            public bool Redirect { get; set; }

            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                Requests.Add(request);
                if (Redirect)
                {
                    var moved = new HttpResponseMessage(HttpStatusCode.Found);
                    moved.Headers.Location = new Uri("https://elsewhere.example/package");
                    return Task.FromResult(moved);
                }
                var start = 0L;
                var ranged = request.Headers.Range != null &&
                    (!request.Headers.Contains("If-Range") || request.Headers.GetValues("If-Range").Single() == ETag);
                if (ranged)
                    start = request.Headers.Range.Ranges.Single().From ?? 0;
                var body = new SendingStream(this, content, start);
                var response = new HttpResponseMessage(ranged ? HttpStatusCode.PartialContent : HttpStatusCode.OK)
                {
                    Content = new StreamContent(body)
                };
                response.Headers.ETag = EntityTagHeaderValue.Parse(ETag);
                response.Content.Headers.ContentLength = content.Length - start;
                if (ranged)
                    response.Content.Headers.ContentRange = new ContentRangeHeaderValue(start, content.Length - 1, content.Length);
                return Task.FromResult(response);
            }

            private sealed class SendingStream : MemoryStream
            {
                private readonly RangeHost host;
                private readonly long start;

                public SendingStream(RangeHost host, byte[] content, long start) : base(content, (int)start, content.Length - (int)start)
                {
                    this.host = host;
                    this.start = start;
                }

                public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellation)
                {
                    if (host.BreakAfter >= 0 && start + Position >= host.BreakAfter)
                    {
                        if (!host.CancelInsteadOfBreak)
                            throw new IOException("Connection reset");
                        host.Cancel.Cancel();
                        cancellation.ThrowIfCancellationRequested();
                    }
                    var limit = host.BreakAfter >= 0 ? (int)Math.Min(count, host.BreakAfter - (start + Position)) : count;
                    var read = Read(buffer, offset, Math.Max(limit, 0));
                    host.BytesSent += read;
                    return Task.FromResult(read);
                }
            }
        }
    }
}
