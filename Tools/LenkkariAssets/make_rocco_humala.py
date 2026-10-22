"""Roccon merimieskannin horjuva kavely videosta (avainnetut ruudut hw/): silmukka ruudut 179-235 (57), joka toinen -> 29 kuvaa.
Assets/Resources/Kanni/rocco_humala_00..28.png (448 x 384, pivot alhaalla keskella). Mittakaava: kuvan mediaanikorkeus 328 px (Roccon idle 331)."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_lenkkari import *
raw, out = sys.argv[1], sys.argv[2]
W, H = 448, 384
frames = list(range(179, 236, 2))
ims = [cv2.imread(f"{raw}/{i:03d}.png", cv2.IMREAD_UNCHANGED) for i in frames]
hs = [np.ptp(np.where(im[..., 3] > 128)[0]) for im in ims]
sc = 328.0 / np.median(hs); floor = int(np.percentile([np.where(im[..., 3] > 128)[0].max() for im in ims], 90))
print("mittakaava", sc, "lattia", floor)
os.makedirs(out, exist_ok=True)
for k, im in enumerate(ims):
    ys, xs = np.where(im[..., 3] > 128); x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1; crop = im[y0:y1, x0:x1]
    r = resize_premult(crop, sc); ax = anchor_x(crop, "torso") * sc; foot = (floor - y0) * sc
    cell = np.zeros((H, W, 4), np.uint8)
    xx = int(round(W / 2 - ax)); yy = int(round(H - 6 - foot))
    sx0, sy0 = max(0, -xx), max(0, -yy); dx0, dy0 = max(0, xx), max(0, yy)
    ww = min(r.shape[1] - sx0, W - dx0); hh = min(r.shape[0] - sy0, H - dy0)
    cell[dy0:dy0 + hh, dx0:dx0 + ww] = r[sy0:sy0 + hh, sx0:sx0 + ww]
    cv2.imwrite(f"{out}/rocco_humala_{k:02d}.png", cell)
print(len(ims))
