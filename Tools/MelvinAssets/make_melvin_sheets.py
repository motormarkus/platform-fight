"""Melvin (Vegasin 2. vihu, mikkiständi): 3 vihreätaustaista videota -> Assets/Sprites/Viholliset/melvin_<nimi>.png.
A (240 kuvaa 24 fps): idle 0-80, kävely 84-118, juoksu 120-239. B (30 kuvaa): isku 1 (heilautus, osuma kuva 16-18). C (38 kuvaa): isku 2 (työntö, osuma 18).
Molempien iskujen staattinen alku (B 0-5, C 0-5, samat kuvat) jätetty pois, kaikki muut mukana. Ruutu 1152 x 768, ankkuri 576, jalat 16 px ylös.
Ajo: python Tools/MelvinAssets/make_melvin_sheets.py <A.mp4> <B.mov> <C.mp4> [ulos]"""
import os, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 600.0
for k in ("idle", "kavely", "juoksu", "isku1", "isku2"): m.DIMS["v_" + k] = (1152, 576, 768)
A, B, C = (m.load(p) for p in sys.argv[1:4]); outs = sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset"
def kf(v):
    x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
def sheet(name, V, idx, mode):
    k, fl = kf(V); m.setdims("v_" + name)
    m.save([m.cell(V[i], mode, k, fl) for i in idx], f"{outs}/melvin_{name}.png"); print(name, len(idx), "kuvaa", idx)
print(len(A), len(B), len(C))
sheet("idle", A, m.best_loop(A, 0, 80, 20, 40, "low")[::2], "low")
sheet("kavely", A, m.best_loop(A, 84, 118, 14, 30, "torso"), "torso")
sheet("juoksu", A, m.best_loop(A, 120, 239, 10, 16, "torso"), "torso")
sheet("isku1", B, list(range(6, len(B))), "low")
sheet("isku2", C, list(range(6, len(C))), "low")
