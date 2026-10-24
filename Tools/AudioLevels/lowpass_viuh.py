"""Viuhahdusäänien hi-cut: 4. asteen Butterworth-alipäästö 3 kHz:ssä (kaksi biquadia), ei muuta tasoa muuten.
Lähde: Lataukset/hitviuh1-5.wav -> Assets/Resources/Sfx/Hit/viuh_1-5.wav (16 bit, alkuperäinen näytteenottotaajuus ja kanavat).
Aja: python Tools/AudioLevels/lowpass_viuh.py [rajataajuus_Hz]"""
import math
import os
import sys
import numpy as np
import soundfile as sf

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
SRC = os.path.join(os.path.expanduser("~"), "Downloads")
OUT = os.path.join(ROOT, "Assets", "Resources", "Sfx", "Hit")
FC = float(sys.argv[1]) if len(sys.argv) > 1 else 3000.0


def biquad_lowpass(fc, sr, q):
    w0 = 2 * math.pi * fc / sr
    alpha = math.sin(w0) / (2 * q)
    cw = math.cos(w0)
    b0 = (1 - cw) / 2; b1 = 1 - cw; b2 = (1 - cw) / 2
    a0 = 1 + alpha; a1 = -2 * cw; a2 = 1 - alpha
    return b0 / a0, b1 / a0, b2 / a0, a1 / a0, a2 / a0


def run(x, c):
    b0, b1, b2, a1, a2 = c
    y = np.zeros_like(x)
    x1 = x2 = y1 = y2 = 0.0
    for i, v in enumerate(x):
        o = b0 * v + b1 * x1 + b2 * x2 - a1 * y1 - a2 * y2
        x2, x1 = x1, v
        y2, y1 = y1, o
        y[i] = o
    return y


for i in range(1, 6):
    d, sr = sf.read(os.path.join(SRC, f"hitviuh{i}.wav"), always_2d=True)
    out = np.zeros_like(d)
    for ch in range(d.shape[1]):
        y = d[:, ch].astype(np.float64)
        for q in (0.5412, 1.3066):   # 4. asteen Butterworth = kaksi 2. asteen osaa
            y = run(y, biquad_lowpass(FC, sr, q))
        out[:, ch] = y
    sf.write(os.path.join(OUT, f"viuh_{i}.wav"), np.clip(out, -1, 1), sr, subtype="PCM_16")
    print(f"viuh_{i}.wav: hi-cut {FC:.0f} Hz, huippu {np.abs(d).max():.2f} -> {np.abs(out).max():.2f}")
