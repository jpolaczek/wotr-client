"""Building blocks for the game's sound effects (AUDIO-01): envelopes, modal and granular sources, filters,
a short room, Karplus-Strong strings and loudness measurement. Everything is deterministic for a given seed."""
import numpy as np
from scipy import signal

RATE = 48000


def seconds(duration):
    return np.arange(int(round(duration * RATE))) / RATE


def silence(duration):
    return np.zeros(int(round(duration * RATE)))


def place(target, sound, at):
    """Adds sound into target starting at `at` seconds, cut at the end of target."""
    start = int(round(at * RATE))
    if start < 0:
        sound, start = sound[-start:], 0
    if start >= len(target):
        return target
    end = min(len(target), start + len(sound))
    target[start:end] += sound[:end - start]
    return target


def fades(x, fade_in=0.0015, fade_out=0.012):
    """Raised-cosine edges, so no clip starts or ends with a step."""
    x = x.copy()
    n_in = max(1, int(fade_in * RATE))
    n_out = max(1, int(fade_out * RATE))
    x[:n_in] *= 0.5 - 0.5 * np.cos(np.linspace(0, np.pi, n_in))
    x[-n_out:] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, n_out))
    return x


def decay(duration, tau, attack=0.0005):
    t = seconds(duration)
    rise = np.clip(t / attack, 0, 1) if attack > 0 else 1
    return rise * np.exp(-t / tau)


def modal(rng, duration, modes, attack=0.0004):
    """A struck object: damped sines (frequency, amplitude, decay) with random phases. The end is always faded, so
    a mode still ringing when the part ends never stops with a click."""
    t = seconds(duration)
    out = np.zeros_like(t)
    for frequency, amplitude, tau in modes:
        phase = rng.uniform(0, 2 * np.pi)
        out += amplitude * np.exp(-t / tau) * np.sin(2 * np.pi * frequency * t + phase)
    rise = np.clip(t / attack, 0, 1)
    return tail(out * rise)


def tail(x, share=0.3, longest=0.03):
    """Rounds off the last part of a sound with a raised cosine."""
    n = max(1, min(int(len(x) * share), int(longest * RATE)))
    x = x.copy()
    x[-n:] *= 0.5 + 0.5 * np.cos(np.linspace(0, np.pi, n))
    return x


def noise(rng, duration):
    return rng.standard_normal(int(round(duration * RATE)))


def pink(rng, duration):
    """Approximately 1/f noise (Voss-McCartney style filter), quieter in the treble than white noise."""
    white = noise(rng, duration)
    b = [0.049922035, -0.095993537, 0.050612699, -0.004408786]
    a = [1, -2.494956002, 2.017265875, -0.522189400]
    return signal.lfilter(b, a, white) * 4


def band(x, low, high, order=2):
    sos = signal.butter(order, [low, high], btype="bandpass", fs=RATE, output="sos")
    return signal.sosfilt(sos, x)


def lowpass(x, cutoff, order=2):
    sos = signal.butter(order, cutoff, btype="lowpass", fs=RATE, output="sos")
    return signal.sosfilt(sos, x)


def highpass(x, cutoff, order=2):
    sos = signal.butter(order, cutoff, btype="highpass", fs=RATE, output="sos")
    return signal.sosfilt(sos, x)


def sweep_band(x, start_hz, end_hz, q=1.6):
    """A state-variable band-pass whose centre glides from start to end (a sliding card, a scraping ring)."""
    centres = np.geomspace(start_hz, end_hz, len(x))
    low = band_out = 0.0
    out = np.empty_like(x)
    damping = 1.0 / q
    for i, sample in enumerate(x):
        f = 2 * np.sin(np.pi * centres[i] / RATE)
        high = sample - low - damping * band_out
        band_out += f * high
        low += f * band_out
        out[i] = band_out
    return out


