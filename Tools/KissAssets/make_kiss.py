"""Rocco + amatsoni suutelevat (vihreätaustainen video): kuvat Assets/Resources/Suukko/suukko_NN.png.
Ruudut FIRST..LAST joka STEP:s (24 fps), kaikki samaan rajaukseen (jalat alareunassa, parin keskikohta keskellä), skaala niin, että Rocco on heron kokoinen (~333 px).
Ajo (projektin juuresta): python Tools/KissAssets/make_kiss.py"""
import sys, os
sys.path.insert(0, os.path.dirname(__file__))
import numpy as np, cv2
import kiss_key as K
FIRST, LAST, STEP = 30, 270, 3
SCALE = 333.0 / 571.0   # Rocco videossa ~571 px, heron kuva ~333 px
OUT = 'Assets/Resources/Suukko/'
fr = K.frames()
idx = list(range(FIRST, LAST + 1, STEP))
bbs = []
for i in idx:
    ys, xs = np.nonzero(fr[i][..., 3] > 100); bbs.append((xs.min(), ys.min(), xs.max(), ys.max()))
x0 = min(b[0] for b in bbs); y0 = min(b[1] for b in bbs); x1 = max(b[2] for b in bbs); y1 = max(b[3] for b in bbs)
cx = (x0 + x1) // 2; W = (int((x1 - x0 + 24) * SCALE) + 2 + 3) // 4 * 4; H = (int((y1 - y0 + 12) * SCALE) + 2 + 3) // 4 * 4   # 4:n monikerta: pakkaus onnistuu
os.makedirs(OUT, exist_ok=True)
for f in os.listdir(OUT):
    if f.startswith('suukko_') and f.endswith('.png'): os.remove(OUT + f)
for n, i in enumerate(idx):
    f = fr[i]
    a = f[..., 3:4].astype(np.float32) / 255.0; pm = np.dstack([f[..., :3].astype(np.float32) * a, f[..., 3:4].astype(np.float32)])
    pm = cv2.resize(pm, None, fx=SCALE, fy=SCALE, interpolation=cv2.INTER_AREA)
    # rajaus: leveys keskitetty parin keskelle, alareuna = jalat (y1)
    sw = int(round((x1 - x0 + 24) * SCALE)); sx0 = int(round((cx - (x1 - x0 + 24) / 2) * SCALE)); sy1 = int(round((y1 + 4) * SCALE))
    cell = np.zeros((H, W, 4), np.float32)
    sy0 = sy1 - H
    for yy in range(H):
        sy = sy0 + yy
        if sy < 0 or sy >= pm.shape[0]: continue
        xa = max(0, sx0); xb = min(pm.shape[1], sx0 + W)
        cell[yy, xa - sx0:xb - sx0] = pm[sy, xa:xb]
    al = cell[..., 3:4]; rgb = np.where(al > 1, cell[..., :3] / np.maximum(al / 255.0, 1e-3), 0)
    out = np.dstack([rgb, al]).clip(0, 255).astype(np.uint8)
    cv2.imwrite(OUT + 'suukko_%02d.png' % n, out)
print(len(idx), 'kuvaa', W, 'x', H)
