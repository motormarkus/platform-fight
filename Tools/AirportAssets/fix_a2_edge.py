"""Kadun lentokenttäkuva 2: oikean reunan lasinpala (x 1650-1671) peitetään pilarilla kopioimalla viereinen ikkunapuite, jotta liitoksesta ei näy taustaa läpi.
Ajo: python Tools/AirportAssets/fix_a2_edge.py   (sitten valikko 263)"""
from PIL import Image
import numpy as np, os
p = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat", "thaitown_lentokentta_katu_2_yla.png")
a = np.array(Image.open(p).convert("RGBA"))
src = a[:, 1640:1650].copy()   # puite
for x0 in range(1650, 1672, 10):
    w = min(10, 1672 - x0)
    for y in range(a.shape[0]):
        for k in range(w):
            if a[y, x0 + k, 3] < 200 and src[y, k, 3] > 200: a[y, x0 + k] = src[y, k]
Image.fromarray(a).save(p); print("ok")
