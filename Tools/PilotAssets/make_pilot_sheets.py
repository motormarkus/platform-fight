"""Lentoperämies (vihu): kolmesta videosta (A idle + kävely, B iskut ja potkut, C osuma + kaatuminen + ylösnousu) ja suojauskuvasta kuvasarjat
Assets/Sprites/Viholliset/lentoperamies_<nimi>.png (4 saraketta, ruutu 768 x 448, seisova hahmo 300 px, jalat 8 px ylös, pivot alhaalla keskellä; hahmo katsoo oikealle).
Kaatuminen: generaattori käänsi putoamisen väärään suuntaan, joten videon C ruudut 41-157 peilataan (hahmo kaatuu taaksepäin, makaa pää vasemmalla ja nousee vasemmalle päin);
viimeinen ylösnousukuva on alkuperäinen (idle oikealle), jolloin käännös tuskin huomataan.
Ajo: python Tools/PilotAssets/make_pilot_sheets.py <videot-kansio> <suojauskuva.png> <ulos>   (videot: A=2_15PM, B=2_36PM, C=2_58PM)"""
import os, sys, glob
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
from key_video import key_frame

CELL_W, CELL_H, COLS = 768, 448, 4
TARGET_H, FOOT_MARGIN = 300.0, 8
VID = {"A": "5718e81e", "B": "70e8cc9f", "C": "62bfd445"}
FLIP_FROM, FLIP_TO = 41, 157          # videon C ruudut, jotka peilataan
KB = TARGET_H / 590.0                 # B/C (720p): seisova hahmo n. 590 px -> 300 px; A (360p) on puolet pienempi -> 2 x

def R(a, b, s=1): return list(range(a, b, s))
# (nimi, lähde, ruudut, ankkuri)   ankkuri: low = jalat, torso = vartalo (kävely), com = painopiste (kaatuminen)
ANIMS = [
    ("idle",        "N", R(5, 31, 2), "low"),   # N = uusi 720p-video (NEWVID): taisteluasento, idle ruudut 5-30
    ("kavely",      "N", R(54, 83, 2), "torso"),  # kävelysilmukka 54-82 (29 ruutua)
    ("jab",         "B", [15, 16, 17, 18, 19, 20, 21, 22, 23], "low"),                     # osuma idx 4 (ruutu 19)
    ("takasuora",   "B", [22, 23, 24, 25, 26, 27, 28, 29, 30], "low"),                     # osuma idx 4 (ruutu 26)
    ("potku",       "B", [46, 48, 49, 50, 51, 52, 53, 54, 55, 56, 58, 60, 62, 64], "low"), # osuma idx 4-6 (ruudut 51-53)
    ("syoksy",      "B", [104, 106, 107, 108, 109, 110, 111, 112, 113, 115], "low"),       # syöksylyönti, osuma idx 6 (ruutu 111)
    ("polvi",       "B", [112, 113, 114, 115, 116, 117, 118, 119, 120, 122], "low"),       # polvipotku, osuma idx 4-5
    ("osuma",       "C", [10, 12, 14, 16, 18, 20, 22, 24], "low"),
    # ilma 5 kuvaa (0,1 s/kuva; heitto on n. 0,5 s) + varakuva (Enemy ei näytä ilmassa viimeistä) + maa 6 kuvaa
    ("kaatuminen",  "C", [24, 30, 36, 40, 44, 46, 48, 50, 52, 54, 57, 62], "com"),
    ("ylosnousu",   "C", [122, 126, 130, 133, 136, 139, 142, 145, 148, 151, 154, 157, 164], "com"),
    # heitetty (Enemy: 0-2 ote, sitten lentokuvat flipFlightFrames kpl, sitten maa): ote 3 (osuman taaksepäin nojaus), lento 6 (0,08 s/kuva), maa 6 (0,1 s/kuva)
    ("heitetty",    "C", [14, 18, 22, 28, 32, 36, 40, 44, 46, 48, 50, 52, 54, 57, 62], "com"),
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
        self.f = os.environ['NEWVID'] if key == 'N' else glob.glob(os.path.join(folder, VID[key] + "*"))[0]
        cap = cv2.VideoCapture(self.f); self.frames = []
        while True:
            ok, fr = cap.read()
            if not ok: break
            self.frames.append(key_frame(fr))
        ys, xs = np.where(self.frames[0][..., 3] > 128)
        self.floor = 636 if key == 'N' else ys.max() + 1   # seisovan hahmon jalkojen alareuna (N: kiinteä kamera)
        self.k = KB * (2.0 if key == "A" else 1.0)   # N on 720p: sama kuin B

def build(folder, guard_path, out):
    os.makedirs(out, exist_ok=True)
    vids = {k: Video(folder, k) for k in ("ABCN" if "NEWVID" in os.environ else "ABC")}
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
        save_sheet(items, lift, f"{out}/lentoperamies_{name}.png")
        print(name, len(items), "ruutua nosto", lift)
    if only: return
    # suojaus: neljä hahmoa vihreästä kuvasta, mittakaava B:n seisovasta (guard) asennosta
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
    save_sheet(res, 0, f"{out}/lentoperamies_suoja.png")

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
    build(sys.argv[1], sys.argv[2], sys.argv[3])
