"""Generate one icon per printed character ability with the owner's Codex CLI (UI-439).

Usage: python3 generate.py [--jobs N] [--pending | key ...]   (no keys: every key in ITEMS)

Each key is a printed ability id of character-ability-text.json (dark-chieftain: the Kings' Dark Chieftains' own form
of not-minion-for-gandalf-white). One Codex call per key, run N at once (codex_image.sh takes each image from its own
session folder); the original's SHA-256 and the prompt go to generations-codex.jsonl, and raw/ keeps the image at
768 pixels wide. Python: numpy and Pillow (scripts/requirements-map-canvas.txt).
"""
import argparse
import hashlib
import json
import subprocess
import tempfile
import threading
from concurrent.futures import ThreadPoolExecutor
from pathlib import Path

from PIL import Image

ART = Path(__file__).resolve().parent
RAW_WIDTH = 768
STYLE = ("Square 1:1 image. A single small game-interface icon, centred and filling most of the frame, on a perfectly "
         "flat pure magenta (#FF00FF) background. Bold, simple silhouette that still reads when shrunk to 20 pixels: "
         "one central motif, thick chunky shapes, no thin lines, no fine detail. Style: polished antique gold (gilt) "
         "with a thick dark brown-black ink outline around every shape, soft bevelled relief lit from the upper left, "
         "like an enamelled metal badge from a premium fantasy board game, with small areas of coloured enamel where "
         "described. Front view, flat icon, no scene, no background, no frame, no circle or medallion behind it, no "
         "cast shadow on the background, no text, no letters, no numbers, no logo. Do not use magenta, pink or purple "
         "anywhere in the icon itself. It is shown on a dark brown interface, so every dark part is edged and "
         "highlighted with bright gold. When the motif itself is a long object (a sword, staff, spear, arrow or rod), "
         "it lies diagonally from lower left to upper right, thick and broad, so the icon fills the square. Never add "
         "an object the description does not name.")

