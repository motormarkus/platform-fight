"""Lentokapteeni (vihu): kolmesta videosta (A idle + kävely, B iskut ja potkut, C osuma + kaatuminen + ylösnousu) ja suojauskuvasta kuvasarjat
Assets/Sprites/Viholliset/lentokapteeni_<nimi>.png (4 saraketta, ruutu 768 x 448, seisova hahmo 300 px, jalat 8 px ylös, pivot alhaalla keskellä; hahmo katsoo oikealle).
Kaatuminen ja osuma ovat videossa oikein päin: ei peilausta. Viimeinen ylösnousukuva on idle oikealle.
Ajo: python Tools/CaptainAssets/make_captain_sheets.py <videot-kansio> <suojauskuva.png> <ulos> <heittokuva.png> <puskukuva.png>   (videot: A=2_15PM, B=2_36PM, C=2_58PM)"""
import os, sys, glob
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
from key_video import key_frame

CELL_W, CELL_H, COLS = 768, 448, 4
TARGET_H, FOOT_MARGIN = 300.0, 8
VID = {"A": "279d2c1d", "B": "2939cb56", "C": "7d521db3"}
FLIP_FROM, FLIP_TO = 9999, 9999      # ei peilausta
KB = TARGET_H / 590.0                 # kaikki videot 720p: seisova hahmo n. 590 px -> 300 px

def R(a, b, s=1): return list(range(a, b, s))
# (nimi, lähde, ruudut, ankkuri)   ankkuri: low = jalat, torso = vartalo (kävely), com = painopiste (kaatuminen)
ANIMS = [
    ("idle",        "A", R(58, 67), "low"),                                                  # kädet alhaalla (videon A alun taistelu-asento ruuduissa 0-36 jää käyttämättä)
    ("kavely",      "A", R(103, 135, 2), "torso"),                                           # 32 ruudun askelsilmukka
    ("jab",         "B", [21, 22, 23, 24, 25, 26, 27, 28, 29], "low"),                      # osuma idx 4 (ruutu 25)
    ("takasuora",   "B", [30, 31, 32, 33, 34, 35, 36, 37, 38], "low"),                      # osuma idx 3 (ruutu 33)
    ("potku",       "B", [53, 54, 55, 56, 57, 58, 59, 60, 61, 62, 63, 64, 66, 68, 70], "low"),   # osuma idx 6-7 (ruudut 59-60)
    ("syoksy",      "B", [152, 154, 156, 158, 160, 162, 164, 166, 168, 171, 174, 177, 180, 183], "low"),   # pitkä syöksylyönti, osuma idx 6-8
    ("polvi",       "B", [121, 122, 123, 124, 125, 126, 127, 128, 129, 130, 132, 134], "low"),   # polvi ja matala potku, osuma idx 5-6
    ("osuma",       "C", [10, 12, 14, 16, 18, 20, 22, 24], "low"),
    # kaatuminen: ilma 5 kuvaa (0,1 s/kuva; heitto on n. 0,5 s) + varakuva (Enemy ei näytä ilmassa viimeistä) + maa 6 kuvaa
    ("kaatuminen",  "C", [24, 30, 36, 42, 48, 52, 54, 56, 58, 60, 64, 70], "com"),
    ("ylosnousu",   "C", [128, 131, 134, 137, 140, 143, 146, 149, 152, 155, 158, 162, 166], "com"),   # viimeinen = idle oikealle
    # heitetty: ote 3 (osuman nojaus), lento 6 (0,08 s/kuva), maa 6 (0,1 s/kuva)
    ("heitetty",    "C", [12, 16, 20, 26, 30, 34, 38, 42, 46, 52, 54, 56, 60, 64, 70], "com"),
]

def resize_premult(rgba, f):
    a = rgba.astype(np.float32); al = a[..., 3:4] / 255.0; a[..., :3] *= al
    h, w = rgba.shape[:2]; nw, nh = max(1, round(w * f)), max(1, round(h * f))
    a = cv2.resize(a, (nw, nh), interpolation=cv2.INTER_AREA if f <= 1 else cv2.INTER_CUBIC)
    if f > 1.2:   # 360p-lähde suurennettuna: pehmennä rosoinen alfareuna
        al2 = cv2.GaussianBlur(a[..., 3], (0, 0), 1.3) / 255.0
        al2 = np.clip((al2 - 0.3) / 0.4, 0, 1); al2 = al2 * al2 * (3 - 2 * al2)
        a[..., 3] = al2 * 255.0
    a2 = a[..., 3:4] / 255.0
    a[..., :3] = np.where(a2 > 1e-3, a[..., :3] / np.maximum(a2, 1e-3), 0)
    return a.clip(0, 255).astype(np.uint8)

