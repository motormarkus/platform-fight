"""Amatsoni (leopardiasuinen soturi): kolmesta vihreätaustaisesta videosta spritesheetit. Hahmo skaalataan ~650 px:ään (ruutu korkeus 768, jalat 16 px ylös, ppu 200 kuten Yakuza).
  a = idle/kävely/juoksu (720p, skaalataan 1080p-kokoon), b = potku (1080p, kiinteä kamera), c = osuma/kaatuminen/nousu (1080p, kiinteä kamera).
Ajo: python Tools/AmazonAssets/make_amazon_sheets.py <a.mp4> <b.mov> <c.mov> <ulos>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
import amazon_key as K

CH, FOOT, SC = 768, 16, 0.78
CW_N, CW_WIDE, ANCHOR_WIDE = 512, 900, 270     # seisova / potku
CW_MOVE, ANCHOR_MOVE = 768, 384                # kävely / juoksu
CW_LIE, ANCHOR_LIE = 960, 560                  # osuma / kaatuminen / nousu (makuulla leveä)

def rescale(f, k):
    a = f.astype(np.float32); a[..., :3] *= a[..., 3:4] / 255.0
    r = cv2.resize(a, (round(f.shape[1] * k), round(f.shape[0] * k)), interpolation=cv2.INTER_AREA)
    al = r[..., 3:4] / 255.0; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    return r.clip(0, 255).astype(np.uint8)

def feet(f):
    a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
    low = a[ys.min() + int(h * 0.8):]; _, lx = np.where(low)
    return ys.max() + 1, lx.mean()

def cell(f, ax, floor, cw, anchor_x):
    c = np.zeros((CH, cw, 4), np.uint8)
    x0 = int(round(ax)) - anchor_x; y0 = int(round(floor)) - (CH - FOOT)
    sy0, sx0 = max(0, y0), max(0, x0); sy1, sx1 = min(f.shape[0], y0 + CH), min(f.shape[1], x0 + cw)
    if sy1 > sy0 and sx1 > sx0: c[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = f[sy0:sy1, sx0:sx1]
    return c

def check(name, cells):
    for n, c in enumerate(cells):
        a = c[..., 3] > 128; ys, xs = np.where(a)
        if xs.min() <= 0 or xs.max() >= c.shape[1] - 1 or ys.min() <= 0: print("  HUOM leikkautuu:", name, n, xs.min(), xs.max(), ys.min())

def save(cells, cols, path):
    rows = (len(cells) + cols - 1) // cols; ch, cw = cells[0].shape[:2]
    img = np.zeros((rows * ch, cols * cw, 4), np.uint8)
    for n, c in enumerate(cells):
        r, k = divmod(n, cols); img[r * ch:(r + 1) * ch, k * cw:(k + 1) * cw] = c
    cv2.imwrite(path, cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA)); print(os.path.basename(path), len(cells), "kuvaa", cw, "x", ch, "ruutua; arkki", img.shape[1], "x", img.shape[0])
    check(os.path.basename(path), cells)

def loop(fr, tx, floor, lo, hi, minlen, maxlen):
    def mask(i):
        m = np.zeros((CH, CW_MOVE), np.float32); a = (fr[i][..., 3] > 128)[floor - (CH - FOOT):floor + FOOT]
        x0 = int(round(tx[i])) - ANCHOR_MOVE; sx0, sx1 = max(0, x0), min(a.shape[1], x0 + CW_MOVE); m[:a.shape[0], sx0 - x0:sx1 - x0] = a[:, sx0:sx1]; return m
    M = {i: mask(i) for i in range(lo, hi)}
    best = min((np.abs(M[s] - M[e]).mean(), s, e) for s in range(lo, hi) for e in range(s + minlen, min(hi, s + maxlen)))
    print("silmukka", best[1], best[2], "ero", round(float(best[0]), 4)); return list(range(best[1], best[2]))

def loop2(F, tx, floor, minlen, maxlen):
    def mask(i):
        m = np.zeros((CH, CW_MOVE), np.float32); a = (F[i][..., 3] > 128)[floor - (CH - FOOT):floor + FOOT]
        x0 = int(round(tx[i])) - ANCHOR_MOVE; sx0, sx1 = max(0, x0), min(a.shape[1], x0 + CW_MOVE); m[:a.shape[0], sx0 - x0:sx1 - x0] = a[:, sx0:sx1]; return m
    ks = sorted(F); M = {i: mask(i) for i in ks}
    best = min((np.abs(M[s]-M[e]).mean(), s, e) for s in ks for e in ks if minlen <= e - s <= maxlen)
    print("silmukka", best[1], best[2], "ero", round(float(best[0]), 4)); return list(range(best[1], best[2]))

def torso_x(f):
    a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
    t = a[ys.min() + int(h * 0.25):ys.min() + int(h * 0.6)]; _, x = np.where(t); return x.mean()

def main():
    av, bv, cv_, out = sys.argv[1:5]
    os.makedirs(out, exist_ok=True)
    # --- a: idle, kävely, juoksu (720p -> 1080p-koko -> ruutukoko) ---
    A = [rescale(f, 1.5 * SC) for f in K.load(av)]
    tx = [torso_x(f) for f in A]
    floor = int(np.median([feet(f)[0] for f in A]))
    f0, a0 = feet(A[0])
    save([cell(A[i], a0, f0, CW_N, CW_N // 2) for i in list(range(0, 33, 3)) + list(range(30, 0, -3))], 8, os.path.join(out, "amatsoni_idle.png"))
    idle_h = np.median([np.ptp(np.where(A[i][..., 3] > 128)[0]) for i in range(33)])
    def norm(lo, hi):   # kävely/juoksu-kuvat ovat videossa idleä isompia: sama korkeus kuin idlessä
        k = idle_h / np.median([np.ptp(np.where(A[i][..., 3] > 128)[0]) for i in range(lo, hi)])
        print("  korkeuskerroin", round(float(k), 3)); return k
    kw = norm(66, 99); W = {i: rescale(A[i], kw) for i in range(66, 99)}
    wtx = {i: tx[i] * kw for i in W}; wfloor = int(np.median([feet(W[i])[0] for i in W]))
    w = loop2(W, wtx, wfloor, 26, 40)
    save([cell(W[i], wtx[i], wfloor, CW_MOVE, ANCHOR_MOVE) for i in w], 8, os.path.join(out, "amatsoni_kavely.png"))
    kr = norm(190, 238); R = {i: rescale(A[i], kr) for i in range(190, 238)}
    rtx = {i: tx[i] * kr for i in R}; rfloor = int(np.median([feet(R[i])[0] for i in R]))
    r = loop2(R, rtx, rfloor, 16, 40)
    save([cell(R[i], rtx[i], rfloor, CW_MOVE, ANCHOR_MOVE) for i in r], 8, os.path.join(out, "amatsoni_juoksu.png"))
    # --- b: potku ---
    B = [rescale(f, SC) for f in K.load(bv)]; fb, ab = feet(B[0])
    kick = list(range(39, 70, 2))
    save([cell(B[i], ab, fb, CW_WIDE, ANCHOR_WIDE) for i in kick], 4, os.path.join(out, "amatsoni_potku.png"))
    # --- c: osuma, kaatuminen, nousu ---
    C = [rescale(f, SC) for f in K.load(cv_)]; fc, ac = feet(C[0])
    mk = lambda idx: [cell(C[i], ac, fc, CW_LIE, ANCHOR_LIE) for i in idx]
    save(mk(range(42, 62, 2)), 5, os.path.join(out, "amatsoni_osuma.png"))
    save(mk(list(range(100, 128, 2))), 5, os.path.join(out, "amatsoni_kaatuminen.png"))
    save(mk(list(range(166, 214, 3))), 4, os.path.join(out, "amatsoni_nousu.png"))
if __name__ == "__main__":
    main()
