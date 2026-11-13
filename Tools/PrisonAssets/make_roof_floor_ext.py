"""Katon lisälattia: vankila_katto_matto_1/2.png (5120 x EXT) kuvien alle (kamera laskee lattian mukana, ks. valikko 377).
Alareunan rivit liukuvat pehmeästi koko katon (kaksi kuvaa yhdessä) tasoitettuun väriin, päälle lattian oma hienorakenne.
Ajo: python Tools/PrisonAssets/make_roof_floor_ext.py"""
import os, numpy as np
from PIL import Image, ImageFilter
D = "Assets/Sprites/Taustat"; EXT = 740; FADE = 30; PATCH = 90
def blur1d(v, s):
    r = int(s * 3); k = np.exp(-0.5 * (np.arange(-r, r + 1) / s) ** 2); k /= k.sum()
    pad = np.concatenate([np.repeat(v[:1], r, 0), v, np.repeat(v[-1:], r, 0)], 0)
    return np.stack([np.convolve(pad[:, c], k, "valid") for c in range(v.shape[1])], 1)
A = np.concatenate([np.array(Image.open(f"{D}/vankila_katto_{i}.png").convert("RGBA")) for i in (1, 2)], axis=1)
h, w = A.shape[:2]
bot = A[h - 8:h - 1, :, :3].astype(float).mean(0); base = blur1d(bot, 120)
strip = Image.fromarray(A[h - PATCH:h, :, :3])
hp = np.clip(np.array(strip, float) - np.array(strip.filter(ImageFilter.GaussianBlur(9)), float), -7, 7)   # raja: ei halkeamia/jälkiä
rng = np.random.default_rng(11); rows = []; n = 0
while n < EXT:
    t = hp if len(rows) % 2 == 0 else hp[::-1]
    rows.append(np.roll(t, int(rng.integers(0, w)), axis=1)); n += t.shape[0]
tex = np.concatenate(rows, 0)[:EXT]
y = np.arange(EXT)[:, None, None]; fade = np.clip(y / float(FADE), 0, 1)
dark = 1 - 0.12 * np.clip(y / EXT, 0, 1)
ext = np.clip((bot[None] * (1 - fade) + base[None] * fade + tex * 0.8 * fade) * dark, 0, 255).astype(np.uint8)
ext = np.concatenate([ext, np.full((EXT, w, 1), 255, np.uint8)], axis=2)
half = w // 2
Image.fromarray(ext[:, :half]).save(f"{D}/vankila_katto_matto_1.png"); Image.fromarray(ext[:, half:]).save(f"{D}/vankila_katto_matto_2.png")
print("valmis", ext.shape)
