"""Kuurobotti-pomo: osuma, kaatuminen, maahan, nousu (video A) ja liukutaklaus (video B) -> robotti_pomo_<nimi>.png.
Video A (osuma 26-40, kaatuminen 40-76 (iskeytyminen 58-64), maahan 78-126, nousu 128-212); video B (valmistelu 8-23, liuku 26-62, palautuminen 65-80).
Ajo: python Tools/RobotAssets/make_robot_boss_sheets.py <osuma_kaatuminen.mp4> <liukutaklaus.mov> [ulos-kansio]"""
import os, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE)
import make_robot_sheets as r
m = r.m
m.DIMS.update({"r_pomo_osuma": (1024, 480, 768), "r_pomo_kaatuminen": (1152, 576, 1024), "r_pomo_maahan": (1152, 576, 768),
               "r_pomo_nousu": (1152, 576, 768), "r_pomo_liukutaklaus": (1792, 832, 896)})

_cell = m.cell
def _despill(c):
    """Liukutaklauksen pöly/kipinät jäävät vihreiksi (taustan vuoto): vihreä rajataan punaisen ja sinisen suurimpaan."""
    c = c.copy(); g = c[..., 1].astype(np.int16); mx = np.maximum(c[..., 0], c[..., 2]).astype(np.int16)
    c[..., 1] = np.minimum(g, mx + 8).astype(np.uint8)
    return c
m.cell = lambda *a, **k: _despill(_cell(*a, **k))

def main(vA, vB, outs):
    A, B = m.load(vA), m.load(vB)
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); print("mittakaavat", round(kA, 3), round(kB, 3))
    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("r_pomo_" + name)
        cells = [m.cell(vid[i], mode if isinstance(mode, str) else mode(i), k, floor) for i in idx]
        m.save(cells, f"{outs}/robotti_pomo_{name}.png"); print(name, len(cells), "kuvaa", list(idx))
    sheet("osuma", A, [26, 28, 30, 32, 34, 36, 38, 40], "low", kA, fA)
    sheet("kaatuminen", A, list(range(40, 78, 3)), "com", kA, fA)
    sheet("maahan", A, [78, 90, 102, 114, 122, 126], "com", kA, fA)
    sheet("nousu", A, list(range(128, 214, 4)), "com", kA, fA)
    idx = [8, 11, 14, 17, 20, 23, 26, 29, 32, 38, 44, 50, 56, 62, 65, 68, 71, 74, 77, 80]
    sheet("liukutaklaus", B, idx, lambda i: "low" if i <= 20 or i >= 68 else "com", kB, fB)
    print("liukutaklaus: valmistelu indeksit 0-5, liuku 6-13 (osuma), palautuminen 14-19")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
