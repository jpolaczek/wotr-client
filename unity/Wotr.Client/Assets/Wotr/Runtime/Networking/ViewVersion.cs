using Wotr.Contracts;

namespace Wotr.Client
{
    // Independent monotonic counters for one viewer, without retaining a mutable DTO.
    // QUAL-09: IsNewer only judges a view; Commit records it once the view has really been applied.
    public sealed class ViewVersion
    {
        private readonly string matchId;
        private Side? viewer;
        private long privateVersion = -1;
        private long handVersion = -1;
        public long Revision { get; private set; } = -1;

        public ViewVersion(string matchId) => this.matchId = matchId;

        public bool IsAhead(long revision, long privateValue) =>
            revision > Revision || revision == Revision && privateValue > privateVersion;

        public bool IsNewer(PlayerView view)
        {
            if (view == null || view.MatchId != matchId || viewer.HasValue && view.You != viewer.Value)
                return false;
            var nextPrivate = view.AutoDecision?.Version ?? 0;
            var nextHand = view.AutoDecision?.HandVersion ?? 0;
            if (view.Revision < Revision || nextPrivate < privateVersion || nextHand < handVersion)
                return false;
            return view.Revision != Revision || nextPrivate != privateVersion || nextHand != handVersion;
        }

        public void Commit(PlayerView view)
        {
            Revision = view.Revision;
            privateVersion = view.AutoDecision?.Version ?? 0;
            handVersion = view.AutoDecision?.HandVersion ?? 0;
            viewer = view.You;
        }

        // OBS-05: another perspective starts the counters again, so the current revision applies from the other side.
        public void Reset()
        {
            Revision = -1;
            privateVersion = -1;
            handVersion = -1;
            viewer = null;
        }

        public bool Accept(PlayerView view)
        {
            if (!IsNewer(view))
                return false;
            Commit(view);
            return true;
        }

        // The key of one exact view, for counting failed attempts to apply it.
        public static string Key(PlayerView view) =>
            view.Revision + "/" + (view.AutoDecision?.Version ?? 0) + "/" + (view.AutoDecision?.HandVersion ?? 0);
    }
}
