"""Kuun pinnan kuvien saumojen pehmennys: joka saumassa kummankin kuvan reunasarakkeet sekoitetaan kohti toisen kuvan reunaväriä (rivi riviltä),
jolloin kova pystyviiva häviää. Geometria ei muutu (kuvat samoilla paikoilla). Lähteenä aina Tools/MoonAssets/alkuperaiset (ajo on toistettavissa).
Käyttö: python Tools/MoonAssets/blend_seams.py"""
import os, shutil, numpy as np
from PIL import Image
T = "Assets/Sprites/Taustat/"; BK = "Tools/MoonAssets/alkuperaiset/"
ORDER = ["muuri_kuupinta_vasen3", "muuri_kuupinta_vasen2", "muuri_kuupinta_vasen1", "muuri_kuuasema_ulko", "muuri_kuupinta_a"]   # vasemmalta oikealle
N = 240; STRENGTH = 0.5
def load(n):
    bk = BK + n + ".png"
    if not os.path.exists(bk): shutil.copy2(T + n + ".png", bk)
    return np.array(Image.open(bk).convert("RGBA")).astype(np.float32)
imgs = {n: load(n) for n in ORDER}
for a, b in zip(ORDER[:-1], ORDER[1:]):
    L, R = imgs[a], imgs[b]
    le = L[:, -6:].mean(axis=1); re_ = R[:, :6].mean(axis=1)   # reunavärit riveittäin (RGBA)
    for d in range(N):
        w = STRENGTH * (1 - d / N) ** 1.5
        L[:, -1 - d] = L[:, -1 - d] * (1 - w) + re_ * w
        R[:, d] = R[:, d] * (1 - w) + le * w
for n, im in imgs.items():
    Image.fromarray(np.clip(im, 0, 255).astype(np.uint8), "RGBA").save(T + n + ".png"); print("blended", n)
