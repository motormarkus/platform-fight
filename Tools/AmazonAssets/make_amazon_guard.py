"""Amatsoni: suojauskuvasarja (4 kuvaa, läpinäkyvä PNG) -> amatsoni_suoja.png (512 x 768 ruudut, jalat 16 px ylhäällä, ppu 200).
Ajo: python Tools/AmazonAssets/make_amazon_guard.py <kuva.png> <ulos>"""
import os, sys
import cv2, numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from make_amazon_sheets import rescale, feet, cell, save
src, out = sys.argv[1:3]
a = np.array(Image.open(src).convert("RGBA"))
cols = (a[..., 3] > 8).any(axis=0)
runs, start = [], None
for x, v in enumerate(cols):
    if v and start is None: start = x
    if not v and start is not None:
        if x - start > 60: runs.append((start, x))
        start = None
if start is not None: runs.append((start, len(cols)))
print("kuvia", len(runs), runs)
frames = []
for x0, x1 in runs:
    c = a[:, x0:x1]; ys = np.where((c[..., 3] > 8).any(axis=1))[0]; frames.append(c[ys.min():ys.max() + 1])
k = 640.0 / frames[0].shape[0]; print("kerroin", round(k, 3))
cells = []
for f in frames:
    r = rescale(f, k); fl, ax = feet(r)
    cells.append(cell(r, ax, fl, 512, 256))
save(cells, 4, os.path.join(out, "amatsoni_suoja.png"))
