"""Don (Vegasin pomo, punapukuinen): 3 vihreätaustaista videota -> Assets/Sprites/Viholliset/don_<nimi>.png (ruutu 1152 x 768, ankkuri 576, kaatuminen 1024 korkea).
A (240 kuvaa 24 fps): idle 0-80, kävely 84-148, juoksu 156-239. B: iskut: jabi 31-50 (osuma 38), etupotku 55-76 (osuma 64), takasuora 82-100 (osuma 87),
kierrospotku 103-127 (osuma 113), nilkkapotku 131-152 (osuma 140), kyynärpää 158-227 (osuma 191; alku ja loppu joka 2. kuva). Staattinen asento 0-26 pois.
C: osuma 29-37 (kipinät pois clean-funktiolla), kaatuminen 38-98 (joka 3.), nousu 100-200 (joka 3.).
Ajo: python Tools/DonAssets/make_don_sheets.py <A.mp4> <B.mp4> <C.mp4> [ulos]"""
import os, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets")); sys.path.insert(0, os.path.join(HERE, "..", "TonyAssets"))
import make_king_sheets as m
from make_tony_sheets import clean
m.TARGET_H = 600.0
NAMES = ("idle", "kavely", "juoksu", "jabi", "potku", "takasuora", "kierros", "nilkka", "kyynarpaa", "osuma", "kaatuminen", "nousu")
for k in NAMES: m.DIMS["v_" + k] = (1152, 576, 768 if k != "kaatuminen" else 1024)
A, B, C = (m.load(p) for p in sys.argv[1:4]); outs = sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset"
def kf(v):
    x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
def sheet(name, V, idx, mode, fn=None, dy=0):
    k, fl = kf(V); fl += dy; m.setdims("v_" + name)
    m.save([m.cell((fn or (lambda x: x))(V[i]), mode, k, fl) for i in idx], f"{outs}/don_{name}.png"); print(name, len(idx), "kuvaa", idx)
sheet("idle", A, m.best_loop(A, 0, 80, 20, 40, "low")[::2], "low")
sheet("kavely", A, m.best_loop(A, 84, 148, 14, 30, "torso"), "torso")
sheet("juoksu", A, m.best_loop(A, 156, 239, 12, 24, "torso"), "torso")
sheet("jabi", B, list(range(31, 51)), "low")
sheet("potku", B, list(range(55, 77)), "low")
sheet("takasuora", B, list(range(82, 101)), "low")
sheet("kierros", B, list(range(103, 128)), "low")
sheet("nilkka", B, list(range(131, 153)), "low")
sheet("kyynarpaa", B, list(range(158, 185, 2)) + list(range(185, 208)) + list(range(209, 228, 2)), "low")
sheet("osuma", C, list(range(29, 38)), "low", clean)
sheet("kaatuminen", C, list(range(38, 98, 3)) + [98], "com", clean, 26)
sheet("nousu", C, list(range(100, 200, 3)) + [200], "com", clean)
