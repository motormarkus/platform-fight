"""Gestapo, osa 2: osuma, kaatuminen, maahan, nousu (zip: 151 siistittyä RGBA-framea, 910x512) sekä suora lyönti ja etupotku (vihreätaustainen video, 43 kuvaa).
Ruudut kuten make_gestapo_sheets.py (hahmo 640 px). Ajo: python Tools/GestapoAssets/make_gestapo_sheets2.py <framet-kansio> <lyonnit.mov> [ulos-kansio]
Framet: osuma 5-13 (iskun väri), kaatuminen 14-56 (ilmassa), maahan 59-86, nousu 88-124 (loput seisoo). Video: lyönti kuvat 3-21 (osuma 6-16), potku 21-37 (jalka ojossa 24-30)."""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m

m.TARGET_H = 640.0
m.DIMS.update({"x_suora1": (896, 320, 768), "x_potku": (1152, 320, 768), "x_osuma": (896, 420, 768), "x_kaatuminen": (1152, 576, 1024), "x_maahan": (1152, 576, 768), "x_nousu": (1152, 576, 768)})


def main(fdir, vC, outs):
    F = [cv2.cvtColor(cv2.imread(os.path.join(fdir, "%04d.png" % i), cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA) for i in range(1, 152)]
    C = m.load(vC)
    def kf(im):
        x0, x1, y0, y1 = m.bbox(im); return m.TARGET_H / (y1 - y0), y1
    kF, fF = kf(F[0]); kC, fC = kf(C[0])
    print("mittakaavat", round(kF, 3), round(kC, 3))

    def sheet(name, frames, idx, mode, k, floor):
        m.setdims("x_" + name)
        cells = [m.cell(frames[i], mode, k, floor) for i in idx]
        m.save(cells, f"{outs}/gestapo_{name}.png"); print(name, len(cells), "kuvaa", list(idx))

    # framet ovat 1-pohjaisia tiedostoja: indeksi = numero - 1
    sheet("osuma", F, [n - 1 for n in (5, 7, 9, 11, 13)], "low", kF, fF)
    sheet("kaatuminen", F, [n - 1 for n in range(14, 57, 3)], "com", kF, fF)
    sheet("maahan", F, [n - 1 for n in (59, 62, 66, 72, 78, 84)], "com", kF, fF)
    sheet("nousu", F, [n - 1 for n in range(86, 125, 2)], "com", kF, fF)
    sheet("suora1", C, list(range(3, 22, 2)), "low", kC, fC); print("suora1: osuma indeksit 2-6 (kuvat 7-15)")
    sheet("potku", C, list(range(21, 38, 2)), "low", kC, fC); print("potku: osuma indeksit 2-4 (kuvat 25-29)")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
