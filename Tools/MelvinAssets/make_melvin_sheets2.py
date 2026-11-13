"""Melvin ilman ständiä: iskut (D, 240 kuvaa 24 fps) ja osuma/kaatuminen/nousu (E) + suojaus (4 hahmoa kuvassa) -> Assets/Sprites/Viholliset/melvin_<nimi>.png.
Nimet: jabi (D 24-31, osuma 28-30), takasuora (D 32-41, osuma 36), potku = etupotku (D 54-75, osuma 64), osuma (E 25-31; kuvat 17-24 ovat iskun kipinätehoste, pois), kaatuminen (E 32-115, joka 3.), nousu (E 118-199, joka 3.), suojaus.
Ajo: python Tools/MelvinAssets/make_melvin_sheets2.py <D.mp4> <E.mp4> <suojaus.png> [ulos]"""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
sys.path.insert(0, os.path.join(HERE, "..", "TonyAssets")); from make_tony_sheets import clean
m.TARGET_H = 600.0
for k in ("jabi", "takasuora", "potku", "osuma", "kaatuminen", "nousu", "suojaus"): m.DIMS["v_" + k] = (1152, 576, 768 if k != "kaatuminen" else 1024)
D, E = m.load(sys.argv[1]), m.load(sys.argv[2]); outs = sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset"
def kf(v):
    x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
def sheet(name, V, idx, mode, fn=None, dy=0):
    k, fl = kf(V); fl += dy; m.setdims("v_" + name)
    m.save([m.cell((fn or (lambda x: x))(V[i]), mode, k, fl) for i in idx], f"{outs}/melvin_{name}.png"); print(name, len(idx), "kuvaa", idx)
sheet("jabi", D, list(range(24, 32)), "low")
sheet("takasuora", D, list(range(32, 42)), "low")
sheet("potku", D, list(range(54, 76)), "low")
sheet("osuma", E, list(range(25, 32)), "low", clean)
sheet("kaatuminen", E, list(range(32, 115, 3)) + [115], "com", None, 26)   # makuuasento on videossa ~22 px lattiaviivan alapuolella: nostetaan jalkojen tasolle
sheet("nousu", E, list(range(118, 199, 3)) + [199], "com")
im = cv2.cvtColor(cv2.imread(sys.argv[3], cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)
cols = (im[..., 3] > 20).sum(0) > 3; runs = []; s0 = None
for x, c in enumerate(cols):
    if c and s0 is None: s0 = x
    if not c and s0 is not None:
        if x - s0 > 80: runs.append((s0, x))
        s0 = None
if s0 is not None: runs.append((s0, len(cols)))
figs = []
for (a, b) in runs[:4]:
    sub = im[:, a:b]; ys, xs = np.where(sub[..., 3] > 128); figs.append(sub[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy())
k = m.TARGET_H / figs[0].shape[0]; print("suojaus: hahmoja", len(runs)); m.setdims("v_suojaus"); cells = []
for f in figs:
    full = np.zeros((f.shape[0] + 40, f.shape[1] + 40, 4), np.uint8); full[20:20 + f.shape[0], 20:20 + f.shape[1]] = f
    cells.append(m.cell(full, "low", k, f.shape[0] + 20))
m.save(cells, f"{outs}/melvin_suojaus.png"); print("suojaus", len(cells), "kuvaa")
