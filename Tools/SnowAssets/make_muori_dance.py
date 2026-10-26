"""Joulumuorin tanssi: läpinäkyvät PNG-ruudut (Codexin taustanpoisto) -> sheet. Joka 3. ruutu (10 kuvaa/s), peilataan (alkuperäinen katsoo vasemmalle).
Ajo: python make_muori_dance.py <ruutukansio muori_%04d.png> -> Assets/Sprites/Joulupukki/muori_tanssi.png (10 saraketta)"""
import os, sys, glob, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
fs = sorted(glob.glob(os.path.join(sys.argv[1], "*.png")))[::3]
ims = [cv2.imread(f, -1) for f in fs]
al = np.max([i[..., 3] for i in ims], axis=0); ys, xs = np.where(al > 20)
x0, x1, y0, y1 = xs.min() - 6, xs.max() + 6, ys.min() - 6, ys.max() + 6
cells = []
for i in ims:
    c = i[y0:y1, x0:x1].copy(); c[c[..., 3] < 4] = 0
    c = cv2.flip(c, 1)
    cells.append(cv2.resize(c, None, fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA))
ch, cw = cells[0].shape[:2]; cols = 10; rows = (len(cells) + cols - 1) // cols
sheet = np.zeros((rows * ch, cols * cw, 4), np.uint8)
for k, c in enumerate(cells): sheet[(k // cols) * ch:(k // cols + 1) * ch, (k % cols) * cw:(k % cols + 1) * cw] = c
cv2.imwrite(os.path.join(OUT, "muori_tanssi.png"), sheet)
d = [float(np.abs(cells[k].astype(int) - cells[(k + 1) % len(cells)].astype(int)).mean()) for k in range(len(cells))]
print("ruutuja", len(cells), "solu", cw, "x", ch, "sauma", round(d[-1], 2), "keskiero", round(float(np.mean(d[:-1])), 2))
