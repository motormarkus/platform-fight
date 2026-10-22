"""Roccon kannin ylosnousu (kuva 1536x1024, 2 x 4, läpinäkyvä, katsoo oikealle): Assets/Resources/Kanni/rocco_nousu_00..07.png (768 x 448).
Mittakaava: viimeinen kuva (tappeluasento) samaan kokoon kuin kaatumissarjan ensimmäinen."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_lenkkari2 import *
SRC = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/009e04be-image.png"
S = float(os.environ.get("SN", 0.95))
out = sys.argv[1]; os.makedirs(out, exist_ok=True)
f = figs(cv2.imread(SRC, cv2.IMREAD_UNCHANGED), (2, 4), 20000)
print(len(f))
K = [0.70, 0.80, 0.90, 0.92, 1.0, 1.0, 1.0, 1.0]
for k, fig in enumerate(f):
    r = resize_premult(fig, S * K[k])
    cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, anchor_x(r, "com" if k < 4 else "low"), r.shape[0])
    cv2.imwrite(f"{out}/rocco_nousu_{k:02d}.png", cell)
    ys, xs = np.where(cell[..., 3] > 128); print(k, ys.max() - ys.min(), xs.max() - xs.min())
