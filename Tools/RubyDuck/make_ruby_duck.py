"""Rubyn alasväistö (kyykistys): ruby_alasvaisto.png. Lähde: ruby_alasvaisto_src.png (läpinäkyvä tausta, 1536 x 1024, 5 kuvaa: ylärivi 3, alarivi 2).
Järjestys vasemmalta oikealle, ylhäältä alas: 0 vartioasento, 1 painuu, 2 kyykky, 3 syvin kyykky, 4 nousu. Tulos: 5 kuvaa, ruutu 512 x 384 (4 saraketta, 2 riviä),
jalkojen alareuna riville 374, takajalka paikallaan (kuten ruby_counter). Mittakaava: vartioasento -> 326 px (sama kuin counterissa).
Käyttö: python make_ruby_duck.py <ulostulo-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GustavAssets"))
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "HeroCounter"))
from split_sheets import defringe
from make_gustav_sheets import resize_premult
from make_counter import rear_foot, CELL_W, CELL_H, FEET_ROW

REF_H = 326.0

def figures(im):
    a = im[..., 3] > 20
    n, lab, st, cen = cv2.connectedComponentsWithStats(cv2.dilate(a.astype(np.uint8), np.ones((5, 5), np.uint8)), connectivity=8)
    big = [i for i in range(1, n) if st[i, cv2.CC_STAT_AREA] >= 3000]
    top = sorted([i for i in big if cen[i][1] < im.shape[0] * 0.5], key=lambda i: cen[i][0])
    bot = sorted([i for i in big if cen[i][1] >= im.shape[0] * 0.5], key=lambda i: cen[i][0])
    out = []
    for i in top + bot:
        m = (lab == i) & a; ys, xs = np.where(m)
        crop = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
        crop[~m[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 3] = 0
        out.append(defringe(crop))
    return out

def main(out):
    os.makedirs(out, exist_ok=True)
    im = cv2.imread(os.path.join(os.path.dirname(__file__), "ruby_alasvaisto_src.png"), cv2.IMREAD_UNCHANGED)
    figs = figures(im)
    assert len(figs) == 5, len(figs)
    scale = REF_H / figs[0].shape[0]
    rear0, center0 = rear_foot(figs[0][..., 3] > 128)
    off = (center0 - rear0) * scale
    sheet = np.zeros((2 * CELL_H, 4 * CELL_W, 4), np.uint8)
    for k, fig in enumerate(figs):
        r = resize_premult(fig, scale)
        rear, _ = rear_foot(r[..., 3] > 128)
        x0 = int(round(CELL_W / 2 - off - rear)); y0 = FEET_ROW - r.shape[0]
        sx0, sy0 = max(0, -x0), max(0, -y0); dx0, dy0 = max(0, x0), max(0, y0)
        ww = min(r.shape[1] - sx0, CELL_W - dx0); hh = min(r.shape[0] - sy0, CELL_H - dy0)
        if ww < r.shape[1] or hh < r.shape[0]: print("  HUOM: kuva ylittää ruudun", k, r.shape, x0, y0)
        ox, oy = (k % 4) * CELL_W, (k // 4) * CELL_H
        sheet[oy + dy0:oy + dy0 + hh, ox + dx0:ox + dx0 + ww] = r[sy0:sy0 + hh, sx0:sx0 + ww]
    cv2.imwrite(os.path.join(out, "ruby_alasvaisto.png"), sheet)
    print("ruby_alasvaisto 5 kuvaa, mittakaava %.3f" % scale)

if __name__ == "__main__":
    main(sys.argv[1])
