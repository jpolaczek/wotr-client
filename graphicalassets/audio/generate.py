#!/usr/bin/env python3
"""Generates the game's sound effects (AUDIO-01) into the Unity Resources and writes their provenance.

Every clip is synthesised here from numbers and fixed seeds: felt and cloth thumps, paper rustle, dice on a mat,
boots, a heavy ring, war drums under an army's roar (AUDIO-11), a soft chime, plucked strings and a deep struck bell for
the Politics track (AUDIO-12), in a small room. AUDIO-10 made
the effects toneless and lower (no "bamboo" modes, no crackle above ~4 kHz). No recording, download or service is used, so
running it again gives the same files. Usage: python generate.py [--preview <html>]
"""
import argparse
import base64
import hashlib
import json
import pathlib
import wave

import numpy as np

from synth import (RATE, band, decay, envelope, finish, grains, loudness, lowpass, modal, noise, pink,
                   place, pluck, room, seconds, silence, sweep_band, tail, true_peak)

ROOT = pathlib.Path(__file__).resolve().parents[2]
OUTPUT = ROOT / "unity/Wotr.Client/Assets/Wotr/Resources/Wotr/Audio"
MANIFEST = pathlib.Path(__file__).resolve().parent / "manifest.json"
VERSION = 6


def thump(rng, duration, cutoff, tau, attack=0.002):
    """A soft, toneless knock on felt or cloth: low-passed noise with a quick, rounded decay (AUDIO-10)."""
    return lowpass(noise(rng, duration) * decay(duration, tau, attack=attack), cutoff, order=4)


def rustle(rng, duration, shape, low, high, count, level=0.5):
    """Paper or cloth moving: band-limited pink noise with an uneven flutter, over small crinkles of the same band."""
    flutter = 0.6 + 0.4 * np.abs(np.sin(2 * np.pi * rng.uniform(13, 21) * seconds(duration) + rng.uniform(0, 6)))
    body = band(pink(rng, duration), low, high) * shape * flutter * level
    crinkle = grains(rng, duration, count, shape + 1e-6, low * 1.5, high, length=(0.001, 0.005), level=(0.1, 0.6))
    return body + crinkle * 0.6


def ui_click(rng, variant):
    """A light tap on felt with the flick of a card edge: soft and dry, no ringing tone (AUDIO-10)."""
    duration = 0.08
    cutoff = [900, 1000, 820][variant]
    knock = thump(rng, duration, cutoff, 0.007)
    flick = band(pink(rng, duration) * decay(duration, 0.012, attack=0.003), 500, 2400) * 0.45
    x = lowpass(knock + flick, 3000, order=4)
    return finish(room(x, rng, tail=0.08, wet=0.05, brightness=2500), -24, fade_out=0.02)


def card_inspect(rng, variant):
    """A sheet of paper lifted for a look: a soft, busy rustle in the middle of the range, without sharp crackle
    (AUDIO-10: lower and softer than the first paper)."""
    duration = 0.24
    shape = envelope(duration, [(0, 0), (0.03, 0.8), (0.08, 1.0), (0.13, 0.7), (0.18, 0.8), (0.24, 0)])
    x = rustle(rng, duration, shape, 350 + 30 * variant, 2600, 110 + 15 * variant, level=0.55)
    x = lowpass(x, 3600, order=4)
    return finish(room(x, rng, tail=0.08, wet=0.05, brightness=2500), -27, fade_out=0.03)


def card_move(rng, variant):
    """A card slid across the cloth and laid down with a papery flop (AUDIO-10: no wooden knock)."""
    duration = 0.27
    slide_shape = envelope(duration, [(0, 0), (0.05, 0.6), (0.15, 1.0), (0.2, 0.25), (0.27, 0)])
    slide = sweep_band(pink(rng, duration), 650 + 60 * variant, 1500, q=2.0) * slide_shape
    crinkle = grains(rng, duration, 18, slide_shape + 1e-6, 900, 2600, length=(0.001, 0.004), level=(0.1, 0.4)) * 0.5
    flop = thump(rng, 0.09, 700, 0.014) + band(pink(rng, 0.09) * decay(0.09, 0.02, attack=0.002), 250, 1300) * 0.5
    x = slide * 0.75 + crinkle
    place(x, flop * 0.8, 0.175 + 0.008 * variant)
    x = lowpass(x, 3500, order=4)
    return finish(room(x, rng, tail=0.1, wet=0.06, brightness=2500), -23, fade_out=0.03)


