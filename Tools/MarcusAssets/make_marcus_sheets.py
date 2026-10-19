"""Marcuksen kuvasarjat videoista (key_marcus.py:n tuottamista .pkl-tiedostoista).
Käyttö: python make_marcus_sheets.py <pkl-kansio> <ulostulo-kansio>
Jokainen animaatio -> marcus_<nimi>.png: 4 saraketta, ruutu 768 x 448 px (ylärivi ensin), hahmon seisova korkeus 300 px,
jalkojen alareuna ruudun alareunasta 8 px ylöspäin (pivot alhaalla keskellä). Videon lattiataso pidetään samana, joten hypyt/lennot säilyvät."""
import os, pickle, sys
import cv2
import numpy as np

CELL_W, CELL_H, COLS = 768, 448, 4
TARGET_H, FOOT_MARGIN = 300.0, 8
# videon vertailuruutu (seisova vartioasento): mittakaava ja lattiataso
REF = {"kaatuminen1": 0, "kaatuminen2": 198, "kavely": 110, "lyonnit1": 0, "lyonnit2": 0, "vaisto": 0}

# Mittakaava housujen koon mukaan (sqrt pinta-ala vartioasennossa -> 78 px): videoiden zoom vaihtelee, ja pelkkä seisovan hahmon korkeus
# antoi väistö/jab/suora-kuville (vaisto-video) n. 12 % liian ison hahmon. Kävely (sivukuva) pysyy korkeuden mukaan.
SCALE = {"vaisto": 0.574, "lyonnit2": 0.375, "kaatuminen1": 0.420, "kaatuminen2": 0.410}

def R(a, b, step=1): return list(range(a, b + 1, step))

# (nimi, video, ruudut, ankkuri)  ankkuri: low = jalkojen keskikohta (pehmennetty), bbox = ruudun keskikohta
ANIMS = [
    ("idle",        "lyonnit2", R(0, 15, 3), "low"),                       # vain rauhallinen vartioasento (myöhemmät ruudut alkavat jo lyönnin)
    ("kavely",      "kavely",   R(56, 96, 2), "low"),                                   # yksi askelsilmukka (jakso 41 ruutua)
    ("lyonti1",     "vaisto",   [10, 12, 14, 16, 18, 20, 22, 24, 26], "low"),          # jab (vasen suora), osuu 16-20
    ("lyonti2",     "vaisto",   [54, 56, 58, 60, 62, 64, 66], "low"),                  # nopea jab
    ("suora",       "vaisto",   [26, 28, 30, 32, 34, 36, 38, 40, 44], "low"),          # takasuora (vartalon kierto)
    ("koukku",      "lyonnit2", [99, 100, 101, 102, 103, 104, 105, 106, 107, 108, 110, 112, 114, 116], "low"),
    ("vaisto",      "vaisto",   [64, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 86, 88, 90, 92, 96], "low"),
    ("osuma",       "kaatuminen1", [4, 8, 12, 16, 20, 24], "low"),
    ("kaatuminen1", "kaatuminen1", [54, 60, 66, 72, 78, 84, 90, 96, 102, 108, 120, 132], "hip"),
    ("ylosnousu1",  "kaatuminen1", [132, 138, 144, 150, 156, 162, 168, 174, 180, 186, 192, 198], "hip"),
    ("kaatuminen2", "kaatuminen2", [66, 72, 76, 80, 84, 88, 92, 96, 99, 108, 117, 126, 135, 144], "hip"),
    ("ylosnousu2",  "kaatuminen2", [150, 153, 156, 159, 162, 166, 171, 175, 180, 185, 190, 195, 200, 205], "hip"),
]


def resize_premult(rgba, w, h):
    f = rgba.astype(np.float32)
    a = f[..., 3:4] / 255.0
    f[..., :3] *= a
    f = cv2.resize(f, (w, h), interpolation=cv2.INTER_AREA)
    a2 = f[..., 3:4] / 255.0
    f[..., :3] = np.where(a2 > 1e-3, f[..., :3] / np.maximum(a2, 1e-3), 0)
    return f.clip(0, 255).astype(np.uint8)


def hip_center(fr):
    """Housujen (tummanpunaisten) keskikohta: lantio pysyy ruudun keskellä, joten kaatuminen ja ylösnousu eivät liu'u sivusuunnassa."""
    im, x0, y0 = fr
    hsv = cv2.cvtColor(np.ascontiguousarray(im[..., 2::-1]), cv2.COLOR_BGR2HSV)
    h, s, v = hsv[..., 0], hsv[..., 1], hsv[..., 2]
    mk = ((h < 8) | (h > 170)) & (s > 110) & (v > 60) & (v < 165) & (im[..., 3] > 200)
    ys, xs = np.where(mk)
    if len(xs) < 200:
        ys, xs = np.where(im[..., 3] > 128)
    return x0 + xs.mean()


