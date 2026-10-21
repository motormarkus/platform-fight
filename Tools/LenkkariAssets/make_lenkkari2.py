"""Pensalenkkari osa 2: juoksu (video 2), torjunta (kuva 1), lakinheitto + suora (kuva 2), lakin nosto (kuva 3).
Kaytto: python make_lenkkari2.py <raw1> <runraw> <ulos>   (raw1 = key_lenkkari.py ruudut video 1, runraw = juoksuvideon avainnetut ruudut)"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_lenkkari import *
UP2 = UP
IMG2 = {"torjunta": "1e911d90-image.png", "lakki": "3bdd29c4-image.png", "nosto": "d6817925-image.png", "heitto": "c7ea57e4-image.png", "ilma1": "a761e81d-image.png", "ilma": "1a803d3c-image.png"}
SC_BLOCK = float(os.environ.get("SB", 0.606)); SC_CAP = float(os.environ.get("SC", 0.5))

def key_white(path):
    im = cv2.imread(UP2 + path, cv2.IMREAD_UNCHANGED)
    if im.shape[2] == 4 and im[..., 3].min() < 250: return im
    bgr = im[..., :3]
    near = ((bgr.min(axis=2) > 236)).astype(np.uint8)
    n, lab = cv2.connectedComponents(near, connectivity=4)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])).tolist()) - {0}
    bg = np.isin(lab, list(edge))
    # tausta-alueen reunan pehmennys
    a = (~bg).astype(np.float32)
    a = cv2.GaussianBlur(a, (0, 0), 0.8)
    n2, lab2, st, _ = cv2.connectedComponentsWithStats((a > 0.5).astype(np.uint8), connectivity=8)
    keep = np.zeros(a.shape, bool)
    for i in range(1, n2):
        if st[i, cv2.CC_STAT_AREA] > 1500: keep |= lab2 == i
    a = a * cv2.dilate(keep.astype(np.uint8), np.ones((5, 5), np.uint8))
    return np.dstack([bgr, (a * 255).astype(np.uint8)])

LAST_OFF = []
def figs(im, grid, bigmin=40000):
    del LAST_OFF[:]
    """Isot osat = hahmot (ruudukkojärjestyksessä); pienet osat (lakki) liitetään lähimpään hahmoon."""
    cols, rows = grid
    a = im[..., 3] > 20
    H, W = a.shape
    n, lab, st, cen = cv2.connectedComponentsWithStats(a.astype(np.uint8), connectivity=8)
    big = [i for i in range(1, n) if st[i, cv2.CC_STAT_AREA] > bigmin]
    big.sort(key=lambda i: (min(rows - 1, int(cen[i][1] / (H / rows))), cen[i][0]))
    groups = {i: (lab == i) for i in big}
    dts = {j: cv2.distanceTransform((lab != j).astype(np.uint8), cv2.DIST_L2, 3) for j in big}
    for i in range(1, n):
        if i in groups or st[i, cv2.CC_STAT_AREA] < 300: continue
        m = lab == i
        j = min(big, key=lambda j: dts[j][m].min()); groups[j] |= m
    out = []
    for j in big:
        keep = groups[j]; ys, xs = np.where(keep)
        crop = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy(); crop[~keep[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 3] = 0
        LAST_OFF.append((int(xs.min()), int(ys.min())))
        out.append(defringe(crop))
    return out

def figs_cut(im, xcuts, ycut):
    """Kuva jaetaan pystyleikkauksilla riveittäin (ylärivi ja alarivi omat leikkauskohdat); osien väliset pienet roskat pois."""
    a = im[..., 3] > 20
    H, W = a.shape
    out = []
    for r, cuts in enumerate(xcuts):
        edges = [0] + cuts + [W]
        for c in range(len(edges) - 1):
            keep = np.zeros(a.shape, bool)
            y0, y1 = (0, ycut) if r == 0 else (ycut, H)
            keep[y0:y1, edges[c]:edges[c + 1]] = True
            keep &= a
            ys, xs = np.where(keep)
            crop = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy(); crop[~keep[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 3] = 0
            out.append(defringe(crop))
    return out

def cell_feet(fig, s, mode="low"):
    r = resize_premult(fig, s)
    cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, anchor_x(r, mode), r.shape[0]); return cell

def main(raw1, runraw, out):
    os.makedirs(out, exist_ok=True)
    # juoksu: silmukka 38..64 (27 ruutua)
    sc = 0.5008; frames = list(range(38, 65))
    bots = []
    for i in frames:
        im = cv2.imread(f"{runraw}/{i:03d}.png", cv2.IMREAD_UNCHANGED); ys, xs = np.where(im[..., 3] > 128); bots.append(ys.max())
    floor = int(np.percentile(bots, 90)); items = []
    for i in frames:
        im = cv2.imread(f"{runraw}/{i:03d}.png", cv2.IMREAD_UNCHANGED); ys, xs = np.where(im[..., 3] > 128)
        x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1; crop = im[y0:y1, x0:x1]
        items.append((resize_premult(crop, sc), anchor_x(crop, "torso") * sc, (floor - y0) * sc))
    below = max(r.shape[0] - foot for r, _, foot in items); lift = max(0, int(np.ceil(below - FOOT_MARGIN + 2)))
    cells = []
    for r, ax, foot in items:
        cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, ax, foot, lift); cells.append(cell)
    write(out, "juoksu", cells)
    # torjunta (6): kuva 1 lÃ¤pinÃ¤kyvÃ¤
    f = figs(cv2.imread(UP2 + IMG2["torjunta"], cv2.IMREAD_UNCHANGED), (3, 2))
    write(out, "torjunta", [cell_feet(x, SC_BLOCK) for x in f])
    # lakinheitto: [asento videosta, heitto, jÃ¤lkiasento] ja suora: [valmistus, isku, palautus]
    f2 = figs(key_white(IMG2["lakki"]), (2, 2))
    fa = figs(cv2.imread(UP2 + IMG2["heitto"], cv2.IMREAD_UNCHANGED), (2, 2))   # nosto, lakki kädessä, irtoaa, lentää
    write(out, "lakki", [cell_feet(x, SC_CAP) for x in fa])
    write(out, "lakki_suora", [cell_feet(f2[1], SC_CAP), cell_feet(f2[2], SC_CAP), cell_feet(f2[3], SC_CAP)])
    f3 = figs(key_white(IMG2["nosto"]), (2, 2))
    # heitettynä: kuva 1 = ensimmäinen ilmakuva erikseen, kuva 2 = lento, kieppuminen ja maahan (8)
    i1 = figs(cv2.imread(UP2 + IMG2["ilma1"], cv2.IMREAD_UNCHANGED), (1, 1))
    i3 = figs(cv2.imread(UP2 + IMG2["ilma"], cv2.IMREAD_UNCHANGED), (4, 2), 20000)
    S_A1 = float(os.environ.get("SA1", 0.33)); S_A3 = float(os.environ.get("SA3", 0.5))
    write(out, "heitetty", [cell_feet(i1[0], S_A1, "com")] + [cell_feet(x, S_A3, "com") for x in i3])
    write(out, "lakki_nosto", [cell_feet(x, SC_CAP) for x in f3])

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3])
