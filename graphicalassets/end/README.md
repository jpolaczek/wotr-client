# End-of-game paintings — UI-233

The end-of-game screen shows a painting of the result from the viewer's side: `free-victory`, `free-defeat`,
`shadow-victory`, `shadow-defeat`; an observer sees the winner's victory (`GameEndState.Scene`,
`docs/ui/SIGNALS_UX.md`).

**Generation.**
- OpenAI Images API (`gpt-image-1.5`, 1536×1024, medium quality, opaque background) with the user's key from the
  environment, as for the battle paintings (`graphicalassets/battles/README.md`).
- [Prompts](v1/prompts.json) share one style: a painted panorama with small distant figures, no text or frame.
- [Generation script](v1/generate.py) stores the full image in `v1/raw/<key>.jpg` (JPEG 90) and appends the prompt,
  model, usage and the SHA-256 of the original and the stored file to the [journal](v1/generations.jsonl). The API key
  never enters the files. Run: an environment with Pillow, `OPENAI_API_KEY=… python3 v1/generate.py [key …]`.

**Build.** [`build.py`](build.py) cuts the 1536×768 band around the subject (row 512, or the `crop` centre in
`prompts.json`), scales it to 960×480 (twice the scene's 480×240 points), saves
`unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Art/end-scenes/<key>.jpg` (JPEG 88) with `.meta` files from the UI kit
template and writes the [manifest](v1/manifest.json). `build.py --check` verifies outputs against the manifest.

Replacement artwork, not taken from the original game's components.
