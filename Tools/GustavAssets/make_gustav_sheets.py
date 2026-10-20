"""Gustavin kuvasarjat: gustav_<nimi>.png, 4 saraketta, ruutu 768 x 448 px (ylärivi ensin), seisova hahmo 300 px, jalkojen alareuna
ruudun alareunasta 8 px ylös, pivot alhaalla keskellä (kuten Marcus). Hahmo katsoo oikealle.
Vaiheet: 1) python key_gustav.py <raw> (videoruudut)  2) python split_sheets.py <src> (kuvasarjat)  3) python make_gustav_sheets.py <raw> <src> <ulos>
Videoiden lattiataso pidetään samana (hypyt ja lennot säilyvät); kuvasarjojen ruudut asetetaan jalat lattialle."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from clean_fx import clean_fx
from scales import SCALE as SHEET_SCALE

CELL_W, CELL_H, COLS = 768, 448, 4
TARGET_H, FOOT_MARGIN = 300.0, 8
K = TARGET_H / 865.0                       # idle-mittakaava (seisova hahmo 865 px) -> 300 px
# video: (mittakaava idleen, lattiataso videossa)
VID = {"idle": (1.0, 1002), "kavely": (1.0, 1002), "juoksu": (1.0, 1002), "kaatuminen": (1.0, 1002), "lyonnit": (1.0, 1002),
       "potkut": (865 / 567, 660), "uppercut": (865 / 409, 474)}   # lattiataso lähdevideon pikseleinä

def R(a, b, s=1): return list(range(a, b, s))
# (nimi, lähde, tyyppi, ruudut, ankkuri)   tyyppi: v = video (raw/<lähde>/NNN.png), s = kuvasarja (src/<lähde>/N.png)
ANIMS = [
    ("idle",        "idle",       "v", R(0, 36, 3), "low"),
    ("kavely",      "kavely",     "v", R(5, 35, 3), "low"),
    ("juoksu",      "juoksu",     "v", R(10, 30, 2), "low"),
    ("hyppypotku",  "juoksu",     "v", [70, 74, 78, 104, 110, 116, 124, 132, 136, 140], "low"),
    ("osuma",       "kaatuminen", "v", [29, 35, 37, 40], "low"),
    ("kaatuminen",  "kaatuminen", "v", [62, 66, 68, 70, 72, 74, 76, 78, 80, 82, 84, 88, 92, 98], "com"),
    ("ylosnousu",   "kaatuminen", "v", [100, 104, 108, 110, 114, 118, 122, 126, 130, 134, 138, 142, 146], "com"),
    ("etupotku",    "potkut",     "v", [20, 22, 23, 24, 25, 26, 28, 30, 31, 32, 34], "low"),
    ("kierrepotku", "potkut",     "v", [66, 68, 70, 72, 74, 76, 78, 84, 88, 90, 92, 94], "low"),
    ("uppercut",    "uppercut",   "v", [20, 25, 30, 35, 40, 45, 50, 55, 60, 90, 95, 100, 105, 110], "low"),
    ("jab",         "lyonnit",    "v", [20, 22, 24, 25, 27, 28, 30, 32, 34], "low"),               # osuma idx 4 (ruutu 27; ruutu 26 sisältää osumavälähdyksen)
    ("takasuora",   "lyonnit",    "v", [98, 100, 102, 104, 107, 108, 110, 113, 116], "low"),       # osuma idx 4 (ruutu 107)
    ("kiertopotku", "kiertopotku","s", R(0, 6), "low"),
    ("kirvespotku", "kirvespotku","s", R(0, 6), "low"),
    ("suoja",       "suojaus",    "s", R(0, 3), "low"),
    ("heitetty",    "heitto",     "s", R(0, 6), "com"),
    ("heitto",      "heittaa",    "s", R(0, 6), "low"),
    ("hiekka",      "hiekka_heitto", "s", R(0, 8), "low"),
]

def resize_premult(rgba, f):
    a = rgba.astype(np.float32); al = a[..., 3:4] / 255.0; a[..., :3] *= al
    h, w = rgba.shape[:2]; nw, nh = max(1, round(w * f)), max(1, round(h * f))
    a = cv2.resize(a, (nw, nh), interpolation=cv2.INTER_AREA)
    a2 = a[..., 3:4] / 255.0
    a[..., :3] = np.where(a2 > 1e-3, a[..., :3] / np.maximum(a2, 1e-3), 0)
    return a.clip(0, 255).astype(np.uint8)

MATCH = {"kirvespotku", "kiertopotku", "uppercut", "etupotku", "kierrepotku", "heitto", "heitetty", "suoja", "hiekka"}   # animaatiot, joiden väri sovitetaan idleen
TARGET_BGR = np.array([38.5, 63.0, 100.0])    # idle-ruutujen keskiväri (peitteiset pikselit), BGR

def match_colors(items):
    """Lähteiden kirkkaus/sävy vaihtelee (kirvespotku oli n. 20 % vaaleampi): koko animaatio vedetään idlen keskivärille (ruutukohtainen vaihtelu säilyy, rajoitettu +-10 %)."""
    means = []
    for r, ax, foot in items:
        m = r[..., 3] > 200
        means.append(r[..., :3][m].mean(0) if m.sum() > 300 else None)
    valid = [m for m in means if m is not None]
    if not valid: return items
    med = np.median(np.array(valid), axis=0)
    gain = np.clip(TARGET_BGR / med, 0.75, 1.25)
    out = []
    for (r, ax, foot), m in zip(items, means):
        g = gain * (np.clip(med / m, 0.9, 1.1) if m is not None else 1.0)
        r2 = r.copy(); r2[..., :3] = np.clip(r[..., :3].astype(np.float32) * g, 0, 255).astype(np.uint8)
        out.append((r2, ax, foot))
    return out

def anchor_x(im, mode):
    a = im[..., 3] > 128
    if mode == "torso":   # kävely: vartalon keskiosa (rivit 25–60 %), ei jalkoja: askelten heiluminen ei heiluta hahmoa sivusuunnassa
        h = a.shape[0]; ys, xs = np.where(a[int(h * 0.25):int(h * 0.6)])
        if len(xs): return xs.mean()
    if mode == "com":
        ys, xs = np.where(a)
    else:
        h = a.shape[0]; ys, xs = np.where(a[int(h * 0.65):])
        if len(xs) == 0: ys, xs = np.where(a)
    return xs.mean()

def paste(cell, im, ax, foot_y, lift=0):
    """im pannaan niin, että sen x = ax osuu ruudun keskelle ja y = foot_y ruudun lattiariville (lift px ylemmäs)."""
    x0 = int(round(CELL_W / 2 - ax)); y0 = int(round(CELL_H - FOOT_MARGIN - foot_y - lift))
    h, w = im.shape[:2]
    sx0, sy0 = max(0, -x0), max(0, -y0); dx0, dy0 = max(0, x0), max(0, y0)
    ww = min(w - sx0, CELL_W - dx0); hh = min(h - sy0, CELL_H - dy0)
    if ww <= 0 or hh <= 0: return
    if ww < w or hh < h: print("   HUOM: kuva ylittää ruudun", im.shape, "x0", x0, "y0", y0)
    cell[dy0:dy0 + hh, dx0:dx0 + ww] = im[sy0:sy0 + hh, sx0:sx0 + ww]

def build(raw, src, out):
    os.makedirs(out, exist_ok=True)
    for name, source, kind, idx, mode in ANIMS:
        items = []                                   # (kuva, ankkuri-x, lattiakorkeus kuvan yläreunasta)
        for i in idx:
            if kind == "v":
                im = clean_fx(cv2.imread(f"{raw}/{source}/{i:03d}.png", cv2.IMREAD_UNCHANGED))
                sc, floor = VID[source]; f = sc * K
                ys, xs = np.where(im[..., 3] > 128)
                x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
                crop = im[y0:y1, x0:x1]
                if source == "uppercut": floor = y1          # hahmo venyy ja jalat laskevat videossa: jalat aina lattialle
                items.append((resize_premult(crop, f), anchor_x(crop, mode) * f, (floor - y0) * f))
            else:
                im = cv2.imread(f"{src}/{source}/{i}.png", cv2.IMREAD_UNCHANGED)
                f = SHEET_SCALE[source] * K; r = resize_premult(im, f)
                items.append((r, anchor_x(im, mode) * f, r.shape[0]))
        if name in MATCH: items = match_colors(items)
        # lattian alle ulottuva osa (liukuva kaatuminen) mahtuu ruutuun: koko animaatio nostetaan tarvittaessa
        below = max(r.shape[0] - foot for r, _, foot in items)
        lift = max(0, int(np.ceil(below - FOOT_MARGIN + 2)))
        cells = []
        for r, ax, foot in items:
            cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, ax, foot, lift); cells.append(cell)
        rows = (len(cells) + COLS - 1) // COLS
        sheet = np.zeros((rows * CELL_H, COLS * CELL_W, 4), np.uint8)
        for k, c in enumerate(cells):
            sheet[(k // COLS) * CELL_H:(k // COLS + 1) * CELL_H, (k % COLS) * CELL_W:(k % COLS + 1) * CELL_W] = c
        cv2.imwrite(f"{out}/gustav_{name}.png", sheet)
        print(name, len(cells), "ruutua", "nosto", lift, "px")
    # hiekka: 5 puuskaa + pilvi (keskitetty), samassa mittakaavassa kuin hiekan heittokuvat
    cells = []
    for nm, n in (("hiekka_puuska", 5), ("hiekka_pilvi", 1)):
        for i in range(n):
            im = cv2.imread(f"{src}/{nm}/{i}.png", cv2.IMREAD_UNCHANGED)
            f = SHEET_SCALE[nm] * K; r = resize_premult(im, f)
            cell = np.zeros((CELL_H, CELL_W, 4), np.uint8)
            paste(cell, r, r.shape[1] / 2, r.shape[0] / 2 + (CELL_H - FOOT_MARGIN) / 2 - 0)   # keskitys ruudun keskelle
            cells.append(cell)
    sheet = np.zeros((2 * CELL_H, COLS * CELL_W, 4), np.uint8)
    for k, c in enumerate(cells):
        sheet[(k // COLS) * CELL_H:(k // COLS + 1) * CELL_H, (k % COLS) * CELL_W:(k % COLS + 1) * CELL_W] = c
    cv2.imwrite(f"{out}/gustav_hiekkapilvet.png", sheet)

if __name__ == "__main__":
    build(sys.argv[1], sys.argv[2], sys.argv[3])
