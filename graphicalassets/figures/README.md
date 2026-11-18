<a id="proste-figury-ui-76"></a>
# Simple figures UI-76

Selected atlas: [board-figures-v2.png](../../unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/board-figures-v2.png); [recty](../../unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/board-figures-v2.json).

Generated with the built-in image_gen tool, 2026-10-08 Original. PNG RGBA 1448×1086 with a real alpha was preserved without editing. The image review confirmed twelve distinct silhouettes: regular, elite, commanders, ranger/wizard/ruler/minion , Nazgûla and Fellowship The first variant had too close rows — the Nazgûla rectangle could have taken the ranker's feet. The second generation improved the intervals. Runtime reads individual, disconnected rects with JSON ; their metadata was determined by an alpha analysis, without saving edited pixels.

The replacement graphics, without the origin of the original game components. Type/page/ID and hitbox remain independent of the image. The rejected v1 variant remains as an iteration source in graphicalassets/figures, outside the build.

SHA-256 v2: `8d83c6a1003ec6fe8664c7c93aa883434584029e905ac0ec0245133bf5d858f8`. Proppy: [generation](prompt.txt), [improvement of intervals](layout-prompt.txt).

<a id="figurki-narodów--art-01-2026-10-15"></a>
## Figures of nations — ART-01 (2026-10-15)

User application: figures of higher quality and more distinctive, with an elite visible immediately. A design from the original game, e.g. the Haradu elite is Mûmak.

**Generation.**
- OpenAI Images API (`gpt-image-1.5`, 1024×1024, medium quality, transparent background), user key.
  User decision with 2026-10-15.
- [Prompty](v3/prompts.json) have a common style description: painted resin figure, all in the
  frame, without base game and shadow.
- [Generation Script](v3/generate.py) saves `v3/raw/<klucz>.png` (remote LANCZOS to 512 px) and adds
  to [journal](v3/generations.jsonl) prompt, model, wear and SHA-256 the original and saved file.
  The API key does not go to files.
- The rejected generations are in `rejected/`:
  - `gondor-elite`: inscription on the breast;
  - `sauron-regular`: ork As massive as troll.

**Figures (22).**

| Nation | Regular | Elite | Leader |
| --- | --- | --- | --- |
| Gondor | infantry with spear and shield with tree | Horse knight in silver armor | Captain with banner of tree and stars |
| Rohan | a round-disc infantry with horse | Rohan's rider in the gallop | Marshal with banner of white horse |
| Elves | archer | a guard in gold armor with a girdle | ruler with star banner |
| Dwarves | A warrior with an axe and a shield | Dwarf in full plate with hammer | ruler with hammer and anvil banner |
| North | Dale archer | The Guardian of the North with the sword | ruler Dale with arrow banner |
| Sauron | Skin ork | a giant troll with a machcha | Nazgûl on a black horse |
| Isengard | Uruk-hai with white hand | Horseman on his lips | — |
| Southrons & Easterlings | warrior Haradu | Mûmak with the tower | — |

**Atlas.**
- [`scripts/build_figure_art.py`](../../scripts/build_figure_art.py) builds `Resources/Wotr/Art/units-v3.png` and
  `.json` (`board-figures-1` scheme):
  - trims the alpha and scales to a cell of height 240 px;
  - the wide beasts are located in 400 px;
  - relative growth (`STATURE`): Regular 0,78, Leader 0,88, Elite 1,0, aligned to the feet, so the
    regular is lower than the elite at the same cell height.
- The [ART-FIGURES](../../docs/sources/ART-FIGURES.json) report contains sources, Pillow and SHA-256
  versions of the results. `--check` checks the compatibility.
- The client selects the `<nation>-<kind>` key figure (`FigureArt`, `NamedAtlas`). When the key is
  not there, the UI-76 figure is left.
- Replacement graphics, no scans of the original components.

<a id="figurki-i-portrety-postaci--art-02-2026-10-15"></a>
## Character figures and portraits — ART-02 (2026-10-15)

User application: each character (Fellowship, Saruman, etc.) has its own, nice graphics in the style of the game, very loosely inspired by films.

**Generation.**
- Same generator, model and log as ART-01.
- For 14, base game characters were created:
  - figurine (`<klucz>`, `character` style, transparent background);
  - portrait of bust (`<klucz>-portrait`, style `portrait`, painted background `opaque` with
    `backgrounds`).
- The six first portraits were too similar to film actors: Strider, Legolas, Boromir, Merry, Pippin
  and Ring-bearers. They were re-generated with clearly their own features (close, gray temples,
  braids, freckles, etc.).
- The rejected versions are in `rejected/*-portrait-a.png`. Merry also has a `-b` version with white
  corners.

