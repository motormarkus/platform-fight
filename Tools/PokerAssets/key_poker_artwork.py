#!/usr/bin/env python3
"""Poistaa pokerin kuvien valkoisen taustan ja kirjoittaa RGBA-PNG:t.

Tausta = lähes valkoinen alue, joka koskettaa kuvan reunaa tai on riittävän iso
suljettu aukko (paneelien välit). Kuvan sisäiset valkoiset yksityiskohdat
(silmät, hampaat, kullan kiilto) säilyvät, koska ne eivät ole taustaa.
Vain taustan vieressä olevalle kapealle reunakaistalle tehdään pehmeä alpha ja
valkoisen värin poisto. Lähdekuvia ei muuteta.
Käyttö: python3 Tools/PokerAssets/key_poker_artwork.py
"""
import numpy as np
from collections import deque
from PIL import Image
import os

ART = "Assets/Resources/Poker/Artwork"
JOBS = [("TableOpponents.jpg", "TableOpponentsKeyed.png"), ("Panels.jpg", "PanelsKeyed.png")]
CORE, MIN_POCKET, BAND = 0.90, 16, 2
FRINGE, GREY_MIN, GREY_SAT, FRINGE_PASSES = 0.60, 0.30, 0.14, 5   # vaalea JPEG-reunus syödään taustaa vasten, kunnes tulee tumma ääriviiva

def smooth(lo, hi, x):
    t = np.clip((x - lo) / (hi - lo), 0, 1)
    return t * t * (3 - 2 * t)

def dilate(m, n):
    for _ in range(n):
        d = m.copy()
        d[1:] |= m[:-1]; d[:-1] |= m[1:]; d[:, 1:] |= m[:, :-1]; d[:, :-1] |= m[:, 1:]
        m = d
    return m

def background(white):
    h, w = white.shape
    seen = np.zeros_like(white); bg = np.zeros_like(white)
    for sy in range(h):
        for sx in range(w):
            if not white[sy, sx] or seen[sy, sx]: continue
            comp = []; q = deque([(sy, sx)]); seen[sy, sx] = True; border = False
            while q:
                y, x = q.popleft(); comp.append((y, x))
                if y in (0, h - 1) or x in (0, w - 1): border = True
                for yy, xx in ((y+1, x), (y-1, x), (y, x+1), (y, x-1)):
                    if 0 <= yy < h and 0 <= xx < w and white[yy, xx] and not seen[yy, xx]:
                        seen[yy, xx] = True; q.append((yy, xx))
            if border or len(comp) >= MIN_POCKET:
                ys, xs = zip(*comp); bg[ys, xs] = True
    return bg

for src, dst in JOBS:
    rgb = np.asarray(Image.open(os.path.join(ART, src)).convert("RGB")).astype(np.float32) / 255
    m = rgb.min(2)
    bg = background(m > CORE)
    for _ in range(FRINGE_PASSES):
        # vaalea tai harmaa (väritön) reunapikseli taustaa vasten = JPEG-halo; iho ja värit säilyvät
        grey = (rgb.max(2) - m < GREY_SAT) & (m > GREY_MIN)
        bg |= dilate(bg, 1) & ~bg & ((m > FRINGE) | grey)
    band = dilate(bg, BAND) & ~bg
    a = np.ones(m.shape, np.float32)
    a[bg] = 0
    a[band] = 1 - smooth(.45, .85, m[band])
    out = rgb.copy()
    k = band & (a > .02)
    out[k] = np.clip((rgb[k] - (1 - a[k, None])) / a[k, None], 0, 1)
    # Läpinäkyvien pikselien väri naapurista, ettei bilineaarinen suodatus tuo valkoista.
    known = (a > .5)
    for _ in range(8):
        grow = dilate(known, 1) & ~known
        acc = np.zeros_like(out); cnt = np.zeros(m.shape, np.float32)
        for dy, dx in ((1,0),(-1,0),(0,1),(0,-1)):
            ks = np.roll(known, (dy, dx), (0, 1)); vs = np.roll(out, (dy, dx), (0, 1))
            acc += vs * ks[..., None]; cnt += ks
        f = grow & (cnt > 0)
        out[f] = acc[f] / cnt[f, None]; known |= f
    rgba = np.dstack([out, a])
    Image.fromarray((rgba * 255 + .5).astype(np.uint8), "RGBA").save(os.path.join(ART, dst))
    print(dst, "tausta", round(float(bg.mean()), 3), "kaista-px", int(band.sum()))
