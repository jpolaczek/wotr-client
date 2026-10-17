using System.Threading.Tasks;

namespace Wotr.Client
{
    // Finishes local recovery writes only. It never sends or retries a game command.
    public sealed class BoardExit
    {
        private readonly LocalMatchPersistence persistence;
        public bool Ready => persistence == null || persistence.Settled && persistence.RecoveryError.Length == 0;

        public BoardExit(LocalMatchPersistence persistence) { this.persistence = persistence; }

        public async Task<string> Prepare(bool retry)
        {
            if (persistence == null)
                return "";
            if (retry)
                persistence.Retry();
            await persistence.FlushStable();
            return persistence.RecoveryError;
        }
    }
}
