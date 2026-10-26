"""Joulupukin idle: videon puhtaat vihreät ruudut -> alfa + despill -> ping-pong-sheet (Assets/Sprites/Joulupukki/pukki_idle.png, 8 saraketta).
Ajo: python make_santa_idle.py <ruutukansio f_%03d.png> """
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
os.makedirs(OUT, exist_ok=True)
src = sys.argv[1]
idx = list(range(88, 121, 2))
def key(im):
    h = cv2.cvtColor(im, cv2.COLOR_BGR2HSV).astype(np.float32)
    f = im.astype(np.float32)
    g = f[..., 1] - np.maximum(f[..., 0], f[..., 2])           # vihreän ylijäämä
    a = np.clip((70.0 - g) / 50.0, 0, 1)   # tausta g>=70 (gradientti 80-107), hahmo g<20
    a = cv2.GaussianBlur(a, (3, 3), 0)
    out = f.copy()
    out[..., 1] = np.minimum(out[..., 1], np.maximum(out[..., 0], out[..., 2]) + 4)   # despill
    return np.dstack([out, a * 255]).astype(np.uint8)
fr = [key(cv2.imread(os.path.join(src, "f_%03d.png" % i))) for i in idx]
al = np.max([f[..., 3] for f in fr], axis=0)
ys, xs = np.where(al > 40)
x0, x1, y0, y1 = xs.min() - 8, xs.max() + 8, ys.min() - 8, ys.max() + 8
seq = fr + fr[-2:0:-1]
scale = 0.5
cells = [cv2.resize(f[y0:y1, x0:x1], None, fx=scale, fy=scale, interpolation=cv2.INTER_AREA) for f in seq]
ch, cw = cells[0].shape[:2]
cols = 8; rows = (len(cells) + cols - 1) // cols
sheet = np.zeros((rows * ch, cols * cw, 4), np.uint8)
for k, c in enumerate(cells): sheet[(k // cols) * ch:(k // cols + 1) * ch, (k % cols) * cw:(k % cols + 1) * cw] = c
cv2.imwrite(os.path.join(OUT, "pukki_idle.png"), sheet)
bg = np.full((ch, cw * 4, 3), (40, 120, 40), np.uint8)
prev = np.hstack([c for c in cells[::max(len(cells) // 4, 1)][:4]])
m = prev[..., 3:4] / 255.0
cv2.imwrite(os.path.join(src, "preview.png"), (bg * (1 - m) + prev[..., :3] * m).astype(np.uint8))
print("kuvia", len(cells), "solu", cw, "x", ch, "sheet", sheet.shape)
