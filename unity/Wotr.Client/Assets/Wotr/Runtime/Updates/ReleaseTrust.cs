using System;

namespace Wotr.Client.Updates
{
    // What a release must be signed with and where its files may come from. The game uses the publisher's key and
    // the downloads origin of its build; tests pass their own key.
    public sealed class ReleaseTrust
    {
        public ReleaseTrust(string publicKeyBase64, string keyId, string downloadsOrigin)
        {
            PublicKey = Convert.FromBase64String(publicKeyBase64);
            if (PublicKey.Length != 32)
                throw new ArgumentException("An Ed25519 public key has 32 bytes.", nameof(publicKeyBase64));
            if (!Uri.TryCreate(downloadsOrigin, UriKind.Absolute, out var origin) || origin.Scheme != Uri.UriSchemeHttps ||
                !downloadsOrigin.EndsWith("/", StringComparison.Ordinal))
                throw new ArgumentException("The downloads origin is an https URL ending with /.", nameof(downloadsOrigin));
            KeyId = keyId;
            DownloadsOrigin = downloadsOrigin;
        }

        private static ReleaseTrust pinned;

        // The publisher's key with the downloads origin of this build (MENU-28: from the build's configuration).
        public static ReleaseTrust Pinned => pinned ??= ForOrigin(ServerConfiguration.Downloads);

        public static ReleaseTrust ForOrigin(string downloadsOrigin) =>
            new ReleaseTrust(ReleasePublisher.PublicKey, ReleasePublisher.KeyId, downloadsOrigin);

        public byte[] PublicKey { get; }
        public string KeyId { get; }
        public string DownloadsOrigin { get; }
    }
}
