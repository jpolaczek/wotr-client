# Miniature stands and token pucks — UI-400 (2026-10-29)

The painted bases the tabletop figures stand on ([TABLETOP_FIGURES_UX](../../docs/ui/TABLETOP_FIGURES_UX.md)): an oval
field stand, a crenellated garrison stand and a small round character stand in each side's enamel (Free Peoples blue
with gold, Shadow red-black with bronze), and the thick enamel puck of the far-zoom token (UI-406). The contact shadow,
the state rims and the landing mark are drawn as SVG in the UI kit (`graphicalassets/ui/src/armies`), since they must be
exact and translucent.

**Generation.** The owner's Codex CLI (`codex exec`, built-in `image_gen` tool, no API key), one call per key
(`generate.py`, `codex_image.sh`), 1536×1024, on a flat magenta background (#FF00FF) to key off. The prompts share one
style — a painted resin miniature's base seen from about 35° above, light from the upper left, nothing standing on it,
no text — and are recorded with the SHA-256 of each original output in [generations-codex.jsonl](generations-codex.jsonl).
`raw/` keeps each output at 768 pixels wide.

**Keying.** `key.py` turns the magenta into alpha by distance (green weighted), removes the magenta spill from the edge
pixels, trims, and scales each stand to fit a fixed canvas at twice its display size, standing on the bottom edge,
centred, so every stand of a kind has one size and one feet line:

| Key | Points | Pixels |
| --- | --- | --- |
| `stand-field-free`, `stand-field-shadow` | 112 × 44 | 224 × 88 |
| `stand-garrison-free`, `stand-garrison-shadow` | 112 × 48 | 224 × 96 |
| `stand-character-free`, `stand-character-shadow` | 40 × 28 | 80 × 56 |
| `token-puck-free`, `token-puck-shadow` | 40 × 32 | 80 × 64 |

The committed PNGs (`unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/UI/armies/`) were keyed from the full-size
originals; keying `raw/` again gives the same art at a slightly lower source resolution. Python: numpy and Pillow
(`scripts/requirements-map-canvas.txt`).