**Atlases.**
- `characters-v1`: 14 figurines. Relative height: Hobbits 0,62–0,64, Gimli 0,74, Gollum 0,55, the
  remaining 1,0.
- `battle-portraits-v2`: 14 160 portraits cut to the wheel with a soft edge. The cell carries the
  character key without the suffix `-portrait`.
- The client takes the character figure by definition key (`FigureAppearances.Character`) and uses
  it:
  - on the map;
  - in the Characters and Fellowship;
  - as a Fellowship (Ring-bearers) tag.
- The portrait of the battle medallion is derived from v2. The expansions character (Sméagol,
  Elrond, Galadriel, Balrog, Gothmog) retains the portraits of UI-120 from v1.

## Expansion characters — ART-04 (2026-10-19)

`v3/prompts.json` adds the figures and portraits of the expansions' own characters (Elrond, Galadriel, the Balrog,
Gothmog, Sméagol, the Dwarven Lord Gimli, the five awakened sovereigns, the three Dark Chieftains) and the portraits
of the five corrupted sovereigns, keyed like their cells (`PortraitArtwork.Key`: "brand-awakened",
"brand-corrupted-portrait", "black-serpent"). Lords versions of base characters reuse the base art with an owner-only
badge (owner decision 2026-10-19). Nothing is generated yet: ART-05 (Fate), ART-06 (Lords) and ART-07 (Kings) run
`v3/generate.py` for their keys and review the results.

## Fate of Erebor — ART-05 (2026-10-19)

Generated with `v3/generate.py` (logged in `v3/generations.jsonl`): `gimli-dwarven-lord` (figure, stature 0.74 like
Gimli) and `gimli-dwarven-lord-portrait`, reviewed for the house style (painted resin figure; candlelit oil bust with
a gilded crowned helm, no actor likeness). `scripts/build_figure_art.py` now builds the atlases from the items that
have raw images and lists the rest as `pending` in `docs/sources/ART-FIGURES.json` (the Lords and Kings characters,
ART-06 and ART-07), instead of refusing to build.

<a id="saruman-and-gandalf-the-white-told-apart--art-11-2026-11-17"></a>
## Saruman and Gandalf the White told apart — ART-11 (2026-11-17)

The owner: "Saruman's and Gandalf the White's figures are too alike; rework them". Both wore white robes with a white
beard, so on the map only the staff told them apart.

- **Saruman** (Shadow) is now unmistakably dark:
  - a black cloak lined with crimson over robes of many colours (iridescent violet, crimson and green);
  - a white-and-grey beard and a small white-hand emblem;
  - a black iron staff crowned with a dark orb.
- **Gandalf the White** (Free Peoples) is unmistakably bright:
  - shining white robes under a pale silver-blue cloak;
  - a plain white staff and a sword.

  He has no glow or outline, so no gold rim is mistaken for a choice glow.
- **Generation.** Both were made with the same generator, model and style as the other figures ([prompts](v3/prompts.json),
  [journal](v3/generations.jsonl)), and `scripts/build_figure_art.py` rebuilt `characters-v1`. The other atlases are
  unchanged (`--check` passes with the pinned Pillow 12.3.0).
- **Rejected** (in `rejected/`):
  - `saruman-white-robes`, `gandalf-white-like-saruman`: the old pair;
  - `gandalf-white-gold-halo`: a first new Gandalf with a gold halo around the figure.
- **Check.** PlayMode `WizardFigureTests` sets both on the map near Orthanc (frame `art11-wizards`).

<a id="saruman-white-again--art-11-second-pass-2026-11-18"></a>
## Saruman white again — ART-11, second pass (2026-11-18)

The owner: "Saruman is now black and red; he must stay white, only clearly different from Gandalf the White". Saruman
keeps the white of his order and is told apart by everything but the robe's colour:

- **Saruman** (Shadow):
  - cold white robes with a tall stiff collar and wide sleeves;
  - a black inner robe showing down the front in one broad black stripe;
  - a black sash with a small white-hand emblem;
  - white hair streaked with black;
  - a black iron staff crowned with claw-like spikes;
  - no cloak, no sword, no blue and no brown leather.
- **Gandalf the White** is unchanged: a pale silver-blue cloak, a plain white staff, a sword on a brown belt.

  On the map, Saruman reads as white and black, Gandalf as white and blue.
- **Generation.** Same generator, model and style ([prompts](v3/prompts.json), [journal](v3/generations.jsonl)).
  `scripts/build_figure_art.py` rebuilt `characters-v1`, and `--check` passes.
- **Rejected** (in `rejected/`):
  - `saruman-black-crimson`: the first pass;
  - `saruman-ivory-robes`: a first white try whose robes read cream and whose beard was short.
- **Unchanged.** The battle portraits.
