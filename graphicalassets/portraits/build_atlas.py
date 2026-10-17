#!/usr/bin/env python3
"""Author the UI-120 portrait atlas as SVG: 5x4 cells of 200x200 px, one medallion bust per named character."""
from pathlib import Path

HERE = Path(__file__).resolve().parent
CELL = 200
COLS, ROWS = 5, 4
ORDER = ["gimli", "boromir", "legolas", "gandalf-grey", "gandalf-white",
         "strider", "aragorn", "pippin", "merry", "gollum",
         "smeagol", "ring-bearers", "elrond", "galadriel", "balrog",
         "gothmog", "saruman", "mouth-of-sauron", "witch-king", "spare"]

SKIN = "#e8c39a"; SKIN_SHADE = "#c99a6a"; PALE = "#d9d6c4"; ORC = "#6f7a5a"; INK = "#1c1510"


def vignette(name, inner, outer):
    return (f'<radialGradient id="v-{name}" cx="0.5" cy="0.42" r="0.55">'
            f'<stop offset="0" stop-color="{inner}"/><stop offset="0.72" stop-color="{outer}"/>'
            f'<stop offset="1" stop-color="{outer}" stop-opacity="0"/></radialGradient>')


def shoulders(cloth, dark, width=1.0):
    w = 72 * width
    return (f'<path d="M{100-w} 196 C{100-w} 150 {100-w*0.55} 132 {100-14} 128 L100 134 L{100+14} 128 '
            f'C{100+w*0.55} 132 {100+w} 150 {100+w} 196 Z" fill="{cloth}" stroke="{dark}" stroke-width="2"/>')


def neck(skin, shade):
    return f'<path d="M86 118 L86 136 Q100 146 114 136 L114 118 Z" fill="{shade}"/><rect x="88" y="112" width="24" height="22" fill="{skin}"/>'


def head(skin, shade, rx=33, ry=39, cy=86):
    return (f'<ellipse cx="100" cy="{cy}" rx="{rx}" ry="{ry}" fill="{skin}" stroke="{shade}" stroke-width="1.5"/>'
            f'<ellipse cx="88" cy="{cy-6}" rx="{rx*0.45}" ry="{ry*0.35}" fill="#fff" opacity="0.10"/>')


def eyes(color=INK, cy=84, dx=12, r=2.6):
    return f'<circle cx="{100-dx}" cy="{cy}" r="{r}" fill="{color}"/><circle cx="{100+dx}" cy="{cy}" r="{r}" fill="{color}"/>'


def beard(color, top=96, width=36, length=44, dark="#3a2a16"):
    return (f'<path d="M{100-width} {top} Q{100-width-6} {top+length*0.6} {100-width*0.6} {top+length} '
            f'Q100 {top+length+14} {100+width*0.6} {top+length} Q{100+width+6} {top+length*0.6} {100+width} {top} '
            f'Q100 {top+10} {100-width} {top} Z" fill="{color}" stroke="{dark}" stroke-width="1.5"/>')


def hair_long(color, dark, cy=86, drop=70, width=40):
    return (f'<path d="M{100-width} {cy-10} Q{100-width-4} {cy+drop} {100-width+14} {cy+drop+8} L{100-width+14} {cy+20} '
            f'Q100 {cy-50} {100+width-14} {cy+20} L{100+width-14} {cy+drop+8} Q{100+width+4} {cy+drop} {100+width} {cy-10} '
            f'Q100 {cy-58} {100-width} {cy-10} Z" fill="{color}" stroke="{dark}" stroke-width="1.5"/>')


def hair_cap(color, dark, cy=86, rx=35, ry=24, curly=False):
    base = f'<ellipse cx="100" cy="{cy-18}" rx="{rx}" ry="{ry}" fill="{color}" stroke="{dark}" stroke-width="1.5"/>'
    if curly:
        base += ''.join(f'<circle cx="{100-rx+6+i*(2*rx-12)/5}" cy="{cy-30+ (4 if i%2 else 0)}" r="7" fill="{color}" stroke="{dark}" stroke-width="1.2"/>' for i in range(6))
    return base


