"""Joulupukin alakoukku: kaksi kuvasarjaa (7 + 3 asentoa vihreällä) -> Assets/Sprites/Joulupukki/pukki_alakoukku.png (10 ruutua, järjestys vasen->oikea, ylhäältä alas, kuva 1 ja sitten kuva 2).
Mittakaava sovitettu idlen kokoon; jalat samalle maalinjalle, vaakasuunnassa laatikon keskelle.
Ajo: python make_santa_uppercut.py <kuva1> <kuva2>"""
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
def key(im):
    f = im.astype(np.float32); g = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = cv2.GaussianBlur(np.clip((70.0 - g) / 50.0, 0, 1), (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4)
    return np.dstack([f, a * 255]).clip(0, 255).astype(np.uint8)
def runs(mask, gap=12, minw=60):
    on = mask.any(1 if mask.ndim == 2 and False else 0) if False else None
def spans(v, gap=10, minlen=60):
    out = []; st = None; last = None
    for i, x in enumerate(v):
        if x:
            if st is None: st = i
            last = i
        elif st is not None and i - last > gap:
            if last - st > minlen: out.append((st, last + 1))
            st = None
    if st is not None and last - st > minlen: out.append((st, last + 1))
    return out
def poses(path, scale, rows):
    """rows: [(y0, y1, [odotetut rajat x])]; rajat hakeutuvat lähimpään alfa-laaksoon."""
    im = key(cv2.imread(path)); a = im[..., 3] > 60; res = []
    for r0, r1, bounds in rows:
        cnt = a[r0:r1].sum(0); edges = [0]
        for b in bounds:
            lo, hi = max(b - 70, 1), min(b + 70, a.shape[1] - 1); edges.append(lo + int(np.argmin(cnt[lo:hi])))
        edges.append(a.shape[1])
        for c0, c1 in zip(edges[:-1], edges[1:]):
            sub = a[r0:r1, c0:c1]; ys, xs = np.where(sub)
            res.append((im[r0 + ys.min():r0 + ys.max() + 1, c0 + xs.min():c0 + xs.max() + 1], scale))
    return res
P = poses(sys.argv[1], 1.19, [(0, 485, [430, 810, 1240]), (485, 941, [560, 1110])]) + poses(sys.argv[2], 0.95, [(0, 941, [590, 1120])])
print("asentoja", len(P))
CW, CH = 640, 700; cells = []
for im, sc in P:
    c = cv2.resize(im, None, fx=sc, fy=sc, interpolation=cv2.INTER_AREA); h, w = c.shape[:2]
    cell = np.zeros((CH, CW, 4), np.uint8); x0 = (CW - w) // 2; y0 = CH - 10 - h
    assert x0 >= 0 and y0 >= 0, (w, h)
    cell[y0:y0 + h, x0:x0 + w] = c; cells.append(cell)
cols = 5; rows = (len(cells) + cols - 1) // cols
sheet = np.zeros((rows * CH, cols * CW, 4), np.uint8)
for k, c in enumerate(cells): sheet[(k // cols) * CH:(k // cols + 1) * CH, (k % cols) * CW:(k % cols + 1) * CW] = c
cv2.imwrite(os.path.join(OUT, "pukki_alakoukku.png"), sheet)
pv = np.vstack([np.hstack(cells[r * 5:r * 5 + 5]) for r in range(rows)]); m = pv[..., 3:4] / 255.0
cv2.imwrite("output/snow-preview/pukki_alakoukku_esikatselu.png", cv2.resize((np.full(pv.shape[:2] + (3,), (40, 120, 40), np.uint8) * (1 - m) + pv[..., :3] * m).astype(np.uint8), None, fx=0.45, fy=0.45))
print("solu", CW, "x", CH, "ruutuja", len(cells))
