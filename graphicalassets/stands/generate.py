"""Generate the tabletop-figure bases and token pucks with the owner's Codex CLI (UI-400, UI-406)."""
import hashlib
import json
import subprocess
import sys
from pathlib import Path

ART = str(Path(__file__).resolve().parent) + "/"
STYLE = ("centered on a perfectly flat pure magenta (#FF00FF) background, seen from a three-quarter view from above (about 35 "
         "degrees), as for a painted resin tabletop miniature. Soft light from the upper left. Nothing standing on it, no figure, "
         "no cast shadow on the background, no text, no letters, no numbers, no logo, no border. Photorealistic painted-miniature "
         "style, crisp clean silhouette.")
FREE = "deep royal-blue enamel with a thin worn gold edge"
SHADOW = "deep blood-red and black enamel with a thin tarnished bronze edge"
ITEMS = {
    "stand-field-free": f"A single empty wide oval miniature-wargame base, low and wide: a smooth bevelled rim of {FREE}, the top "
                        "surface flat and covered with fine dark earth, a few small stones and two or three tufts of muted green grass.",
    "stand-field-shadow": f"A single empty wide oval miniature-wargame base, low and wide: a smooth bevelled rim of {SHADOW}, the top "
                          "surface flat and covered with dark ashen earth, cracked stones and a few tufts of dead brown grass.",
    "stand-garrison-free": f"A single empty wide oval miniature-wargame base shaped as a small fortress floor: its rim is a low "
                           f"crenellated wall of pale grey stone with battlements all around, trimmed below with {FREE}, the top "
                           "surface paved with flat grey flagstones.",
    "stand-garrison-shadow": f"A single empty wide oval miniature-wargame base shaped as a small fortress floor: its rim is a low "
                             f"crenellated wall of black jagged stone with battlements all around, trimmed below with {SHADOW}, the "
                             "top surface paved with dark scorched flagstones.",
    "stand-character-free": f"A single empty small round miniature-wargame base for one hero: a smooth bevelled rim of {FREE}, the top "
                            "surface flat with fine dark earth and one tuft of muted green grass.",
    "stand-character-shadow": f"A single empty small round miniature-wargame base for one villain: a smooth bevelled rim of {SHADOW}, "
                              "the top surface flat with dark ashen earth and a cracked stone.",
    "token-puck-free": f"A single thick round game token like a heavy enamelled coin lying flat on a table: the top face is smooth "
                       f"plain {FREE}, slightly domed with a raised gold rim, and its side band shows its thickness in darker blue "
                       "with a fine gold line. The top face is empty: no emblem, no symbol.",
    "token-puck-shadow": f"A single thick round game token like a heavy enamelled coin lying flat on a table: the top face is smooth "
                         f"plain {SHADOW}, slightly domed with a raised bronze rim, and its side band shows its thickness in "
                         "darker red-black with a fine bronze line. The top face is empty: no emblem, no symbol.",
    # UI-406: a garrison's token is the same puck walled with battlements, as its crenellated stand.
    "token-puck-garrison-free": f"A single thick round game token like a heavy enamelled coin lying flat on a table, crowned "
                                f"by a low ring of small crenellated battlements of pale grey stone standing all around its "
                                f"top rim: inside the battlements the top face is smooth plain {FREE}, and its side band "
                                "shows its thickness in darker blue with a fine gold line. The top face is empty: no emblem, "
                                "no symbol.",
    "token-puck-garrison-shadow": f"A single thick round game token like a heavy enamelled coin lying flat on a table, crowned "
                                  f"by a low ring of small crenellated battlements of black jagged stone standing all around "
                                  f"its top rim: inside the battlements the top face is smooth plain {SHADOW}, and its side "
                                  "band shows its thickness in darker red-black with a fine bronze line. The top face is "
                                  "empty: no emblem, no symbol.",
    # UI-406: a Corrupted king's marker is a small dark puck; the code lays the crown and the Eye on its top face.
    "puck-corrupted": "A single small thick round game token like a heavy coin lying flat on a table: the top face is smooth "
                      "plain blackened iron with a faint ash-grey sheen, slightly domed, inside a raised rim of dull "
                      "smouldering ember-orange metal, and its side band shows its thickness in black iron with a thin "
                      "ember-red line. The top face is empty: no emblem, no symbol.",
}
keys = sys.argv[1:] or list(ITEMS)
log = open(ART + "generations-codex.jsonl", "a")
for key in keys:
    prompt = ITEMS[key] + " " + STYLE
    out = ART + "raw/" + key + ".png"
    result = subprocess.run(["bash", ART + "codex_image.sh", prompt, out],
                            capture_output=True, text=True)
    print(key, result.stdout.strip()[-120:], flush=True)
    if result.returncode != 0:
        continue
    digest = hashlib.sha256(open(out, "rb").read()).hexdigest()
    log.write(json.dumps({"key": key, "tool": "codex built-in image_gen", "prompt": prompt, "sha256": digest}) + "\n")
    log.flush()
print("GENERATED")
