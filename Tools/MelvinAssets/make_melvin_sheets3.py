"""Melvin ILMAN ständiä: idle, kävely, juoksu yhdestä videosta (240 kuvaa 24 fps) -> Assets/Sprites/Viholliset/melvin_ilman_<nimi>.png.
Videossa: idle 0-64, kävely 66-148, juoksu 150-239. Ruutu 1152x768, sama mittakaava kuin muut Melvin-sheetit (TARGET_H 600).
Ajo: python Tools/MelvinAssets/make_melvin_sheets3.py <video.mp4> [ulos]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 600.0
for k in ("ilman_idle", "ilman_kavely", "ilman_juoksu"): m.DIMS["v_" + k] = (1152, 576, 768)
A = m.load(sys.argv[1]); outs = sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset"
def kf(v):
    x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
def sheet(name, V, idx, mode):
    k, fl = kf(V); m.setdims("v_" + name)
    m.save([m.cell(V[i], mode, k, fl) for i in idx], f"{outs}/melvin_{name}.png"); print(name, len(idx), "kuvaa", idx)
print(len(A))
sheet("ilman_idle", A, m.best_loop(A, 0, 64, 20, 40, "low")[::2], "low")
sheet("ilman_kavely", A, m.best_loop(A, 66, 148, 14, 30, "torso"), "torso")
sheet("ilman_juoksu", A, m.best_loop(A, 150, 239, 10, 16, "torso"), "torso")