def low_center(fr):
    im, x0, y0 = fr
    a = im[..., 3] > 128
    h = a.shape[0]
    ys, xs = np.where(a[int(h * 0.65):])
    return x0 + (xs.mean() if len(xs) else im.shape[1] / 2)


def trim_streaks(fr):
    """Ohuet vaakaviirut (horisontin jäänteet) pois: alfa avataan 9 px:n pyöreällä ytimellä, vain avatun alueen läheiset pikselit jäävät."""
    im, x0, y0 = fr
    a = (im[..., 3] > 128).astype(np.uint8)
    op = cv2.morphologyEx(a, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (17, 17)))
    keep = cv2.dilate(op, np.ones((7, 7), np.uint8)) > 0
    im = im.copy(); im[~keep, 3] = 0
    return im, x0, y0


def clean_walk(fr):
    """Kävelyvideon sininen varjo ja irtonaiset tahrat pois: vain suurin yhtenäinen osa, jalkojen sininen varjo pois."""
    im, x0, y0 = fr
    a = (im[..., 3] > 128).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(a, connectivity=8)
    if n > 2:
        k = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
        im = im.copy(); im[lab != k, 3] = 0
    h = im.shape[0]
    low = im[int(h * 0.6):]
    r, g, b = low[..., 0].astype(int), low[..., 1].astype(int), low[..., 2].astype(int)
    blue = (b - r > 8) & (b > g + 3) & (b < 190)
    low[blue, 3] = 0
    return im, x0, y0


def build(pkl_dir, out_dir):
    os.makedirs(out_dir, exist_ok=True)
    cache = {}
    def video(name):
        if name not in cache:
            fr = pickle.load(open(os.path.join(pkl_dir, name + ".pkl"), "rb"))
            if name == "kavely": fr = [clean_walk(x) if x else x for x in fr]
            if name in ("kaatuminen2", "kavely", "lyonnit2"): fr = [trim_streaks(x) if x else x for x in fr]
            cache[name] = fr
        return cache[name]
    for name, vid, frames, anchor in ANIMS:
        fr = video(vid)
        ref = fr[REF[vid]]
        scale = SCALE.get(vid, TARGET_H / ref[0].shape[0])
        ground = ref[2] + ref[0].shape[0]
        cx_all = np.array([low_center(fr[i]) if anchor == "low" else hip_center(fr[i]) if anchor == "hip" else fr[i][1] + fr[i][0].shape[1] / 2 for i in frames])
        if anchor in ("low", "hip") and len(cx_all) >= 5:
            cx_all = np.array([np.median(cx_all[max(0, i - 2):i + 3]) for i in range(len(cx_all))])
        sheet_rows = (len(frames) + COLS - 1) // COLS
        sheet = np.zeros((sheet_rows * CELL_H, COLS * CELL_W, 4), np.uint8)
        for n, i in enumerate(frames):
            im, x0, y0 = fr[i]
            sm = resize_premult(im, max(1, round(im.shape[1] * scale)), max(1, round(im.shape[0] * scale)))
            cx = (cx_all[n] - x0) * scale                          # ankkurin x kuvassa
            left = int(round(CELL_W / 2 - cx))
            bottom = CELL_H - FOOT_MARGIN + int(round((y0 + im.shape[0] - ground) * scale))   # lattiaan nähden
            top = bottom - sm.shape[0]
            cell = np.zeros((CELL_H, CELL_W, 4), np.uint8)
            sx0, sy0 = max(0, -left), max(0, -top)
            dx0, dy0 = max(0, left), max(0, top)
            w = min(sm.shape[1] - sx0, CELL_W - dx0); h = min(sm.shape[0] - sy0, CELL_H - dy0)
            if w > 0 and h > 0: cell[dy0:dy0 + h, dx0:dx0 + w] = sm[sy0:sy0 + h, sx0:sx0 + w]
            if sm.shape[0] > CELL_H or left < 0 or left + sm.shape[1] > CELL_W: print("  VAROITUS: ruutu", name, i, "leikkautuu")
            r, c = divmod(n, COLS)
            sheet[r * CELL_H:(r + 1) * CELL_H, c * CELL_W:(c + 1) * CELL_W] = cell
        path = os.path.join(out_dir, f"marcus_{name}.png")
        cv2.imwrite(path, cv2.cvtColor(sheet, cv2.COLOR_RGBA2BGRA))
        print(name, len(frames), "ruutua, mittakaava %.3f" % scale)


if __name__ == "__main__":
    build(sys.argv[1], sys.argv[2])
