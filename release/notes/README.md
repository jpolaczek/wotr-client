<a id="notatki-wydań"></a>
# Release Notes

Each release has a `<version>.md` file (e.g. `0.9.0.md`) with a description of changes in English, 1–4000 characters after cutting off white characters. The player sees it in the update offer; the same text goes to the signed release manifest and to draft GitHub Release. Without the `release.yml` file, he refuses to release before the build (`python3 scripts/release.py check-tag v<version>`).
