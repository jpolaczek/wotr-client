using System;
using NUnit.Framework;
using Wotr.Client;

namespace Wotr.EditorTests
{
    // QUAL-22 (owner: the client sometimes hangs on Waiting for server): a correspondence game notices a silent socket
    // too. The server sends a notice at least every 5 s, so 15 s without one (three notices) ends the socket and the
    // connection starts again; a stuck attempt ends after 20 s. A ranked game keeps its tighter limits.
    public sealed class ConnectionPolicyTests
    {
        [Test]
        public void ACorrespondenceGameEndsASilentSocketAfterThreeMissedNotices()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(15), ConnectionPolicy.Standard.Silence);
            Assert.AreEqual(TimeSpan.FromSeconds(20), ConnectionPolicy.Standard.AttemptTimeout);
            Assert.AreEqual(15, ConnectionPolicy.Standard.LongestDelaySeconds);
        }

        [Test]
        public void ARankedGameKeepsItsTighterLimits()
        {
            Assert.AreEqual(TimeSpan.FromSeconds(8), ConnectionPolicy.Ranked.Silence);
            Assert.AreEqual(TimeSpan.FromSeconds(5), ConnectionPolicy.Ranked.AttemptTimeout);
            Assert.Less(ConnectionPolicy.Ranked.Silence.Value, ConnectionPolicy.Standard.Silence.Value);
        }

        [Test]
        public void TheListeningTokenEndsAfterTheSilence()
        {
            using var lifetime = new System.Threading.CancellationTokenSource();
            using var listening = ConnectionPolicy.Standard.Listening(lifetime.Token);
            Assert.IsFalse(listening.IsCancellationRequested, "It listens at first.");
            lifetime.Cancel();
            Assert.IsTrue(listening.IsCancellationRequested, "The connection's end ends it at once.");
        }
    }
}
