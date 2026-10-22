"""Roccon merimieskannin kaatumissarja (kuva 1536x1024, 2 saraketta x 4 riviä, läpinäkyvä tausta, katsoo oikealle):
Assets/Resources/Kanni/rocco_kanni_00..07.png (768 x 448, pivot alhaalla keskellä, ppu 100 koodissa Sprite.Create)."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_lenkkari2 import *
SRC = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/17b8384d-image.png"
S = float(os.environ.get("SK", 0.95))
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
im = cv2.imread(SRC, cv2.IMREAD_UNCHANGED)
f = figs(im, (2, 4), 20000)
print(len(f))
K = [1.0, 1.0, 0.88, 0.88, 0.72, 0.78, 0.78, 0.72]   # makaavat kuvat olivat lahteessa ~1,7 x pidempia kuin Roccon omat (330-370 px)
for k, fig in enumerate(f):
    r = resize_premult(fig, S * K[k])
    cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, anchor_x(r, "com" if k else "low"), r.shape[0])
    cv2.imwrite(f"{out}/rocco_kanni_{k:02d}.png", cell)
    ys, xs = np.where(cell[..., 3] > 128); print(k, ys.max() - ys.min(), xs.max() - xs.min())
