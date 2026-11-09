"""Kenraalin suojaussarja (4 kuvaa vierekkäin) ja heijari (6 kuvaa, 3 x 2) käyttäjän omista kuvista -> kenraali_suoja.png ja kenraali_heijari.png.
Ruudut kuten muillakin kenraalin sheeteillä; ensimmäisen kuvan korkeus skaalataan 640 px:ksi (sama asento kuin videoiden idlen ensimmäinen kuva).
Ajo: python Tools/GeneralAssets/make_general_extra.py <suoja.png> <heijari.png> [ulos-sheetit]"""
import os, sys
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
m.DIMS.update({"g_suoja": (768, 384, 768), "g_heijari": (1280, 440, 768)})

import cv2

def frames(path, cols, rows):
    """Jakaa kuvan hahmoihin yhtenäisten alueiden mukaan (ei tasaisiin ruutuihin: käsi ja nyrkki ylittävät ruudun rajan, ja pienet roskat naapuriruudusta tulevat mukaan).
    Pienet pikselijäljet (alle 2 % suurimmasta) hylätään; hahmot järjestetään riveittäin vasemmalta oikealle."""
    a = np.array(Image.open(path).convert("RGBA")); mask = (a[..., 3] > 20).astype(np.uint8)
    n, lab, stats, cen = cv2.connectedComponentsWithStats(cv2.dilate(mask, np.ones((15, 15), np.uint8)))
    comps = [(i, stats[i, cv2.CC_STAT_AREA]) for i in range(1, n)]
    biggest = max(ar for _, ar in comps)
    keep = [i for i, ar in comps if ar > 0.02 * biggest]
    if len(keep) != cols * rows: print("HUOM: löytyi", len(keep), "hahmoa, odotettiin", cols * rows)
    keep.sort(key=lambda i: (int(cen[i][1] // (a.shape[0] / rows)), cen[i][0]))
    out = []
    for i in keep:
        x, y, w, h = stats[i, 0], stats[i, 1], stats[i, 2], stats[i, 3]
        f = a[y:y + h, x:x + w].copy(); own = (lab[y:y + h, x:x + w] == i)
        f[~own, 3] = 0   # naapurihahmojen ja roskien pikselit pois
        out.append(f)
    return out

def sheet(name, frs, outs):
    bb = [m.bbox(f) for f in frs]
    k = m.TARGET_H / (bb[0][3] - bb[0][2])   # mittakaava ensimmäisestä kuvasta
    m.setdims("g_" + name)
    cells = [m.cell(f, "low", k, b[3]) for f, b in zip(frs, bb)]   # jokaisen kuvan jalat lattialla (kuvat on leikattu hahmoittain)
    m.save(cells, f"{outs}/kenraali_{name}.png"); print(name, len(cells), "kuvaa, mittakaava", round(k, 3))

if __name__ == "__main__":
    outs = sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset"
    sheet("suoja", frames(sys.argv[1], 4, 1), outs)
    sheet("heijari", frames(sys.argv[2], 3, 2), outs)
