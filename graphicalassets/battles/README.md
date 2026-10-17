<a id="ilustracje-bitew--ui-206"></a>
# Pictures of battles — UI-206

The user's application from 2026-10-16: pass above the window battle is to show the illustration battle dependent on the region — other villages, other cities — two versions for the places Shadow and Free Peoples.

**Set (20).** `<rodzaj>-<strona>-<1|2>`:

| Kind | Pages |
| --- | --- |
| `open` (without settlement) | `free`, `shadow`, `wild` (without owner) |
| `fortification` | `free` (Osgiliath, Fords of Isen) |
| `town`, `city`, `stronghold` | `free`, `shadow` |

Selection for the region: `BattleSceneArt` in the client (`docs/ui/BATTLE_WINDOW_UX.md`, section UI-206).

**Generation.**
- OpenAI Images API (`gpt-image-1.5`, 1536×1024, medium quality, opaque background), user key from
  the environment, as in ART-01 (`graphicalassets/figures/README.md`).
- [Prompty](v1/prompts.json) have a common style description: a painted panorama of battle, armies
  from a distance in the middle of pass height, without text and frames.
- [Generation Script](v1/generate.py) saves the full image in `v1/raw/<klucz>.jpg` (JPEG 90) and adds
  to [journal](v1/generations.jsonl) prompt, model, wear and SHA-256 the original and saved file.
  The API key does not enter the files.
- Startup: the environment with Pillow, `OPENAI_API_KEY=… python3 v1/generate.py [klucz …]`; without
  keys generates missing.

- It's a construction. [`build.py`](build.py) cut pass 1536×320 around the line 560 (arms and horizon; another measure can be given in `crop` in prompts.json), saves `unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/battle-illustrations/<klucz>.jpg` ( JPEG 88 ) with files `.meta` from the set template UI (without mepmap, Clamp) and [manifest](v1/manifest.json) ed SHA-256 sources and results. `build.py --check` check the compatibility.

Replacement graphics, no origin from the original game components.

## Nation set — UI-226 (2026-10-17)

The owner's request: battle pictures of towns, cities and strongholds by nation, not only by side. **Set (30).**
`<town|city|stronghold>-<nation>-<1|2>`, two versions where the nation has two or more such places, otherwise one;
`city-dwarves-1` covers Fate of Erebor's dwarven City. `BattleSceneArt` asks for the nation's picture first and falls
back to the side's picture of the set above.

- Prompts: the same style as above, items `*-<nation>-*` in [prompts.json](v1/prompts.json); scenes are original and
  name no film or illustrator.
- Generation: the owner's Codex CLI (`codex exec`, built-in `image_gen` tool, no API key), one call per key, 1536×1024,
  copied to `v1/raw/<key>.jpg` (JPEG 90); [generations-codex.jsonl](v1/generations-codex.jsonl) records the prompt and
  the SHA-256 of each saved file.
- Crop centres in `crop` were chosen by eye so each band holds the place and the battle; `build.py` cuts them like
  the set above.
