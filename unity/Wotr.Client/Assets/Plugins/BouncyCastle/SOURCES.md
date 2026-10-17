# BouncyCastle (DIST-07)

`lib/netstandard2.0` library with nuget.org, unchanged, without any dependencies. Updateer in the game checks the signature of Ed25519 manifest of release (reviewer key pinned in `Runtime/Networking/ReleasePublisher.cs`); we do not write our own cryptography. License MIT: [LICENSE.md](LICENSE.md) from the package.

| File | Pack NuGet | SHA-512 packages (NuGet catalogue) | SHA-256 packs |
| --- | --- | --- | --- |
| BouncyCastle.Cryptography.dll | bouncycastle.cryptography 2.7.0 | Lys+dJthpD8quO On AkRV9ke8DgET0CwBW91NeJjMVHZGvRRNO8DkdReYFfCBOCcvMpz Eat. C3DuLrpRuntYnq9Iw== | f091ffccab4d03993e660bace277659a79dee0972f54d7f1f4bd46d680966241 |

SHA-256 from the DLL file (checked by `tests/release/test_velopack_plugins.py`):

```text
d61c1f2ba929a230a58e101ccd850e21f2675fa6b9814ec279633e8a089c3495  BouncyCastle.Cryptography.dll
```
