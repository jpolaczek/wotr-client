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
