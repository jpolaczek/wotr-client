# Security policy

The game, its updater and installers are maintained here; the server, game site and release tooling are in
[wotr-server](https://github.com/jpolaczek/wotr-server). Both follow the same policy:
[wotr-server SECURITY.md](https://github.com/jpolaczek/wotr-server/blob/main/SECURITY.md).

## Reporting a vulnerability

Use **[Report a vulnerability](https://github.com/jpolaczek/wotr-client/security/advisories/new)** on this repository's
Security tab. The report stays private until a fix is published; please do not open a public issue for it. Expect a
first answer within 7 days.

## Official endpoints

| Purpose | Address |
| --- | --- |
| Game site, account pages, game API and update catalog | `https://wotr.vibestudios.org` |
| Installers, update packages and signed release manifests | `https://downloads.vibestudios.org:50443/` |
| Releases and source | `https://github.com/jpolaczek/wotr-client/releases` |

## How the game checks an update

- The catalog comes from the game server over HTTPS; each release has a manifest signed with the release key below.
- The game verifies the signature over the exact manifest bytes, then requires the manifest to name this version and
  platform and a package on the downloads origin built into the game.
- The package must have the manifest's size and SHA-256; the hash is checked again right before installation.
- Only a newer version installs; the same version installs only as a repair chosen in Settings.

| | |
| --- | --- |
| Algorithm | Ed25519 |
| Key id | `95ffc109e1bb9b15` |
| Public key (base64) | `assXbu7Zbo494JMal54lWXX3LdodwnzpOiwz40RXDG4=` |
| SHA-256 of the raw public key | `95ffc109e1bb9b15710cb7ba298fb959751630ae8d96ab3e9dcab9ddadd8b5d3` |

The pinned copy is [`ReleasePublisher.cs`](unity/Wotr.Client/Assets/Wotr/Runtime/Networking/ReleasePublisher.cs);
`python3 scripts/check_release_key.py` in wotr-server checks that this table matches it.

Installers have no Apple or Microsoft signature (decision D13); the
[player guide](docs/release/PLAYER-GUIDE.md) explains the warning on first installation.
