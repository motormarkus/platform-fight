"""Pensalenkkari osa 3: idle (video 3), ylosnousu (kuva 1), pullo/esineen poiminta ja heitto (kuva 2, 13 kuvan rakenne kuten punk_pullo).
Kaytto: python make_lenkkari3.py <video3-avainnetut ruudut> <ulos>
Tulostaa C#-taulukon kadenpaikoista (yksikkoa, pivot alareunan keskella)."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_lenkkari2 import *
import make_lenkkari2 as m2
IMG3 = {"nousu": "9006a176-image.png", "pullo": "36e6b279-image.png"}
SC_UP = float(os.environ.get("SU", 0.47)); SC_PICK = float(os.environ.get("SP", 0.62))

def cell_info(fig, s, off, mode="low"):
    r = resize_premult(fig, s)
    ax = anchor_x(r, mode)
    cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, ax, r.shape[0])
    x0 = int(round(CELL_W / 2 - ax)); y0 = int(round(CELL_H - FOOT_MARGIN - r.shape[0]))
    def pt(sx, sy):   # lahdekuvan piste -> yksikot (pivot alhaalla keskella, ppu 100)
        cx = x0 + s * (sx - off[0]); cy = y0 + s * (sy - off[1])
        return ((cx - CELL_W / 2) / 100.0, (CELL_H - cy) / 100.0)
    return cell, pt

def main(raw3, out):
    os.makedirs(out, exist_ok=True)
    # idle: 120 ruutua, joka 4. (30 kuvaa)
    i0 = cv2.imread(f"{raw3}/000.png", cv2.IMREAD_UNCHANGED); ys, xs = np.where(i0[..., 3] > 128)
    sc = 300 / (ys.max() - ys.min()) * float(os.environ.get("SI", 0.9995)); floor = ys.max()
    print("idle-mittakaava", sc)
    items = []
    for i in range(0, 120, 4):
        im = cv2.imread(f"{raw3}/{i:03d}.png", cv2.IMREAD_UNCHANGED); ys, xs = np.where(im[..., 3] > 128)
        x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1; crop = im[y0:y1, x0:x1]
        items.append((resize_premult(crop, sc), anchor_x(crop, "low") * sc, (floor - y0) * sc))
    below = max(r.shape[0] - foot for r, _, foot in items); lift = max(0, int(np.ceil(below - FOOT_MARGIN + 2)))
    idle_cells = []
    for r, ax, foot in items:
        cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, ax, foot, lift); idle_cells.append(cell)
    write(out, "idle", idle_cells)
    # ylosnousu
    up = figs(cv2.imread(UP + IMG3["nousu"], cv2.IMREAD_UNCHANGED), (5, 2), 20000)
    K_UP = [1.0, 0.95, 0.9, 0.85, 0.8, 0.75, 0.75, 0.85, 0.9, 0.95]   # kyykky- ja polvikuvat olivat lahteessa liian isoja
    write(out, "ylosnousu", [cell_feet(x, SC_UP * K_UP[k], "com" if k < 5 else "low") for k, x in enumerate(up)])
    # pullo: 8 kuvaa -> 13 kuvan rakenne
    pf = figs(cv2.imread(UP + IMG3["pullo"], cv2.IMREAD_UNCHANGED), (4, 2), 40000)
    offs = list(m2.LAST_OFF)
    info = [cell_info(f, SC_PICK, o) for f, o in zip(pf, offs)]
    cells = [c for c, _ in info]; pts = [p for _, p in info]
    layout = [None, 0, 1, 2, 3, 3, 4, 5, 5, 6, 7, 7, 7]   # sarjan kuva -> lahdekuva (0 = tappeluasento videosta)
    stand = idle_cells[0]
    sheet = [stand if k is None else cells[k] for k in layout]
    write(out, "pullo", sheet)
    # kadenpaikat lahdekuvan pisteista (kuvan 2 arviot): kuva -> (sx, sy, rot)
    hands = {2: (1, (645, 448), 90), 3: (2, (975, 372), 90), 4: (3, (1335, 230), 45), 5: (3, (1335, 230), 10), 6: (4, (120, 530), 10), 7: (5, (715, 598), 40), 9: (6, (1210, 568), -60)}
    res = []
    for k in range(13):
        if k in hands:
            src, (sx, sy), rot = hands[k]; x, y = pts[src](sx, sy); res.append(f"new Vector4({x:.2f}f, {y:.2f}f, {rot}f, 1f)")
        else: res.append("Vector4.zero")
    print("GRIPS:", ", ".join(res))

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