def dice_roll(rng, variant):
    """Two dice tumbling on a felt mat: dull bounces that come closer and softer, then a short cloth roll
    (AUDIO-10: no hard, high clicks)."""
    duration = 0.48
    x = silence(duration)
    for die, offset in enumerate([0.0, 0.017 + 0.006 * variant]):
        at = offset
        for bounce, gap in enumerate([0.0, 0.072, 0.054, 0.042, 0.033, 0.026, 0.021]):
            at += gap * rng.uniform(0.9, 1.1)
            level = 0.82 ** bounce * rng.uniform(0.8, 1.0) * (1.0 if die == 0 else 0.8)
            knock = thump(rng, 0.05, rng.uniform(1100, 1700), rng.uniform(0.004, 0.007), attack=0.0008)
            body = thump(rng, 0.05, 420, 0.012) * (0.6 if bounce < 3 else 0.2)
            place(x, (knock + body) * level, at)
    roll_shape = envelope(duration, [(0, 0), (0.06, 0.5), (0.25, 0.35), (0.38, 0.0), (0.48, 0)])
    roll = rustle(rng, duration, roll_shape, 350, 1800, 40, level=0.3)
    x = lowpass(x + roll * 0.5, 3500, order=4)
    return finish(room(x, rng, tail=0.14, wet=0.07, brightness=2500), -21, fade_out=0.04)


def action_accepted(rng, variant):
    """A piece set firmly on the cloth: a round, dull thump with a short rustle (AUDIO-10: no wooden ring)."""
    duration = 0.17
    weight = [1.0, 0.92][variant]
    knock = thump(rng, duration, 520 * weight, 0.03, attack=0.003)
    low = np.sin(2 * np.pi * 105 * weight * seconds(duration)) * decay(duration, 0.025, attack=0.003) * 0.25
    cloth = band(pink(rng, duration) * decay(duration, 0.02, attack=0.004), 400, 1800) * 0.35
    x = lowpass(knock + low + cloth, 2500, order=4)
    return finish(room(x, rng, tail=0.12, wet=0.06, brightness=2200), -21, fade_out=0.03)


def war_drum(rng, length, pitch):
    """One stroke on a great war drum (AUDIO-11): a low skin whose pitch falls as it rings, two inharmonic overtones
    that carry it on small speakers, the thud of the beater and the slap of the skin, all under about 1.2 kHz."""
    t = seconds(length)
    fall = pitch * (0.8 + 0.2 * np.exp(-t / 0.07))
    phase = 2 * np.pi * np.cumsum(fall) / RATE
    body = np.sin(phase + rng.uniform(0, 0.4)) * decay(length, 0.14, attack=0.003) * 0.8
    overtone = np.sin(1.59 * phase + rng.uniform(0, 6)) * decay(length, 0.09, attack=0.003) * 0.7
    upper = np.sin(2.14 * phase + rng.uniform(0, 6)) * decay(length, 0.06, attack=0.003) * 0.5
    thud = lowpass(noise(rng, length) * decay(length, 0.025, attack=0.001), 420, order=4) * 0.8
    slap = band(noise(rng, length) * decay(length, 0.012), 250, 1200) * 0.5
    return tail(body + overtone + upper + thud + slap, longest=0.08)


def distant_clash(rng, length):
    """Iron meeting iron far off: a few low, dulled metal strikes, darkened so they rumble rather than ring."""
    x = silence(length)
    base = [(720, 0.5, 0.12), (1130, 0.4, 0.09), (1610, 0.3, 0.07)]
    for at, level in [(0.0, 1.0), (0.031, 0.7), (0.074, 0.55), (0.12, 0.4)]:
        scale = rng.uniform(0.8, 1.2)
        piece = modal(rng, 0.2, [(f * scale, a * rng.uniform(0.6, 1.0), tau) for f, a, tau in base])
        strike = band(noise(rng, 0.2) * decay(0.2, 0.004), 400, 1600) * 0.4
        place(x, (piece + strike) * level, at + rng.uniform(-0.004, 0.004))
    return lowpass(x, 1800, order=4)


