"""Luurankosheetit: hehkuva silmä joka ruutuun. Videon osassa ruuduista silmäkuoppa on pelkän musta; lisätään sinivihreä hehkupiste (sama kuin hehkuvissa ruuduissa)."""
import cv2, numpy as np, os, sys
SP = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "Assets", "Sprites", "Viholliset")
FR = {'idle': (640, 768), 'kavely': (640, 768), 'juoksu': (640, 768), 'jab': (768, 768), 'takasuora': (768, 768), 'potku': (1024, 768), 'hyppypotku': (1024, 1024)}

def head_info(cell):
    a = cell[..., 3]; ys, xs = np.where(a > 128)
    if len(ys) < 500: return None
    y0 = ys.min(); sel = xs[ys < y0 + 100]; hx = int(np.median(sel))
    return y0, hx

def glow_mask(bgr):
    b, g, r = bgr[..., 0].astype(int), bgr[..., 1].astype(int), bgr[..., 2].astype(int)
    return (g > 150) & (b > 110) & (r < 110) & (g - r > 70)

def find_socket(cell, y0, hx):
    bgr = cell[..., :3]; a = cell[..., 3]
    box = (slice(y0 + 60, y0 + 175), slice(max(0, hx - 70), min(cell.shape[1], hx + 100)))
    sub = bgr[box]; asub = a[box]
    dark = ((sub.max(2) < 70) & (asub > 200)).astype(np.uint8)
    n, lab, st, cen = cv2.connectedComponentsWithStats(dark, connectivity=8)
    best = None
    light = (sub.min(2) > 120) & (asub > 200)
    for i in range(1, n):
        x, y, w, h, ar = st[i]
        if ar < 90 or ar > 2200 or w < 8 or h < 8: continue
        if x <= 1 or y <= 1 or x + w >= dark.shape[1] - 1: continue
        m = (lab == i).astype(np.uint8)
        ring = (cv2.dilate(m, np.ones((9, 9), np.uint8)) > 0) & (m == 0)
        if ring.sum() < 20: continue
        frac = light[ring].mean()
        if frac < 0.45: continue          # silmäkuoppaa ympäröi vaalea luu
        score = (y, -x)
        if best is None or score < best[0]: best = (score, i, st[i])
    if best is None: return None
    _, i, (x, y, w, h, ar) = best
    return x + box[1].start, y + box[0].start, w, h

def paint(cell, sx, sy, sw, sh):
    # sama suhde kuin hehkuruuduissa: piste kuopan oikeassa alakeskellä
    cx = sx + 0.58 * sw; cy = sy + 0.58 * sh; rad = max(3.0, 0.19 * sw)
    yy, xx = np.mgrid[0:cell.shape[0], 0:cell.shape[1]]
    d = np.sqrt((xx - cx) ** 2 + (yy - cy) ** 2)
    core = np.clip(1 - (d - rad * 0.55) / (rad * 0.5), 0, 1)
    halo = np.clip(1 - (d - rad) / (rad * 0.9), 0, 1) * 0.35
    inside = (cell[..., 3] > 200)
    col_core = np.array([185, 235, 60], np.float32); col_edge = np.array([140, 210, 20], np.float32)   # BGR
    t = np.clip(d / rad, 0, 1)[..., None]
    col = col_core * (1 - t) + col_edge * t
    w = np.maximum(core, halo)[..., None] * inside[..., None]
    # halo vain tummalle (kuopan) päälle
    dark = (cell[..., :3].max(2) < 110)[..., None]
    w = np.where(dark, np.maximum(core[..., None] * inside[..., None], halo[..., None] * inside[..., None]), core[..., None] * inside[..., None])
    cell[..., :3] = (cell[..., :3] * (1 - w) + col * w).astype(np.uint8)

if __name__ == "__main__":
    for n, (fw, fh) in FR.items():
        f = os.path.join(SP, f"luuranko_{n}.png"); im = cv2.imread(f, cv2.IMREAD_UNCHANGED); H, W = im.shape[:2]
        done = have = miss = 0
        for r in range(H // fh):
            for c in range(W // fw):
                cell = im[r * fh:(r + 1) * fh, c * fw:(c + 1) * fw]
                hi = head_info(cell)
                if hi is None: continue
                y0, hx = hi
                reg = glow_mask(cell[y0 + 60:y0 + 175, max(0, hx - 70):hx + 100, :3])
                if reg.sum() > 25: have += 1; continue
                s = find_socket(cell, y0, hx)
                if s is None: miss += 1; continue
                paint(cell, *s); done += 1
        cv2.imwrite(f, im); print(n, "hehkuva jo", have, "lisätty", done, "ei löytynyt", miss)
