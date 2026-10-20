"""Roccon ja Rubyn counter-isku (suojaus + lyönti kesken vihun kombon): rocco_counter.png ja ruby_counter.png.
Lähteet: käyttäjän kaksi kuvaa (3 x 2 ruutua, läpinäkyvä tausta, 1536 x 1024): 0 vartioasento (EI käytetä sarjassa), 1 kämmenellä torjuntakäsi, 2 käsi alas, 3 veto taakse,
4 lyönti, 5 lyönti pitkänä. Tulos: 5 kuvaa, ruutu 512 x 384 (kuten muut sankarikuvat), jalkojen alareuna riville 374, takajalka paikallaan.
Mittakaava: vartioasento (ruutu 0) -> Rocco 335 px, Ruby 326 px.
Käyttö: python make_counter.py <ulostulo-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GustavAssets"))
from split_sheets import split
from make_gustav_sheets import resize_premult
UP = "C:/Users/marku/.claude/uploads/6d277d2e-0dfe-4331-9679-b4155f74f179/"
CELL_W, CELL_H, FEET_ROW = 512, 384, 374
HEROES = [("rocco_counter", "35ce5339-image.png", 335.0), ("ruby_counter", "c07e86f6-image.png", 326.0)]

def rear_foot(a):
    """Takajalan (vasen, hahmo katsoo oikealle) saappaan keskikohta: alimpien rivien (12 %) vasen puolisko."""
    h = a.shape[0]; ys, xs = np.where(a[int(h * 0.88):])
    mid = (xs.min() + xs.max()) / 2.0
    return xs[xs < mid].mean(), xs.mean()

def main(out):
    os.makedirs(out, exist_ok=True)
    for name, src, ref_h in HEROES:
        im = cv2.imread(UP + src, cv2.IMREAD_UNCHANGED)
        figs = split(im, (3, 2))
        scale = ref_h / figs[0].shape[0]
        rear0, center0 = rear_foot(figs[0][..., 3] > 128)
        off = (center0 - rear0) * scale                      # takajalan etäisyys jalkojen keskikohdasta vartioasennossa
        cells = []
        for fig in figs[1:]:
            r = resize_premult(fig, scale)
            rear, _ = rear_foot(r[..., 3] > 128)
            x0 = int(round(CELL_W / 2 - off - rear)); y0 = FEET_ROW - r.shape[0]
            cell = np.zeros((CELL_H, CELL_W, 4), np.uint8)
            sx0, sy0 = max(0, -x0), max(0, -y0); dx0, dy0 = max(0, x0), max(0, y0)
            ww = min(r.shape[1] - sx0, CELL_W - dx0); hh = min(r.shape[0] - sy0, CELL_H - dy0)
            cell[dy0:dy0 + hh, dx0:dx0 + ww] = r[sy0:sy0 + hh, sx0:sx0 + ww]
            if ww < r.shape[1] or hh < r.shape[0]: print("  HUOM: kuva ylittää ruudun", name, r.shape, x0, y0)
            cells.append(cell)
        sheet = np.zeros((CELL_H, 4 * CELL_W, 4), np.uint8)    # 5 kuvaa: 4 sarakkeen leveys + toinen rivi
        sheet = np.zeros((2 * CELL_H, 4 * CELL_W, 4), np.uint8)
        for k, c in enumerate(cells):
            sheet[(k // 4) * CELL_H:(k // 4 + 1) * CELL_H, (k % 4) * CELL_W:(k % 4 + 1) * CELL_W] = c
        cv2.imwrite(os.path.join(out, name + ".png"), sheet)
        print(name, len(cells), "kuvaa, mittakaava %.3f" % scale)

if __name__ == "__main__":
    main(sys.argv[1])
