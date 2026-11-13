"""Melvin: osuma-reaktio ständi kädessä (8 hahmoa kuvassa melvin_osuma_standi_lahde.png) -> Assets/Sprites/Viholliset/melvin_osuma_standi.png (ruutu 1152 x 768).
Mittakaava 600/414 = sama kuin idle (hahmon korkeus ~600 px). Ajo: python Tools/MelvinAssets/make_melvin_osuma_standi.py"""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.DIMS["v_os"] = (1152, 576, 768); m.setdims("v_os")
im = cv2.cvtColor(cv2.imread(os.path.join(HERE, "melvin_osuma_standi_lahde.png"), cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)
cols = (im[..., 3] > 128).sum(0) > 2; runs = []; s0 = None
for x, c in enumerate(cols):
    if c and s0 is None: s0 = x
    if not c and s0 is not None: runs.append((s0, x)); s0 = None
k = 600.0 / 414.0; cells = []
for (a, b) in runs:
    full = np.zeros((480, b - a + 40, 4), np.uint8); full[:, 20:20 + b - a] = im[:, a:b]
    cells.append(m.cell(full, "low", k, 448))
m.save(cells, os.path.join(HERE, "..", "..", "Assets/Sprites/Viholliset/melvin_osuma_standi.png")); print(len(cells), "kuvaa")
