"""Joulupukin mahatörmäys: videon nopea törmäysosa hidastetaan optisella virtauksella (ruutujen väliin lasketaan uudet ruudut), pidot lyhennetään.
Aikajana (ulostulo s -> lähderuutu) on KNOTS-taulukossa; ulostulo 20 ruutua/s -> aseta Unityssä frameTime 0.05.
Ajo: python make_santa_bump.py <ruutukansio f_%03d.png>"""
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
src = sys.argv[1]; FPS = 20
KNOTS = [(0.0, 26), (0.5, 44), (0.65, 56), (1.75, 72), (2.05, 84), (2.2, 94), (2.9, 118)]   # törmäys 56->72 kulkee 2x hitaammin
X0, X1, Y0, Y1 = 560, 1460, 70, 1076
_c = {}
def frame(i):
    if i not in _c: _c[i] = cv2.imread(os.path.join(src, "f_%03d.png" % i))[Y0:Y1, X0:X1]
    return _c[i]
_f = {}
def flow(i):
    if i not in _f: _f[i] = cv2.calcOpticalFlowFarneback(cv2.cvtColor(frame(i), cv2.COLOR_BGR2GRAY), cv2.cvtColor(frame(i + 1), cv2.COLOR_BGR2GRAY), None, 0.5, 4, 25, 5, 7, 1.5, 0)
    return _f[i]
def at(s):
    i = int(np.floor(s)); t = s - i
    if t < 1e-3: return frame(i)
    fl = flow(i); h, w = fl.shape[:2]; gx, gy = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
    a = cv2.remap(frame(i), gx - t * fl[..., 0], gy - t * fl[..., 1], cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
    b = cv2.remap(frame(i + 1), gx + (1 - t) * fl[..., 0], gy + (1 - t) * fl[..., 1], cv2.INTER_LINEAR, borderMode=cv2.BORDER_REPLICATE)
    return a if t < 0.5 else b          # vain lähempi ruutu vääntyy (ei haamukuvia)
def key(im):
    f = im.astype(np.float32); g = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = cv2.GaussianBlur(np.clip((70.0 - g) / 50.0, 0, 1), (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4)
    return np.dstack([f, a * 255]).clip(0, 255).astype(np.uint8)
T = KNOTS[-1][0]; n = int(round(T * FPS)) + 1
cells = []
for k in range(n):
    s = np.interp(k / FPS, [a for a, _ in KNOTS], [b for _, b in KNOTS])
    cells.append(cv2.resize(key(at(float(s))), None, fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA))
ch, cw = cells[0].shape[:2]; cols = 10; rows = (n + cols - 1) // cols
sheet = np.zeros((rows * ch, cols * cw, 4), np.uint8)
for k, c in enumerate(cells): sheet[(k // cols) * ch:(k // cols + 1) * ch, (k % cols) * cw:(k % cols + 1) * cw] = c
cv2.imwrite(os.path.join(OUT, "pukki_mahaisku.png"), sheet)
pv = np.hstack([cells[i] for i in (0, 14, 22, 28, 34, 40)]); m = pv[..., 3:4] / 255.0
cv2.imwrite(os.path.join(src, "preview.png"), (np.full(pv.shape[:2] + (3,), (40, 120, 40), np.uint8) * (1 - m) + pv[..., :3] * m).astype(np.uint8))
print("ruutuja", n, "solu", cw, "x", ch, "kesto", T, "s @", FPS, "fps")