# The motif of every ability: what it does or whom it belongs to, one object each, all different (owner, UI-439).
ITEMS = {
    # Base game
    "combat-strength-plus-one": "A round deep royal-blue enamel shield with a gold rim and a single gold five-pointed "
                                "star in its centre, a gold sword standing upright behind it: a captain's arms.",
    "politics-dwarves-at-erebor": "A single tall lone mountain peak of grey-blue stone with gold-edged snowy slopes, a "
                                  "small gold war banner planted on its very summit.",
    "politics-gondor-at-city-or-stronghold": "A tall slender white stone tower with a pointed gold roof, a long black "
                                             "pennant streaming from its top.",
    "politics-elves-at-stronghold": "A hanging war standard of deep emerald-green enamel with a pointed lower edge, "
                                    "hung from a gold crossbar on a slender gold pole, a single gold leaf emblem on "
                                    "the cloth.",
    "guide-draw-after-event-die-card": "A single upright game card with a gold border and a dark blue back, a grey "
                                       "wizard's tall pointed hat resting on its top edge.",
    "may-transform-to-white": "A wizard's long staff held upright, its lower half weathered grey wood and its upper "
                              "half shining white, a radiant white star of light bursting at its tip.",
    "guide-hide-with-any-action-die": "A ranger's deep hood seen from the front, dark grey-green cloth with the face "
                                      "completely in black shadow, fastened at the throat with a gold star brooch.",
    "may-transform-to-aragorn": "The tall white winged crown of Gondor: a white and silver helm-crown with a "
                                "sea-bird's silver wing on each side and a row of small white gems, gold trim.",
    "guide-separate-to-reduce-hunt-one": "A small traveller in a dark green hooded cloak with a walking staff, "
                                         "striding away to the right, a faint red eye breaking apart behind him.",
    "return-after-fellowship-elimination-except-mordor": "An elven leaf-shaped brooch of green enamel veined with "
                                                         "silver on a gold stem, lying on a loop of cut rope.",
    "move-four-alone-or-with-one-hobbit": "The head and flowing mane of a galloping white horse in profile, "
                                          "silver-white with a gold outline, the mane streaming with speed.",
    "forfeit-leadership-to-cancel-nazgul-for-battle": "A black spiked iron crown of a Ringwraith cracking apart, "
                                                      "struck by a radiant white beam of light from above.",
    "enter-and-guide-when-no-companions": "The gaunt pale grey-green face of a hunched cave creature with two huge "
                                          "round pale eyes and a few lank strands of hair, peering forward.",
    "ignore-standard-numbered-reveal": "A great fiery red eye with a gold rim, shut tight: a heavy black eyelid "
                                       "closed over it, an eye that sees nothing.",
    "voluntary-reveal-to-reduce-hunt-one": "A rough dark grey boulder split open down the middle, bright golden "
                                           "light bursting out of the crack: a hiding place revealed.",
    "add-gollum-when-no-companions": "A thin pale grey-green bony hand with long fingers reaching up toward a "
                                     "single plain gold ring floating above it.",
    "cannot-leave-orthanc": "A tall black rock tower with four sharp horns at its top, a heavy iron chain wrapped "
                            "around its base.",
    "muster-each-isengard-settlement-or-upgrade-two": "A tall black wizard's staff topped with a glowing white orb, "
                                                      "three curved rings of pale-gold sound radiating from the orb.",
    "isengard-elites-have-leadership": "A black Uruk-hai battle helmet seen from the front, a white human hand "
                                       "painted on its face-plate.",
    "muster-as-army-once-per-turn": "A tall black iron helmet-mask with no eyes, only a huge wide grinning mouth of "
                                    "teeth, red runes etched on the black iron.",
    "entry-activates-all-free-peoples": "A dark hooded wraith in a black cloak wearing a spiked gold crown, "
                                        "screaming, three jagged pale shockwave rings bursting outward around it.",
    "draw-after-first-round-combat-card": "A single upright game card with a gold border engulfed in tall pale-green "
                                          "witch-fire, the flames dominating the icon.",
    # Lords of Middle-earth
    "vilya": "A gold ring set with a large deep-blue sapphire, sparkling, seen at a slight angle.",
    "household-of-elrond": "A tall elegant elven war helm of silver with a gold leaf crest and a long blue plume.",
    "combat-card-level-three": "A single upright game card with a gold border and a dark red back, three gold stars "
                               "stacked in a vertical row on its face.",
    "cannot-leave-rivendell": "A small elven house with a steep pointed arched roof, a blue waterfall falling "
                              "beside it, a heavy gold chain looped across its front.",
    "nenya": "A slender silver ring set with a brilliant white diamond stone that glitters like a star.",
    "valour-of-the-elven-people": "A tall green elven shield with a gold mallorn leaf emblem, an elven spear "
                                  "standing upright behind it.",
    "cannot-leave-lorien": "A great tree with a silver trunk and a crown of golden leaves, a heavy dark iron chain "
                           "wrapped around its trunk.",
    "narya": "A gold ring set with a large red ruby, a small flame rising from the stone.",
    "guide-conditional-narya-die": "A single red enamel game die with rounded corners seen from the front, a gold "
                                   "flame emblem on its face instead of pips, a gold rim.",
    "always-guide": "A small square game tile of dusty green with a gold rim, a pair of big round pale friendly "
                    "eyes peering out from it.",
    "level-of-highest-other-companion": "A large gold five-pointed star with a slightly smaller pale-blue copy of "
                                        "the same star behind it, offset to the upper right.",
    "smeagol-departure": "A silver fish leaping away out of a small ring of dark water, frozen mid-jump.",
    "fire-and-shadow": "The horned head of a demon made of black shadow, burning orange-red eyes, wreathed in "
                       "flames licking around its horns.",
    "flame-of-udun": "A long sword whose blade is a tongue of bright orange-yellow fire, a black iron hilt, "
                     "pointing up.",
    "balrog-activation": "A whip of fire cracking in a curling S-shape, its many lashes made of orange flame, a "
                         "black iron handle.",
    "not-minion-for-gandalf-white": "A white wizard's staff standing upright, crossed out by a thick diagonal "
                                    "dark-red bar.",
    "fling-them-into-the-fray": "A spiked black iron war banner with a red lidless eye on black cloth, a curved orc "
                                "scimitar crossed behind its pole.",
    "evil-knowledge": "A heavy closed black book bound in iron with a single glowing red rune shaped like an eye on "
                      "its cover.",
    "great-sorcery": "A swirling vortex of dark crimson sorcerous fire spiralling inward, a black rune glowing at "
                     "its centre.",
    "spear-of-terror": "A long black barbed spear lying diagonally, its jagged iron head glowing with cold red fire.",
    "he-sees-he-knows": "A black winged fell beast swooping down from above, leathery wings spread wide, a tiny red "
                        "eye.",
    "champion-gondor": "A black heater shield with a gold rim bearing a silver-white tree, a gold sword crossing "
                       "behind it.",
    "messenger-gondor": "A great curved war horn of white ivory banded with silver and gold, a short red cord.",
    "movement-level-two": "A brown leather traveller's boot in profile, two small gold stars above it.",
    "champion-dwarves": "A heavy double-bladed dwarven battle axe standing upright, steel blades with gold "
                        "geometric engraving, a thick haft.",
    "messenger-dwarves": "A black raven in flight with wings spread, a small rolled message in its claws.",
    "champion-elves": "Two curved elven long knives crossed, slender silver blades with green-and-gold leaf hilts.",
    "prince-of-mirkwood": "A graceful elven longbow held upright with an arrow nocked, pale wood with gold inlay and "
                          "green fletching.",
    "guide-cancel-standard-hunt": "A beige square game tile shattering into pieces, a small leaf-bladed sword "
                                  "stabbing down through its middle.",
    "champion-north": "A round green door with a gold doorknob in its centre, a short leaf-bladed sword placed "
                      "across it diagonally.",
    "art-of-disappearing": "A swirl of grey mist curling upward, in its middle the faint fading outline of a small "
                           "cloaked figure vanishing.",
    "messenger-north": "A folded parchment letter sealed with a large blue wax seal stamped with a white "
                       "eight-pointed star.",
    "guide-first-move-to-fellowship-box": "A winding trail of five dark footprints leading up and away, a bright gold "
                                          "guiding star above the end of the trail.",
    # Kings of Middle-earth
    "heir-of-bard": "A single long black arrow with a dark iron head and black fletching, lying diagonally, glinting "
                    "with gold edges.",
    "call-to-arms": "A bronze war bell hanging from a short wooden beam, ringing, curved motion lines on both sides.",
    "corrupted-ruler": "A gold king's crown cracked in two, blackened and charred along the crack, a red ember glow "
                       "inside the break.",
    "shadows-in-the-north": "A single upright game card with a gold border torn by three black claw marks, dark "
                            "smoke curling from the tears.",
    "dwarvenking": "A heavy angular dwarven king's crown of gold with square blue gems, a steel war hammer standing "
                   "upright behind it.",
    "iron-guard": "A dwarven iron war helmet with a full face mask of riveted steel plates shaped as a stern beard, "
                  "gold trim.",
    "fate-of-the-lonely-mountain": "A great carved stone gate of a mountain hall, broken and cracked open, a fierce "
                                   "red-orange fire glowing within.",
    "stewardship": "A tall white rod of office with gold bands and a gold knob, standing upright, a small silver "
                   "tree emblem on its head.",
    "palantir-of-anarion": "A dark crystal sphere on a small black stand, glowing with swirling orange fire inside.",
    "fate-of-the-white-tower": "A funeral pyre: a stone slab stacked with logs, tall red and orange flames roaring "
                               "upward.",
    "forth-eorlingas": "A sword raised upright with a bright golden sunrise bursting behind it, long rays of dawn "
                       "light.",
    "mustering-of-the-mark": "A gold horseshoe, open end up, with three upright spears with green pennons rising "
                             "behind it.",
    "mark-of-the-white-hand": "A small grey stone fortress keep with battlements, a large white handprint smeared "
                              "across its gate.",
    "elvenking": "A crown woven of autumn leaves in gold and russet with small red berries.",
    "far-sighted": "A single silver-white elven arrowhead flying far to the right, a long trail of pale green light "
                   "behind it.",
    "fate-of-mirkwood": "A dead blackened twisted tree with grey spider webs hanging in its branches, a small red "
                        "glow at its roots.",
    "red-wrath": "A curved scimitar whose blade glows blood red with rage, red flames licking from its edge, a gold "
                 "hilt.",
    "dark-chieftain": "A black iron chieftain's war helmet with two sweeping bronze horns and a red crest, seen from "
                      "the front.",
    "lord-of-the-bats": "A black bat with spread jagged wings and small glowing red eyes.",
    "i-command": "A clenched black iron gauntlet fist raised high, a red cloth band at the wrist.",
    "we-march-day-and-night": "A round disc split down the middle, its left half a gold sun and its right half a "
                              "pale silver crescent moon, a short row of small black footprints beneath it.",
    # The Fate of Erebor
    "politics-dwarves-at-fate-homelands": "A long thick auburn dwarven beard braided into two plaits bound with gold "
                                          "clasps.",
}

