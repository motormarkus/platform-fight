"""Tuhoajan ammukset: viisi arkkia (violetti tausta, 5 kuvaa vaakajonossa) -> e2ball, e2b, e2s, e2arc (_0.._4.png).
Käyttö: slice_boss2_shots.py <kansio jossa 7.webp..10.webp> <ulos>   (7 = plasmapallo, 8 = nuolet, 9 = tulipallot, 10 = kaari)"""
import sys, numpy as np, cv2
src, out = sys.argv[1], sys.argv[2]
MAP = {"7": "e2ball", "8": "e2b", "9": "e2s", "10": "e2arc"}

def key(im):
    rgb = cv2.cvtColor(im, cv2.COLOR_BGR2RGB)
    hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV).astype(np.float32)
    h, s, v = hsv[..., 0], hsv[..., 1] / 255, hsv[..., 2]
    bgm = ((h > 135) & (h < 162)).astype(np.float32) * np.clip((s - 0.45) / 0.25, 0, 1) * np.clip((v - 50) / 40, 0, 1)
    a = 1 - bgm
    a = cv2.GaussianBlur(a, (0, 0), 0.7); a = np.clip((a - 0.1) / 0.8, 0, 1)
    f = rgb.astype(np.float32)
    edge = (a > 0.02) & (a < 0.98)
    aa = np.maximum(a[edge], 0.25)[:, None]
    f[edge] = np.clip((f[edge] - (1 - aa) * np.array([123, 0, 125], np.float32)) / aa, 0, 255)
    return np.dstack([f, a * 255]).astype(np.uint8)

for k, name in MAP.items():
    im = cv2.imread(f"{src}/{k}.webp")
    rgba = key(im)
    cols = (rgba[..., 3] > 128).sum(axis=0)
    spans, st = [], None
    for x in range(len(cols)):
        if cols[x] > 1 and st is None: st = x
        if cols[x] <= 1 and st is not None:
            if x - st > 30: spans.append((st, x))
            st = None
    if st is not None: spans.append((st, len(cols)))
    print(name, len(spans), spans)
    assert len(spans) == 5, name
    boxes = []
    for x0, x1 in spans:
        ys = np.where((rgba[:, x0:x1, 3] > 40).any(axis=1))[0]
        boxes.append((x0, x1, ys.min(), ys.max() + 1))
    W = max(b[1] - b[0] for b in boxes) + 8; H = max(b[3] - b[2] for b in boxes) + 8
    for i, (x0, x1, y0, y1) in enumerate(boxes):
        can = np.zeros((H, W, 4), np.uint8)
        c = rgba[y0:y1, x0:x1]
        ox, oy = (W - c.shape[1]) // 2, (H - c.shape[0]) // 2
        can[oy:oy + c.shape[0], ox:ox + c.shape[1]] = c
        cv2.imwrite(f"{out}/{name}_{i}.png", cv2.cvtColor(can, cv2.COLOR_RGBA2BGRA))
    print(name, W, H)
