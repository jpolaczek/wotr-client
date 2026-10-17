using System;
using NUnit.Framework;
using Wotr.Client.Updates;
using Wotr.Contracts;

namespace Wotr.Client.Tests
{
    // D13 modes: RequiredNow > RequiredSoon > Optional; ServerBehind downloads nothing; Repair needs the installed offer.
    public class UpdatePolicyTests
    {
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 10, 20, 18, 0, 0, TimeSpan.Zero);

        private static ClientReleaseOffer Offer(string version) => new ClientReleaseOffer
        {
            Version = version,
            ProtocolVersion = 2,
            ManifestUrl = TestReleases.Origin + $"releases/{version}/macos-universal/manifest.json"
        };

        private static ClientReleaseView View(string latest = null, string minimum = "", string scheduled = null,
            ClientCompatibilityStatus status = ClientCompatibilityStatus.Compatible, string installed = "0.9.0") => new ClientReleaseView
        {
            Platform = ClientPlatforms.MacOS,
            Latest = latest == null ? null : Offer(latest),
            MinimumVersion = minimum,
            Status = status,
            ServerTime = Now,
            ScheduledMinimum = scheduled == null ? null : new ClientScheduledMinimum { Version = scheduled, EffectiveAt = Now.AddHours(2) },
            Installed = installed == null ? null : Offer(installed)
        };

        [Test]
        public void SameOrOlderLatestNeedsNothing()
        {
            Assert.AreEqual(UpdateMode.None, UpdatePolicy.Plan("0.9.0", View("0.9.0"), false).Mode);
            Assert.AreEqual(UpdateMode.None, UpdatePolicy.Plan("0.9.0", View("0.8.0"), false).Mode);
            Assert.AreEqual(UpdateMode.None, UpdatePolicy.Plan("0.9.0", View(), false).Mode);
        }

        [Test]
        public void NewerReleaseIsOptionalUntilAMinimumNamesIt()
        {
            var plan = UpdatePolicy.Plan("0.9.0", View("0.9.1"), false);
            Assert.AreEqual(UpdateMode.Optional, plan.Mode);
            Assert.AreEqual("0.9.1", plan.Offer.Version);
            Assert.AreEqual(UpdateMode.RequiredSoon, UpdatePolicy.Plan("0.9.0", View("0.9.1", scheduled: "0.9.1"), false).Mode);
            Assert.AreEqual(UpdateMode.RequiredNow, UpdatePolicy.Plan("0.9.0", View("0.9.1", minimum: "0.9.1"), false).Mode);
        }

        [Test]
        public void ScheduledMinimumCarriesItsDeadline()
        {
            var plan = UpdatePolicy.Plan("0.9.0", View("0.9.2", scheduled: "0.9.1"), false);
            Assert.AreEqual(UpdateMode.RequiredSoon, plan.Mode);
            Assert.AreEqual(Now.AddHours(2), plan.Deadline);
            Assert.AreEqual("0.9.2", plan.Offer.Version);
            Assert.AreEqual(UpdateMode.Optional, UpdatePolicy.Plan("0.9.1", View("0.9.2", scheduled: "0.9.1"), false).Mode);
        }

        [Test]
        public void ServerRefusalOutranksEverythingButAnOlderServer()
        {
            Assert.AreEqual(UpdateMode.RequiredNow, UpdatePolicy.Plan("0.9.0", View("0.9.1", scheduled: "0.9.1"), true).Mode);
            Assert.AreEqual(UpdateMode.RequiredNow,
                UpdatePolicy.Plan("0.9.0", View("0.9.1", status: ClientCompatibilityStatus.ClientUpdateRequired), false).Mode);
            var behind = UpdatePolicy.Plan("0.9.0", View("0.9.1", status: ClientCompatibilityStatus.ServerUpdateRequired), false);
            Assert.AreEqual(UpdateMode.ServerBehind, behind.Mode);
            Assert.IsNull(behind.Offer);
        }

        [Test]
        public void RequiredWithoutAPublishedReleaseHasNoOffer()
        {
            var plan = UpdatePolicy.Plan("0.9.0", View("0.9.0", minimum: "0.9.1"), false);
            Assert.AreEqual(UpdateMode.RequiredNow, plan.Mode);
            Assert.IsNull(plan.Offer);
        }

        [Test]
        public void RepairOfferIsTheInstalledReleaseUntilItIsWithdrawn()
        {
            Assert.AreEqual("0.9.0", UpdatePolicy.Plan("0.9.0", View("0.9.1"), false).RepairOffer.Version);
            Assert.IsNull(UpdatePolicy.Plan("0.9.0", View("0.9.1", installed: null), false).RepairOffer);
        }
    }
}