def hood(color, dark, cy=86, open_face=True, face=None):
    inner = f'<ellipse cx="100" cy="{cy+2}" rx="24" ry="30" fill="{face or "#0d0a08"}"/>' if not open_face else ''
    return (f'<path d="M100 {cy-56} C60 {cy-46} 50 {cy+10} 58 {cy+50} L58 140 Q100 150 142 140 L142 {cy+50} '
            f'C150 {cy+10} 140 {cy-46} 100 {cy-56} Z" fill="{color}" stroke="{dark}" stroke-width="2"/>'
            f'<path d="M100 {cy-44} C74 {cy-36} 68 {cy+6} 74 {cy+42} Q100 {cy+52} 126 {cy+42} C132 {cy+6} 126 {cy-36} 100 {cy-44} Z" '
            f'fill="{dark}" opacity="0.55"/>' + inner)


def pointed_hat(color, dark, cy=86):
    return (f'<path d="M100 {cy-96} L126 {cy-32} L74 {cy-32} Z" fill="{color}" stroke="{dark}" stroke-width="2"/>'
            f'<ellipse cx="100" cy="{cy-30}" rx="58" ry="12" fill="{color}" stroke="{dark}" stroke-width="2"/>'
            f'<path d="M78 {cy-32} Q100 {cy-24} 122 {cy-32}" fill="none" stroke="{dark}" stroke-width="1.5"/>')


def crown(gilt="#e2bd62", edge="#6a4c1d", cy=86, winged=False):
    pts = ''.join(f'L{66+i*17} {cy-56+(0 if i%2 else 12)} ' for i in range(5))
    svg = f'<path d="M66 {cy-32} {pts}L134 {cy-32} Z" fill="{gilt}" stroke="{edge}" stroke-width="1.6"/>'
    if winged:
        svg += (f'<path d="M66 {cy-40} Q48 {cy-52} 52 {cy-74} Q62 {cy-62} 70 {cy-48} Z" fill="#d9dde2" stroke="{edge}" stroke-width="1"/>'
                f'<path d="M134 {cy-40} Q152 {cy-52} 148 {cy-74} Q138 {cy-62} 130 {cy-48} Z" fill="#d9dde2" stroke="{edge}" stroke-width="1"/>')
    return svg


def helm(color, dark, cy=86, nose=True):
    svg = (f'<path d="M64 {cy-6} Q64 {cy-56} 100 {cy-56} Q136 {cy-56} 136 {cy-6} L136 {cy} L64 {cy} Z" fill="{color}" stroke="{dark}" stroke-width="2"/>'
           f'<rect x="60" y="{cy-6}" width="80" height="8" rx="2" fill="{dark}"/>')
    if nose:
        svg += f'<rect x="96" y="{cy-4}" width="8" height="26" rx="2" fill="{color}" stroke="{dark}" stroke-width="1.5"/>'
    return svg


def flames(cx=100, cy=30, n=7, color1="#ff9a2e", color2="#ffd27a"):
    out = ''
    for i in range(n):
        x = cx - 54 + i * 18
        h = 36 + (i % 3) * 14
        out += (f'<path d="M{x} {cy+40} Q{x-10} {cy+10} {x} {cy+40-h} Q{x+10} {cy+10} {x} {cy+40} Z" fill="{color1}" opacity="0.95"/>'
                f'<path d="M{x} {cy+40} Q{x-5} {cy+20} {x} {cy+46-h*0.65} Q{x+5} {cy+20} {x} {cy+40} Z" fill="{color2}" opacity="0.9"/>')
    return out


