using System;

namespace Wotr.Client.Updates
{
    // A release, manifest or download the updater refuses; the running installation stays as it is.
    public sealed class UpdateRejected : Exception
    {
        public UpdateRejected(string message) : base(message)
        {
        }
    }
}
