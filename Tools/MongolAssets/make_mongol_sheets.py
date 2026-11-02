"""Mongolisoturi (vihollinen): vihreätaustaisista videoista + suojakuvasta kuvasarjat Assets/Sprites/Viholliset/mongoli_<nimi>.png.
Video A (idle + kivenheitto), B (hyppypotku), C (potkut: 1 korkea etupotku, 2 vaakapotku, 3 pyöräytyspotku), suoja.png 4 kuvaa vierekkäin.
Ruudut kuten mainarilla (Tools/MinerAssets): 4 saraketta, hahmo ~640 px, jalat 16 px ylös.
Video D (kaksi suoraa lyöntiä + etupotku, potkua ei käytetä), video E (idle, kävely, juoksu; kypärän punainen tupsu poistetaan kuvista, koska muissa videoissa sitä ei ole).
Ajo: python Tools/MongolAssets/make_mongol_sheets.py <idle_heitto.mov> <hyppypotku.mov> <potkut.mov> <suoja.png> <suorat.mov> <liike.mp4> <osuma_kaatuminen.mp4> [ulos-sheetit]
Video F: osuma, kaatuminen, maahan, nousu."""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m

m.TARGET_H = 640.0
m.DIMS.update({"g_heitto": (896, 320, 768), "g_hyppypotku": (1152, 384, 1024), "g_potku1": (1024, 320, 768),
               "g_potku2": (1152, 320, 768), "g_potku3": (1152, 448, 896), "g_suoja": (640, 320, 768),
               "g_osuma": (896, 420, 768), "g_kaatuminen": (1152, 576, 1024), "g_maahan": (1152, 576, 768), "g_nousu": (1152, 576, 768),
               "g_suora1": (896, 320, 768), "g_suora2": (896, 320, 768), "g_kavely": (640, 320, 768), "g_juoksu": (768, 320, 768), "g_idle": (640, 320, 768)})


def strip_plume(im):
    """Poistaa kypärän punaisen tupsun: punaiset pikselit kultaisen vanteen yläreunan yläpuolelta (vartalon ja niskasuojan punainen jää)."""
    a = im[..., 3] > 128; ys, xs = np.where(a); y0 = ys.min(); h = ys.max() - y0
    rgb = im[..., :3].astype(np.int32); R, G, B = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    gold = a & (R > 140) & (G > 85) & (G < 175) & (B < 115) & (R > G * 1.15)
    zone = slice(y0, y0 + int(h * 0.30))
    rows = gold[zone].sum(1); band = np.where(rows >= 14)[0]
    if len(band) == 0: return im
    top = y0 + band[0]
    red = a & (R > 40) & (R > G * 1.45) & (R > B * 1.3)
    mx = np.maximum(np.maximum(R, G), B); mn = np.minimum(np.minimum(R, G), B)
    core = a & ~red & ((mx - mn) < 32) & (mx > 70)
    k7 = np.ones((7, 7), np.uint8)
    keepz = cv2.dilate(core.astype(np.uint8), k7).astype(bool) | cv2.dilate(gold.astype(np.uint8), k7).astype(bool)
    kill = np.zeros_like(a); kill[:top] = (red | (a & ~keepz))[:top]
    kill = cv2.dilate(kill.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool) & a & ~core
    kill[top:] = False
    out = im.copy(); out[kill, 3] = 0
    return out


def main(vA, vB, vC, guard, vD, vE, vF, outs):
    A, B, C, D = m.load(vA), m.load(vB), m.load(vC), m.load(vD)
    E0 = m.load(vE); E = [strip_plume(f) for f in E0]   # mittakaava lasketaan tupsun/piikin poistoa edeltävästä kuvasta
    F = m.load(vF)
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    kA, fA = kf(A); kB, fB = kf(B); kC, fC = kf(C); kD, fD = kf(D); kE, fE = kf(E0); kF, fF = kf(F)
    print("mittakaavat", round(kA, 3), round(kB, 3), round(kC, 3), round(kD, 3), round(kE, 3), round(kF, 3))

    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("g_" + name)
        cells = [m.cell(vid[i], mode if isinstance(mode, str) else mode(i), k, floor) for i in idx]
        m.save(cells, f"{outs}/mongoli_{name}.png"); print(name, len(cells), "kuvaa", list(idx))

    sheet("heitto", A, list(range(51, 120, 3)), "low", kA, fA); print("heitto: nosto indeksi 9 (kuva 78), irtipäästö noin 9–10")
    sheet("hyppypotku", B, [4, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32], lambda i: "low" if i < 12 or i > 28 else "com", kB, fB); print("hyppypotku: osuma indekseissä 6–10 (kuvat 16–24)")
    sheet("potku1", C, [12, 14, 16, 18, 20, 22, 24, 26], "low", kC, fC); print("potku1: osuma indeksi 3–4 (kuvat 18–20)")
    sheet("potku2", C, [39, 41, 43, 45, 47, 49, 51, 53, 55, 57], "low", kC, fC); print("potku2: osuma indeksit 3–5 (kuvat 45–49)")
    sheet("potku3", C, [58, 60, 62, 64, 66, 68, 70, 72, 74, 76, 78, 81], "low", kC, fC); print("potku3: pyöräytys, osuma indeksit 6–8 (kuvat 70–74)")
    sheet("suora1", D, [0, 2, 4, 6, 8, 10, 12, 14, 16], "low", kD, fD); print("suora1: osuma indeksit 3–6 (kuvat 6–12)")
    sheet("suora2", D, [16, 18, 20, 22, 24, 26, 28, 30], "low", kD, fD); print("suora2: osuma indeksit 2–4 (kuvat 20–24)")
    sheet("idle", E, m.best_loop(E, 0, 36, 14, 36, "low"), "low", kE, fE)
    sheet("kavely", E, m.best_loop(E, 39, 112, 16, 50, "torso"), "torso", kE, fE)
    sheet("juoksu", E, m.best_loop(E, 114, 210, 10, 30, "torso"), "torso", kE, fE)
    # osuma/kaatuminen/maahan/nousu: video F (iskun välähdys kuvissa 10–14 jätetään pois)
    sheet("osuma", F, [9, 15, 17, 19, 21, 23], "low", kF, fF)
    sheet("kaatuminen", F, list(range(24, 84, 3)), "com", kF, fF)
    sheet("maahan", F, [84, 90, 96, 102, 108, 114], "com", kF, fF)
    sheet("nousu", F, list(range(117, 170, 3)), "com", kF, fF)
    G = cv2.imread(guard, cv2.IMREAD_UNCHANGED); gh, gw = G.shape[:2]; qw = gw // 4
    quads = [cv2.cvtColor(G[:, i * qw:(i + 1) * qw], cv2.COLOR_BGRA2RGBA) for i in range(4)]
    bb = [m.bbox(q) for q in quads]; kg = m.TARGET_H / (bb[0][3] - bb[0][2])
    m.setdims("g_suoja")
    cells = [m.cell(q, "low", kg, b[3]) for q, b in zip(quads, bb)]
    m.save(cells, f"{outs}/mongoli_suoja.png"); print("suoja 4 kuvaa")


if __name__ == "__main__":
    main(*sys.argv[1:8], sys.argv[8] if len(sys.argv) > 8 else "Assets/Sprites/Viholliset")
