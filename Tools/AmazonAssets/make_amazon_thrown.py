"""Amatsoni: heitettynä oleminen (käyttäjän korjaama PNG-sarja 121 kuvaa, 910 x 704, RGBA, 30 fps) -> amatsoni_heitetty.png (1024 x 832 ruudut, 5 saraketta, 13 kuvaa).
Enemy.flipThrownSprites: 0-2 ote (seisova), 3-8 lento (6 kuvaa: kallistuu ja pyörähtää selälleen), 9-12 alastulo (4 kuvaa, selkä/jalat lattialla). Hahmo skaalataan seisovan kuvan mukaan ~650 px:ään; pivot jalkojen kohdalla (x 0,5, y 16/832).
Ajo: python Tools/AmazonAssets/make_amazon_thrown.py <PNG-kansio> <ulos>"""
import os, sys, glob
import cv2, numpy as np
CW, CH, FOOT = 1024, 832, 16
src, out = sys.argv[1:3]
fs = sorted(glob.glob(os.path.join(src, "*.png")))
F = [cv2.imread(f, cv2.IMREAD_UNCHANGED) for f in fs]
def bbox(f):
    ys, xs = np.where(f[..., 3] > 128); return xs.min(), xs.max(), ys.min(), ys.max()
x0, x1, y0, y1 = bbox(F[0]); k = 650.0 / (y1 - y0 + 1); ax0 = (x0 + x1) / 2.0; fl0 = y1 + 1
print("seisova korkeus", y1 - y0 + 1, "kerroin", round(k, 3))
def scaled(f):
    a = f.astype(np.float32); a[..., :3] *= a[..., 3:4] / 255.0
    r = cv2.resize(a, (round(f.shape[1] * k), round(f.shape[0] * k)), interpolation=cv2.INTER_AREA)
    al = r[..., 3:4] / 255.0; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    return r.clip(0, 255).astype(np.uint8)
def put(f, anchor):   # anchor: (x_in_src_px, y_in_src_px) -> solun kohta
    s = scaled(f); c = np.zeros((CH, CW, 4), np.uint8)
    (px, py), (cx, cy) = anchor
    ox, oy = int(round(cx - px * k)), int(round(cy - py * k))
    sy0, sx0 = max(0, oy), max(0, ox); sy1, sx1 = min(CH, oy + s.shape[0]), min(CW, ox + s.shape[1])
    if sy1 > sy0 and sx1 > sx0: c[sy0:sy1, sx0:sx1] = s[sy0 - oy:sy1 - oy, sx0 - ox:sx1 - ox]
    a = c[..., 3] > 128; ys, xs = np.where(a)
    if len(xs) and (xs.min() <= 0 or xs.max() >= CW - 1 or ys.min() <= 0): print("  HUOM leikkautuu", anchor)
    return c
floor, cxm = CH - FOOT, CW // 2
cells = []
# ote: seisovan hahmon kuvia (jalat lattialla)
for i in (14, 17, 20):
    cells.append(put(F[i], ((ax0, fl0), (cxm, floor))))
# lento: kuva 24..78 (6 kuvaa), hahmon keskipiste ruudun keskelle (pyöriminen näkyy kuvissa)
fl_idx = [int(round(v)) for v in np.linspace(26, 76, 6)]
for i in fl_idx:
    b = bbox(F[i]); cells.append(put(F[i], (((b[0] + b[1]) / 2.0, (b[2] + b[3]) / 2.0), (cxm, floor - 330))))
# alastulo: kuvat 84..120 (4 kuvaa), makaava hahmo alareunasta lattiaan
for i in np.linspace(84, 120, 4).astype(int):
    b = bbox(F[i]); cells.append(put(F[i], (((b[0] + b[1]) / 2.0, b[3] + 1), (cxm, floor))))
rows = (len(cells) + 4) // 5; img = np.zeros((rows * CH, 5 * CW, 4), np.uint8)
for n, c in enumerate(cells): r, q = divmod(n, 5); img[r * CH:(r + 1) * CH, q * CW:(q + 1) * CW] = c
cv2.imwrite(os.path.join(out, "amatsoni_heitetty.png"), img); print(len(cells), "kuvaa", img.shape[1], "x", img.shape[0])
