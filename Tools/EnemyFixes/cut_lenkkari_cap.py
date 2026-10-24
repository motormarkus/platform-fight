"""Pensalenkkarin lippis irti: lentävä lakki leikataan kuvista omaksi spriteksi (Assets/Resources/Lenkkari/cap.png), jotta peli voi lennättää sen
sujuvasti ammuksena. Lakki poistetaan hahmokuvista, joissa se on irti (lenkkari_lakki 2-3, lenkkari_lakki_suora 0); kuvissa 0-1 se on vielä kädessä/päällä.
Alkuperäiset: Tools/EnemyFixes/orig/. Ajo: python Tools/EnemyFixes/cut_lenkkari_cap.py (suorita fix_lenkkari_lakki_pants.py ensin)"""
import os
import numpy as np
import cv2
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPR = os.path.join(ROOT, "Assets", "Sprites", "Viholliset")
ORIG = os.path.join(ROOT, "Tools", "EnemyFixes", "orig")
OUT = os.path.join(ROOT, "Assets", "Resources", "Lenkkari")
CW, CH = 768, 448
JOBS = [("lenkkari_lakki.png", SPR, [2, 3]), ("lenkkari_lakki_suora.png", ORIG, [0])]


def cap_component(cell):
    a = (cell[..., 3] > 20).astype(np.uint8)
    k = cv2.dilate(a, np.ones((5, 5), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(k, connectivity=8)
    if n < 3: return None
    comps = sorted(range(1, n), key=lambda j: -st[j, 4])
    return (lab == comps[1]).astype(np.uint8)       # toiseksi suurin = irti oleva lakki


def main():
    os.makedirs(OUT, exist_ok=True)
    best = None
    for name, src, cells in JOBS:
        im = np.array(Image.open(os.path.join(src, name)).convert("RGBA"))
        out = im.copy()
        for i in cells:
            c = im[:, i * CW:(i + 1) * CW]
            comp = cap_component(c)
            if comp is None: continue
            mask = comp.astype(bool) & (c[..., 3] > 0)
            area = int(mask.sum())
            if best is None or area > best[0]:
                ys, xs = np.where(mask)
                y0, y1, x0, x1 = ys.min() - 2, ys.max() + 3, xs.min() - 2, xs.max() + 3
                piece = c[y0:y1, x0:x1].copy(); piece[..., 3] = np.where(mask[y0:y1, x0:x1], piece[..., 3], 0)
                best = (area, piece, name, i, (x0 + x1) / 2.0, (y0 + y1) / 2.0)
            cell = out[:, i * CW:(i + 1) * CW]
            cell[comp.astype(bool)] = 0
        dst = os.path.join(SPR, name)
        Image.fromarray(out, "RGBA").save(dst)
    Image.fromarray(best[1], "RGBA").save(os.path.join(OUT, "cap.png"))
    print("cap", best[1].shape, "lahde", best[2], best[3], "keskipiste solussa", best[4], best[5])


if __name__ == "__main__":
    main()
