using System;
using Wotr.Contracts;

namespace Wotr.Client.Updates
{
    public enum UpdateMode
    {
        None,
        // A newer release the player may download (Download update / Later); nothing is fetched before consent.
        Optional,
        // A minimum from a future time: downloaded in the background, play continues until the deadline.
        RequiredSoon,
        // The server refuses this version now: new moves wait for Restart and update (or Decline and quit).
        RequiredNow,
        // The server is older than this client: nothing to download, the player waits for the server update.
        ServerBehind
    }

    // What the catalog means for this copy. Offer is the release to install (the newest announced one); it is
    // null when no newer release exists, e.g. a required update with nothing published for this platform.
    // RepairOffer is the release of the running version, null once it is withdrawn (DS23).
    public sealed class UpdatePlan
    {
        public UpdatePlan(UpdateMode mode, ClientReleaseOffer offer, DateTimeOffset? deadline, ClientReleaseOffer repairOffer)
        {
            Mode = mode;
            Offer = offer;
            Deadline = deadline;
            RepairOffer = repairOffer;
        }

        public UpdateMode Mode { get; }
        public ClientReleaseOffer Offer { get; }
        public DateTimeOffset? Deadline { get; }
        public ClientReleaseOffer RepairOffer { get; }
    }

    // D13 modes in priority: RequiredNow > RequiredSoon > Optional; ServerBehind downloads nothing. Repair is the
    // player's own choice in Settings and only needs RepairOffer. Pure: the same catalog answer gives the same plan.
    public static class UpdatePolicy
    {
        public static UpdatePlan Plan(string ownVersion, ClientReleaseView view, bool refusedByServer)
        {
            var own = ClientVersion.Parse(ownVersion);
            var repair = view.Installed;
            if (view.Status == ClientCompatibilityStatus.ServerUpdateRequired)
                return new UpdatePlan(UpdateMode.ServerBehind, null, null, repair);
            var newer = Newer(view.Latest, own);
            var minimum = Version(view.MinimumVersion);
            if (refusedByServer || view.Status == ClientCompatibilityStatus.ClientUpdateRequired || Below(own, minimum))
                return new UpdatePlan(UpdateMode.RequiredNow, newer, null, repair);
            var scheduled = view.ScheduledMinimum;
            if (scheduled != null && Below(own, Version(scheduled.Version)))
                return new UpdatePlan(UpdateMode.RequiredSoon, newer, scheduled.EffectiveAt, repair);
            return new UpdatePlan(newer == null ? UpdateMode.None : UpdateMode.Optional, newer, null, repair);
        }

        private static ClientReleaseOffer Newer(ClientReleaseOffer offer, ClientVersion own) =>
            offer != null && ClientVersion.TryParse(offer.Version, out var version) && version.CompareTo(own) > 0 ? offer : null;

        private static ClientVersion Version(string text) =>
            ClientVersion.TryParse(text, out var version) ? version : null;

        private static bool Below(ClientVersion own, ClientVersion minimum) => minimum != null && own.CompareTo(minimum) < 0;
    }
}
