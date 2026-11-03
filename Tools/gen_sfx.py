"""Synthesizes the retro sound effects that the CC0 packs don't cover.

Run from the project root:  python3 Tools/gen_sfx.py
Writes 16-bit mono WAV files to Assets/_Project/Audio/Sfx/Generated.
"""
import os
import wave

import numpy as np

RATE = 44100
OUT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "Assets", "_Project", "Audio", "Sfx", "Generated")
rng = np.random.default_rng(7)


def t_axis(dur):
    return np.arange(int(RATE * dur)) / RATE


def env(n, attack=0.005, release=0.6):
    e = np.ones(n)
    a = max(1, int(RATE * attack))
    e[:a] = np.linspace(0, 1, a)
    r = int(n * release)
    if r > 0:
        e[-r:] *= np.linspace(1, 0, r) ** 2
    return e


def sweep(f0, f1, dur, wave_kind="square"):
    t = t_axis(dur)
    f = np.linspace(f0, f1, len(t))
    phase = 2 * np.pi * np.cumsum(f) / RATE
    if wave_kind == "square":
        return np.sign(np.sin(phase)) * 0.5
    if wave_kind == "tri":
        return 2 / np.pi * np.arcsin(np.sin(phase))
    return np.sin(phase)


def noise(dur):
    return rng.uniform(-1, 1, int(RATE * dur))


def lowpass(x, k):
    kernel = np.ones(k) / k
    return np.convolve(x, kernel, mode="same")


def tone(freq, dur, kind="square"):
    return sweep(freq, freq, dur, kind)


def write(name, x, gain=0.8):
    x = np.asarray(x, dtype=np.float64)
    peak = np.max(np.abs(x)) or 1.0
    x = x / peak * gain
    os.makedirs(OUT, exist_ok=True)
    with wave.open(os.path.join(OUT, name + ".wav"), "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(RATE)
        w.writeframes((x * 32767).astype(np.int16).tobytes())


def main():
    s = sweep(220, 660, 0.14)
    write("jump", s * env(len(s), release=0.5), 0.45)

    n = lowpass(noise(0.12), 40)
    write("land", n * env(len(n), release=0.9), 0.5)

    n = lowpass(noise(0.28), 8) * np.linspace(0.3, 1, int(RATE * 0.28)) ** 0.5
    write("roll", n * env(len(n), attack=0.05, release=0.6), 0.45)

    s = sweep(400, 90, 0.25) + lowpass(noise(0.25), 6) * 0.6
    write("hurt", s * env(len(s), release=0.7), 0.6)

    s = np.concatenate([sweep(330, 160, 0.25, "square"), sweep(160, 60, 0.45, "square")])
    write("player_death", s * env(len(s), release=0.5), 0.6)

    s = sweep(300, 50, 0.35, "tri") + lowpass(noise(0.35), 20) * 0.7
    write("enemy_death", s * env(len(s), release=0.8), 0.6)

    s = lowpass(noise(0.18), 5) + sweep(140, 70, 0.18, "sine")
    write("enemy_attack", s * env(len(s), release=0.6), 0.5)

    t = t_axis(1.2)
    growl = np.sin(2 * np.pi * (70 + 15 * np.sin(2 * np.pi * 7 * t)) * t) + lowpass(noise(1.2), 30) * 1.2
    write("boss_roar", growl * env(len(t), attack=0.08, release=0.5), 0.8)

    n = lowpass(noise(0.5), 25) + sweep(90, 35, 0.5, "sine") * 0.8
    write("debris_crash", n * env(len(n), release=0.85), 0.7)

    n = lowpass(noise(0.6), 12) * (0.6 + 0.4 * np.sin(2 * np.pi * 9 * t_axis(0.6)))
    write("fire", n * env(len(n), attack=0.1, release=0.5), 0.45)

    notes = np.concatenate([tone(f, 0.08, "tri") * env(int(RATE * 0.08), release=0.3) for f in (660, 880, 1320)])
    write("checkpoint", notes, 0.5)

    notes = np.concatenate([tone(f, 0.07, "square") * env(int(RATE * 0.07), release=0.3) for f in (523, 659, 784)])
    write("heart", notes, 0.4)

    seq = [(523, 0.12), (659, 0.12), (784, 0.12), (1046, 0.4)]
    notes = np.concatenate([tone(f, d, "square") * env(int(RATE * d), release=0.4) for f, d in seq])
    write("level_complete", notes, 0.45)

    seq = [(392, 0.18), (330, 0.18), (262, 0.18), (196, 0.5)]
    notes = np.concatenate([tone(f, d, "tri") * env(int(RATE * d), release=0.4) for f, d in seq])
    write("game_over", notes, 0.55)

    s = sweep(1200, 1500, 0.05, "square")
    write("clang_ring", s * env(len(s), release=0.9), 0.3)

    t = t_axis(0.9)
    thunder = lowpass(noise(0.9), 10) * np.exp(-t * 3) + lowpass(noise(0.9), 60) * np.exp(-t * 1.5)
    write("thunder", thunder, 0.7)

    s = sweep(80, 160, 0.4, "square") + lowpass(noise(0.4), 15) * 0.5
    write("gate_open", s * env(len(s), attack=0.05, release=0.4), 0.5)

    print("sfx written to", os.path.normpath(OUT))


if __name__ == "__main__":
    main()
