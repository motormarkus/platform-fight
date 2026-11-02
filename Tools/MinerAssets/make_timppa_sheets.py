"""Timppa (mainareiden pomo): iskuvideosta (jabi, takasuora, potku, osuma, kaatuminen, maahan, nousu) ja suojakuvasta kuvasarjat Assets/Sprites/Viholliset/timppa_<nimi>.png.
Ruudut: 4 saraketta, hahmo 720 px, jalat 16 px ylös. Mitat (leveys, ankkurin x, korkeus): jabi/takasuora/potku (1280, 560, 768), osuma (1024, 440, 768), kaatuminen/maahan/nousu (1152, 576, 768), suoja (768, 384, 768).
Ajo: python Tools/MinerAssets/make_timppa_sheets.py <iskut.mp4> <suoja.png> [ulos-sheetit]
Idle, kävely, heitto ja vasaraisku tulevat omista videoistaan (lisätään myöhemmin samaan skriptiin)."""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m

m.TARGET_H = 720.0
m.DIMS.update({"t_jab": (1024, 380, 768), "t_takasuora": (1024, 380, 768), "t_potku": (1280, 560, 768), "t_osuma": (1024, 440, 768),
               "t_kaatuminen": (1152, 576, 768), "t_maahan": (1152, 576, 768), "t_nousu": (1152, 576, 768), "t_suoja": (768, 384, 768),
               "t_idle": (768, 384, 896), "t_kavely": (768, 384, 896), "t_heitto": (1152, 400, 896), "t_vasara": (1024, 380, 896), "t_puskupaa": (1152, 400, 896)})


