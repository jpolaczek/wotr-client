# Avatars — ART-09

Ten square avatars for the client and the website, by the identifiers of AVATAR-01 A1: `eye`, `ring`, `gondor`, `rohan`,
`elves`, `dwarves`, `north`, `sauron`, `isengard`, `southrons`.

`build.py` draws them, so they stay replaceable (R08): nothing here is drawn by hand. Each regular figure is cropped to its
head and shoulders on a framed disc, and the Eye and the Ring stand on the same disc, so the ten read as one set at 16 px and
at 128 px. To change one, change its source or its crop in `REGULARS`, and run the script again.

```
python3 graphicalassets/avatars/build.py --site <wotr-server>/infra/site/assets/avatars
python3 graphicalassets/avatars/build.py --check --site <wotr-server>/infra/site/assets/avatars
python3 graphicalassets/avatars/build.py --preview contact-sheet.png
```

It needs Pillow, and Chrome for the two SVG sources (`graphicalassets/ui/render.py`).

## Sources

- The eight soldiers: `graphicalassets/figures/v3/raw/<nation>-regular.png`, the project's generated figure art, recorded in
  `docs/sources/ART-FIGURES.json` on the server.
- The Eye: `graphicalassets/ui/src/dice/faces/eye.svg`. The Ring: `graphicalassets/ui/src/icons/ring.svg`. Both are drawn
  for this project.

Nothing here is scanned from the physical game. `manifest.json` records each output's source and SHA-256, and `--check` fails
if an avatar is missing, or stale against its source.
