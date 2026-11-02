"""Mainari (työmaan vihollinen): neljästä vihreätaustaisesta videosta + suojakuvasta kuvasarjat Assets/Sprites/Viholliset/mainari_<nimi>.png.
A = liike (idle, kävely, juoksu), B = osuma/kaatuminen/maahan/nousu, C = jabi, D = kuperkeikka (alku, pallo, loppu + nousu), suojakuva 4 kuvaa (seisoma, suoja, suoja, seisoma).
Ruudut kuten luurangolla (Tools/SkeletonAssets): 4 saraketta, hahmo ~640 px, jalat 16 px ylös.
Ajo: python Tools/MinerAssets/make_miner_sheets.py <liike.mp4> <osuma.mov> <jabi.mov> <kuper.mov> <suoja.png> [ulos-sheetit]"""
import os, sys
import av, cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
from key_video import key_frame

m.TARGET_H = 640.0
m.DIMS.update({"m_idle": (640, 320, 768), "m_kavely": (640, 320, 768), "m_juoksu": (640, 320, 768), "m_suoja": (640, 320, 768), "m_jab": (768, 300, 768),
               "m_osuma": (896, 420, 768), "m_kaatuminen": (1024, 512, 768), "m_maahan": (1024, 512, 768), "m_nousu": (1024, 512, 768),
               "m_kuper_alku": (768, 384, 768), "m_kuper_loppu": (1024, 512, 768)})


def main(vA, vB, vC, vD, guard, outs):
    A, B, C, D = m.load(vA), m.load(vB), m.load(vC), m.load(vD)
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); kC, fC = kf(C); kD, fD = kf(D)
    print("mittakaavat", round(kA, 3), round(kB, 3), round(kC, 3), round(kD, 3))

    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("m_" + name)
        cells = [m.cell(vid[i], mode if isinstance(mode, str) else mode(i), k, floor) for i in idx]
        m.save(cells, f"{outs}/mainari_{name}.png"); print(name, len(cells), "kuvaa", list(idx))

    sheet("idle", A, m.best_loop(A, 0, 56, 14, 40, "low"), "low", kA, fA)
    sheet("kavely", A, m.best_loop(A, 60, 140, 16, 48, "torso"), "torso", kA, fA)
    sheet("juoksu", A, m.best_loop(A, 142, 239, 10, 26, "torso"), "torso", kA, fA)
    sheet("jab", C, [11, 12, 13, 14, 15, 16, 18, 20], "low", kC, fC); print("jab: osuma = indeksi 3")
    sheet("osuma", B, [7, 15, 17, 19, 21, 23], "low", kB, fB)
    sheet("kaatuminen", B, list(range(24, 54, 2)), "com", kB, fB)
    sheet("maahan", B, [54, 58, 62, 66, 70, 74, 78], "com", kB, fB)
    sheet("nousu", B, list(range(82, 126, 3)), "com", kB, fB)
    sheet("kuper_alku", D, list(range(3, 25, 3)), "low", kD, fD)
    sheet("kuper_loppu", D, [39, 42, 45, 48, 51, 54, 57, 60, 63, 66, 69, 72], lambda i: "com" if i < 55 else "low", kD, fD)
    # pallo: kaksi kyyristettyä kuvaa (39..45), kuva 512 x 512 keskitettynä (pyöritetään koodissa)
    balls = []
    for i in (42, 45):
        fr = D[i]; ys, xs = np.where(fr[..., 3] > 10)
        c = m.rescale(fr[ys.min():ys.max() + 1, xs.min():xs.max() + 1], kD)
        cell = np.zeros((512, 512, 4), np.uint8); h, w = c.shape[:2]
        y0, x0 = 256 - h // 2, 256 - w // 2
        cell[max(0, y0):y0 + h, max(0, x0):x0 + w] = c[max(0, -y0):, max(0, -x0):][:512 - max(0, y0), :512 - max(0, x0)]
        balls.append(cell)
    cv2.imwrite(f"{outs}/mainari_pallo.png", cv2.cvtColor(np.hstack(balls), cv2.COLOR_RGBA2BGRA)); print("pallo 2 kuvaa 512x512")
    # suoja: 4 kuvaa vierekkäin (läpinäkyvä tausta)
    G = cv2.imread(guard, cv2.IMREAD_UNCHANGED); gh, gw = G.shape[:2]; qw = gw // 4
    quads = [cv2.cvtColor(G[:, i * qw:(i + 1) * qw], cv2.COLOR_BGRA2RGBA) for i in range(4)]
    bb = [m.bbox(q) for q in quads]; kg = m.TARGET_H / (bb[0][3] - bb[0][2])
    m.setdims("m_suoja")
    cells = [m.cell(q, "low", kg, b[3]) for q, b in zip(quads, bb)]
    m.save([cells[i] for i in (0, 1, 2, 1)], f"{outs}/mainari_suoja.png"); print("suoja 4 kuvaa")


if __name__ == "__main__":
    main(*sys.argv[1:6], sys.argv[6] if len(sys.argv) > 6 else "Assets/Sprites/Viholliset")