def main(vid, guard, outs):
    V = m.load(vid)
    x0, x1, y0, y1 = m.bbox(V[0]); k = m.TARGET_H / (y1 - y0); floor = y1
    print("mittakaava", round(k, 3), "korkeus", y1 - y0, "lattia", floor)

    def sheet(name, idx, mode):
        m.setdims("t_" + name)
        cells = [m.cell(V[i], mode if isinstance(mode, str) else mode(i), k, floor) for i in idx]
        m.save(cells, f"{outs}/timppa_{name}.png"); print(name, len(cells), "kuvaa", list(idx))

    sheet("jab", [20, 22, 24, 26, 28, 30, 32, 34, 36], "low"); print("jab: osuma = indeksi 5")
    sheet("takasuora", [36, 38, 42, 44, 46, 48, 50], "low"); print("takasuora: osuma = indeksi 2")
    sheet("potku", [54, 56, 58, 60, 62, 64, 66, 68, 70], "low"); print("potku: osuma = indeksi 4")
    sheet("osuma", [92, 94, 96, 98, 100, 102, 104, 106], "low")
    sheet("kaatuminen", list(range(108, 128, 2)), "com")
    sheet("maahan", [128, 132, 136, 140, 144, 148, 152], "com")
    sheet("nousu", [154, 160, 170, 182, 192, 196, 200, 204, 208, 212], "com")
    # suoja: 4 kuvaa vierekkäin (valkoinen tai läpinäkyvä tausta)
    G = cv2.imread(guard, cv2.IMREAD_UNCHANGED); gh, gw = G.shape[:2]
    if G.ndim == 3 and G.shape[2] == 4 and (G[..., 3] < 250).mean() > 0.2:
        rgba = cv2.cvtColor(G, cv2.COLOR_BGRA2RGBA)
    else:   # avoin valkoinen tausta: avataan reunoilta
        bgr = G[..., :3]; white = (bgr.min(2) > 235).astype(np.uint8)
        n, lab, st, _ = cv2.connectedComponentsWithStats(white, connectivity=4)
        bg = np.zeros(white.shape, bool)
        for i in range(1, n):
            xx, yy, ww, hh, ar = st[i]
            if xx == 0 or yy == 0 or xx + ww == gw or yy + hh == gh: bg |= lab == i
        bg = cv2.dilate(bg.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
        al = ((1 - cv2.GaussianBlur(bg.astype(np.float32), (0, 0), 0.8)) * 255).astype(np.uint8)
        rgba = np.dstack([cv2.cvtColor(bgr, cv2.COLOR_BGR2RGB), al])
    qw = gw // 4
    quads = [rgba[:, i * qw:(i + 1) * qw] for i in range(4)]
    bb = [m.bbox(q) for q in quads]; kg = m.TARGET_H / (bb[0][3] - bb[0][2])
    m.setdims("t_suoja")
    cells = [m.cell(q, "low", kg, b[3]) for q, b in zip(quads, bb)]
    m.save([cells[i] for i in (0, 1, 2, 1)], f"{outs}/timppa_suoja.png"); print("suoja 4 kuvaa")


def main2(vIW, vGrab, vHammer, outs):
    """Toinen erä: idle + kävely (yksi video), heitto (kurotus, ote) ja vasaraisku ylhäältä. Mittakaava jokaisen videon ensimmäisestä kuvasta (seisova hahmo)."""
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    A, B, C = m.load(vIW), m.load(vGrab), m.load(vHammer)
    kA, fA = kf(A); kB, fB = kf(B); kC, fC = kf(C)
    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("t_" + name)
        cells = [m.cell(vid[i], mode, k, floor) for i in idx]
        m.save(cells, f"{outs}/timppa_{name}.png"); print(name, len(cells), "kuvaa", list(idx))
    sheet("idle", A, m.best_loop(A, 0, 36, 18, 36, "low"), "low", kA, fA)
    sheet("kavely", A, m.best_loop(A, 40, len(A) - 1, 16, 48, "torso"), "torso", kA, fA)
    # heitto (8 kuvaa, kuten muilla pomoilla: 0 kurotus, 1 ote, 2-4 nosto, 5-6 heitto, 7 asento): videossa kurotus ja ote; nosto ja heitto puuttuvat, joten käytetään otteen ja vetäytymisen kuvia
    sheet("heitto", B, [42, 46, 52, 58, 66, 40, 42, 72], "low", kB, fB)
    # vasaraisku: nyrkki ylös, isku alas (osuma kuva 29), palautuminen
    sheet("vasara", C, [2, 6, 10, 16, 22, 25, 27, 29, 31, 34, 40, 48, 56, 64], "low", kC, fC); print("vasara: osuma = indeksi 7")


def keyed(path):
    """Kuva RGBA:ksi: läpinäkyvä tausta säilyy, muuten valkoinen tausta avataan reunoilta."""
    from PIL import Image
    im = np.array(Image.open(path).convert("RGBA"))
    if (im[..., 3] < 250).mean() > 0.2: return im
    bgr = im[..., :3]; white = (bgr.min(2) > 235).astype(np.uint8); h, w = white.shape
    n, lab, st, _ = cv2.connectedComponentsWithStats(white, connectivity=4)
    bg = np.zeros(white.shape, bool)
    for i in range(1, n):
        x, y, ww, hh, ar = st[i]
        if x == 0 or y == 0 or x + ww == w or y + hh == h: bg |= lab == i
    bg = cv2.dilate(bg.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
    al = ((1 - cv2.GaussianBlur(bg.astype(np.float32), (0, 0), 0.8)) * 255).astype(np.uint8)
    return np.dstack([bgr, al])


def main3(img6, img2, vid, outs):
    """Pääpusku (8 kuvaa): 2 kurotuskuvaa (img2) + 6 kuvan sarja (img6, 3 x 2 ruudukko). Mittakaava: kuva 2 (nyrkit ojossa) sovitetaan videon takasuora-asennon (kuva 30) korkeuteen."""
    V = m.load(vid)
    x0, x1, y0, y1 = m.bbox(V[0]); k = m.TARGET_H / (y1 - y0)
    bx = m.bbox(V[30]); hv = (bx[3] - bx[2]) * k
    A = keyed(img6); B = keyed(img2)
    ah, aw = A.shape[:2]; bh, bw = B.shape[:2]
    cellsA = [A[r * ah // 2:(r + 1) * ah // 2, c * aw // 3:(c + 1) * aw // 3] for r in range(2) for c in range(3)]
    cellsB = [B[:, i * bw // 2:(i + 1) * bw // 2] for i in range(2)]
    ref = m.bbox(cellsA[1]); kk = hv / (ref[3] - ref[2]); print("kuvan mittakaava", round(kk, 3), "viite", round(hv))
    frames = cellsB + cellsA
    m.setdims("t_puskupaa")
    hA0 = m.bbox(cellsA[0]); hB1 = m.bbox(cellsB[1]); kB = kk * (hA0[3] - hA0[2]) / (hB1[3] - hB1[2])   # kurotuskuvat ovat eri kokoisia: sovitetaan sarjan ensimmäiseen kurotuskuvaan
    print("kurotuskuvien mittakaava", round(kB, 3))
    cells = [m.cell(f, "low", kB if i < 2 else kk, m.bbox(f)[3]) for i, f in enumerate(frames)]
    m.save(cells, f"{outs}/timppa_puskupaa.png"); print("puskupaa 8 kuvaa (kurotus 0-1, sarja 2-7; osuma = indeksi 6)")


if __name__ == "__main__":
    if len(sys.argv) > 1 and sys.argv[1] == "3":
        main3(sys.argv[2], sys.argv[3], sys.argv[4], sys.argv[5] if len(sys.argv) > 5 else "Assets/Sprites/Viholliset")
    elif len(sys.argv) > 1 and sys.argv[1] == "2":
        main2(sys.argv[2], sys.argv[3], sys.argv[4], sys.argv[5] if len(sys.argv) > 5 else "Assets/Sprites/Viholliset")
    else:
        main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
