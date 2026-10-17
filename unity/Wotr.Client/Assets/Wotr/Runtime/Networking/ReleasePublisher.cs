namespace Wotr.Client
{
    // DIST-05: the pinned Ed25519 key that signs release manifests (manifest.json.sig). The updater (DIST-07)
    // accepts a package only after verifying its manifest with this key. The private key stays on the release
    // machine, outside both repositories and the download host; rotating it needs a client release.
    public static class ReleasePublisher
    {
        public const string PublicKey = "assXbu7Zbo494JMal54lWXX3LdodwnzpOiwz40RXDG4=";
        public const string KeyId = "95ffc109e1bb9b15";
        // DIST-04/D15/MENU-28: the only origin update packages come from is the build's WOTR_DOWNLOADS_URL
        // (ServerConfiguration.Downloads, from outside git). A signed manifest naming another origin is refused;
        // moving the host needs a client release.
    }
}