def anchor_x(im, mode):
    a = im[..., 3] > 128; h = a.shape[0]
    if mode == "torso":
        ys, xs = np.where(a[int(h * 0.25):int(h * 0.6)])
        if len(xs): return xs.mean()
    if mode == "com": ys, xs = np.where(a)
    else:
        ys, xs = np.where(a[int(h * 0.65):])
        if len(xs) == 0: ys, xs = np.where(a)
    return xs.mean()

def paste(cell, im, ax, foot_y, lift=0):
    x0 = int(round(CELL_W / 2 - ax)); y0 = int(round(CELL_H - FOOT_MARGIN - foot_y - lift))
    h, w = im.shape[:2]
    sx0, sy0 = max(0, -x0), max(0, -y0); dx0, dy0 = max(0, x0), max(0, y0)
    ww = min(w - sx0, CELL_W - dx0); hh = min(h - sy0, CELL_H - dy0)
    if ww <= 0 or hh <= 0: return
    if ww < w or hh < h: print("   HUOM: kuva ylittää ruudun", im.shape)
    cell[dy0:dy0 + hh, dx0:dx0 + ww] = im[sy0:sy0 + hh, sx0:sx0 + ww]

class Video:
    def __init__(self, folder, key):
        self.f = glob.glob(os.path.join(folder, VID[key] + "*"))[0]
        cap = cv2.VideoCapture(self.f); self.frames = []
        while True:
            ok, fr = cap.read()
            if not ok: break
            self.frames.append(key_frame(fr))
        ys, xs = np.where(self.frames[0][..., 3] > 128)
        self.floor = ys.max() + 1                       # seisovan hahmon jalkojen alareuna
        self.k = KB

def navy_mean(crops):
    v = []
    for c in crops:
        R, G, B = c[..., 0].astype(int), c[..., 1].astype(int), c[..., 2].astype(int)
        v.append(((B > R + 25) & (B > G + 10) & (c[..., 3] > 200) & (B < 170)).sum() ** 0.5)
    return float(np.mean(v))

def key_green(bgr):
    """Hellävarainen vihreän avaus (ei eroosiota): ohuet sormet säilyvät."""
    f = bgr.astype(np.float32); b, g, r = f[..., 0], f[..., 1], f[..., 2]
    dom = g - np.maximum(r, b)
    alpha = np.clip((62.0 - dom) / 40.0, 0, 1)          # dom <= 22 täysin peittävä, >= 62 läpinäkyvä
    n, lab, st, _ = cv2.connectedComponentsWithStats((alpha > 0.15).astype(np.uint8), connectivity=8)
    if n > 2:
        keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); alpha = np.where(lab == keep, alpha, 0)
    out = f.copy(); out[..., 1] = np.minimum(g, np.maximum(r, b) + 6)   # vihreä reunahohto pois
    return np.dstack([out[..., 2], out[..., 1], out[..., 0], alpha * 255.0]).clip(0, 255).astype(np.uint8)