def battle_started(rng, variant):
    """Armies meet (AUDIO-11, after the user found the horn comical): great war drums under the swelling roar of an
    army and a far-off clash of iron. No horn and no melody. Variant 1 strikes twice, variant 2 three times."""
    duration = 0.88
    if variant == 0:
        strokes = [(0.0, 1.0, 92.0), (0.34, 0.85, 86.0)]
    else:
        strokes = [(0.0, 0.8, 90.0), (0.17, 0.75, 90.0), (0.42, 1.0, 84.0)]
    drums = silence(duration)
    for at, level, pitch in strokes:
        place(drums, war_drum(rng, 0.46, pitch) * level, at)
    roar_shape = envelope(duration, [(0, 0), (0.1, 0.25), (0.42, 1.0), (0.7, 0.6), (0.88, 0)])
    roar = band(pink(rng, duration), 140, 900) * roar_shape
    murmur = band(noise(rng, duration), 90, 240) * roar_shape * 0.5
    clash = silence(duration)
    place(clash, distant_clash(rng, 0.3), strokes[-1][0] + 0.03)
    x = drums * 0.8 + (roar + murmur) * 0.5 + clash * 0.25
    return finish(room(x, rng, tail=0.35, wet=0.14, brightness=1800), -17, fade_out=0.08)


def boot_step(rng, level):
    """One step on packed earth: a firm heel strike, the sole rolling to the toe a moment later, and the crunch of
    grit under it. Noise shaped by quick envelopes, as real steps are, not tones (revised after the listening check)."""
    duration = 0.16
    step = silence(duration)
    heel = lowpass(noise(rng, 0.08), 1100, order=4) * decay(0.08, 0.016, attack=0.0015)
    weight = np.sin(2 * np.pi * 75 * seconds(0.08)) * decay(0.08, 0.02, attack=0.002) * 0.35
    place(step, heel + weight, 0.0)
    toe = band(noise(rng, 0.06), 300, 1800) * decay(0.06, 0.011, attack=0.0015) * 0.55
    place(step, toe, rng.uniform(0.042, 0.055))
    grit_shape = envelope(duration, [(0, 0), (0.004, 1), (0.06, 0.5), (0.1, 0.1), (0.16, 0)])
    grit = grains(rng, duration, 24, grit_shape + 1e-6, 800, 2800, length=(0.0008, 0.003), level=(0.1, 0.6)) * 0.45
    return tail(step + grit) * level

def fellowship_advanced(rng, variant):
    """Two light steps, left then right: the Fellowship moves on."""
    duration = 0.38
    x = silence(duration)
    place(x, boot_step(rng, 1.0), 0.0)
    place(x, boot_step(rng, 0.85), 0.2 + 0.012 * variant)
    x = lowpass(x, 3500, order=4)
    return finish(room(x, rng, tail=0.1, wet=0.06), -22, fade_out=0.04)

def corruption_changed(rng, variant):
    """The heavy Ring drags along its track and settles with a deep, muffled ring (revised after the listening
    check: deeper, no glassy tick)."""
    duration = 0.38
    x = silence(duration)
    spread = [1.0, 1.04, 0.95][variant]
    slide_shape = envelope(0.22, [(0, 0), (0.04, 0.8), (0.16, 1.0), (0.22, 0)])
    roughness = 0.6 + 0.4 * np.abs(np.sin(2 * np.pi * 47 * seconds(0.22) + rng.uniform(0, 6)))
    grind = sweep_band(noise(rng, 0.22), 900 * spread, 1700 * spread, q=2.5) * slide_shape * roughness
    ring = [(176 * spread, 1.0, 0.3), (447 * spread, 0.6, 0.22), (853 * spread, 0.32, 0.14), (1372 * spread, 0.14, 0.08)]
    hum = modal(rng, 0.22, [(f, a * 0.22, t) for f, a, t in ring[:2]]) * slide_shape
    place(x, grind * 0.3 + hum, 0.0)
    thud = np.sin(2 * np.pi * 68 * seconds(0.2)) * decay(0.2, 0.05, attack=0.003) * 0.6
    settle = modal(rng, 0.2, ring, attack=0.002) + thud + lowpass(noise(rng, 0.2) * decay(0.2, 0.005), 700) * 0.3
    place(x, settle * 0.9, 0.18)
    x = lowpass(x, 2200, order=4)
    return finish(room(x, rng, tail=0.16, wet=0.09, brightness=2000), -21, fade_out=0.05)

def chime(rng, frequency, duration, tau):
    """A small bell: sine partials in bell ratios, a soft attack and a clean decay."""
    ratios = [(1.0, 1.0, 1.0), (2.0, 0.25, 0.6), (2.76, 0.1, 0.4)]
    return modal(rng, duration, [(frequency * r, a, tau * d) for r, a, d in ratios], attack=0.014)


