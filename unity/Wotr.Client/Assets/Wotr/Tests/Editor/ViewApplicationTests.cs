using System;
using System.Collections.Generic;
using NUnit.Framework;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // QUAL-09 (review R09): a view counts as applied only when every observer applied it; a drawing failure is not a
    // network status and does not move the version, so the same view is applied again (at most ApplyAttempts times).
    public sealed class ViewApplicationTests
    {
        [Test]
        public void AFailingObserverKeepsTheVersionAndTheSameViewIsAppliedAgain()
        {
            var (connection, log, statuses) = Connection();
            var failing = true;
            var seen = new List<long>();
            connection.Updated += view =>
            {
                if (failing)
                    throw new InvalidOperationException("drawing failed");
            };
            connection.Updated += view => seen.Add(view.Revision);
            var view4 = View(4);
            view4.Hand.Add("secret-card");
            Assert.IsFalse(connection.Apply(view4));
            Assert.AreEqual(-1, connection.Revision, "A failed application does not move the version.");
            CollectionAssert.AreEqual(new long[] { 4 }, seen, "The other observer still received the view.");
            CollectionAssert.AreEqual(new[] { MatchConnection.DisplayError }, statuses);
            Assert.AreEqual(1, log.Count);
            StringAssert.StartsWith("[wotr] presentation match=match revision=4 error=InvalidOperationException", log[0]);
            StringAssert.DoesNotContain("secret-card", log[0]);
            StringAssert.DoesNotContain("drawing failed", log[0]);
            failing = false;
            Assert.IsTrue(connection.Apply(view4));
            Assert.AreEqual(4, connection.Revision);
            CollectionAssert.AreEqual(new long[] { 4, 4 }, seen);
            CollectionAssert.AreEqual(new[] { MatchConnection.DisplayError, "Connected" }, statuses);
            Assert.IsFalse(connection.Apply(View(4)), "An applied view is not applied twice.");
            Assert.IsFalse(connection.Apply(View(3)), "An older view never replaces a newer one.");
        }

        [Test]
        public void AViewThatKeepsFailingIsTriedAFewTimesUntilANewerViewOrAReconnect()
        {
            var (connection, _, _) = Connection();
            var attempts = 0;
            connection.Updated += view =>
            {
                attempts++;
                throw new InvalidOperationException();
            };
            for (var i = 0; i < 5; i++)
                connection.Apply(View(7));
            Assert.AreEqual(MatchConnection.ApplyAttempts, attempts, "A drawing failure does not become a loop.");
            connection.Apply(View(8));
            Assert.AreEqual(MatchConnection.ApplyAttempts + 1, attempts, "A newer view is tried.");
            connection.Reconnect();
            connection.Apply(View(8));
            Assert.AreEqual(MatchConnection.ApplyAttempts + 2, attempts, "A reconnect gives a fresh attempt.");
        }

        [Test]
        public void AFreshPrivateVersionOfTheSameRevisionIsAppliedAndJudgingDoesNotRecord()
        {
            var version = new ViewVersion("match");
            Assert.IsTrue(version.IsNewer(View(4)));
            Assert.IsTrue(version.IsNewer(View(4)), "Judging a view records nothing.");
            version.Commit(View(4));
            Assert.IsFalse(version.IsNewer(View(4)));
            var preference = View(4);
            preference.AutoDecision.Version = 1;
            Assert.IsTrue(version.IsNewer(preference));
            Assert.AreEqual("4/1/0", ViewVersion.Key(preference));
        }

        [Test]
        public void TransportFailuresNameTheirStatusAndCodeWithoutSecrets()
        {
            var line = ClientDiagnostics.Line(DiagnosticCategory.Transport, "match", 9,
                new ApiException(503, "ServerBusy", TimeSpan.FromSeconds(2)));
            Assert.AreEqual("[wotr] transport match=match revision=9 error=ApiException status=503 code=ServerBusy", line);
        }

        private static (MatchConnection Connection, List<string> Log, List<string> Statuses) Connection()
        {
            var log = new List<string>();
            var statuses = new List<string>();
            var connection = new MatchConnection(new GameApi("http://127.0.0.1:1"), "match")
            {
                Diagnostics = new ClientDiagnostics(log.Add)
            };
            connection.StatusChanged += statuses.Add;
            return (connection, log, statuses);
        }

        private static PlayerView View(long revision) => new PlayerView
        {
            MatchId = "match", Revision = revision, You = Side.FreePeoples, AutoDecision = new AutoDecisionView()
        };
    }
}
