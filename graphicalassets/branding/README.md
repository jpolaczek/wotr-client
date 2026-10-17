# Release application icon

The shared War of the Ring Online icon is a gold ring on a warm charcoal
rounded-square plate with transparent outer corners.

**Since UI-260 (2026-10-18)** the master is [v2](v2/): the owner found the first ring
flat and crooked (its band thinned to one side and the hole sat off centre). The new
ring lies tilted back in correct perspective with a band of constant thickness, the
far inner surface visible and a soft contact shadow; it is mirror-symmetric about the
vertical axis and reads at 16–32 px. Generated with the OpenAI Images API
(`gpt-image-1.5`, 1024×1024, high quality, transparent background) with the owner's key
from the environment: [prompts](v2/prompts.json), [script](v2/generate.py) and
[journal](v2/generations.jsonl) with three candidates in `v2/raw/` (`ring-tilted` chosen;
`ring-upright` read as a letter O, `ring-floating` leaned). The key never enters the files.
The first icon (2026-10-16, built-in imagegen tool) is kept as [v1](v1/wotr-icon-master.png).
No existing publisher artwork or other image was supplied as a reference.

## Files

- [wotr-icon-master.png](wotr-icon-master.png): the chosen generated master (a copy of `v2/raw/ring-tilted.png`).
- [wotr.png](../../release/icons/wotr.png): 1024×1024 portable PNG.
- [wotr.ico](../../release/icons/wotr.ico): Windows, 16/24/32/48/64/128/256 px.
- [wotr.icns](../../release/icons/wotr.icns): macOS standard and Retina sizes
  through 1024 px.
- [WotrAppIcon.png](../../unity/Wotr.Client/Assets/Wotr/Branding/WotrAppIcon.png):
  the imported texture referenced by Unity's default application icon setting.

The default Unity icon is shared by both desktop targets. The Windows launcher
packager already retains the Unity executable's icon for its launcher and
Setup.exe. The native files are also available directly for release tooling.
Application identifiers, product names and saved-data locations are unchanged.

## Regenerate format variants

On macOS, run `python3 scripts/export_app_icons.py`. It uses the system's
`sips` and `iconutil` for deterministic size conversion and wraps the resized
PNGs in a Windows ICO directory. It does not invoke an image generation API.
Keep alpha and the complete app-icon plate; do not crop away the ring.
The master remains untouched. Review small sizes before replacing the artwork.

## First icon's generation prompt (v1)

Tool: built-in `image_gen.imagegen`; transparent_background: true.

```text
Use case: logo-brand. Asset type: final desktop application icon for War of the Ring Online, shared across macOS and Windows. Create one polished square 1024x1024 icon, not a mockup or a sheet of variants. Subject: one bold, thick, beautifully crafted aged-gold ring, seen almost front-on with a subtle three-dimensional tilt, centered on a warm charcoal black rounded-square plate with a very restrained fine stone/paper texture. The ring is the unmistakable main silhouette and occupies about 65 percent of the square, with a large dark open center. A faint ember-red inner reflection suggests dark epic fantasy; keep this subtle, not fire or flames. Use the project's sober charcoal and antique-gold visual direction. Lighting: controlled warm highlights on the metal, readable shadows, premium game icon finish, clean and striking at 16 to 32 pixels. Composition: one centered emblem, generous simple shapes, no scene, no characters, no extra symbols, no typography, no letters, no visible inscription, no title, no watermark, no border decoration. The rounded-square plate occupies about 90 percent of the canvas with clean antialiased edges; genuinely transparent pixels outside the plate. Deliver only the isolated finished icon on transparency.
```
