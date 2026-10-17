<a id="velopack-i-zależności-dist-06"></a>
# Velopack and Dependency (DIST-06)

`lib/netstandard2.0` library with nuget.org, unchanged. Newtonsoft.Json is not included: Velopack 1.2.161 uses the `com.unity.nuget.newtonsoft-json` version (13.0.0.0 set version). Spikes S1–S3 (MACOS ARM64) and S2 (Windows, GitHub Actions 2026-10-15) confirmed these files in Mono player of both platforms; `runtimes/win` variant is not needed.

| File | Pack NuGet | SHA-256 packs |
| --- | --- | --- |
| Velopack.dll | velopack 1.2.161 | d57275692b299751a12ee3e18283b5f7fe3e55339f96b1623345da095dc484ed |
| Microsoft.Win32.Registry.dll | microsoft.win32.registry 5.0.0 | f64ca53c67ca65ce7cc85a8d29aefbb2da2672836731e1115e8cd62730dc5080 |
| System.Security.AccessControl.dll | system.security.accesscontrol 5.0.0 | b9e486f989fcd9ebf1c86067138f4de03fa780e0c35e0a2b9e506d4373a6c39e |
| System.Security.Principal.Windows.dll | system.security.principal.windows 5.0.0 | 081390c25f6f78592b28ada853c24514488a221fe9f9a24efaaf5373643ff3d6 |

SHA-256 files DLL (checks `tests/release/test_velopack_plugins.py`):

```text
1d99045f82a397e859859f5a5fc19baba26e1b225a8dfa4824719427e5d4ecba  Velopack.dll
340933ad6701077ae9b8035e4671803d86074ab32f2de8165acfdb954bd260f6  Microsoft.Win32.Registry.dll
93084849c17a21f641c13c9f17545cfe18c1ec097561f3f0ebbbe26f358ba120  System.Security.AccessControl.dll
7828dfd952a9fd49404477baff714849177d9f18c0654adafadbdcafb4b21f47  System.Security.Principal.Windows.dll
```

The `vpk` packaging tool must have the same version (`VPK_VERSION` in `scripts/package_velopack.py`). Changing Velopack version requires all files, new sums and re-trying installation, upgrade and repair on both systems.
