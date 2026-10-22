"""Rubyn merimieskannin sarjat: kaatuminen (kuva 2), ylosnousu (kuva 1), horjuva kavely (video, silmukka 63-96).
Assets/Resources/Kanni/ruby_kanni_00..07, ruby_nousu_00..07, ruby_humala_00..33. Roccon tapaan: seisova ~322 px (Rubyn idle), makaavat enintaan 400 px pitkia."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_lenkkari2 import *
UPL = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
NOUSU, KAATUMINEN = UPL + "bbf2a594-image.png", UPL + "6b9ec0a2-image.png"
raw, out = sys.argv[1], sys.argv[2]
os.makedirs(out, exist_ok=True)
STAND, MAXLEN = 322.0, 400.0

def height(fig): return float(np.ptp(np.where(fig[..., 3] > 128)[0]))
def width(fig): return float(np.ptp(np.where(fig[..., 3] > 128)[1]))
def series(path, ref, name):
    f = figs(cv2.imread(path, cv2.IMREAD_UNCHANGED), (2, 4), 20000)
    s0 = STAND / height(f[ref])
    for k, fig in enumerate(f):
        s = s0
        if width(fig) * s > MAXLEN: s = MAXLEN / width(fig)
        r = resize_premult(fig, s)
        cell = np.zeros((CELL_H, CELL_W, 4), np.uint8); paste(cell, r, anchor_x(r, "com" if height(fig) * s < 250 else "low"), r.shape[0])
        cv2.imwrite(f"{out}/ruby_{name}_{k:02d}.png", cell)
        ys, xs = np.where(cell[..., 3] > 128); print(name, k, ys.max() - ys.min(), xs.max() - xs.min())
series(KAATUMINEN, 0, "kanni")
series(NOUSU, 7, "nousu")
# kavely
W, H = 448, 384
frames = list(range(63, 97))
ims = [cv2.imread(f"{raw}/{i:03d}.png", cv2.IMREAD_UNCHANGED) for i in frames]
hs = [np.ptp(np.where(im[..., 3] > 128)[0]) for im in ims]
sc = 322.0 / np.median(hs); floor = int(np.percentile([np.where(im[..., 3] > 128)[0].max() for im in ims], 90))
for k, im in enumerate(ims):
    ys, xs = np.where(im[..., 3] > 128); x0, x1, y0, y1 = xs.min(), xs.max() + 1, ys.min(), ys.max() + 1; crop = im[y0:y1, x0:x1]
    r = resize_premult(crop, sc); ax = anchor_x(crop, "torso") * sc; foot = (floor - y0) * sc
    cell = np.zeros((H, W, 4), np.uint8)
    xx = int(round(W / 2 - ax)); yy = int(round(H - 6 - foot))
    sx0, sy0 = max(0, -xx), max(0, -yy); dx0, dy0 = max(0, xx), max(0, yy)
    ww = min(r.shape[1] - sx0, W - dx0); hh = min(r.shape[0] - sy0, H - dy0)
    cell[dy0:dy0 + hh, dx0:dx0 + ww] = r[sy0:sy0 + hh, sx0:sx0 + ww]
    cv2.imwrite(f"{out}/ruby_humala_{k:02d}.png", cell)
print("humala", len(ims), "mittakaava", sc)
