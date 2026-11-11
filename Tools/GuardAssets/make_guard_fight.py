"""Vartija: taistelukuvat. Video A (suorat + potku), video B (osuma, kaatuminen, maahan, nousu) -> Assets/Sprites/Viholliset/vartija_<nimi>.png.
Valitut kuvat: suora1 = jab (24-31), suora2 = takasuora (125-149), potku = takajalan etupotku (70-84); B: osuma 12-24, kaatuminen 26-60, maahan, nousu 132-188.
Ajo: python Tools/GuardAssets/make_guard_fight.py <A.mp4> <B.mp4> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
m.DIMS.update({"v_suora1": (896, 320, 768), "v_suora2": (1024, 320, 768), "v_potku": (1152, 384, 768),
               "v_osuma": (896, 420, 768), "v_kaatuminen": (1152, 576, 1024), "v_maahan": (1152, 576, 768), "v_nousu": (1152, 576, 768)})

def main(vA, vB, outs):
    A, B = m.load(vA), m.load(vB)
    def kf(v):
        x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); print("mittakaavat", round(kA, 3), round(kB, 3))
    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("v_" + name)
        m.save([m.cell(vid[i], mode if isinstance(mode, str) else mode(i), k, floor) for i in idx], f"{outs}/vartija_{name}.png"); print(name, len(idx), "kuvaa", idx)
    sheet("suora1", A, [24, 26, 27, 28, 29, 30, 31], "low", kA, fA); print("suora1: osuma indeksit 2-4")
    sheet("suora2", A, [125, 127, 129, 131, 134, 143, 146, 149], "low", kA, fA); print("suora2: osuma indeksit 2-4")
    sheet("potku", A, [70, 72, 73, 74, 75, 76, 77, 78, 80, 82, 84], "low", kA, fA); print("potku: osuma indeksit 3-7")
    sheet("osuma", B, [12, 16, 18, 20, 22, 24], "low", kB, fB)
    sheet("kaatuminen", B, [26, 29, 32, 35, 38, 41, 44, 47, 50, 53, 56, 60], "com", kB, fB)
    sheet("maahan", B, [64, 70, 90, 110, 122, 130], "com", kB, fB)
    sheet("nousu", B, list(range(132, 189, 4)), "com", kB, fB)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
