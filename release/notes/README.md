<a id="notatki-wydań"></a>
# Release Notes

Each release has a `<version>.md` file (e.g. `0.9.0.md`) with a description of changes in English, 1–4000 characters after cutting off white characters. The player sees it in the update offer; the same text goes to the signed release manifest and to draft GitHub Release.

DIST-15: the game site's What's new page (`/updates`, opened by `What's new` in the game) shows these notes to
players. Write them for players, short and plain: one line per change starting with `- `, such as
`- New: chat and challenges on the Open games screen.` or `- Fixed: the muster icons no longer jump back.`
A line without `- ` becomes a paragraph; no other Markdown is shown. Without the `release.yml` file, he refuses to release before the build (`python3 scripts/release.py check-tag v<version>`).
