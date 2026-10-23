"""Tiskivideo: poistaa harmaan tiskin (paneeli, reunaviivat, kansilevy) alfasta."""
import numpy as np, cv2
def clean(a):
    rgb = a[..., :3]; al = a[..., 3].astype(np.float32) / 255
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV)
    h, s, v = hsv[..., 0].astype(int), hsv[..., 1] / 255.0, hsv[..., 2].astype(int)
    # harmaa paneeli: matala kylläisyys, keskikirkas
    panel = (s < 0.22) & (v > 38) & (v < 175) & (al > 0.5)
    panel = cv2.morphologyEx(panel.astype(np.uint8), cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(panel, connectivity=8)
    big = np.zeros_like(panel)
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] > 1500: big[lab == i] = 1
    if big.sum() == 0: return a, None
    ys, xs = np.where(big > 0)
    top, bot, x0, x1 = ys.min(), ys.max(), xs.min(), xs.max()
    out = a.copy()
    # paneeli + sen reunat (laajennus) pois
    rem = cv2.dilate(big, np.ones((5, 5), np.uint8)).astype(bool)
    # kansilevy: kapea kaista paneelin yläpuolella, tiskin värinen
    band = np.zeros_like(rem); band[max(0, top - 36):top + 3, x0:x1 + 1] = True
    ref = np.median(rgb[max(0, top - 22):top - 10, x0 + 3:x0 + 40].reshape(-1, 3), axis=0) if top > 20 else None
    if ref is not None:
        d = np.linalg.norm(rgb.astype(np.float32) - ref, axis=2)
        rem |= band & (d < 38)
    # alareunan musta viiva
    rem |= (np.zeros_like(rem) | (np.arange(a.shape[0])[:, None] > bot - 3) & (np.arange(a.shape[0])[:, None] < bot + 8) & (np.arange(a.shape[1])[None] >= x0 - 3) & (np.arange(a.shape[1])[None] <= x1 + 3) & (v < 70))
    out[..., 3][rem & (al > 0)] = 0
    return out, (top, bot, x0, x1)
