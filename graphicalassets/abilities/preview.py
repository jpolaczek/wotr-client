"""UI-439: the contact sheet preview.png. Above, every ability icon of the kit (Art/UI/abilities) at 4x its display size
(its PNG's pixels enlarged), at its display size (28 points) and at the roster's (18), with its title and id; below,
every character card's icons side by side at 28 points, so the icons one character carries can be compared.

Usage: python3 preview.py
"""
import json
from pathlib import Path

from PIL import Image, ImageDraw, ImageFont

from key import OUT, POINTS, ROOT, SIZE

ART = Path(__file__).resolve().parent
TEXT = ROOT / "unity/Wotr.Client/Assets/Wotr/Resources/character-ability-text.json"
# The Kings' Dark Chieftains read not-minion-for-gandalf-white as their own ability (generate.py).
CHIEFTAINS = {"kings.character.black-serpent", "kings.character.shadow-of-mirkwood", "kings.character.ugluk"}
PLATE = (36, 29, 25, 255)
CELL = (200, 178)
COLUMNS = 8
INK = (240, 222, 180, 255)
MUTED = (170, 150, 120, 255)
# A system font with accented letters (Udûn, Lórien); Pillow's own font otherwise.
FONTS = ["/System/Library/Fonts/Supplemental/Arial.ttf", "DejaVuSans.ttf"]


def font(size):
    for name in FONTS:
        try:
            return ImageFont.truetype(name, size)
        except OSError:
            continue
    return ImageFont.load_default(size=size)


def icon_key(card, ability):
    if ability == "not-minion-for-gandalf-white" and card in CHIEFTAINS:
        return "dark-chieftain"
    return ability


def catalogue():
    cards = json.loads(TEXT.read_text(encoding="utf-8"))["cards"]
    titles, rows = {}, []
    for card in cards:
        keys = []
        for ability in card["abilities"]:
            key = icon_key(card["id"], ability["id"])
            titles.setdefault(key, [])
            if ability["title"] not in titles[key]:
                titles[key].append(ability["title"])
            keys.append(key)
        rows.append((card["id"], keys))
    return titles, rows


def draw_cell(sheet, draw, fonts, origin, key, titles):
    x, y = origin
    icon = Image.open(OUT / (key + ".png")).convert("RGBA")
    sheet.alpha_composite(icon.resize((SIZE * 2, SIZE * 2), Image.NEAREST), (x + 8, y + 8))
    sheet.alpha_composite(icon.resize((POINTS, POINTS), Image.LANCZOS), (x + 136, y + 30))
    sheet.alpha_composite(icon.resize((18, 18), Image.LANCZOS), (x + 141, y + 76))
    draw.text((x + 8, y + 126), " / ".join(titles)[:30], font=fonts[0], fill=INK)
    draw.text((x + 8, y + 144), key[:32], font=fonts[1], fill=MUTED)


def main():
    titles, rows = catalogue()
    keys = sorted(titles)
    missing = [key for key in keys if not (OUT / (key + ".png")).exists()]
    keys = [key for key in keys if key not in missing]
    grid_rows = (len(keys) + COLUMNS - 1) // COLUMNS
    card_rows = (len(rows) + 1) // 2
    width = COLUMNS * CELL[0]
    height = grid_rows * CELL[1] + 40 + card_rows * 40 + 20
    sheet = Image.new("RGBA", (width, height), PLATE)
    draw = ImageDraw.Draw(sheet)
    fonts = (font(13), font(11))
    for index, key in enumerate(keys):
        origin = ((index % COLUMNS) * CELL[0], (index // COLUMNS) * CELL[1])
        draw_cell(sheet, draw, fonts, origin, key, titles[key])
    top = grid_rows * CELL[1] + 20
    draw.line([(0, top - 10), (width, top - 10)], fill=MUTED, width=1)
    half = width // 2
    for index, (card, card_keys) in enumerate(rows):
        x = (index % 2) * half + 8
        y = top + (index // 2) * 40
        draw.text((x, y + 8), card.replace(".character.", " · "), font=fonts[0], fill=INK)
        for slot, key in enumerate(card_keys):
            if key in missing:
                continue
            icon = Image.open(OUT / (key + ".png")).convert("RGBA").resize((POINTS, POINTS), Image.LANCZOS)
            sheet.alpha_composite(icon, (x + 330 + slot * 36, y + 2))
    sheet.convert("RGB").save(ART / "preview.png", optimize=True)
    print(json.dumps({"icons": len(keys), "missing": missing, "cards": len(rows), "size": [width, height]}))


if __name__ == "__main__":
    main()
