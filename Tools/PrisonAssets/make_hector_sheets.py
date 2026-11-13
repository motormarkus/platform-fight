"""Hector (vanki): vihreätaustainen video (idle 0-58, kävely 68-145, juoksu 150-239) -> Assets/Sprites/Viholliset/hector_<nimi>.png.
Ruudut kuten vartijalla (4 saraketta, hahmo ~640 px, jalat 16 px ylös).
Ajo: python Tools/PrisonAssets/make_hector_sheets.py <video.mp4> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
from make_warden_uppercut import load_soft   # pehmeä vihreäavain (pehmeät reunat)
m.load = load_soft
m.DIMS.update({"v_idle": (640, 320, 768), "v_kavely": (640, 320, 768), "v_juoksu": (896, 384, 768)})

def main(video, outs):
    V = m.load(video); print(len(V), "kuvaa")
    x0, x1, y0, y1 = m.bbox(V[0]); k = m.TARGET_H / (y1 - y0); floor = y1
    def sheet(name, idx, mode):
        m.setdims("v_" + name)
        m.save([m.cell(V[i], mode, k, floor) for i in idx], f"{outs}/hector_{name}.png"); print(name, len(idx), "kuvaa", idx[0], "-", idx[-1])
    sheet("idle", m.best_loop(V, 0, 59, 30, 58, "low")[::2], "low")
    sheet("kavely", m.best_loop(V, 68, 145, 16, 50, "torso")[::2], "torso")
    sheet("juoksu", m.best_loop(V, 150, 239, 10, 16, "torso"), "torso")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
