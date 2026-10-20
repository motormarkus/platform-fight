"""Tomin kuvasarjat: tom_<nimi>.png, 4 saraketta, ruutu 768 x 448 px (ylärivi ensin), seisova hahmo 300 px, jalat ruudun alareunasta 8 px ylös,
pivot alhaalla keskellä (kuten Marcus ja Gustav). Hahmo katsoo oikealle.
Vaiheet: 1) python key_tom.py <raw>  2) python make_tom_sheets.py <raw> <ulos>
Heittokuvat (7) tulevat käyttäjän kuvasta (läpinäkyvä tausta, 4 + 3 hahmoa); muut videoista."""
import glob, os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GustavAssets"))
from make_gustav_sheets import resize_premult, anchor_x, paste, CELL_W, CELL_H, COLS, FOOT_MARGIN
from split_sheets import defringe
sys.path.insert(0, os.path.dirname(__file__))
from outline import add_outline
UP = "C:/Users/marku/.claude/uploads/6d277d2e-0dfe-4331-9679-b4155f74f179/"
# video: (mittakaava -> 300 px seisova, lattiataso videossa)
VID = {"idle_pose": (300 / 885, 1046), "lyonnit": (300 / 589, 692), "kavely": (300 / 573, 688)}
def R(a, b, s=1): return list(range(a, b, s))
ANIMS = [
    ("idle",    "idle_pose", R(15, 64, 4)),                                  # painonsiirto (edestakaisin)
    ("pose",    "idle_pose", R(84, 180, 4)),                                 # lihasposeeraus
    ("kavely",  "kavely",    R(159, 188)),                                   # yksi askelsilmukka: jakso 29 ruutua (24 fps), kaikki ruudut; saumaero pienin alkukohdalla 159
    ("jab",     "lyonnit",   [19, 22, 25, 28, 31, 34, 37]),                  # osuma idx 4 (ruutu 31)
    ("suora",   "lyonnit",   [37, 40, 42, 44, 46, 49, 52, 55]),             # osuma idx 3 (ruutu 44)
    ("voima",   "lyonnit",   [138, 140, 142, 144, 146, 148, 150, 154, 158]), # iso kiertolyönti, osuma idx 4 (ruutu 146)
]
HEITTO_SCALE = 0.67   # kuvan seisova kurotusasento (ruutu 2) ~445 px -> 300 px

def build_heitto():
    im = cv2.imread(UP + "151fb7de-image.png", cv2.IMREAD_UNCHANGED)
    a = im[..., 3] > 20
    n, lab, st, cen = cv2.connectedComponentsWithStats(cv2.dilate(a.astype(np.uint8), np.ones((9, 9), np.uint8)), connectivity=8)
    ids = [i for i in range(1, n) if st[i, cv2.CC_STAT_AREA] > 8000]
    H = a.shape[0]
    ids.sort(key=lambda i: (0 if cen[i][1] < H * 0.5 else 1, cen[i][0]))    # ylärivi vasemmalta oikealle, sitten alarivi
    cells = []
    for i in ids:
        m = (lab == i) & a; ys, xs = np.where(m)
        crop = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy(); crop[~m[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 3] = 0
        crop = defringe(crop)
        f = HEITTO_SCALE * 300 / 300; r = resize_premult(crop, f)
        cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, anchor_x(r, "low"), r.shape[0]); cells.append(cell)
    return cells

def build(raw, out):
    os.makedirs(out, exist_ok=True)
    for name, source, idx in ANIMS:
        sc, floor = VID[source]; items = []
        for i in idx:
            im = cv2.imread(f"{raw}/{source}/{i:03d}.png", cv2.IMREAD_UNCHANGED)
            ys, xs = np.where(im[..., 3] > 128)
            x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
            crop = im[y0:y1, x0:x1]
            items.append((resize_premult(crop, sc), anchor_x(crop, "torso" if name == "kavely" else "low") * sc, (floor - y0) * sc))
        below = max(r.shape[0] - foot for r, _, foot in items)
        lift = max(0, int(np.ceil(below - FOOT_MARGIN + 2)))
        cells = []
        for r, ax, foot in items:
            cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, ax, foot, lift); cells.append(cell)
        write(out, name, cells); print(name, len(cells), "ruutua nosto", lift)
    write(out, "heitto", build_heitto()); print("heitto 7")

def write(out, name, cells):
    rows = (len(cells) + COLS - 1) // COLS
    sheet = np.zeros((rows * CELL_H, COLS * CELL_W, 4), np.uint8)
    for k, c in enumerate(cells):
        sheet[(k // COLS) * CELL_H:(k // COLS + 1) * CELL_H, (k % COLS) * CELL_W:(k % COLS + 1) * CELL_W] = c
    cv2.imwrite(f"{out}/tom_{name}.png", add_outline(sheet))   # paksumpi ääriviiva (Tom näytti irralliselta)

if __name__ == "__main__":
    sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GustavAssets"))
    build(sys.argv[1], sys.argv[2])
