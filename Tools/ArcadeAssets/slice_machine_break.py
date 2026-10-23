"""Pelikoneiden hajoamiskuvat (3 x 2 ruudukko, 6 kuvaa/kone) -> Assets/Sprites/Pelihalli/kone_<nimi>_<0-5>.png
0 ehjä, 1 halkeaa, 2 pahasti rikki, 3 hajoaa, 4 romuaa, 5 romukasa. Aja: python Tools/ArcadeAssets/slice_machine_break.py"""
import os, sys
import cv2
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "SpaceGame"))
import slice_sprites as S

OUT = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Pelihalli")
os.makedirs(OUT, exist_ok=True)
SCALE = 0.5
for name in ("flipper", "turpaan", "ralli"):
    img = cv2.imread(os.path.join(HERE, "src", "hajoaa_" + name + ".png"), cv2.IMREAD_COLOR)
    H, W = img.shape[:2]
    rgba = S.key(img)
    lab, comps = S.components(rgba, 2, min_area=20)
    cells = {}
    for (x, y, w, h, i) in comps:
        cx, cy = x + w / 2, y + h / 2
        c = min(2, int(cx // (W / 3))); r = 0 if cy < H * 0.5 else 1
        cells.setdefault((r, c), []).append(i)
    frames = []
    for r in range(2):
        for c in range(3):
            cs = cells.get((r, c), [])
            if cs:
                big = max(comps, key=lambda q: q[2] * q[3] if q[4] in cs else -1)
                bx0, by0, bx1, by1 = big[0], big[1], big[0] + big[2], big[1] + big[3]
                def gap(q):
                    dx = max(q[0] - bx1, bx0 - (q[0] + q[2]), 0)
                    dy = max(q[1] - by1, by0 - (q[1] + q[3]), 0)
                    return max(dx, dy)
                ids = [q[4] for q in comps if q[4] in cs and gap(q) <= 90]
            else:
                ids = []
            m = np.isin(lab, ids)
            sub = rgba.copy()
            sub[~m] = 0
            # rajaa solun alueeseen ja trimmaa
            x0, x1 = int(c * W / 3) - 40, int((c + 1) * W / 3) + 40
            y0, y1 = (0, int(H * 0.5) + 10) if r == 0 else (int(H * 0.5) - 10, H)
            sub = sub[max(0, y0):y1, max(0, x0):min(W, x1)]
            frames.append(S.trim(sub, 2))
    for k, f in enumerate(frames):
        out = S.resize(f, SCALE)
        cv2.imwrite(os.path.join(OUT, "kone_%s_%d.png" % (name, k)), out)
    print(name, [f.shape[:2] for f in frames])