def decision_required(rng, variant):
    """A gentle two-note chime rising a fourth: your move."""
    duration = 0.7
    x = silence(duration)
    # Lower again after the second listening check (AUDIO-10): G4 rising to C5, soft and warm.
    place(x, chime(rng, 392.0, duration, 0.26) * 0.75, 0.0)
    place(x, chime(rng, 523.3, duration - 0.11, 0.3), 0.11)
    x = lowpass(x, 3200, order=4)
    return finish(room(x, rng, tail=0.22, wet=0.1, brightness=3000), -19, fade_out=0.12)


def action_failed(rng, variant):
    """Two soft, muted notes falling a minor third: that did not work."""
    duration = 0.26
    x = silence(duration)
    for frequency, at, level in [(220.0, 0.0, 1.0), (185.0, 0.085, 0.85)]:
        t = seconds(0.16)
        note = (np.sin(2 * np.pi * frequency * t) + 0.25 * np.sin(4 * np.pi * frequency * t)) * decay(0.16, 0.05, attack=0.006)
        place(x, tail(note, share=0.5, longest=0.06) * level, at)
    x = lowpass(x, 1400)
    return finish(room(x, rng, tail=0.1, wet=0.05, brightness=2000), -20, fade_out=0.05)


def arpeggio(rng, notes, gap, brightness, cutoff):
    duration = 1.0
    x = silence(duration)
    for i, frequency in enumerate(notes):
        place(x, pluck(rng, frequency, duration - i * gap, brightness=brightness, damping=0.9975) * (0.9 - 0.05 * i), i * gap)
    return lowpass(x, cutoff)


def victory(rng, variant):
    """A calm rising major arpeggio on a plucked string."""
    x = arpeggio(rng, [293.66, 369.99, 440.0, 587.33], 0.075, 0.3, 3200)
    return finish(room(x, rng, tail=0.25, wet=0.1, brightness=3000), -17, fade_out=0.25)


def defeat(rng, variant):
    """A calm falling minor arpeggio, darker and softer."""
    x = arpeggio(rng, [440.0, 349.23, 293.66, 220.0], 0.09, 0.22, 2500)
    return finish(room(x, rng, tail=0.25, wet=0.1, brightness=2500), -18, fade_out=0.25)


def politics_moved(rng, variant):
    """A nation's token slides along the Politics track (AUDIO-12, owner 2026-10-27): one deep, struck metal note
    between a bell and an anvil that rings through the 0.7 s slide and fades with the token's glow (about 8 dB down
    when the slide ends, 13 dB a little later). The bell partials (hum, prime, minor tierce, quint, nominal) ring
    longest, each with a slightly detuned twin for a slow, warm beat; the anvil's inharmonic upper modes, up to about
    1.6 kHz, carry the metal and die sooner; a felt-wrapped hammer gives a soft attack, and nothing passes about
    2.6 kHz, so it stays deep and calm rather than shrill (its spectrum sits near CorruptionChanged's)."""
    duration = 1.6
    root = [123.5, 116.5][variant]
    partials = [
        (0.5, 0.12, 1.3),
        (1.0, 0.45, 1.15),
        (1.19, 0.45, 1.05),
        (1.5, 0.4, 0.95),
        (2.0, 0.85, 0.9),
        (2.52, 0.6, 0.75),
        (3.0, 0.5, 0.65),
        (4.2, 0.6, 0.55),
        (5.4, 0.55, 0.42),
        (6.9, 0.5, 0.32),
        (8.6, 0.4, 0.24),
        (10.8, 0.28, 0.16),
        (13.1, 0.16, 0.1),
    ]
    modes = []
    for ratio, level, tau in partials:
        modes.append((root * ratio, level, tau))
        modes.append((root * ratio * 1.0035, level * 0.35, tau * 0.9))
    x = modal(rng, duration, modes, attack=0.004)
    place(x, thump(rng, 0.12, 900, 0.02) * 0.45, 0.0)
    x = lowpass(x, 2600, order=4)
    return finish(room(x, rng, tail=0.4, wet=0.12, brightness=2200), -22, fade_out=0.35)


