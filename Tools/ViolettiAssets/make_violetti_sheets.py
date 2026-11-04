"""Violetti (NPC, natsilinnakkeen juhlahalli): neljä videota -> Assets/Sprites/Viholliset/violetti_<nimi>.png.
A tanssi (vihreä tausta, 720x1280, 240 kuvaa), B keskustelu/idle (vihreä, 720x1280), C kävely + käännös tiskille (turkoosi liukuväritausta, 1080x1440, 30 kuvaa/s:
  kävely oikealle 0-215, käännös selin 214-256, seisoo selin 258-299), D idle (vihreä, 1080x1440, 30 kuvaa/s).
Ruudut kuten Nadinella (4 saraketta, hahmo 600 px, jalat 16 px ylös; enintään 8192 px korkea kuva).
Pelkkä tanssi: python Tools/ViolettiAssets/make_violetti_sheets.py --dance <tanssi.mp4> <korjatut-framet-kansio> [ulos]
Ajo: python Tools/ViolettiAssets/make_violetti_sheets.py <tanssi.mp4> <keskustelu.mp4> <kavely.mov> <idle.mov> [ulos-kansio]"""
import os, sys
import av, cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m

m.TARGET_H = 600.0
m.DIMS.update({"v_idle": (640, 320, 768), "v_kavely": (640, 320, 768), "v_kaanto": (640, 320, 768), "v_selin": (640, 320, 768),
               "v_puhe": (640, 320, 768), "v_tanssi": (768, 384, 672)})


def key_teal(bgr):
    """Turkoosi liukuväritausta (matala kylläisyys, vihertävä sävy) + varjo -> läpinäkyväksi; suurin yhtenäinen hahmo jää."""
    hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV); h, s = hsv[..., 0].astype(int), hsv[..., 1].astype(int)
    bgm = (s < 75) & (h > 70) & (h < 110)
    fg = (~bgm).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(fg, connectivity=8)
    keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); fg = (lab == keep).astype(np.uint8)
    fg = cv2.morphologyEx(fg, cv2.MORPH_OPEN, np.ones((3, 3), np.uint8))
    fg = cv2.erode(fg, np.ones((3, 3), np.uint8))   # 1 px sisään: taustan vaalea reuna pois
    a = fg * 255
    # reunapikselit saavat lähimmän tumman (ääriviivan) värin: ei valkoista hapsua
    band = cv2.dilate((fg == 0).astype(np.uint8), np.ones((9, 9), np.uint8)).astype(bool)
    out = np.where(band[..., None], cv2.erode(bgr, np.ones((5, 5), np.uint8)), bgr)
    return np.dstack([out[..., 2], out[..., 1], out[..., 0], a]).astype(np.uint8)


def load_teal(path): return [key_teal(f.to_ndarray(format="bgr24")) for f in av.open(path).decode(video=0)]


def main(vA, vB, vC, vD, outs):
    """Yksi video kerrallaan (muisti: 1080x1440-videoiden kaikki ruudut yhtä aikaa eivät mahdu)."""
    import gc
    from make_king_sheets import key_frame
    def load_safe(path): return [key_frame(f.to_ndarray(format="bgr24"), cyan_safe=True) for f in av.open(path).decode(video=0)]   # cyan_safe: siniset farkut eivät katoa vihreän taustan poistossa
    def kf(vid):
        x0, x1, y0, y1 = m.bbox(vid[0]); return m.TARGET_H / (y1 - y0), y1
    def sheet(name, vid, idx, mode, k, floor):
        m.setdims("v_" + name)
        cells = [m.cell(vid[i], mode, k, floor) for i in idx]
        m.save(cells, f"{outs}/violetti_{name}.png"); print(name, len(cells), "kuvaa", list(idx)[:3], "...", list(idx)[-2:])
    D = load_safe(vD); kD, fD = kf(D)
    sheet("idle", D, m.best_loop(D, 0, len(D) - 2, 20, 40, "low"), "low", kD, fD); del D; gc.collect()
    C = load_teal(vC); kC, fC = kf(C)
    sheet("kavely", C, m.best_loop(C, 8, 210, 16, 40, "torso"), "torso", kC, fC)
    sheet("kaanto", C, list(range(214, 258, 2)), "low", kC, fC)
    sheet("selin", C, m.best_loop(C, 258, len(C) - 2, 8, 40, "low"), "low", kC, fC); del C; gc.collect()
    B = load_safe(vB); kB, fB = kf(B)
    sheet("puhe", B, list(range(70, 128, 2)), "low", kB, fB); del B; gc.collect()
    A = load_safe(vA); kA, fA = kf(A)
    sheet("tanssi", A, [round(i * 239 / 47) for i in range(48)], "low", kA, fA)


def main_dance(vA, frames_dir, outs):
    """Vain tanssi uudelleen: farkut eivät enää katoa taustan poistossa (cyan_safe) ja korjatut kokokuvaruudut (tanssi_NNNN.png, hahmo kokonaan kuvassa) korvaavat videon ruudut."""
    import glob, re
    from make_king_sheets import key_frame
    A = [key_frame(f.to_ndarray(format="bgr24"), cyan_safe=True) for f in av.open(vA).decode(video=0)]
    fix = {int(re.search(r"(\d+)\.png$", f).group(1)): f for f in glob.glob(os.path.join(frames_dir, "tanssi_*.png"))}
    full = [i for i in range(len(A)) if i not in fix and A[i][..., 3].max() > 0]
    def bb(im):
        ys, xs = np.where(im[..., 3] > 128); return xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
    hv = float(np.median([bb(A[i])[3] - bb(A[i])[2] for i in full])); fy = int(np.median([bb(A[i])[3] for i in full]))
    for i, f in fix.items():
        c = cv2.imread(f, cv2.IMREAD_UNCHANGED)
        c = cv2.cvtColor(c, cv2.COLOR_BGRA2RGBA)   # imread antaa BGRA; videoruudut ovat RGBA (väriviat: hahmo sinisenä)
        x0, x1, y0, y1 = bb(c); c = c[y0:y1, x0:x1]
        k = hv / c.shape[0]; c = cv2.resize(c, (max(1, int(c.shape[1] * k)), int(hv)), interpolation=cv2.INTER_AREA)
        H, W = A[i].shape[:2]; canvas = np.zeros((H, W, 4), np.uint8)
        ox = W // 2 - c.shape[1] // 2; oy = fy - c.shape[0]
        xa, xb = max(ox, 0), min(ox + c.shape[1], W); ya = max(oy, 0)
        canvas[ya:oy + c.shape[0], xa:xb] = c[ya - oy:, xa - ox:xb - ox]
        A[i] = canvas
    x0, x1, y0, y1 = m.bbox(A[0]); kA = m.TARGET_H / (y1 - y0); fA = y1
    m.setdims("v_tanssi")
    cells = [m.cell(A[min(round(i * 239 / 47), len(A) - 1)], "low", kA, fA) for i in range(48)]
    m.save(cells, f"{outs}/violetti_tanssi.png"); print("tanssi", len(cells), "kuvaa; korjattuja ruutuja", len(fix), "mittakaava", round(kA, 3))


if __name__ == "__main__":
    if sys.argv[1] == "--dance":
        main_dance(sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset"); sys.exit()
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4], sys.argv[5] if len(sys.argv) > 5 else "Assets/Sprites/Viholliset")