def grains(rng, duration, count, density, low, high, length=(0.001, 0.004), level=(0.3, 1.0)):
    """Paper crinkle: many tiny band-limited bursts whose times follow the density curve (0..1 over duration)."""
    out = silence(duration)
    weights = density / density.sum()
    times = rng.choice(len(out), size=count, p=weights)
    for start in times:
        size = int(rng.uniform(*length) * RATE)
        burst = rng.standard_normal(size) * np.exp(-np.linspace(0, 5, size))
        centre = rng.uniform(low, high)
        burst = band(burst, centre * 0.7, min(centre * 1.4, RATE / 2 - 100))
        place(out, burst * rng.uniform(*level), start / RATE)
    return out


def envelope(duration, points):
    """Piecewise-linear amplitude over (time, level) points."""
    t = seconds(duration)
    xs, ys = zip(*points)
    return np.interp(t, xs, ys)


def room(x, rng, tail=0.16, wet=0.08, brightness=3500):
    """A small wooden room: a short decaying, darkened noise response mixed lightly under the dry sound."""
    ir = noise(rng, tail) * np.exp(-seconds(tail) / (tail / 6.9))
    ir = lowpass(ir, brightness)
    ir[: int(0.004 * RATE)] = 0
    ir /= np.sqrt(np.sum(ir ** 2))
    wet_signal = signal.fftconvolve(x, ir)[: len(x)]
    return x + wet * wet_signal


def pluck(rng, frequency, duration, brightness=0.5, damping=0.996):
    """Karplus-Strong string: a soft harp or lute note. Lower brightness gives a darker, felt-like attack."""
    n = int(round(duration * RATE))
    period = int(round(RATE / frequency))
    excitation = rng.uniform(-1, 1, period)
    excitation = lowpass(np.concatenate([excitation, np.zeros(period)]), 800 + brightness * 7000)[:period]
    buffer = excitation.copy()
    out = np.empty(n)
    for i in range(n):
        sample = buffer[i % period]
        out[i] = sample
        following = buffer[(i + 1) % period]
        buffer[i % period] = damping * 0.5 * (sample + following)
    return out


def k_weighted(x):
    """ITU-R BS.1770 K-weighting at 48 kHz (pre-filter shelf and RLB high-pass)."""
    shelf_b = [1.53512485958697, -2.69169618940638, 1.19839281085285]
    shelf_a = [1.0, -1.69065929318241, 0.73248077421585]
    rlb_b = [1.0, -2.0, 1.0]
    rlb_a = [1.0, -1.99004745483398, 0.99007225036621]
    return signal.lfilter(rlb_b, rlb_a, signal.lfilter(shelf_b, shelf_a, x))


def loudness(x):
    """Loudness of a short effect in LUFS-like units: K-weighted power over the part within 30 dB of its peak."""
    weighted = k_weighted(x)
    power = weighted ** 2
    smooth = np.convolve(power, np.ones(480) / 480, mode="same")
    active = smooth > smooth.max() * 10 ** (-30 / 10)
    return -0.691 + 10 * np.log10(np.mean(power[active]) + 1e-20)


def true_peak(x):
    """Peak after 4x oversampling, in dBFS."""
    return 20 * np.log10(np.max(np.abs(signal.resample_poly(x, 4, 1))) + 1e-20)


def finish(x, target_lufs, fade_in=0.0015, fade_out=0.012, ceiling_db=-1.0):
    """Removes DC and rumble, rounds the edges, then scales to the target loudness under the true-peak ceiling."""
    x = fades(highpass(x, 25), fade_in, fade_out)
    return normalise(x, target_lufs, ceiling_db)


def normalise(x, target_lufs, ceiling_db=-3.0):
    """Scales to the target loudness, then lowers it if the true peak would pass the ceiling."""
    x = x * 10 ** ((target_lufs - loudness(x)) / 20)
    peak = true_peak(x)
    if peak > ceiling_db:
        x = x * 10 ** ((ceiling_db - peak) / 20)
    return x
