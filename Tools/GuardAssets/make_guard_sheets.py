"""Vartija (vankila): vihreätaustainen video (idle 0-55, kävely 58-128, juoksu 130-239) -> Assets/Sprites/Viholliset/vartija_<nimi>.png.
Ruudut kuten Mongolisoturilla (4 saraketta, hahmo ~640 px, jalat 16 px ylös).
Ajo: python Tools/GuardAssets/make_guard_sheets.py <video.mp4> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
m.DIMS.update({"v_idle": (640, 320, 768), "v_kavely": (640, 320, 768), "v_juoksu": (896, 384, 768)})

def main(video, outs):
    V = m.load(video); print(len(V), "kuvaa")
    x0, x1, y0, y1 = m.bbox(V[0]); k = m.TARGET_H / (y1 - y0); floor = y1
    def sheet(name, idx, mode):
        m.setdims("v_" + name)
        m.save([m.cell(V[i], mode, k, floor) for i in idx], f"{outs}/vartija_{name}.png"); print(name, len(idx), "kuvaa", idx[0], "-", idx[-1])
    sheet("idle", m.best_loop(V, 0, 55, 14, 48, "low")[::2], "low")   # joka toinen kuva (12 kuvaa/s), muistia säästämään
    sheet("kavely", m.best_loop(V, 58, 128, 16, 50, "torso")[::2], "torso")
    sheet("juoksu", m.best_loop(V, 130, 239, 10, 30, "torso"), "torso")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
