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
from PIL import Image, ImageFilter
import os

UPSCALE_MIN_WIDTH = 2560   # jos lähdekuva on tätä kapeampi, se suurennetaan 2x; muuten käytetään sellaisenaan
UPSCALE = 2   # Unity suurentaa 1280x720-kuvan ruudulle; Lanczos 2x + kevyt terävöinti on tarkempi kuin pelkkä bilineaarinen suurennus
ART = os.environ.get("POKER_ART", "Assets/Resources/Poker/Artwork")
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

def background(white, min_pocket):
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
            if border or len(comp) >= min_pocket:
                ys, xs = zip(*comp); bg[ys, xs] = True
    return bg

for src, dst in JOBS:
    rgb = np.asarray(Image.open(os.path.join(ART, src)).convert("RGB")).astype(np.float32) / 255
    m = rgb.min(2)
    S = rgb.shape[1] / 1280.0                      # lähdekuvan koko suhteessa 1280x720-asetteluun
    UPSCALE = 1 if rgb.shape[1] >= UPSCALE_MIN_WIDTH else 2
    min_pocket, band_px, passes = max(4, int(MIN_POCKET * S * S)), max(1, round(BAND * S)), max(1, round(FRINGE_PASSES * S))
    bg = background(m > CORE, min_pocket)
    for _ in range(passes):
        # vaalea tai harmaa (väritön) reunapikseli taustaa vasten = JPEG-halo; iho ja värit säilyvät
        grey = (rgb.max(2) - m < GREY_SAT) & (m > GREY_MIN)
        bg |= dilate(bg, 1) & ~bg & ((m > FRINGE) | grey)
    band = dilate(bg, band_px) & ~bg
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
    h, w = a.shape; size = (w * UPSCALE, h * UPSCALE)
    if UPSCALE == 1:
        Image.fromarray((np.dstack([out, a]) * 255 + .5).astype(np.uint8), "RGBA").save(os.path.join(ART, dst)); print(dst, "valmis (ei suurennusta)"); continue
    pre = out * a[..., None]                       # premultiplied: ei valkoista vuotoa reunoille
    chans = [np.asarray(Image.fromarray(pre[..., c].astype(np.float32), "F").resize(size, Image.LANCZOS)) for c in range(3)]
    alpha = np.clip(np.asarray(Image.fromarray(a.astype(np.float32), "F").resize(size, Image.LANCZOS)), 0, 1)
    rgb2 = np.clip(np.dstack(chans) / np.maximum(alpha[..., None], .02), 0, 1)
    rgb_img = Image.fromarray((rgb2 * 255 + .5).astype(np.uint8), "RGB").filter(ImageFilter.UnsharpMask(radius=1.6, percent=55, threshold=2))
    rgba = np.dstack([np.asarray(rgb_img).astype(np.float32) / 255, alpha])
    Image.fromarray((rgba * 255 + .5).astype(np.uint8), "RGBA").save(os.path.join(ART, dst))
    print(dst, "tausta", round(float(bg.mean()), 3), "kaista-px", int(band.sum()))