# Second round (same day, after preview.png): the motifs that shrank to a thin line at 18-28 points (long objects
# standing upright), that were dark on the dark interface, or that read as something else (the shut eye as lips).
REVISIONS = {
    "combat-strength-plus-one": "A large round deep royal-blue enamel shield filling most of the frame, a broad gold "
                                "rim and one big gold five-pointed star in its centre, the hilt and point of a gold "
                                "sword showing diagonally behind it.",
    "politics-gondor-at-city-or-stronghold": "A broad sturdy white stone citadel tower with battlements and a gold "
                                             "roof, wide and solid, a large black pennant edged in silver flying "
                                             "from its top.",
    "may-transform-to-white": "A thick white wizard's staff lying diagonally, a large radiant burst of white light "
                              "with long rays at its head filling the upper right, its foot dark grey wood.",
    "guide-hide-with-any-action-die": "A ranger's deep hood seen from the front, weathered grey-green cloth with "
                                      "bright gold trim along its edge, the face hidden in black shadow, a large gold "
                                      "star brooch at the throat.",
    "guide-separate-to-reduce-hunt-one": "The bold silhouette of a small cloaked traveller with a hood and a walking "
                                         "staff, striding away to the right, his dark green cloak edged in gold and "
                                         "billowing behind him.",
    "ignore-standard-numbered-reveal": "The great fiery red-and-orange eye of Sauron with a black slit pupil and a "
                                       "gold rim, covered across its middle by a dark grey cloth blindfold: an eye "
                                       "that cannot see.",
    "add-gollum-when-no-companions": "A large plain gold ring held up between the long bony fingers of a pale "
                                     "grey-green hand, the ring big and bright in front of the hand.",
    "cannot-leave-orthanc": "A black rock tower with four sharp horns at its top, its edges highlighted in bright "
                            "gold, a heavy bright gold chain wrapped twice around its middle and closed with a "
                            "large gold padlock.",
    "muster-each-isengard-settlement-or-upgrade-two": "A large glowing white orb on top of a short thick black "
                                                      "staff head, three broad curved rings of pale-gold sound "
                                                      "radiating from the orb, the orb and rings filling the frame.",
    "isengard-elites-have-leadership": "A black Uruk-hai battle helmet seen from the front, its edges trimmed with "
                                       "bright gold rivets and bands, a large bright white human hand painted over "
                                       "its whole face-plate.",
    "flame-of-udun": "A broad sword lying diagonally whose wide blade is a roaring tongue of bright orange-yellow "
                     "fire, a heavy black iron hilt edged in gold.",
    "not-minion-for-gandalf-white": "A thick white wizard's staff with a large white glowing orb at its head, lying "
                                    "diagonally, crossed out by a broad dark-red bar from upper left to lower right.",
    "fling-them-into-the-fray": "A large black war banner with a big red lidless eye on its cloth, bordered with "
                                "jagged gold, on a short spiked pole: the cloth fills most of the frame.",
    "spear-of-terror": "The large jagged barbed head of a black spear and a short piece of its shaft, diagonal, the "
                       "head big and broad and glowing with cold red fire, edged in gold.",
    "he-sees-he-knows": "A black winged fell beast swooping down toward the viewer, huge leathery wings spread "
                        "wide with bright gold bones and edges, a large glowing red eye.",
    "prince-of-mirkwood": "An elven leather quiver full of green-fletched arrows, standing slightly tilted, with "
                          "gold leaf patterns on the quiver and a gold strap.",
    "art-of-disappearing": "An empty green hooded cloak with a gold leaf brooch collapsing in a swirl of pale silver "
                           "mist, its wearer vanished.",
    "guide-first-move-to-fellowship-box": "Three large bold dark-brown footprints climbing diagonally from the lower "
                                          "left toward a big bright gold eight-pointed star at the upper right.",
    "heir-of-bard": "A single massive black arrow lying diagonally: a thick shaft, a huge broad black iron arrowhead "
                    "edged in gold, and large black fletching.",
    "shadows-in-the-north": "A single upright game card of pale parchment with a gold border, torn through by three "
                            "deep black claw marks with dark smoke curling from the tears.",
    "stewardship": "A tall high-backed throne seat of black stone trimmed with gold, a white rod of office with gold "
                   "bands lying across its seat.",
    "lord-of-the-bats": "A black bat with wide spread jagged wings, its wing bones and edges bright gold, large "
                        "glowing red eyes.",
    # Third round (pending): the raven seen from the front was close to the Lord of the Bats' spread wings.
    "messenger-dwarves": "A black raven seen in profile flying to the right, wings raised, its feathers edged in "
                         "bright gold, a small rolled parchment message held in its beak.",
}
ITEMS.update(REVISIONS)
# Keys still to regenerate once the owner's Codex usage allows (README, Status): the second round's six that hit the
# usage limit, the raven, and five second-round icons that gained a rod or sword the prompt did not name.
PENDING = ["art-of-disappearing", "guide-first-move-to-fellowship-box", "heir-of-bard", "shadows-in-the-north",
           "stewardship", "lord-of-the-bats", "messenger-dwarves", "add-gollum-when-no-companions",
           "ignore-standard-numbered-reveal", "guide-hide-with-any-action-die", "isengard-elites-have-leadership",
           "cannot-leave-orthanc"]


