"""Pensalenkkarin kavely: yksi 28 ruudun silmukka videosta (ruudut 78-105, saumaero pienin). python make_lenkkari_kavely.py <raw> <ulos>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_lenkkari import *
raw, out = sys.argv[1], sys.argv[2]
frames = list(range(78, 106)); sc = 0.5008
ims = [cv2.imread(f"{raw}/{i:03d}.png", cv2.IMREAD_UNCHANGED) for i in frames]
floor = int(np.percentile([np.where(im[..., 3] > 128)[0].max() for im in ims], 90)); items = []
for im in ims:
    ys, xs = np.where(im[..., 3] > 128); x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1; crop = im[y0:y1, x0:x1]
    items.append((resize_premult(crop, sc), anchor_x(crop, "torso") * sc, (floor - y0) * sc))
below = max(r.shape[0] - foot for r, _, foot in items); lift = max(0, int(np.ceil(below - FOOT_MARGIN + 2)))
cells = []
for r, ax, foot in items:
    cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, ax, foot, lift); cells.append(cell)
os.makedirs(out, exist_ok=True); write(out, "kavely", cells)
