"""Joulumuorin kävely: videosta 40 ruudun askelsilmukka (kaksi askelta), lopun 8 ruutua ristiinhäivytetään alkua edeltäviin ruutuihin -> saumaton.
Tulos: Assets/Sprites/Joulupukki/muori_kavely.png (10 saraketta, joka toinen ruutu = 20 ruutua).
Ajo: python make_santa_walk.py <ruutukansio f_%03d.png> [alku=58] [jakso=40]"""
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
src = sys.argv[1]; start = int(sys.argv[2]) if len(sys.argv) > 2 else 58; per = int(sys.argv[3]) if len(sys.argv) > 3 else 40
FADE = 8
def key(im):
    f = im.astype(np.float32); g = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = cv2.GaussianBlur(np.clip((70.0 - g) / 50.0, 0, 1), (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4)
    return np.dstack([f * a[..., None], a])          # esikerrottu alfa: sekoitus oikein
def load(i): return key(cv2.imread(os.path.join(src, "f_%03d.png" % i)))
seq = []
for k in range(per):
    f = load(start + k)
    if k >= per - FADE:
        w = (k - (per - FADE) + 1) / (FADE + 1)       # 0..1: kohti ruutua start-FADE+..., joka jatkaa silmukan alkuun
        f = f * (1 - w) + load(start - FADE + (k - (per - FADE))) * w
    seq.append(f)
seq = seq[::2]
al = np.max([f[..., 3] for f in seq], axis=0); ys, xs = np.where(al > 0.15)
x0, x1, y0, y1 = xs.min() - 8, xs.max() + 8, ys.min() - 8, ys.max() + 8
cells = []
for f in seq:
    c = f[y0:y1, x0:x1]; a = c[..., 3:4]
    rgb = np.where(a > 1e-3, c[..., :3] / np.maximum(a, 1e-3), 0)
    c = np.dstack([rgb, a * 255]).clip(0, 255).astype(np.uint8)
    cells.append(cv2.resize(c, None, fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA))
ch, cw = cells[0].shape[:2]; cols = 10; rows = (len(cells) + cols - 1) // cols
sheet = np.zeros((rows * ch, cols * cw, 4), np.uint8)
for k, c in enumerate(cells): sheet[(k // cols) * ch:(k // cols + 1) * ch, (k % cols) * cw:(k % cols + 1) * cw] = c
cv2.imwrite(os.path.join(OUT, "muori_kavely.png"), sheet)
pv = np.hstack(cells[::5][:4]); m = pv[..., 3:4] / 255.0
cv2.imwrite(os.path.join(src, "preview.png"), (np.full(pv.shape[:2] + (3,), (40, 120, 40), np.uint8) * (1 - m) + pv[..., :3] * m).astype(np.uint8))
# silmukan sauma: viimeinen ruutu vs ensimmäinen
print("ruutuja", len(cells), "solu", cw, "x", ch, "sauman ero", round(float(np.abs(cells[-1].astype(int) - cells[0].astype(int)).mean()), 2))
print("laatikko x", x0, x1, "y", y0, y1)
