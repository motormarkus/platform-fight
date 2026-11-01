"""Etsii kuvan B sijainnin kuvan A päältä (limitys) ja yhdistää ne ristiinhäivytyksellä.
Käyttö: python overlap.py A B ulos [strip=260]"""
import sys, numpy as np, cv2
a = cv2.imread(sys.argv[1]); b = cv2.imread(sys.argv[2]); ow = sys.argv[3]
h, w = a.shape[:2]; S = int(sys.argv[4]) if len(sys.argv) > 4 else 260
best = (-1, 0, 0)
g = lambda i: cv2.cvtColor(i, cv2.COLOR_BGR2GRAY)
for x0 in (0, 200, 400):      # B:n eri kaistat etsintämallina
    tpl = g(b)[100:h - 100, x0:x0 + S]
    r = cv2.matchTemplate(g(a), tpl, cv2.TM_CCOEFF_NORMED)
    _, mv, _, ml = cv2.minMaxLoc(r)
    print("kaista", x0, "osuma", round(mv, 3), "A:n x", ml[0], "y", ml[1], "-> B alkaa", ml[0] - x0, "dy", ml[1] - 100)
    if mv > best[0]: best = (mv, ml[0] - x0, ml[1] - 100)
_, off, dy = best; print("valittu offset", off, "dy", dy)
M = np.float32([[1, 0, 0], [0, 1, -dy]]); b2 = cv2.warpAffine(b, M, (w, h), borderMode=cv2.BORDER_REPLICATE)
W = off + w; out = np.zeros((h, W, 3), np.float32)
out[:, :off] = a[:, :off]; out[:, w:] = b2[:, w - off:]
ov = w - off                                    # limityksen leveys
t = np.linspace(0, 1, ov, dtype=np.float32); t = t * t * (3 - 2 * t)
out[:, off:w] = a[:, off:w] * (1 - t)[None, :, None] + b2[:, :ov] * t[None, :, None]
cv2.imwrite(ow, np.clip(out, 0, 255).astype(np.uint8)); print("leveys", W)
