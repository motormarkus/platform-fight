"""Gustavin käyttäjän toimittamat kuvasarjat (3 hahmoa / kuva, läpinäkyvä tausta; kiertopotkun 3. kuva valkoinen tausta) -> yksittäiset RGBA-ruudut.
Käyttö: python split_sheets.py <ulos-kansio>"""
import os, sys, cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
from key_video import key_frame
UP = "C:/Users/marku/.claude/uploads/6d277d2e-0dfe-4331-9679-b4155f74f179/"
SHEETS = [("kiertopotku", ["f316864f-image.png", "e1651863-image.png"]),
          ("kirvespotku", ["db9fa5ba-image.png", "6ee90335-image.png"]),
          ("suojaus", ["cfcf7676-image.png"]),
          ("heitto", ["4f04c567-image.png", "83858158-image.png"]),
          ("heittaa", ["0d9d133f-image.png", "c0a13f18-image.png"]),
          ("hiekka_heitto", ["ba0cdb4e-image.png"], (4, 2)),
          ("hiekka_puuska", ["96df7678-image.png"], ("seq", 5)),
          ("hiekka_pilvi", ["b8ca6e0b-image.png"], (1, 1))]

def load_rgba(p):
    im = cv2.imread(UP + p, cv2.IMREAD_UNCHANGED)
    if im.shape[2] == 4: return im
    # valkoinen tausta: reunoista yhteydessä oleva lähes valkoinen alue pois (valkoiset siteet ääriviivan sisällä säilyvät)
    d = 255 - im.astype(np.int32).min(axis=2)
    near = (d < 14).astype(np.uint8)
    n, lab = cv2.connectedComponents(near, connectivity=4)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])).tolist()) - {0}
    bg = np.isin(lab, list(edge)).astype(np.uint8)
    bg = cv2.dilate(bg, np.ones((3, 3), np.uint8))      # reunan vaalea sävy mukaan
    alpha = cv2.GaussianBlur(1.0 - bg.astype(np.float32), (0, 0), 0.7)
    alpha = np.where(bg > 0, np.minimum(alpha, 0.0), alpha) if False else alpha
    return np.dstack([im, (alpha * 255).astype(np.uint8)])

def defringe(im, solid=230):
    """Puoliläpinäkyvien reunapikselien väri korvataan lähimpien peitteisten pikselien värillä (poistaa punaisen/keltaisen reunahohdon)."""
    a = im[..., 3]
    op = (a >= solid).astype(np.float32)
    col = im[..., :3].astype(np.float32) * op[..., None]
    out = im[..., :3].astype(np.float32).copy()
    todo = a < solid
    for sig in (1.5, 3, 6, 12):
        num = cv2.GaussianBlur(col, (0, 0), sig); den = cv2.GaussianBlur(op, (0, 0), sig)
        ok = todo & (den > 0.02)
        out[ok] = num[ok] / den[ok][:, None]
        todo = todo & ~ok
    return np.dstack([out.clip(0, 255).astype(np.uint8), a])

def split(im, grid=(3, 1)):
    """Ruudukko (sarakkeet, rivit): osat jaetaan ruutuihin painopisteen mukaan (raajat voivat ylittää ruudun rajan)."""
    a = (im[..., 3] > 20)
    if grid[0] == "seq":      # N erillistä kuvaa vasemmalta oikealle (hiekkapuuskat): hajonneet hiukkaset liitetään isoon osaan
        n, lab, st, cen = cv2.connectedComponentsWithStats(cv2.dilate(a.astype(np.uint8), np.ones((41, 41), np.uint8)), connectivity=8)
        ids = sorted(range(1, n), key=lambda i: -st[i, cv2.CC_STAT_AREA])[:grid[1]]
        out = []
        for i in sorted(ids, key=lambda i: st[i, cv2.CC_STAT_LEFT]):
            m = (lab == i) & a; ys, xs = np.where(m)
            crop = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
            crop[~m[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 3] = 0
            out.append(defringe(crop))
        return out
    cols, rows = grid
    n, lab, st, cen = cv2.connectedComponentsWithStats(cv2.dilate(a.astype(np.uint8), np.ones((5, 5), np.uint8)), connectivity=8)
    H, W = a.shape
    cells = [np.zeros(a.shape, bool) for _ in range(cols * rows)]
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] < 3000: continue
        c = min(cols - 1, int(cen[i][0] / (W / cols))); r = min(rows - 1, int(cen[i][1] / (H / rows)))
        cells[r * cols + c] |= (lab == i) & a
    out = []
    for m in cells:
        ys, xs = np.where(m)
        crop = im[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy()
        crop[~m[ys.min():ys.max() + 1, xs.min():xs.max() + 1], 3] = 0
        out.append(defringe(crop))
    return out

if __name__ == "__main__":
    o = sys.argv[1]
    for name, files, *g in SHEETS:
        k = 0
        for f in files:
            for c in split(load_rgba(f), g[0] if g else (3, 1)):
                os.makedirs(f"{o}/{name}", exist_ok=True)
                cv2.imwrite(f"{o}/{name}/{k}.png", c); print(name, k, c.shape[1], c.shape[0]); k += 1
