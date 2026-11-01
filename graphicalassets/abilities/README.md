# Character ability icons — UI-439 (2026-11-01)

One small icon for every printed ability of every character card — the base game, Lords of Middle-earth (with the
Council's companions), Kings of Middle-earth and The Fate of Erebor — each drawn for what its ability does or whose
it is, all different from one another (the owner's playtest of 2026-11-01: the old ones were too simple, repeated and
recycled other icons; the Balrog's three abilities shared one flame). The same icon stands for the ability wherever a
button, chip or tooltip shows it. [INVENTORY.md](INVENTORY.md) lists every ability, its rules text, the icon it had,
the new one, and every place the client shows an ability.

**Keys.** A key is the printed ability id of `character-ability-text.json` (the public content package), so the icon
of an ability is `Wotr/Art/UI/abilities/<ability id>`. One more key, `dark-chieftain`, is the Kings' Dark Chieftains'
own form of `not-minion-for-gandalf-white` (the Black Serpent, the Shadow of Mirkwood and Uglúk print it as *Dark
Chieftain*, with the shared Ruler die; the Balrog, Gothmog and the Witch-king print it as *Gandalf the White entry*).
An ability printed on several cards (Captain of the West on eight) is one ability with one icon. The ids are already
lowercase letters, digits and hyphens, so a file name is the id as it is.

**Generation.** The owner's Codex CLI (`codex exec`, built-in `image_gen` tool, no API key), one call per key, four at
once (`generate.py --jobs 4`); `codex_image.sh` is the stands' script (`graphicalassets/stands`) except that it takes
each image from its own run's session folder, so parallel runs never swap images, and that the agent, which only hands
the prompt to the image tool, runs at low reasoning effort with its stdin closed. Square images (1254 pixels) on a flat
magenta background (#FF00FF). The prompts share one style (`STYLE`) — a bold single motif in polished gilt with a
thick dark ink outline and small areas of coloured enamel, like an enamelled badge, readable at 20 pixels, no text —
and are recorded with the SHA-256 and size of each original output in
[generations-codex.jsonl](generations-codex.jsonl); the last entry of a key is the image in `raw/`, which keeps each
output at 768 pixels.

**Rounds (2026-11-01).** First, all 77 keys (`ITEMS`). After the preview, a second round (`REVISIONS`) for 22 keys
whose motif shrank to a thin line at 18–28 points (a sword, staff, spear, arrow or rod standing upright, the White
Tower), was dark on the dark interface (Orthanc, the fell beast, the bat, the Uruk helm, the hood, the torn card) or
read as something else (Gollum's shut eye read as lips); `STYLE` gained the sentences on gold edges and diagonal long
objects. 16 came through; 6 hit the owner's Codex usage limit and keep their first-round image. The new style
sentence also made five second-round icons add a rod or sword the prompt did not name, so it now forbids that.

**Keying.** `key.py` estimates each pixel's share of the background from its magenta cast (min(red, blue) − green; the
icons are drawn without magenta, pink or purple, so a pixel between the icon and the background is a mix of the two),
removes the background from the edge colours, trims, and fits the icon, centred, into a square at twice its display
size. Its Unity meta copies the UI kit's texture import settings (`Art/UI/characters/ability.png.meta`: no mipmaps,
uncompressed RGBA32, bilinear, clamp) with a GUID derived from its path, as `graphicalassets/ui/render.py` does.

| Keys | Points | Pixels | Output |
| --- | --- | --- | --- |
| every ability id, `dark-chieftain` | 28 × 28 | 56 × 56 | `unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/UI/abilities/<key>.png` |

28 points is the largest size the client shows an ability at; the roster's 15–19-point buttons and the chips'
16-point icons draw the same file smaller.

**Preview.** `preview.py` writes [preview.png](preview.png): every icon enlarged 4× (its PNG's own pixels), at 28 and
at 18 points, with its title and key; below, each character card's icons side by side at 28 points, so the icons one
character carries can be compared at a glance.

**Status.** Every key has its icon: 77 distinct files, each character's icons all different (the Balrog's: a horned
head of shadow and flame, a sword of fire, a whip of fire, a crossed-out white staff). Twelve keys (`PENDING` in
`generate.py`) wait for a third round when the owner's Codex usage allows: the six second-round failures (Art of
Disappearing, Strider's Lords Guide, Heir of Bard, Shadows in the North, Stewardship — still thin or dark — and the
Lord of the Bats), the Dwarves' raven (its spread wings are close to the bat's; now in profile), and the five icons
with an unrequested rod or sword (Master of the Precious, Gollum's Unseen path, Strider's hood, Servants of the White
Hand, Bound to Orthanc). Their current images work at 28 points; the paths and metas will not change.

**In the client.** One map, `AbilityArt` (`unity/Wotr.Client/Assets/Wotr/Runtime/Presentation/AbilityArt.cs`), names
the icon of an ability, of a card's own form of it and of every command, reaction and decision that uses it; the
roster rows, the character window's lines, the component action buttons (with `AbilityBadges` where one ability has
several commands side by side), Narya's activation, the battle window's Evil Knowledge, reaction portraits and combat
replacements, and the Hunt's Guide responses all ask it
([UX_SPEC UI-439](../../docs/ui/UX_SPEC.md#every-ability-has-its-own-icon--ui-439)).
`AbilityArtTests` fails for an ability without its own icon, for two abilities sharing one, and for two identical
files; `UiArtKitTests` holds the 77 files to 28 points and the kit's import settings. A regenerated icon keeps its path
and meta, so nothing in the client changes.

**Reproduce.** Python with numpy and Pillow (`scripts/requirements-map-canvas.txt`):

```
python3 graphicalassets/abilities/generate.py [--jobs 4] [--pending | key ...]   # Codex; appends to generations-codex.jsonl
python3 graphicalassets/abilities/key.py [key ...]                               # raw/ -> Art/UI/abilities (+ .meta)
python3 graphicalassets/abilities/preview.py                                     # preview.png
```

The committed PNGs were keyed from the 768-pixel `raw/` images, so keying again gives the same files.
