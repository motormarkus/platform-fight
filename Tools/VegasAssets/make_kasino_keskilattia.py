"""Kasinon keskitason oma lattiakaistale (parallaksi 0,5), jotta pöydät eivät näytä leijuvan takataustan (0,2) lattian päällä.
Kaistale = etualan oman maton pätkä: ylhäällä messinkireunus (sama kuin etualan lattian yläreuna), alla maton pintaa.
Kuvat 5120x1844 (sama asettelu kuin keski-kuvat): kaistale on riveillä TOP..BOTTOM, muu läpinäkyvä.
Ajo: python Tools/VegasAssets/make_kasino_keskilattia.py"""
import os
import numpy as np
from PIL import Image
T = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Sprites", "Taustat")
TOP, BOTTOM, TRIM = 950, 1206, 26      # pöytien jalat ~1020-1040, etualan lattia alkaa ~1200
for front, out in (("etu", "keski_lattia"), ("etu_baari", "keski_lattia_baari"), ("etu_vasen", "keski_lattia_vasen")):
    a = np.array(Image.open(os.path.join(T, f"vegas_kasino_{front}.png")).convert("RGBA"))
    full = np.where((a[..., 3] > 250).mean(1) > 0.99)[0]; t0 = int(full[full > 1100].min())   # etualan lattian ensimmäinen täysi rivi (reunus alkaa)
    res = np.zeros_like(a)
    res[TOP:TOP + TRIM] = a[t0:t0 + TRIM]
    n = BOTTOM - (TOP + TRIM)
    carpet = a[t0 + TRIM + 4:t0 + TRIM + 4 + n].copy()
    res[TOP + TRIM:BOTTOM] = carpet
    res[..., 3] = np.where(np.arange(a.shape[0])[:, None] >= TOP, np.where(np.arange(a.shape[0])[:, None] < BOTTOM, 255, 0), 0)
    Image.fromarray(res).save(os.path.join(T, f"vegas_kasino_{out}.png")); print(out, "t0", t0, "kaistale", TOP, "-", BOTTOM)
