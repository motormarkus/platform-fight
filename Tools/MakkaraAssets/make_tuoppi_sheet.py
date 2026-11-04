"""Kaljatuoppi (Resources/Rekvisiitta pullo_-sheet 128x96 -> 7 kuvaa): kuva 0 ehjä, 1 säröt, 2-5 sirpaleet lentävät, 6 sirpaleet + olutlätty lattialla.
Käyttö: python make_tuoppi_sheet.py tuoppi.webp ulos.png"""
import sys, cv2, numpy as np
src, out = sys.argv[1], sys.argv[2]
burst_src = sys.argv[3] if len(sys.argv) > 3 else None   # tuopin posahdus (kuva 15)
scatter_src = sys.argv[4] if len(sys.argv) > 4 else None   # palat ja läikkä (kuva 14)
S = 2; CW, CH = 128 * S, 96 * S
im = cv2.imread(src, cv2.IMREAD_UNCHANGED)
if im.shape[2] == 3: im = cv2.cvtColor(im, cv2.COLOR_BGR2BGRA)
ys, xs = np.where(im[..., 3] > 10); im = im[ys.min():ys.max()+1, xs.min():xs.max()+1]
H = int(CH * 0.86); f = H / im.shape[0]; W = int(im.shape[1] * f)
mug = cv2.resize(im, (W, H), interpolation=cv2.INTER_AREA)
rng = np.random.RandomState(7)
x0 = (CW - W) // 2 - int(W * 0.08); y0 = CH - H - 4 * S   # kahva oikealla: hieman vasemmalle
def canvas(): return np.zeros((CH, CW, 4), np.uint8)
def paste(dst, spr, x, y):
    h, w = spr.shape[:2]; xa, ya = max(x, 0), max(y, 0); xb, yb = min(x + w, CW), min(y + h, CH)
    if xb <= xa or yb <= ya: return
    s = spr[ya - y:yb - y, xa - x:xb - x].astype(np.float32); d = dst[ya:yb, xa:xb].astype(np.float32)
    a = s[..., 3:4] / 255.0; da = d[..., 3:4] / 255.0; oa = a + da * (1 - a)
    rgb = (s[..., :3] * a + d[..., :3] * da * (1 - a)) / np.maximum(oa, 1e-6)
    dst[ya:yb, xa:xb] = np.concatenate([rgb, oa * 255], -1).astype(np.uint8)
# sirpaleet: Voronoi
alpha = mug[..., 3] > 10
pts = np.argwhere(alpha); seeds = pts[rng.choice(len(pts), 10, replace=False)]
d2 = ((np.indices(alpha.shape).transpose(1, 2, 0)[:, :, None, :] - seeds[None, None]) ** 2).sum(-1)
lab = d2.argmin(-1)
shards = []
for k in range(len(seeds)):
    m = (lab == k) & alpha
    if m.sum() < 30: continue
    s = mug.copy(); s[..., 3] = np.where(m, mug[..., 3], 0)
    cy, cx = seeds[k]
    shards.append((s, cx - W / 2, cy - H / 2, rng.uniform(-1, 1)))
def beer_px():
    a = mug[..., :3].astype(int); m = (a[..., 2] > 200) & (a[..., 1] > 120) & (a[..., 1] < 200) & (a[..., 0] < 90) & alpha   # BGR: oranssikeltainen
    return np.argwhere(m)