def generate(key, log, lock, logs):
    prompt = ITEMS[key] + " " + STYLE
    out = ART / "raw" / (key + ".png")
    result = subprocess.run(["bash", str(ART / "codex_image.sh"), prompt, str(out), str(logs / (key + ".log"))],
                            capture_output=True, text=True)
    print(key, result.stdout.strip()[-120:], flush=True)
    if result.returncode != 0:
        return False
    digest = hashlib.sha256(out.read_bytes()).hexdigest()
    image = Image.open(out)
    size = list(image.size)
    if image.width > RAW_WIDTH:
        height = round(image.height * RAW_WIDTH / image.width)
        image.convert("RGB").resize((RAW_WIDTH, height), Image.LANCZOS).save(out, optimize=True)
    with lock:
        log.write(json.dumps({"key": key, "tool": "codex built-in image_gen", "prompt": prompt, "size": size,
                              "sha256": digest}) + "\n")
        log.flush()
    return True


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--jobs", type=int, default=4)
    parser.add_argument("--pending", action="store_true", help="regenerate the PENDING keys")
    parser.add_argument("keys", nargs="*")
    args = parser.parse_args()
    keys = PENDING if args.pending else args.keys or list(ITEMS)
    unknown = [key for key in keys if key not in ITEMS]
    if unknown:
        raise SystemExit("Unknown keys: " + ", ".join(unknown))
    logs = Path(tempfile.gettempdir()) / "wotr-abilities-logs"
    logs.mkdir(exist_ok=True)
    lock = threading.Lock()
    with open(ART / "generations-codex.jsonl", "a") as log, ThreadPoolExecutor(args.jobs) as pool:
        done = list(pool.map(lambda key: generate(key, log, lock, logs), keys))
    failed = [key for key, ok in zip(keys, done) if not ok]
    print("GENERATED", len(keys) - len(failed), "FAILED", " ".join(failed))


if __name__ == "__main__":
    main()
