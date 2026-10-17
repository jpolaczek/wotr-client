<a id="typografia--cinzel-i-alegreya"></a>
# Typography — Cinzel and Alegreya

User decision with 2026-10-14: Cinzel + Alegreya, after comparing five cuts in the sampler. Fonts are part of the client and work offline.

| Role | Cow |
| --- | --- |
| Window, panel titles, cards and character names | Cinzel SemiBold, 600 |
| Content of cards, descriptions, statuses, lists and fields | Alegreya Regular, 400 |
| Buttons and Bold Highlights | Alegreya SemiBold, 600 |

We do not change the size or height of panels globally. We apply fixed dimensions and positions with UX_SPEC and TURN_CONSOLE_UX. We evaluate the cuts with 1366×768 and 1920×1080, in the scales 100%/125%, on parchment and dark panels. Long content preserve existing wrap rules; symbols require a fallback. Small digits and tags Guide have heights corresponding to the natural Aligreya metrics; we do not reduce the writing nor change the height of the panel.

<a id="integracja"></a>
## Integration

One `Assets/Wotr/Resources/Wotr/Styles/Typography.uss` sheet is imported by the main `Wotr.uss`. The content font is inherited by board and the menu; headers and buttons have open families. We do not set the font in each C# component and we do not play UI when changing the view.

The TTF included are static instances. UI Toolkit uses dynamic FontAsset SDFAA, with 1024×1024 atlas and multi-gym support. Weight table binds the Alegreyi Bold to the real SemiBold; headers retain Cinzel SemiBold. The ASS prepares `WoTR/Prepare typography` or CLI:

```sh
bash scripts/unity.sh -quit -executeMethod Wotr.Editor.TypographyAssets.Prepare -logFile .artifacts/typography-import.log
```

The usual build uses a versioned Asset; it does not download fonts from the network and does not require system installation.

<a id="pochodzenie-i-licencje"></a>
## Origin and Licence

Files are in `Assets/Wotr/Resources/Wotr/Fonts` . `sources.json` store source URL and SHA-256 , weight, conversion tool and SHA-256 ready TTF Original copyright information and full text SIL OFL 1.1 are located in `Assets/StreamingAssets/ThirdPartyLicenses/cinzel-OFL.txt` and `alegreya-OFL.txt` , including `FONT-NOTICES.txt` Unity copies them to build as regular text files: `Wotr.app/Contents/Resources/Data/StreamingAssets` for macOS and `Wotr_Data/StreamingAssets` on Windows. You can read them without tools to unpack the Assets.

- [Cinzel — Licence](https://raw.githubusercontent.com/google/fonts/main/ofl/cinzel/OFL.txt)
- [Alegreya — Licence](https://raw.githubusercontent.com/google/fonts/main/ofl/alegreya/OFL.txt)
- [Rules of Use OFL](https://openfontlicense.org/how-to-use-ofl-fonts/)

The instances were prepared fontTools 4.67.0 with `wght=400` or `600` axis, without changing the contours of the cut design. The English family, style, full-name and PostScript names, copyright and license metadata are updated and the time-stamp update for the repeatable result is preserved. Polish characters and accents of the names Éomer, Lórien, Sméagol and Nazgûl are present in the attached TTF.

<a id="odbiór"></a>
## Acceptance

Evidence of import, actual used fonts, layouts, dumps and builds are recorded in the task TYPO-01 of the central server registry. The HTML sampler outside the repository was used for selection, it does not replace the check in Unity.