def grid_figs(path):
    """Kuvan hahmot (vihreä tausta) riveittäin vasemmalta oikealle."""
    g = cv2.imread(path); gf = g.astype(np.int32)
    non = ~((gf[..., 1] - np.maximum(gf[..., 0], gf[..., 2])) > 60)
    n, lab, st, _ = cv2.connectedComponentsWithStats(cv2.dilate(non.astype(np.uint8), np.ones((5, 5), np.uint8)), connectivity=8)
    comps = sorted([i for i in range(1, n) if st[i, 4] > 20000], key=lambda i: (st[i, 1] // 400, st[i, 0]))
    out = []
    for i in comps:
        x, y, w, h, a = st[i]
        sub = key_green(g[max(0, y - 10):y + h + 10, max(0, x - 10):x + w + 10].copy())
        ys, xs = np.where(sub[..., 3] > 30)
        out.append(sub[ys.min():ys.max() + 1, xs.min():xs.max() + 1])
    return out

def build(folder, guard_path, out, grab_path=None, push_path=None):
    os.makedirs(out, exist_ok=True)
    vids = {k: Video(folder, k) for k in "ABC"}
    only = os.environ.get('ONLY')
    for name, key, idx, mode in ANIMS:
        if only and name not in only.split(','): continue
        v = vids[key]; items = []
        for i in idx:
            im = v.frames[i].copy()
            flip = key == "C" and FLIP_FROM <= i <= FLIP_TO
            ys, xs = np.where(im[..., 3] > 128)
            x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
            crop = im[y0:y1, x0:x1]
            if flip: crop = crop[:, ::-1].copy()
            floor = (y1 if key == "A" else v.floor)
            items.append((resize_premult(crop, v.k), anchor_x(crop, mode) * v.k, (floor - y0) * v.k))
        below = max(r.shape[0] - foot for r, _, foot in items)
        lift = max(0, int(np.ceil(below - FOOT_MARGIN + 2)))
        save_sheet(items, lift, f"{out}/lentokapteeni_{name}.png")
        print(name, len(items), "ruutua nosto", lift)
    # suojaus: neljä hahmoa vihreästä kuvasta, mittakaava B:n seisovasta (guard) asennosta
    if only: return
    g = cv2.imread(guard_path)
    gf = g.astype(np.int32); nongreen = ~((gf[..., 1] - np.maximum(gf[..., 0], gf[..., 2])) > 60)
    cols = nongreen.sum(0) > 6; runs = []; s0 = None
    for x, c in enumerate(cols):
        if c and s0 is None: s0 = x
        if not c and s0 is not None:
            if x - s0 > 80: runs.append((s0, x))
            s0 = None
    if s0 is not None: runs.append((s0, len(cols)))
    print("suojaus: hahmoja", len(runs))
    items = []; h0 = None
    for (x0, x1) in runs[:4]:
        sub = key_frame(g[:, max(0, x0 - 12):min(g.shape[1], x1 + 12)].copy())
        ys, xs = np.where(sub[..., 3] > 128)
        crop = sub[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
        if h0 is None: h0 = crop.shape[0]
        items.append(crop)
    ys, xs = np.where(vids["B"].frames[0][..., 3] > 128)
    hB = ys.max() - ys.min() + 1
    kg = KB * hB / h0
    res = [(resize_premult(c, kg), anchor_x(c, "low") * kg, c.shape[0] * kg) for c in items]
    save_sheet(res, 0, f"{out}/lentokapteeni_suoja.png")
    # heitto (8 kuvaa: 0 kurotus, 1 ote, 2-4 veto, 5-6 heitto, 7 asento) ja pusku (6 kuvaa): mittakaava vartalon (tummansinisen) pinta-alan mukaan, kuten suojauskuvassa
    ref = navy_mean(items)
    for nm, pth, picks in (("heitto", grab_path, [0, 1, 2, 3, 4, 5, 5, None]), ("pusku", push_path, [0, 1, 2, 3, 4, 5])):
        if not pth: continue
        fg = grid_figs(pth)
        print(nm, "hahmoja", len(fg))
        k = kg * ref / navy_mean(fg)
        seq = []
        for pk in picks:
            if pk is None: c, kk = items[0], kg        # asento heiton jälkeen = suojauskuvan 1. (taisteluasento)
            else: c, kk = fg[pk], k
            seq.append((resize_premult(c, kk), anchor_x(c, "low") * kk, c.shape[0] * kk))
        save_sheet(seq, 0, f"{out}/lentokapteeni_{nm}.png")

def save_sheet(items, lift, path):
    cells = []
    for r, ax, foot in items:
        cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, ax, foot, lift); cells.append(cell)
    rows = (len(cells) + COLS - 1) // COLS
    sheet = np.zeros((rows * CELL_H, COLS * CELL_W, 4), np.uint8)
    for k, c in enumerate(cells):
        sheet[(k // COLS) * CELL_H:(k // COLS + 1) * CELL_H, (k % COLS) * CELL_W:(k % COLS + 1) * CELL_W] = c
    cv2.imwrite(path, cv2.cvtColor(sheet, cv2.COLOR_RGBA2BGRA))

if __name__ == "__main__":
    build(*sys.argv[1:6])