# Cue → channel, file stem, variants, longest allowed duration and the generator (AUDIO_UX catalogue).
CATALOGUE = [
    ("UiClick", "Effects", "ui-click", 3, 0.12, ui_click),
    ("CardInspect", "Effects", "card-inspect", 3, 0.25, card_inspect),
    ("CardMove", "Effects", "card-move", 3, 0.32, card_move),
    ("DiceRoll", "Effects", "dice-roll", 2, 0.55, dice_roll),
    ("ActionAccepted", "Effects", "action-accepted", 2, 0.22, action_accepted),
    ("BattleStarted", "Alerts", "battle-started", 2, 0.9, battle_started),
    ("FellowshipAdvanced", "Effects", "fellowship-advanced", 3, 0.4, fellowship_advanced),
    ("CorruptionChanged", "Effects", "corruption-changed", 3, 0.4, corruption_changed),
    ("DecisionRequired", "Alerts", "decision-required", 1, 0.75, decision_required),
    ("ActionFailed", "Alerts", "action-failed", 1, 0.3, action_failed),
    ("Victory", "Alerts", "victory", 1, 1.0, victory),
    ("Defeat", "Alerts", "defeat", 1, 1.0, defeat),
    # Last, so the earlier cues keep their seeds (and files).
    ("PoliticsMoved", "Effects", "politics-moved", 2, 1.7, politics_moved),
]


def write_wav(path, x):
    pcm = np.clip(np.round(x * 32767), -32768, 32767).astype("<i2")
    with wave.open(str(path), "wb") as out:
        out.setnchannels(1)
        out.setsampwidth(2)
        out.setframerate(RATE)
        out.writeframes(pcm.tobytes())
    return path.read_bytes()


def checks(x):
    """Objective file checks: silent edges, no DC, no clipping."""
    return {
        "startAmplitude": round(float(abs(x[0])), 6),
        "endAmplitude": round(float(abs(x[-1])), 6),
        "dcOffset": round(float(np.mean(x)), 6),
        "clippedSamples": int(np.sum(np.abs(x) >= 0.999)),
    }


def generate():
    OUTPUT.mkdir(parents=True, exist_ok=True)
    clips, catalogue = [], {}
    for index, (cue, channel, stem, variants, longest, make) in enumerate(CATALOGUE):
        paths = []
        for variant in range(variants):
            seed = 1000 * (index + 1) + variant
            x = make(np.random.default_rng(seed), variant)
            name = f"{stem}-{variant + 1}"
            data = write_wav(OUTPUT / (name + ".wav"), x)
            duration = len(x) / RATE
            assert duration <= longest + 1e-9, f"{name} lasts {duration:.3f} s, over {longest} s"
            clips.append({"cue": cue, "file": name + ".wav", "seed": seed, "seconds": round(duration, 3),
                          "loudness": round(float(loudness(x)), 1), "truePeakDb": round(float(true_peak(x)), 1),
                          "sha256": hashlib.sha256(data).hexdigest(), **checks(x)})
            paths.append("Wotr/Audio/" + name)
        catalogue[cue] = {"channel": channel, "clips": paths, "longestSeconds": longest}
    (OUTPUT / "audio-catalog.json").write_text(json.dumps({"schemaVersion": 1, "cues": catalogue}, indent=2) + "\n")
    MANIFEST.write_text(json.dumps({
        "generator": "graphicalassets/audio/generate.py", "version": VERSION, "sampleRate": RATE,
        "format": "WAV PCM 16-bit mono", "source": "Procedural synthesis only; no recordings or downloaded samples.",
        "clips": clips}, indent=2) + "\n")
    return clips


def preview(path, clips):
    """A page to listen to every clip and the two track sequences (Journey: boots then Ring; Mordor: the reverse)."""
    def source(file):
        return "data:audio/wav;base64," + base64.b64encode((OUTPUT / file).read_bytes()).decode()
    rows = "".join(
        f"<tr><td>{c['cue']}</td><td>{c['file']}</td><td>{c['seconds']:.2f} s</td>"
        f"<td><audio controls preload='none' src='{source(c['file'])}'></audio></td></tr>" for c in clips)
    path.write_text("<!doctype html><meta charset='utf-8'><title>Sound preview</title>"
                    f"<table>{rows}</table>")


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--preview")
    arguments = parser.parse_args()
    clips = generate()
    for clip in clips:
        print(f"{clip['file']:26} {clip['seconds']:.3f}s {clip['loudness']:6.1f} LU {clip['truePeakDb']:6.1f} dBTP "
              f"edges {clip['startAmplitude']:.4f}/{clip['endAmplitude']:.4f} dc {clip['dcOffset']:.5f}")
    if arguments.preview:
        preview(pathlib.Path(arguments.preview), clips)


if __name__ == "__main__":
    main()
