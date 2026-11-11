"""Rex (pelastettava, E13): video A (idle 0-72, kävely 83-145, juoksu 155-239), video B (iskut) -> Assets/Sprites/Viholliset/rex_<nimi>.png.
isku1 = tupla-jabi (27-48, osumat kuvissa 3 ja 5), isku2 = jabi (99-112, osuma 3-4), isku3 = takasuora (111-135, osuma 3-6). Video B:n 140-196 on varaus (kyyristys, puuttuva isku).
Ruudut kuten vartijalla (hahmo ~640 px, jalat 16 px ylös).
Ajo: python Tools/RexAssets/make_rex_sheets.py <A.mp4> <B.mp4> [ulos-kansio]"""
import os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
m.DIMS.update({"v_idle": (640, 320, 768), "v_kavely": (640, 320, 768), "v_juoksu": (896, 384, 768),
               "v_isku1": (1024, 320, 768), "v_isku2": (1024, 320, 768), "v_isku3": (1152, 320, 768)})

def main(vA, vB, outs):
    A, B = m.load(vA), m.load(vB)
    def kf(v):
        x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); print("mittakaavat", round(kA, 3), round(kB, 3))
    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("v_" + name)
        m.save([m.cell(vid[i], mode, k, floor) for i in idx], f"{outs}/rex_{name}.png"); print(name, len(idx), "kuvaa", idx)
    sheet("idle", A, m.best_loop(A, 0, 72, 14, 60, "low")[::2], "low", kA, fA)
    sheet("kavely", A, m.best_loop(A, 82, 150, 16, 50, "torso")[::2], "torso", kA, fA)
    sheet("juoksu", A, m.best_loop(A, 153, 239, 10, 30, "torso"), "torso", kA, fA)
    sheet("isku1", B, [27, 30, 33, 36, 39, 42, 45, 48], "low", kB, fB)
    sheet("isku2", B, [99, 102, 104, 105, 106, 108, 110, 112], "low", kB, fB)
    sheet("isku3", B, [111, 112, 114, 116, 117, 120, 123, 126, 129, 132, 135], "low", kB, fB)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
