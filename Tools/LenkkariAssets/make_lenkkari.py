"""Pensalenkkarin kuvasarjat: lenkkari_<nimi>.png, 4 saraketta, ruutu 768 x 448, seisova hahmo 300 px, pivot alhaalla keskellä.
Vaiheet: 1) python key_lenkkari.py (raaka-ruudut)  2) python make_lenkkari.py <raw> <ulos>
Video: taisteluasento, lyönnit ja potkut (hahmo katsoo oikealle). Osuma ja kaatuminen käyttäjän kuvista (katsovat jo oikealle)."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, "G:/unity pelit/mopopeli/platform fight/Tools/GustavAssets")
sys.path.insert(0, "G:/unity pelit/mopopeli/platform fight/Tools/TomAssets")
from make_gustav_sheets import resize_premult, anchor_x, paste, CELL_W, CELL_H, COLS, FOOT_MARGIN
from split_sheets import defringe
from outline import add_outline
UP = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
IMG = {1: "e66bdc56-image.png", 2: "80866322-image.png", 3: "a0c1bb87-image.png"}
def R(a, b, s=1): return list(range(a, b, s))
ANIMS = [
    ("idle",   R(0, 12)),            # painonsiirto, ping-pong
    ("jab",    R(12, 22)),           # etukäsi, ojennettuna 14-17
    ("suora",  R(75, 92)),           # takakäsi, ojennettuna 78-82
    ("potku",  R(104, 120)),         # etupotku, ojennettuna 109-113
    ("pyora",  R(174, 190)),         # pyörähdyspotku, ojennettuna 178-181
    ("lyonti2", R(20, 34)),          # askel ja lyönti
]

def load(raw, i):
    return cv2.imread(f"{raw}/{i:03d}.png", cv2.IMREAD_UNCHANGED)

def write(out, name, cells):
    rows = (len(cells) + COLS - 1) // COLS
    sheet = np.zeros((rows * CELL_H, COLS * CELL_W, 4), np.uint8)
    for k, c in enumerate(cells):
        sheet[(k // COLS) * CELL_H:(k // COLS + 1) * CELL_H, (k % COLS) * CELL_W:(k % COLS + 1) * CELL_W] = c
    cv2.imwrite(f"{out}/lenkkari_{name}.png", add_outline(sheet)); print(name, len(cells))

def figures(path, grid, mincnt=400):
    cols, rows = grid
    im = cv2.imread(UP + path, cv2.IMREAD_UNCHANGED)
    a = im[..., 3] > 20
    H, W = a.shape
    n, lab, st, cen = cv2.connectedComponentsWithStats(a.astype(np.uint8), connectivity=8)
    cells = [np.zeros(a.shape, bool) for _ in range(cols * rows)]
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] < mincnt: continue
        c = min(cols - 1, int(cen[i][0] / (W / cols))); r = min(rows - 1, int(cen[i][1] / (H / rows)))
        cells[r * cols + c] |= lab == i
    out = []
    for keep in cells:
        ys, xs = np.where(keep)
        crop = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy(); crop[~keep[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 3] = 0
        out.append(defringe(crop))
    return out

def build(raw, out):
    os.makedirs(out, exist_ok=True)
    i0 = load(raw, 0); ys, xs = np.where(i0[..., 3] > 128)
    sc = 300 / (ys.max() - ys.min()); floor = ys.max()
    print("mittakaava", sc, "lattia", floor)
    for name, idx in ANIMS:
        items = []
        for i in idx:
            im = load(raw, i); ys, xs = np.where(im[..., 3] > 128)
            x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1
            crop = im[y0:y1, x0:x1]
            items.append((resize_premult(crop, sc), anchor_x(crop, "low") * sc, (floor - y0) * sc))
        below = max(r.shape[0] - foot for r, _, foot in items)
        lift = max(0, int(np.ceil(below - FOOT_MARGIN + 2)))
        cells = []
        for r, ax, foot in items:
            cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, ax, foot, lift); cells.append(cell)
        write(out, name, cells)
    # osuma ja kaatuminen kuvista
    f1 = figures(IMG[1], (1, 1)); f2 = figures(IMG[2], (2, 2)); f3 = figures(IMG[3], (3, 2))
    S1 = float(os.environ.get("S1", 0.285)); K2 = float(os.environ.get("K2", 1.4)); K3 = float(os.environ.get("K3", 1.5))
    def cell_of(fig, mode, s):
        r = resize_premult(fig, s)
        cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, anchor_x(r, mode), r.shape[0]); return cell
    write(out, "osuma", [cell_of(f1[0], "low", S1)])
    write(out, "kaatuminen", [cell_of(f, "com", S1 * K2) for f in f2] + [cell_of(f, "com", S1 * K3) for f in f3])

if __name__ == "__main__":
    build(sys.argv[1], sys.argv[2])
