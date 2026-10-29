"""Kapteenin lisäliikkeet: taklaus (video), alakoukku (kuva 3 x 2) ja suojaus (kuva 2 x 2). Ruutu 1280 x 768, ankkuri x=640, jalat 16 px ylös, seisova hahmo 648 px.
Ajo (projektin juuresta): python Tools/LatkaAssets/make_kapteeni_lisa.py <taklaus.mov> <alakoukku.png> <suoja.png> <ulos>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from apuri_key import key
import make_latka_sheets
make_latka_sheets.CH = 768
from make_latka_sheets import feet, cell, save, rescale
from make_apuri_nyrkki import smooth, between, check, load as _load
CW, ANCH = 1280, 640
K = 640 / 541.0
STAND = 548 * K   # seisova hahmo (nyrkkiasento) px
r = lambda a, b, s=1: list(range(a, b + 1, s))

def grid(path, cols, rows):
    im = cv2.imread(path); H, W = im.shape[:2]; k = key(im, largest=False)
    m = (k[..., 3] > 128).astype(np.uint8); n, lab, st, cen = cv2.connectedComponentsWithStats(m, connectivity=8)
    groups = [np.zeros(m.shape, bool) for _ in range(cols * rows)]
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] < 400: continue
        c = min(cols - 1, int(cen[i][0] / (W / cols))); rr = min(rows - 1, int(cen[i][1] / (H / rows)))
        groups[rr * cols + c] |= lab == i
    out = []
    for g in groups:
        f = k.copy(); f[..., 3] = np.where(g, f[..., 3], 0); out.append(f)
    return out, W / cols

def sheet(path, cols, rows):
    fr, cw0 = grid(path, cols, rows)
    h0 = feet(fr[0])[2]; k = STAND / h0
    _, ax0, _ = feet(fr[0]); ax_rel = ax0 % cw0 if cols > 1 else ax0   # jalkojen keskikohta ruudussa
    cells = []
    for i, f in enumerate(fr):
        col = i % cols; fl = feet(f)[0]
        cells.append(cell(rescale(f, k), (ax_rel + col * cw0) * k, fl * k, CW, ANCH))
    return cells

def main():
    vid, pa, ps, out = sys.argv[1:5]; os.makedirs(out, exist_ok=True); sets = {}
    f = _load(vid); fl, ax, _ = feet(f[0]); C = lambda i: cell(rescale(f[i], K), ax * K, fl * K, CW, ANCH)
    sets["kapteeni_taklaus"] = smooth([C(i) for i in r(0, 34, 2)] + [C(i) for i in r(36, 84, 4)] + [C(i) for i in r(88, 172, 6)])
    u = sheet(pa, 3, 2); seq = []
    for i, c in enumerate(u):
        seq.append(c)
        if i < len(u) - 1: seq += between(c, u[i + 1], 2)
    sets["kapteeni_alakoukku"] = seq
    g = sheet(ps, 2, 2)
    sets["kapteeni_suoja"] = [g[0]] + between(g[0], g[1], 2) + [g[1], g[2], g[3]]
    for name, cells in sets.items():
        check(name, cells); save(cells, 4, os.path.join(out, name + ".png"))
if __name__ == "__main__": main()
