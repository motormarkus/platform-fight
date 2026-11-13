"""Rico (vanki): kolme videota -> Assets/Sprites/Viholliset/rico_<nimi>.png.
Video 1 (vihreä tausta): idle 0-83, kävely 90-145, juoksu 150-239. Video 2: osuma 10-18 (kipinä kuvassa 9 jätetty pois), kaatuminen 18-63, nousu 84-180.
Video 3: jab 6-16, takasuora 14-28, koukku 34-50, etupotku 54-76, roundhouse 86-97.
Pölypilvet ja kipinät (pienet irralliset alueet) poistetaan. Ruudut kuten muilla (hahmo ~640 px, jalat 16 px ylös).
Ajo: python Tools/PrisonAssets/make_rico_sheets.py <video1> <video2> <video3> [ulos-kansio]"""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
CELLS = {"idle": (640, 320, 768), "kavely": (640, 320, 768), "juoksu": (896, 384, 768),
         "isku1": (1024, 320, 768), "isku2": (1152, 320, 768), "isku3": (1024, 320, 768), "potku1": (1280, 320, 768), "potku2": (1280, 320, 768),
         "osuma": (896, 420, 768), "kaatuminen": (1152, 576, 1024), "nousu": (1152, 576, 768)}
m.DIMS.update({"v_" + k: v for k, v in CELLS.items()})

def clean(im):
    """Poistaa irralliset pienet alueet (pöly, kipinät): alle 4 % suurimmasta alueesta."""
    a = (im[..., 3] > 128).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(a, 8)
    if n <= 2: return im
    big = st[1:, cv2.CC_STAT_AREA].max()
    out = im.copy()
    for j in range(1, n):
        if st[j, cv2.CC_STAT_AREA] < 0.04 * big: out[lab == j, 3] = 0
    return out

def main(v1, v2, v3, outs):
    V1, V2, V3 = m.load(v1), m.load(v2), m.load(v3)
    V2 = [clean(f) for f in V2]
    def kf(v):
        x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
    ks = [kf(v) for v in (V1, V2, V3)]; print("mittakaavat", [round(k, 3) for k, _ in ks])
    def sheet(name, vi, idx, mode):
        V = (V1, V2, V3)[vi]; k, fl = ks[vi]
        m.setdims("v_" + name)
        m.save([m.cell(V[i], mode, k, fl) for i in idx], f"{outs}/rico_{name}.png"); print(name, len(idx), "kuvaa", idx)
    sheet("idle", 0, m.best_loop(V1, 0, 83, 20, 40, "low")[::2], "low")
    sheet("kavely", 0, m.best_loop(V1, 88, 148, 16, 50, "torso")[::2], "torso")
    sheet("juoksu", 0, m.best_loop(V1, 150, 239, 10, 16, "torso"), "torso")
    sheet("isku1", 2, [6, 8, 10, 12, 14, 16], "low")                          # jabi: osuma 3 (kuva 12)
    sheet("isku2", 2, [14, 16, 18, 20, 22, 24, 26, 28], "low")                # takasuora: osuma 3 (kuva 20)
    sheet("isku3", 2, [34, 36, 38, 40, 42, 44, 46, 48, 50], "low")            # koukku: osuma 4 (kuva 42)
    sheet("potku1", 2, [54, 56, 58, 60, 62, 64, 66, 68, 70, 72, 74, 76], "low")   # etupotku: osuma 4 (kuva 62)
    sheet("potku2", 2, [86, 88, 90, 92, 94, 96, 97], "low")                   # roundhouse: osuma 4 (kuva 94)
    sheet("osuma", 1, [10, 12, 14, 16], "low")
    sheet("kaatuminen", 1, [18, 24, 31, 37, 42, 46, 50, 54, 58, 63], "com")
    sheet("nousu", 1, [84, 92, 100, 108, 116, 124, 132, 140, 148, 156, 164, 172, 180], "com")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset")