def cell(name):
    g = []
    if name == "gimli":
        g += [vignette(name, "#7a3a2a", "#2a1510"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders("#7a2d22", "#3a1510", 1.1), neck(SKIN, SKIN_SHADE), head(SKIN, SKIN_SHADE, 36, 36, 90),
              beard("#8a4a1e", 100, 38, 60, "#4a2510"), helm("#8c9096", "#3c3f45", 90, nose=True), eyes(cy=88)]
    elif name == "boromir":
        g += [vignette(name, "#5a3b2a", "#1d1410"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders("#6b1f1a", "#2e0d0b"), neck(SKIN, SKIN_SHADE), hair_long("#5a3a1e", "#2e1c0c", 86, 56, 38),
              head(SKIN, SKIN_SHADE), beard("#5a3a1e", 102, 24, 22, "#2e1c0c"), eyes(),
              '<path d="M60 150 Q80 120 128 150" fill="none" stroke="#3a2412" stroke-width="4"/>'
              '<path d="M118 134 Q150 128 150 154 Q150 170 134 166 Q140 150 118 148 Z" fill="#e8dcc0" stroke="#6a4c1d" stroke-width="2"/>']
    elif name == "legolas":
        g += [vignette(name, "#3f5a36", "#15200f"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              '<path d="M150 40 Q178 100 150 160" fill="none" stroke="#6a4c1d" stroke-width="4"/><path d="M150 40 L150 160" stroke="#d9d6c4" stroke-width="1.5"/>',
              shoulders("#4a6a3a", "#1f2f18"), neck(SKIN, SKIN_SHADE), hair_long("#e8d48a", "#a88a3a", 86, 76, 40),
              head(SKIN, SKIN_SHADE, 31, 38), eyes(),
              '<path d="M66 82 L58 72 L68 90 Z" fill="#e8c39a" stroke="#c99a6a" stroke-width="1"/><path d="M134 82 L142 72 L132 90 Z" fill="#e8c39a" stroke="#c99a6a" stroke-width="1"/>']
    elif name in ("gandalf-grey", "gandalf-white"):
        white = name == "gandalf-white"
        robe, dark, hair = ("#e6e6e0", "#8a8a84", "#f4f4f0") if white else ("#6b6b66", "#2e2e2b", "#c9c9c3")
        g += [vignette(name, "#bfc7d6" if white else "#4a4f5a", "#4a4f5a" if white else "#16181c"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>']
        if white:
            g.append('<circle cx="100" cy="96" r="78" fill="#fff" opacity="0.18"/>')
        g += [shoulders(robe, dark), neck(SKIN, SKIN_SHADE), hair_long(hair, dark, 86, 60, 40), head(SKIN, SKIN_SHADE),
              beard(hair, 98, 32, 72, dark), eyes(), pointed_hat(robe, dark)]
    elif name == "strider":
        g += [vignette(name, "#2f4a2c", "#0e170c"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders("#2f3f2a", "#101a0e", 1.05), neck(SKIN, SKIN_SHADE), head(SKIN, SKIN_SHADE),
              beard("#4a3a2a", 104, 22, 16, "#2a1c10"), eyes(cy=88), hood("#3b5636", "#16241a", 86, open_face=True),
              head("#d8b48c", "#b8906a", 26, 32, 92), eyes(cy=92, dx=10, r=2.2)]
    elif name == "aragorn":
        g += [vignette(name, "#3a4a6a", "#101624"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders("#263a5e", "#0f1a30"), '<path d="M30 196 Q100 170 170 196 L170 180 Q100 150 30 180 Z" fill="#c9ccd3" stroke="#6b7078" stroke-width="2"/>',
              neck(SKIN, SKIN_SHADE), hair_long("#3a2a1e", "#1c130b", 86, 56, 38), head(SKIN, SKIN_SHADE),
              beard("#3a2a1e", 104, 22, 16, "#1c130b"), eyes(), crown(cy=86, winged=True)]
    elif name in ("pippin", "merry"):
        hair, dark, scarf = ("#7a4a22", "#3e2410", "#4a7a3a") if name == "pippin" else ("#d9b45a", "#8a6a2a", "#d9a62a")
        g += [vignette(name, "#6a5a3a", "#241c10"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders("#5a4a30", "#2a2214", 0.9), f'<path d="M62 150 Q100 170 138 150 L138 166 Q100 186 62 166 Z" fill="{scarf}" stroke="#2a2214" stroke-width="2"/>',
              neck(SKIN, SKIN_SHADE), head(SKIN, SKIN_SHADE, 34, 36, 98), hair_cap(hair, dark, 98, 36, 26, curly=True), eyes(cy=96),
              '<path d="M90 110 Q100 118 110 110" fill="none" stroke="#8a5a3a" stroke-width="2" stroke-linecap="round"/>']
    elif name in ("gollum", "smeagol"):
        soft = name == "smeagol"
        skin, shade = ("#bfc9a8", "#7f8a68") if soft else ("#b8b4a0", "#767260")
        g += [vignette(name, "#4a5a4a" if soft else "#3a3a40", "#0f1a12" if soft else "#121216"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders(skin, shade, 0.75), neck(skin, shade), head(skin, shade, 30, 38, 90),
              ''.join(f'<path d="M{80+i*8} 54 q{-4+i} -14 {2} -22" fill="none" stroke="#6a6a5a" stroke-width="1.6" stroke-linecap="round"/>' for i in range(6)),
              f'<circle cx="86" cy="86" r="7" fill="#d9e8ef"/><circle cx="114" cy="86" r="7" fill="#d9e8ef"/><circle cx="86" cy="87" r="3" fill="#2a4a5a"/><circle cx="114" cy="87" r="3" fill="#2a4a5a"/>',
              ('<path d="M88 110 Q100 120 112 110" fill="none" stroke="#5a6a4a" stroke-width="2" stroke-linecap="round"/>' if soft
               else '<path d="M88 114 Q100 106 112 114" fill="none" stroke="#5a5a4a" stroke-width="2" stroke-linecap="round"/>')]
    elif name == "ring-bearers":
        g += [vignette(name, "#5a4a3a", "#1c1610"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>']
        for cx, cloth, dark in ((72, "#4a5a3a", "#1f2a18"), (128, "#6a5a3a", "#2e2616")):
            g += [f'<path d="M{cx-34} 196 C{cx-34} 150 {cx-18} 132 {cx} 128 C{cx+18} 132 {cx+34} 150 {cx+34} 196 Z" fill="{cloth}" stroke="{dark}" stroke-width="2"/>',
                  f'<ellipse cx="{cx}" cy="104" rx="22" ry="25" fill="{SKIN}" stroke="{SKIN_SHADE}" stroke-width="1.5"/>',
                  f'<path d="M{cx} 60 C{cx-30} 66 {cx-30} 100 {cx-24} 130 Q{cx} 136 {cx+24} 130 C{cx+30} 100 {cx+30} 66 {cx} 60 Z" fill="{cloth}" stroke="{dark}" stroke-width="2"/>',
                  f'<ellipse cx="{cx}" cy="106" rx="16" ry="20" fill="{SKIN}"/>', eyes(cy=104, dx=7, r=2).replace('cx="93"', f'cx="{cx-7}"').replace('cx="107"', f'cx="{cx+7}"')]
        g.append('<path d="M60 142 Q72 150 84 142" fill="none" stroke="#e2bd62" stroke-width="2"/><circle cx="72" cy="150" r="5" fill="none" stroke="#e2bd62" stroke-width="3"/>')
    elif name == "elrond":
        g += [vignette(name, "#4a3a6a", "#161024"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders("#4a2f6a", "#1d1030"), neck(SKIN, SKIN_SHADE), hair_long("#2a1e14", "#120c08", 86, 76, 40), head(SKIN, SKIN_SHADE, 30, 38), eyes(),
              '<path d="M68 70 Q100 56 132 70" fill="none" stroke="#e2bd62" stroke-width="3"/><circle cx="100" cy="62" r="4" fill="#e2bd62" stroke="#6a4c1d" stroke-width="1"/>',
              '<path d="M68 82 L60 72 L70 90 Z" fill="#e8c39a" stroke="#c99a6a" stroke-width="1"/><path d="M132 82 L140 72 L130 90 Z" fill="#e8c39a" stroke="#c99a6a" stroke-width="1"/>']
    elif name == "galadriel":
        g += [vignette(name, "#e8e2c8", "#5a5a50"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              '<circle cx="100" cy="96" r="80" fill="#fff" opacity="0.16"/>',
              shoulders("#f1ede0", "#a8a496"), neck("#f0d8bc", "#d4b08c"), hair_long("#f0d890", "#c0a450", 86, 84, 42), head("#f0d8bc", "#d4b08c", 30, 38), eyes("#3a4a6a"),
              '<path d="M70 66 Q100 56 130 66" fill="none" stroke="#f4f0e0" stroke-width="2.5"/>']
    elif name == "balrog":
        g += [vignette(name, "#7a2a10", "#1a0a04"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              '<circle cx="100" cy="96" r="76" fill="#ff7a1a" opacity="0.22"/>', flames(100, 24),
              shoulders("#1d0c08", "#000", 1.15), neck("#2a1410", "#0d0604"), head("#2a1410", "#0d0604", 36, 40, 92),
              '<path d="M70 70 Q50 40 56 18 Q74 40 82 66 Z" fill="#3a2a22" stroke="#120a06" stroke-width="2"/><path d="M130 70 Q150 40 144 18 Q126 40 118 66 Z" fill="#3a2a22" stroke="#120a06" stroke-width="2"/>',
              eyes("#ffd27a", cy=90, dx=13, r=4), '<circle cx="87" cy="90" r="2" fill="#ff5a1a"/><circle cx="113" cy="90" r="2" fill="#ff5a1a"/>']
    elif name == "gothmog":
        g += [vignette(name, "#3a3a2a", "#0e0e0a"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders("#1e1e1e", "#000", 1.2), '<path d="M28 150 L44 118 L60 150 Z M172 150 L156 118 L140 150 Z" fill="#2b2b2b" stroke="#000" stroke-width="2"/>',
              neck(ORC, "#4a5240"), head(ORC, "#4a5240", 36, 36, 92), eyes("#e8d060", cy=88, dx=13),
              '<path d="M80 110 Q100 100 120 110" fill="none" stroke="#2a3020" stroke-width="3"/><path d="M84 110 L86 118 M98 106 L98 116 M114 110 L112 118" stroke="#f0ead8" stroke-width="2.5"/>',
              '<path d="M70 72 L120 100" stroke="#8a2a20" stroke-width="3" stroke-linecap="round"/>']
    elif name == "saruman":
        g += [vignette(name, "#5a5a6a", "#18181e"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              '<rect x="150" y="20" width="6" height="170" rx="3" fill="#2b2b2b"/><circle cx="153" cy="22" r="7" fill="#d9d6c4" stroke="#6a4c1d" stroke-width="2"/>',
              shoulders("#e8e8e4", "#8a8a8a"),
              ''.join(f'<path d="M{34+i*10} 196 Q{36+i*10} 160 {44+i*10} 140" fill="none" stroke="{c}" stroke-width="4" opacity="0.8"/>' for i, c in enumerate(["#c84a3a", "#e2bd62", "#4a8a5a", "#3a5aa8", "#9a4ab8", "#e8a24a"])),
              neck("#e8d8c4", "#c8b09a"), hair_long("#f4f4f0", "#9a9a94", 86, 70, 40), head("#e8d8c4", "#c8b09a"), beard("#f4f4f0", 98, 30, 76, "#9a9a94"), eyes("#1c1510")]
    elif name == "mouth-of-sauron":
        g += [vignette(name, "#2a2a30", "#08080a"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders("#121214", "#000", 1.15), neck(PALE, "#9a9684"), head(PALE, "#9a9684", 32, 40, 92),
              '<path d="M62 88 Q62 26 100 26 Q138 26 138 88 L138 96 L62 96 Z" fill="#1a1a1e" stroke="#000" stroke-width="2"/><rect x="60" y="86" width="80" height="12" fill="#000"/>',
              '<path d="M76 112 Q100 128 124 112 Q100 120 76 112 Z" fill="#5a1410" stroke="#2a0a08" stroke-width="1"/><path d="M80 114 L82 120 M90 116 L92 123 M100 118 L100 125 M110 116 L108 123 M120 114 L118 120" stroke="#e8e0c8" stroke-width="2"/>']
    elif name == "witch-king":
        g += [vignette(name, "#2a2a36", "#06060a"), f'<circle cx="100" cy="100" r="88" fill="url(#v-{name})"/>',
              shoulders("#0d0d12", "#000", 1.15), hood("#15151c", "#000", 86, open_face=False, face="#020204"),
              '<path d="M64 60 L70 24 L84 52 L100 14 L116 52 L130 24 L136 60 Z" fill="#5a5f66" stroke="#1c1f24" stroke-width="2"/><rect x="62" y="58" width="76" height="8" fill="#3c3f45" stroke="#1c1f24" stroke-width="1.5"/>']
    else:
        return ''
    return ''.join(g)


def build():
    defs, bodies = [], []
    for index, name in enumerate(ORDER):
        row, col = divmod(index, COLS)
        body = cell(name)
        if not body:
            continue
        # vignette gradients are declared inline inside the cell; wrap in a translated group with a clip to the cell.
        bodies.append(f'<g transform="translate({col*CELL} {row*CELL})"><defs>{body[:body.index("/>")+2] if body.startswith("<radialGradient") else ""}</defs>'
                      f'<clipPath id="c-{name}"><circle cx="100" cy="100" r="92"/></clipPath><g clip-path="url(#c-{name})">'
                      f'{body[body.index("/>")+2:] if body.startswith("<radialGradient") else body}</g></g>')
    svg = (f'<svg xmlns="http://www.w3.org/2000/svg" width="{COLS*CELL}" height="{ROWS*CELL}" viewBox="0 0 {COLS*CELL} {ROWS*CELL}">\n'
           f'  <!-- UI-120 portrait atlas: {COLS}x{ROWS} cells of {CELL} px, order {", ".join(ORDER)}. Hand-authored vector busts; replaceable. -->\n'
           + '\n'.join(bodies) + '\n</svg>\n')
    (HERE / "src" / "portraits-v1.svg").write_text(svg, encoding="utf-8")
    return svg


if __name__ == "__main__":
    build()
    print("src/portraits-v1.svg written")
