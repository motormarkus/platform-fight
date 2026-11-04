"""Gestapo (vihollinen, natsilinnakkeen juhlahalli): kaksi vihreätaustaista videota -> Assets/Sprites/Viholliset/gestapo_<nimi>.png.
Video A (idle 0-64, kävely 68-144, juoksu 148-239), video B (hyppypotku: kyykistys 64, ponnistus 72, potku 82-100, lasku 108-118).
Ruudut kuten mongolilla (Tools/MongolAssets): 4 saraketta, hahmo 640 px, jalat 16 px ylös.
Ajo: python Tools/GestapoAssets/make_gestapo_sheets.py <liike.mp4> <hyppypotku.mov> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m

m.TARGET_H = 640.0
m.DIMS.update({"x_idle": (640, 320, 768), "x_kavely": (640, 320, 768), "x_juoksu": (896, 400, 768), "x_hyppypotku": (1152, 384, 1024)})
KICK = list(range(64, 120, 3))   # 19 kuvaa


def main(vA, vB, outs):
    A, B = m.load(vA), m.load(vB)
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B)
    print("mittakaavat", round(kA, 3), round(kB, 3))

    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("x_" + name)
        cells = [m.cell(vid[i], mode if isinstance(mode, str) else mode(i), k, floor) for i in idx]
        m.save(cells, f"{outs}/gestapo_{name}.png"); print(name, len(cells), "kuvaa", list(idx))

    sheet("idle", A, m.best_loop(A, 0, 64, 14, 36, "low"), "low", kA, fA)
    sheet("kavely", A, m.best_loop(A, 68, 144, 16, 50, "torso"), "torso", kA, fA)
    sheet("juoksu", A, m.best_loop(A, 148, 239, 10, 30, "torso"), "torso", kA, fA)
    sheet("hyppypotku", B, KICK, lambda i: "com" if 72 <= i <= 108 else "low", kB, fB)
    print("hyppypotku: potku (jalka ojossa) kuvat 82-100 = indeksit", [n for n, i in enumerate(KICK) if 82 <= i <= 100], "; ponnistus 72 = indeksi", KICK.index(73) if 73 in KICK else "~3")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
