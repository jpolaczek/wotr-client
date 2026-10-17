#!/usr/bin/env python3
"""UI-138: inventory of the client's buttons.

Every place that creates a button (BoardScreen.Button, IconButton.Create, new Button) is listed with its kind:
icon (IconButton with an action icon and a tooltip), text (a label of words, framed by frames/button), art (an
empty label: the element draws its own artwork, e.g. dice, cards, figures) or glyph (a label of one or two symbols
such as "×" or "›"), which UI-138 replaces with icons. --check also fails when a glyph reaches a button another
way: a glyph literal as the first argument of BoardScreen.Button(...) (e.g. a ternary label), in a
new Button { text = … } initializer, or assigned to the text of a variable declared as a button. Art whose glyph
shows a state (the Fellowship marker: hidden ◇, revealed ◆) is listed in STATE_GLYPHS. A label passed through a helper or
taken from data is out of reach; BoardChromeLayoutTests checks the buttons on screen.
Usage: button_inventory.py [--write PATH] [--check]
"""
import argparse
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RUNTIME = ROOT / "unity/Wotr.Client/Assets/Wotr/Runtime"
CREATE = re.compile(r'(IconButton\.Create\(\s*"(?P<icon>[^"]+)"|BoardScreen\.Button\(\s*(?P<label>"(?:[^"\\]|\\.)*"|[^,]+)(?P<concat>\s*\+)?|new Button\b)')


def kind(match):
    if match.group("icon"):
        return "icon", match.group("icon")
    label = match.group("label")
    if label is None:
        return "art", "(new Button)"
    if not label.startswith('"'):
        return "text", label.strip()
    text = label[1:-1]
    if match.group("concat"):
        return "text", text + "…"
    if text == "":
        return "art", '""'
    if re.search(r"[A-Za-z]{2,}", text):
        return "text", text
    return ("glyph" if len(text.strip()) <= 2 else "text"), text


GLYPHS = "×‹›✓◇◆↻⚔☑☐+−⇥"
GLYPH_LITERAL = re.compile(r'"([' + GLYPHS + r'\s]{1,3})"')
BUTTON_VARIABLE = re.compile(r'(?:\bButton\s+|\bvar\s+)(\w+)\s*(?:=\s*(?:BoardScreen\.Button|IconButton\.Create|new Button)\b|[;,)])')
TEXT_ASSIGNMENT = re.compile(r'\b(\w+)\.text\s*=\s*([^;]*);')
INITIALIZER = re.compile(r'new Button\b[^;{]*\{[^}]*\btext\s*=\s*"([' + GLYPHS + r'\s]{1,3})"')
# Art buttons whose glyph is a state of the artwork, not an action symbol (path relative to Runtime, variable).
STATE_GLYPHS = {
    ("Map/MapFellowshipMarker.cs", "marker"),
}


def first_argument(text, start):
    """The first argument of the call whose opening parenthesis is at index start."""
    depth = 0
    quoted = False
    for index in range(start + 1, len(text)):
        char = text[index]
        if char == '"' and text[index - 1] != "\\":
            quoted = not quoted
        elif quoted:
            continue
        elif char in "([{":
            depth += 1
        elif char in ")]}" and depth > 0:
            depth -= 1
        elif char in ",)" and depth == 0:
            return text[start + 1:index]
    return text[start + 1:]


def stray_glyphs():
    found = []
    for path in sorted(RUNTIME.rglob("*.cs")):
        text = path.read_text(encoding="utf-8")
        relative = path.relative_to(RUNTIME).as_posix()
        line = lambda index: text.count("\n", 0, index) + 1
        for match in re.finditer(r"BoardScreen\.Button\(", text):
            label = first_argument(text, match.end() - 1)
            # A glyph joined to words ("✓ " + step) is a text label with a state mark, as kind() counts it.
            literal = None if re.search(r'"\s*\+|\+\s*"|\)\s*\+', label) else GLYPH_LITERAL.search(label)
            if literal and literal.group(1).strip():
                found.append((path.relative_to(ROOT).as_posix(), line(match.start()), literal.group(1)))
        for match in INITIALIZER.finditer(text):
            if match.group(1).strip():
                found.append((path.relative_to(ROOT).as_posix(), line(match.start()), match.group(1)))
        buttons = {m.group(1) for m in BUTTON_VARIABLE.finditer(text)}
        for match in TEXT_ASSIGNMENT.finditer(text):
            name, value = match.group(1), match.group(2)
            if name not in buttons or (relative, name) in STATE_GLYPHS or re.search(r'"[^"]*[A-Za-z0-9]', value):
                continue
            literal = GLYPH_LITERAL.search(value)
            if literal and literal.group(1).strip():
                found.append((path.relative_to(ROOT).as_posix(), line(match.start()), literal.group(1)))
    return found


def inventory():
    rows = []
    for path in sorted(RUNTIME.rglob("*.cs")):
        for number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), 1):
            for match in CREATE.finditer(line):
                what, label = kind(match)
                rows.append((path.relative_to(ROOT).as_posix(), number, what, label))
    return rows


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--write", help="Markdown file for the table")
    parser.add_argument("--check", action="store_true", help="fail when a glyph button remains")
    args = parser.parse_args()
    rows = inventory()
    counts = {k: sum(1 for r in rows if r[2] == k) for k in ("icon", "text", "art", "glyph")}
    if args.write:
        lines = ["# Client button inventory — UI-138", "",
                 "Generated by `python3 scripts/button_inventory.py --write docs/ui/BUTTONS.md`.", "",
                 "Kinds: `icon` (action icon with a tooltip), `text` (word label on a `frames/button` frame),",
                 "`art` (control with its own artwork: dice, cards, figures, tokens), `glyph` (symbol instead of an icon; none after UI-138).", "",
                 "| Kind | Count |", "| --- | --- |"]
        lines += [f"| {k} | {v} |" for k, v in counts.items()]
        lines += ["", "| File | Line | Kind | Label or icon |", "| --- | --- | --- | --- |"]
        lines += [f"| `{p}` | {n} | {k} | {l.replace('|', '¦')} |" for p, n, k, l in rows]
        Path(ROOT / args.write).write_text("\n".join(lines) + "\n", encoding="utf-8")
    print(" ".join(f"{k}={v}" for k, v in counts.items()), f"total={len(rows)}")
    stray = stray_glyphs() if args.check else []
    if args.check and (counts["glyph"] or stray):
        for p, n, k, l in rows:
            if k == "glyph":
                print(f"glyph button: {p}:{n} {l}", file=sys.stderr)
        for p, n, l in stray:
            print(f"glyph on a button: {p}:{n} {l}", file=sys.stderr)
        return 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
