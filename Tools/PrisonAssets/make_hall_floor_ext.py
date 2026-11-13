"""Sellikäytävien (D- ja E-osasto) kuviin lisälattiaa alas: EXT riviä lisää kuvan alle.
vankila_kaytava_{d1,d1b,e1,e1b}.png (2560 x 922) -> vankila_kaytava_*_iso.png (2560 x 922 + EXT). Alkuperäiset säilyvät ennallaan.
Lisälattia ei venytä kuvaa (venytys kallisti lattian sauma-viivoja ja lattia näytti putoavan), vaan: alareunan rivit liukuvat pehmeästi
koko osion (kaksi kuvaa yhdessä, saumaton) tasoitettuun pohjaväriin ja päälle tulee lattian oma hienorakenne (ylipäästösuodatettu kohina).
Ajo: python Tools/PrisonAssets/make_hall_floor_ext.py [kuvakansio]"""
import os, sys
import numpy as np
from PIL import Image, ImageFilter
EXT = 140          # lisättävät rivit (lähdepikseleinä); sama arvo BeatEmUpSetup.HallFloorExt:ssä
FLOOR_TOP = 898    # puhdasta lattiaa tästä alaspäin
FADE = 24          # rivit, joiden aikana alareunan oikea rivi liukuu tasoitettuun väriin

def blur1d(a, sig):
    r = int(sig * 3); k = np.exp(-0.5 * (np.arange(-r, r + 1) / sig) ** 2); k /= k.sum()
    pad = np.concatenate([np.repeat(a[:1], r, 0), a, np.repeat(a[-1:], r, 0)], 0)
    return np.stack([np.convolve(pad[:, c], k, "valid") for c in range(a.shape[1])], 1)

def extend_pair(d, left, right):
    A = np.concatenate([np.array(Image.open(os.path.join(d, left + ".png")).convert("RGBA")),
                        np.array(Image.open(os.path.join(d, right + ".png")).convert("RGBA"))], axis=1)
    h, w = A.shape[:2]
    rgb = A[:, :, :3].astype(float)
    bot = rgb[h - 8:h - 1].mean(0)                       # (w,3) alareunan väri
    base = blur1d(bot, 90)
    strip = Image.fromarray(A[FLOOR_TOP:h, :, :3])
    hp = np.array(strip, float) - np.array(strip.filter(ImageFilter.GaussianBlur(7)), float)   # lattian hienorakenne
    rng = np.random.default_rng(7)
    rows = []
    n = 0
    while n < EXT:
        t = hp if len(rows) % 2 == 0 else hp[::-1]
        rows.append(np.roll(t, int(rng.integers(0, w)), axis=1)); n += t.shape[0]
    tex = np.concatenate(rows, 0)[:EXT]
    y = np.arange(EXT)[:, None, None]
    fade = np.clip(y / float(FADE), 0, 1)
    ext = bot[None] * (1 - fade) + base[None] * fade + tex * 0.8 * fade
    ext = np.clip(ext, 0, 255).astype(np.uint8)
    ext = np.concatenate([ext, np.full((EXT, w, 1), 255, np.uint8)], axis=2)
    res = np.concatenate([A, ext], axis=0)
    half = w // 2
    Image.fromarray(res[:, :half]).save(os.path.join(d, left + "_iso.png"))
    Image.fromarray(res[:, half:]).save(os.path.join(d, right + "_iso.png"))
    print(left, right, A.shape[:2], "->", res.shape[:2])

def main(d):
    extend_pair(d, "vankila_kaytava_d1", "vankila_kaytava_d1b")
    extend_pair(d, "vankila_kaytava_e1", "vankila_kaytava_e1b")

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "Assets/Sprites/Taustat")