bp = beer_px()
def frame(i):
    c = canvas()
    if i == 0: paste(c, mug, x0, y0); return c
    if i == 1:
        paste(c, mug, x0, y0)
        for _ in range(7):
            p = shards[rng.randint(len(shards))]; a = (int(x0 + rng.uniform(.15, .8) * W), int(y0 + rng.uniform(.2, .9) * H)); b = (a[0] + rng.randint(-40, 40), a[1] + rng.randint(-40, 40))
            cv2.line(c, a, b, (255, 255, 255, 255), 2 * S // 2 + 1, cv2.LINE_AA)
        return c
    prog = [0, 0, .25, .55, .85, 1.0, 1.0][i]
    for (s, dx, dy, rot) in shards:
        if i == 6:
            # lattialle: litistyneet palat
            ang = rot * 60; sc = 0.55
            M = cv2.getRotationMatrix2D((W / 2 + dx, H / 2 + dy), ang, sc)
            w2 = cv2.warpAffine(s, M, (W, H), flags=cv2.INTER_AREA)
            sq = cv2.resize(w2, (W, int(H * 0.45)), interpolation=cv2.INTER_AREA)
            px = int(x0 + dx * 0.9); py = CH - sq.shape[0] - 6 * S - int(abs(dy) * 0.1) - int(rng.uniform(0, 8))
            paste(c, sq, px, py)
        else:
            fall = prog ** 2 * 0.9
            ox = dx * (0.1 + prog * 0.75); oy = dy * (0.05 + prog * 0.15) + fall * H * 0.45
            M = cv2.getRotationMatrix2D((W / 2 + dx, H / 2 + dy), rot * 150 * prog, 1 - prog * 0.25)
            w2 = cv2.warpAffine(s, M, (W, H), flags=cv2.INTER_AREA)
            paste(c, w2, int(x0 + ox), int(y0 + oy))
    # olutroiskeet
    n = [0, 0, 10, 16, 14, 8, 0][i]
    for _ in range(n):
        if len(bp) == 0: break
        y, x = bp[rng.randint(len(bp))]; dx = x - W / 2; dy = y - H / 2
        px = int(x0 + x + dx * prog * 0.8 + rng.uniform(-8, 8)); py = int(y0 + y + dy * prog * 0.1 + prog * prog * H * 0.5)
        cv2.circle(c, (px, min(py, CH - 8 * S)), rng.randint(3, 7) * S // 2 + 1, (20, 160, 245, 255), -1, cv2.LINE_AA)
    if i == 6:
        pud = canvas(); cx = x0 + W // 2
        cv2.ellipse(pud, (cx, CH - 12 * S), (int(W * .62), 9 * S), 0, 0, 360, (20, 170, 250, 235), -1, cv2.LINE_AA)
        cv2.ellipse(pud, (cx - 6 * S, CH - 12 * S), (int(W * .45), 5 * S), 0, 0, 360, (60, 215, 255, 235), -1, cv2.LINE_AA)
        for _ in range(5): cv2.circle(pud, (cx + rng.randint(-W // 2, W // 2), CH - 13 * S + rng.randint(-6, 4)), rng.randint(3, 7), (225, 245, 255, 255), -1, cv2.LINE_AA)
        paste(pud, c, 0, 0); c = pud
    return c
def load(path):
    g = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    if g.shape[2] == 3: g = cv2.cvtColor(g, cv2.COLOR_BGR2BGRA)
    return g
def fit(g, maxw, maxh):
    ys, xs = np.where(g[..., 3] > 10); g = g[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    k = min(maxw / g.shape[1], maxh / g.shape[0]); return cv2.resize(g, (max(1, int(g.shape[1] * k)), max(1, int(g.shape[0] * k))), interpolation=cv2.INTER_AREA)
def bottom(g, w=None):
    c = canvas(); paste(c, g, (CW - g.shape[1]) // 2, CH - g.shape[0] - 4 * S); return c
frames = [frame(0), frame(1)]
if burst_src and scatter_src:
    bu = load(burst_src); sc = load(scatter_src)
    ys, xs = np.where(sc[..., 3] > 10); sc = sc[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    k = (CW - 4) / sc.shape[1]; scs = cv2.resize(sc, None, fx=k, fy=k, interpolation=cv2.INTER_AREA); sh = scs.shape[0]
    frames.append(bottom(fit(bu, CW - 4, CH - 4 * S)))                    # posahdus
    frames.append(bottom(scs))                                            # palat ilmassa
    frames.append(bottom(cv2.resize(scs, None, fx=0.88, fy=0.75, interpolation=cv2.INTER_AREA)))   # palat laskeutuvat
    frames.append(bottom(cv2.resize(scs, None, fx=0.8, fy=0.52, interpolation=cv2.INTER_AREA)))   # palat ja lätty
    frames.append(bottom(scs[int(sh * 0.74):]))                           # lätty ja alimmat palat (jää lattialle)
else:
    frames += [frame(i) for i in range(2, 7)]
sheet = np.concatenate([cv2.resize(f, (128, 96), interpolation=cv2.INTER_AREA) for f in frames], 1)
cv2.imwrite(out, sheet); print(sheet.shape)
