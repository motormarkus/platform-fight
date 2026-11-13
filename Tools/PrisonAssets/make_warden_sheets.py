"""Warden: video1 idle 0-56, kävely 60-150, juoksu 174-239; video2 iskut (jabi, takasuora, haymaker, etupotku, roundhouse); video3 osuma/kaatuminen/nousu. Alkuperäinen docstring: kolme videota -> Assets/Sprites/Viholliset/warden_<nimi>.png.
Video 1 (vihreä tausta): idle 0-83, kävely 90-145, juoksu 150-239. Video 2: osuma 10-18 (kipinä kuvassa 9 jätetty pois), kaatuminen 18-63, nousu 84-180.
Video 3: jab 6-16, takasuora 14-28, koukku 34-50, etupotku 54-76, roundhouse 86-97.
Pölypilvet ja kipinät (pienet irralliset alueet) poistetaan. Ruudut kuten muilla (hahmo ~640 px, jalat 16 px ylös).
Ajo: python Tools/PrisonAssets/make_warden_sheets.py <video1> <video2> <video3> [ulos-kansio]"""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
from make_warden_uppercut import load_soft   # pehmeä vihreäavain (pehmeä alfa, mitalinauhat säilyvät)
m.load = load_soft
CELLS = {"idle": (768, 320, 768), "kavely": (768, 320, 768), "juoksu": (1024, 384, 768),
         "isku1": (1152, 320, 768), "isku2": (1152, 320, 768), "isku3": (1408, 448, 768), "potku1": (1408, 320, 768), "potku2": (1408, 320, 768),
         "osuma": (1024, 420, 768), "kaatuminen": (1280, 576, 1024), "nousu": (1280, 576, 768)}
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
    V3 = [clean(f) for f in V3]
    def kf(v):
        x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
    ks = [kf(v) for v in (V1, V2, V3)]; print("mittakaavat", [round(k, 3) for k, _ in ks])
    def sheet(name, vi, idx, mode):
        V = (V1, V2, V3)[vi]; k, fl = ks[vi]
        m.setdims("v_" + name)
        m.save([m.cell(V[i], mode, k, fl) for i in idx], f"{outs}/warden_{name}.png"); print(name, len(idx), "kuvaa", list(idx))
    sheet("idle", 0, m.best_loop(V1, 0, 56, 12, 28, "low")[::2], "low")
    sheet("kavely", 0, m.best_loop(V1, 60, 150, 16, 50, "torso")[::2], "torso")
    sheet("juoksu", 0, m.best_loop(V1, 174, 239, 10, 16, "torso"), "torso")
    sheet("isku1", 1, [8, 10, 12, 14, 16, 18, 22, 26], "low")                  # jabi: osuma 4 (kuva 16)
    sheet("isku2", 1, [30, 32, 34, 36, 38, 40, 44, 48, 52], "low")             # takasuora: osuma 3 (kuva 36)
    sheet("isku3", 1, [156, 160, 164, 168, 172, 176, 180, 184, 188, 192, 196, 200, 204, 208], "low")   # haymaker: osuma 7 (kuva 184)
    sheet("potku1", 1, [76, 80, 82, 84, 86, 88, 90, 92, 96], "low")           # etupotku: osuma 5 (kuva 88)
    sheet("potku2", 1, [98, 100, 102, 104, 106, 108, 110, 112, 114, 116, 118, 120], "low")   # roundhouse: osuma 7 (kuva 112)
    sheet("osuma", 2, [8, 12, 16, 20], "low")
    sheet("kaatuminen", 2, [20, 26, 32, 38, 44, 50, 56, 60, 64, 70], "com")
    sheet("nousu", 2, [100, 106, 112, 118, 124, 130, 136, 142, 148, 156, 164, 172, 180], "com")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset")
