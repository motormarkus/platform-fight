"""Joulupukin iskut (video 1280x720, 24 fps) ja suojaus (kuvasarja) -> Assets/Sprites/Joulupukki/pukki_isku1..6.png, pukki_suoja.png.
Kaikilla videoiskuilla sama kiinteä rajaus -> jalat pysyvät samassa kohdassa; mittakaava vastaa idleä/kävelyä (0.75).
Ajo: python make_santa_attacks.py <ruutukansio f_%03d.png> <suoja-kuva>"""
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
src, guard = sys.argv[1], sys.argv[2]
SC = 0.75
MOVES = {
    "isku1": [22, 24, 26, 28, 30, 32, 36, 42, 48, 50, 52, 54],            # suora oikea (pitkä pito lyhennetty)
    "isku2": list(range(54, 72, 2)),                                        # kiertoisku
    "isku3": list(range(84, 102, 2)),                                       # kiertoisku 2
    "isku4": list(range(100, 124, 2)),                                      # koukku
    "isku5": list(range(124, 140, 2)),                                      # suora 2
    "isku6": list(range(138, 154, 2)),                                      # kiertoisku 3
}
def key(im):
    f = im.astype(np.float32); g = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = cv2.GaussianBlur(np.clip((70.0 - g) / 50.0, 0, 1), (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4)
    return np.dstack([f, a * 255]).clip(0, 255).astype(np.uint8)
def save(name, cells, cols=8):
    ch, cw = cells[0].shape[:2]; rows = (len(cells) + cols - 1) // cols
    s = np.zeros((rows * ch, min(cols, len(cells)) * cw, 4), np.uint8)
    for k, c in enumerate(cells): s[(k // cols) * ch:(k // cols + 1) * ch, (k % cols) * cw:(k % cols + 1) * cw] = c
    cv2.imwrite(os.path.join(OUT, name), s); print(name, len(cells), "ruutua, solu", cw, "x", ch)
X0, X1, Y0, Y1 = 320, 1040, 56, 712
prev = []
for name, idx in MOVES.items():
    cells = [cv2.resize(key(cv2.imread(os.path.join(src, "f_%03d.png" % i)))[Y0:Y1, X0:X1], None, fx=SC, fy=SC, interpolation=cv2.INTER_AREA) for i in idx]
    save("pukki_" + name + ".png", cells); prev.append(cells[len(cells) // 2])
# suojaus: kuvan 4 asentoa (erotetaan alfa-sarakkeista)
im = key(cv2.imread(guard)); a = im[..., 3] > 60
cols = a.any(0); runs = []; st = None
for x, v in enumerate(cols):
    if v and st is None: st = x
    if not v and st is not None:
        if x - st > 40: runs.append([st, x])
        st = None
if st is not None: runs.append([st, len(cols)])
while len(runs) > 4:                                    # yhdistä lähimmät kaksi
    gaps = [runs[i + 1][0] - runs[i][1] for i in range(len(runs) - 1)]; k = int(np.argmin(gaps)); runs[k][1] = runs[k + 1][1]; del runs[k + 1]
print("suoja-asennot x:", runs)
ys, xs = np.where(a); gy0, gy1 = ys.min() - 8, ys.max() + 8
mid = [(r[0] + r[1]) // 2 for r in runs]; half = 250
# asentojen rajat: kuvan tyhjät sarakkeet asentojen välissä (ei vuotoa viereisistä asennoista)
cnt = a.sum(0); edges = [0]
for r0, r1 in zip(runs[:-1], runs[1:]):
    lo, hi = r0[1] - 20, r1[0] + 20
    edges.append(lo + int(np.argmin(cnt[lo:hi])))
edges.append(im.shape[1])
cells = []
for k, m in enumerate(mid):
    c = np.zeros((gy1 - gy0, 2 * half, 4), np.uint8)
    xa, xb = max(m - half, edges[k]), min(m + half, edges[k + 1])   # vain tämän asennon sarakkeet
    c[:, xa - (m - half):xb - (m - half)] = im[gy0:gy1, xa:xb]
    cells.append(cv2.resize(c, None, fx=0.79, fy=0.79, interpolation=cv2.INTER_AREA))
save("pukki_suoja.png", cells, 4); prev.append(cells[1])
h = max(c.shape[0] for c in prev); row = np.hstack([cv2.copyMakeBorder(c, h - c.shape[0], 0, 0, 0, cv2.BORDER_CONSTANT, value=(0, 0, 0, 0)) for c in prev]); m = row[..., 3:4] / 255.0
cv2.imwrite(os.path.join(src, "preview.png"), (np.full(row.shape[:2] + (3,), (40, 120, 40), np.uint8) * (1 - m) + row[..., :3] * m).astype(np.uint8))
