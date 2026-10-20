"""Tomin osuma, kaatuminen, ylösnousu ja heitettynä olo käyttäjän kuvista (3 kuvaa, läpinäkyvä tausta): tom_osuma.png, tom_kaatuminen.png,
tom_nousu.png, tom_heitetty.png (ruutu 768 x 448, 4 saraketta, ylärivi ensin). Alkuperäiset katsovat vasemmalle -> peilataan oikealle (kuten muut kuvat).
Kuvat: 1 = osuma (5) + kaatuminen (5), 2 = heitettynä (lento + kieppuminen, 8), 3 = ylösnousu (8).
Käyttö: python make_tom_falls.py <ulostulo-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GustavAssets"))
from make_gustav_sheets import resize_premult, anchor_x, paste, CELL_W, CELL_H, COLS
from split_sheets import defringe
from outline import add_outline
UP = "C:/Users/marku/.claude/uploads/6d277d2e-0dfe-4331-9679-b4155f74f179/"
SRC = {1: "d2a242c9-image.png", 2: "a79364e3-image.png", 3: "12ee0371-image.png", 4: "23160956-image.png"}   # 4 = proteiinishakerin juonti (berserk)
SCALE = 0.66   # seisova ruutu (kuva 1, ruutu 0) ~455 px -> 300 px

def figures(path, grid):
    """Ruudukko (sarakkeet, rivit): osat jaetaan ruutuihin painopisteen mukaan; punainen kohina (alle 400 px:n osat) pois."""
    cols, rows = grid
    im = cv2.imread(UP + path, cv2.IMREAD_UNCHANGED)
    a = im[..., 3] > 20
    H, W = a.shape
    n, lab, st, cen = cv2.connectedComponentsWithStats(a.astype(np.uint8), connectivity=8)
    cells = [np.zeros(a.shape, bool) for _ in range(cols * rows)]
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] < 400: continue
        c = min(cols - 1, int(cen[i][0] / (W / cols))); r = min(rows - 1, int(cen[i][1] / (H / rows)))
        cells[r * cols + c] |= lab == i
    out = []
    for keep in cells:
        ys, xs = np.where(keep)
        crop = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy(); crop[~keep[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 3] = 0
        out.append(cv2.flip(defringe(crop), 1))   # peilaus: katsoo oikealle
    return out

def cell_of(fig, mode, scale=SCALE):
    r = resize_premult(fig, scale)
    cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, anchor_x(r, mode), r.shape[0])
    return cell

def write(out, name, cells):
    rows = (len(cells) + COLS - 1) // COLS
    sheet = np.zeros((rows * CELL_H, COLS * CELL_W, 4), np.uint8)
    for k, c in enumerate(cells):
        sheet[(k // COLS) * CELL_H:(k // COLS + 1) * CELL_H, (k % COLS) * CELL_W:(k % COLS + 1) * CELL_W] = c
    cv2.imwrite(f"{out}/tom_{name}.png", add_outline(sheet)); print(name, len(cells), "ruutua")   # paksumpi ääriviiva

def main(out):
    os.makedirs(out, exist_ok=True)
    f1, f2, f3 = figures(SRC[1], (5, 2)), figures(SRC[2], (4, 2)), figures(SRC[3], (4, 2))
    write(out, "osuma", [cell_of(f, "low") for f in f1[0:5]])                        # 0 seisoo, 1–4 päätä taakse ja horjahdus
    # maassa makaavat kuvat olivat lähdekuvissa pienempiä (hahmo näytti kutistuvan): kerroin per ruutu
    K_FALL = [1.0, 1.0, 1.0, 1.12, 1.25, 1.40]
    K_RISE = [1.10, 1.08, 1.05, 1.0, 1.0, 1.0, 1.0, 1.0]
    K_THROWN = [1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.0, 1.05, 1.10]
    write(out, "kaatuminen", [cell_of(f, "com", SCALE * k) for f, k in zip(f1[4:10], K_FALL)])   # 4–6 ilmassa, 7–9 maassa
    write(out, "heitetty", [cell_of(f1[0], "low")] + [cell_of(f, "com", SCALE * k) for f, k in zip(f2, K_THROWN[1:])])  # 0 asento, 1–8 lento ja kieppuminen
    write(out, "nousu", [cell_of(f, "com", SCALE * k) for f, k in zip(f3, K_RISE)])
    write(out, "juoma", [cell_of(f, "low", 0.62) for f in figures(SRC[4], (5, 2))])   # 10 kuvaa: ote, shakeri esiin, juonti, takaisin (seisova ~485 px -> 300 px)

if __name__ == "__main__":
    main(sys.argv[1])
