"""Roccon ja Rubyn huumausreaktio (Gustavin hiekka): kuvasarjat rocco_huumaus.png ja ruby_huumaus.png, 3 kuvaa (0 vavahdus, 1-2 silmien hieronta).
Lähteet: käyttäjän 3 hahmon kuvat (läpinäkyvä tausta). Ruutu 512 x 384, ylärivi ensin, jalkojen alareuna riville 374, x = jalkojen keskikohta ruudun x 256.
Mittakaava: Rocco 335 px / 729 px, Ruby 326 px / 718 px (seisova vartioasento vs. vavahduskuvan korkeus).
Käyttö: python make_hero_daze.py <ulostulo-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from split_sheets import split
from make_gustav_sheets import resize_premult
UP = "C:/Users/marku/.claude/uploads/6d277d2e-0dfe-4331-9679-b4155f74f179/"
CELL_W, CELL_H, FEET_ROW, ANCHOR_X = 512, 384, 374, 256
HEROES = [("rocco_huumaus", "6510274f-image.png", 335.0 / 729.0), ("ruby_huumaus", "e4e2d723-image.png", 326.0 / 718.0)]

def main(out):
    os.makedirs(out, exist_ok=True)
    for name, src, f in HEROES:
        im = cv2.imread(UP + src, cv2.IMREAD_UNCHANGED)
        figs = split(im)
        sheet = np.zeros((CELL_H, 4 * CELL_W, 4), np.uint8)
        for k, fig in enumerate(figs):
            r = resize_premult(fig, f)
            a = r[..., 3] > 128; h = a.shape[0]
            ys, xs = np.where(a[int(h * 0.7):])
            ax = xs.mean()
            x0 = int(round(ANCHOR_X - ax)); y0 = FEET_ROW - r.shape[0]
            cell = np.zeros((CELL_H, CELL_W, 4), np.uint8)
            sx0, sy0 = max(0, -x0), max(0, -y0); dx0, dy0 = max(0, x0), max(0, y0)
            ww = min(r.shape[1] - sx0, CELL_W - dx0); hh = min(r.shape[0] - sy0, CELL_H - dy0)
            cell[dy0:dy0 + hh, dx0:dx0 + ww] = r[sy0:sy0 + hh, sx0:sx0 + ww]
            if ww < r.shape[1] or hh < r.shape[0]: print("  HUOM: kuva ylittää ruudun", name, k, r.shape, x0, y0)
            sheet[:, k * CELL_W:(k + 1) * CELL_W] = cell
        cv2.imwrite(os.path.join(out, name + ".png"), sheet)
        print(name, len(figs), "kuvaa", [fig.shape for fig in figs])

if __name__ == "__main__":
    main(sys.argv[1])
