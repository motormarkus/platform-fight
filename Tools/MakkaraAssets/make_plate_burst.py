"""Makkara-annoksen hajoaminen: hajoaa_1/2 (annos halkeaa ja sirpaleet lentävät) ja sirpale_0-5 (lautasen palat reunasta) annoskuvasta (annos.png).
Käyttö: python make_plate_burst.py <Resources/Makkara>"""
import sys, os, cv2, numpy as np
d = sys.argv[1]; rng = np.random.RandomState(3)
a = cv2.imread(os.path.join(d, "annos.png"), cv2.IMREAD_UNCHANGED); h, w = a.shape[:2]
alpha = a[..., 3] > 10
pts = np.argwhere(alpha)
def voronoi(n, mask):
    p = np.argwhere(mask); seeds = p[rng.choice(len(p), n, replace=False)]
    idx = np.indices(mask.shape).transpose(1, 2, 0)
    return ((idx[:, :, None, :] - seeds[None, None]) ** 2).sum(-1).argmin(-1), seeds
def paste(dst, spr, x, y):
    H, W = dst.shape[:2]; sh, sw = spr.shape[:2]
    xa, ya = max(x, 0), max(y, 0); xb, yb = min(x + sw, W), min(y + sh, H)
    if xb <= xa or yb <= ya: return
    s = spr[ya - y:yb - y, xa - x:xb - x].astype(np.float32); dd = dst[ya:yb, xa:xb].astype(np.float32)
    al = s[..., 3:4] / 255.; da = dd[..., 3:4] / 255.; oa = al + da * (1 - al)
    rgb = (s[..., :3] * al + dd[..., :3] * da * (1 - al)) / np.maximum(oa, 1e-6)
    dst[ya:yb, xa:xb] = np.concatenate([rgb, oa * 255], -1).astype(np.uint8)
# burst-kuvat: annos paloina, paloja liikutetaan keskipisteestä ulos ja ylös
CW, CH = int(w * 1.5), int(h * 1.9)
def burst(n, spread, rise, rot):
    lab, seeds = voronoi(n, alpha); c = np.zeros((CH, CW, 4), np.uint8); ox, oy = (CW - w) // 2, (CH - h) // 2 + int(h * 0.1)
    for k in range(n):
        m = (lab == k) & alpha
        if m.sum() < 20: continue
        s = a.copy(); s[..., 3] = np.where(m, a[..., 3], 0)
        dx = seeds[k][1] - w / 2; dy = seeds[k][0] - h / 2
        M = cv2.getRotationMatrix2D((float(seeds[k][1]), float(seeds[k][0])), rng.uniform(-rot, rot), 1.0)
        s = cv2.warpAffine(s, M, (w, h), flags=cv2.INTER_AREA)
        paste(c, s, int(ox + dx * spread), int(oy + dy * spread * 0.6 - rise))
    return c
cv2.imwrite(os.path.join(d, "hajoaa_1.png"), burst(4, 0.25, int(h * 0.08), 8))
cv2.imwrite(os.path.join(d, "hajoaa_2.png"), burst(9, 0.6, int(h * 0.2), 30))
# lautasen palat: epäsäännölliset kermanväriset kolmiot ja nelikulmiot ääriviivalla (lautasen väri kuvasta)
col = tuple(int(v) for v in np.median(a[cream_pts[:, 0], cream_pts[:, 1], :3], 0)) if False else (176, 214, 238)
for k in range(6):
    sz = rng.randint(13, 25); n = 3 + k % 2
    ang = np.sort(rng.uniform(0, 2 * np.pi, n)); rad = rng.uniform(0.5, 1.0, n) * sz
    pts = np.stack([sz + 2 + rad * np.cos(ang), sz // 2 + 2 + rad * np.sin(ang) * 0.6], 1).astype(np.int32)
    out = np.zeros((sz + 4, 2 * sz + 4, 4), np.uint8); mm = np.zeros(out.shape[:2], np.uint8)
    cv2.fillPoly(mm, [pts], 255)
    edge = cv2.dilate(mm, np.ones((3, 3), np.uint8), iterations=1) > 0
    out[edge] = (20, 30, 60, 255); out[mm > 0] = (*col, 255)
    ys, xs = np.where(out[..., 3] > 0); out = out[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    cv2.imwrite(os.path.join(d, "sirpale_%d.png" % k), out)
