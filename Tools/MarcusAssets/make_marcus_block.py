"""Marcuksen suojaus (marcus_suoja.png): käyttäjän toimittama kuva suoja_src.png (3 x 2 ruutua, 512 x 512, läpinäkyvä tausta).
Mittakaava: ruudun 0 (vartioasento) korkeus -> 300 px kuten muut Marcus-kuvat; jalat ruudun alareunasta 8 px ylös, x = jalkojen keskikohta."""
import cv2
import numpy as np
CELL_W, CELL_H, COLS = 768, 448, 4
src = cv2.imread("Tools/MarcusAssets/suoja_src.png", cv2.IMREAD_UNCHANGED)
H, W = src.shape[:2]
cw, ch = W // 3, H // 2
tiles = [src[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw].copy() for r in range(2) for c in range(3)]

def only_figure(t):
    """Ruudun reunalle vuotanut naapurikuvan osa (esim. yläruudun saappaat) pois: vain suurin osa ja sen lähellä (15 px) olevat."""
    a = (t[..., 3] > 20).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(a, connectivity=8)
    k = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
    near = cv2.dilate((lab == k).astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (31, 31))) > 0
    keep = np.zeros(a.shape, bool)
    for i in range(1, n):
        if i == k or (st[i, cv2.CC_STAT_AREA] > 150 and near[lab == i].mean() > 0.5): keep |= lab == i
    t[~keep, 3] = 0
    return t

tiles = [only_figure(t) for t in tiles]

def bbox(t):
    ys, xs = np.where(t[..., 3] > 40)
    return xs.min(), xs.max() + 1, ys.min(), ys.max() + 1

def premult_resize(rgba, w, h):
    f = rgba.astype(np.float32); a = f[..., 3:4] / 255.0; f[..., :3] *= a
    f = cv2.resize(f, (w, h), interpolation=cv2.INTER_AREA)
    a2 = f[..., 3:4] / 255.0; f[..., :3] = np.where(a2 > 1e-3, f[..., :3] / np.maximum(a2, 1e-3), 0)
    return f.clip(0, 255).astype(np.uint8)

x0, x1, y0, y1 = bbox(tiles[0])
scale = 300.0 / (y1 - y0)
sheet = np.zeros((2 * CELL_H, COLS * CELL_W, 4), np.uint8)
for n, t in enumerate(tiles):
    x0, x1, y0, y1 = bbox(t)
    crop = t[y0:y1, x0:x1]
    a = crop[..., 3] > 128; h = a.shape[0]
    ys, xs = np.where(a[int(h * 0.65):]); cx = xs.mean() if len(xs) else crop.shape[1] / 2
    sm = premult_resize(crop, max(1, round(crop.shape[1] * scale)), max(1, round(crop.shape[0] * scale)))
    left = int(round(CELL_W / 2 - cx * scale)); top = CELL_H - 8 - sm.shape[0]
    cell = np.zeros((CELL_H, CELL_W, 4), np.uint8)
    dx0, dy0 = max(0, left), max(0, top); sx0, sy0 = max(0, -left), max(0, -top)
    w = min(sm.shape[1] - sx0, CELL_W - dx0); hh = min(sm.shape[0] - sy0, CELL_H - dy0)
    cell[dy0:dy0 + hh, dx0:dx0 + w] = sm[sy0:sy0 + hh, sx0:sx0 + w]
    r, c = divmod(n, COLS)
    sheet[r * CELL_H:(r + 1) * CELL_H, c * CELL_W:(c + 1) * CELL_W] = cell
cv2.imwrite("Assets/Sprites/Viholliset/marcus_suoja.png", sheet)
print("ok", scale, sheet.shape)
