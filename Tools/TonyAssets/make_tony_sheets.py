"""Tankki-Tony (Vegasin 1. vihu): neljä vihreätaustaista videota + suojauskuva -> Assets/Sprites/Viholliset/tony_<nimi>.png.
Video A (240 kuvaa): idle 0-55, kävely 58-118, juoksu 120-239. Video B (91): jabi 18-40, takasuora 48-58, potku 72-88.
Video C (240): osuma 22-34 (kipinät 18-22 pois), kaatuminen 36-100, nousu 116-188. Video D (40): heitto (ote, nosto, ojennus).
Suojaus: kuva jossa 4 hahmoa vierekkäin (Tools/TonyAssets/tony_suojaus_lahde.png). Hahmo ~640 px, jalat 16 px ylös.
Video E (haymaker, 72 kuvaa 30 fps): lataus 32-46, isku 46-54 (osuma 52). Ajo: python Tools/TonyAssets/make_tony_sheets.py <A> <B> <C> <D> <suojaus.png> <ulos-kansio> <haymaker.mp4> <matala-potku.mp4> <alakoukku.mp4>"""
import os, sys
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
m.TARGET_H = 640.0
CELLS = {"idle": (640, 320, 768), "kavely": (640, 320, 768), "juoksu": (896, 384, 768), "isku1": (1024, 320, 768), "isku2": (1152, 320, 768), "isku3": (1152, 320, 768),
         "potku": (1280, 320, 768), "potku2": (1280, 320, 768), "uppercut": (1024, 320, 768), "osuma": (896, 420, 768), "kaatuminen": (1152, 576, 1024), "nousu": (1152, 576, 768), "heitto": (1024, 320, 768), "suojaus": (640, 320, 768)}
m.DIMS.update({"v_" + k: v for k, v in CELLS.items()})

def despill(im):
    """Vihreä heijastus pölystä ja reunoista: vihreä kanava ei saa ylittää punaista/sinistä."""
    o = im.copy(); r, g, b = (o[..., i].astype(np.int16) for i in range(3))
    o[..., 1] = np.minimum(g, np.maximum(r, b) + 6).astype(np.uint8); return o

def clean(im):
    im = despill(im)
    a = (im[..., 3] > 128).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(a, 8)
    if n <= 2: return im
    big = st[1:, cv2.CC_STAT_AREA].max(); out = im.copy()
    for j in range(1, n):
        if st[j, cv2.CC_STAT_AREA] < 0.04 * big: out[lab == j, 3] = 0
    return out

def main(vA, vB, vC, vD, guard, outs, vH=None, vL=None, vU=None):
    A, B, C, D = m.load(vA), m.load(vB), m.load(vC), m.load(vD)
    C = [clean(f) for f in C]
    def kf(v):
        x0, x1, y0, y1 = m.bbox(v[0]); return m.TARGET_H / (y1 - y0), y1
    ks = [kf(v) for v in (A, B, C, D)]; print("mittakaavat", [round(k, 3) for k, _ in ks])
    def sheet(name, vi, idx, mode, fname=None):
        V = (A, B, C, D)[vi]; k, fl = ks[vi]
        m.setdims("v_" + name)
        m.save([m.cell(V[i], mode, k, fl) for i in idx], f"{outs}/tony_{fname or name}.png"); print(name, len(idx), "kuvaa", idx)
    sheet("idle", 0, m.best_loop(A, 0, 55, 20, 40, "low")[::2], "low")
    sheet("kavely", 0, m.best_loop(A, 58, 118, 16, 50, "torso")[::2], "torso")
    sheet("juoksu", 0, m.best_loop(A, 120, 239, 10, 16, "torso"), "torso")
    sheet("isku1", 1, list(range(18, 42, 2)), "low")                           # jabi: osuma ~ kuva 32 (indeksi 7)
    sheet("isku2", 1, list(range(46, 60, 2)), "low")                           # takasuora: osuma ~ kuva 54 (indeksi 4)
    sheet("potku", 1, [72, 74, 76, 78, 80, 82, 84, 86, 88], "low")             # korkea potku: osuma kuva 86 (indeksi 7)
    sheet("osuma", 2, [22, 24, 26, 28, 30, 32, 34], "low")
    sheet("kaatuminen", 2, [36, 41, 46, 51, 55, 59, 63, 67, 72, 78, 86, 100], "com")
    sheet("nousu", 2, [116, 122, 128, 134, 140, 146, 152, 158, 164, 170, 176, 182, 188], "com")
    sheet("heitto", 3, [0, 7, 9, 12, 15, 18, 21, 24, 26, 28, 32, 38], "low")
    if vH:   # haymaker (30 fps, 72 kuvaa): lataus 32-46 (hidas), isku 46-54 (osuma kuva 52), vauhti vie hahmon selin kameraan 56-71 (kaikki mukana)
        H = m.load(vH); kh, fh = kf(H); m.setdims("v_isku3")
        idx = [32, 34, 36, 38, 40, 42, 44, 46, 48, 50, 52, 54, 56, 58, 60, 62, 64, 66, 68, 70, 71]
        m.save([m.cell(H[i], "low", kh, fh) for i in idx], f"{outs}/tony_isku3.png"); print("isku3", len(idx), "kuvaa; osuma indeksi", idx.index(52))
    if vL:   # matala potku "viikate" (30 fps, 75 kuvaa): alku 0-16 paikallaan (otetaan 16:sta), käännös 17-32, potku 33-40 (osuma 36), paluu 41-55, kääntyminen selin 56-74 (kaikki mukana)
        L = m.load(vL); kl, fl = kf(L); m.setdims("v_potku2")
        idx = [16, 16, 18, 18, 20, 20] + list(range(22, 75, 2))   # 3 ensimmäistä kuvaa kahdesti: hitaampi alku (kaikki videon kuvat mukana)
        m.save([m.cell(L[i], "low", kl, fl) for i in idx], f"{outs}/tony_potku2.png"); print("potku2", len(idx), "kuvaa; osuma indeksi", idx.index(36))
    if vU:   # alakoukku (30 fps, 78 kuvaa): alku 0-33 paikallaan (otetaan 34:stä), lataus 34-53 (kyykky, käsi taakse), isku 54-60 (osuma 58), käsi ylhäällä 60-77 (kaikki mukana)
        U = m.load(vU); ku, fu = kf(U); m.setdims("v_uppercut")
        idx = list(range(34, 54, 2)) + list(range(54, 63)) + [64, 66, 68, 70, 72, 74, 76, 77]
        m.save([m.cell(U[i], "low", ku, fu) for i in idx], f"{outs}/tony_uppercut.png"); print("uppercut", len(idx), "kuvaa; osuma indeksi", idx.index(58))
    # suojaus: 4 hahmoa vierekkäin (RGBA), mittakaava 1. hahmon korkeudesta
    im = cv2.cvtColor(cv2.imread(guard, cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)
    cols = (im[..., 3] > 20).sum(0) > 3; runs = []; s0 = None
    for x, c in enumerate(cols):
        if c and s0 is None: s0 = x
        if not c and s0 is not None:
            if x - s0 > 80: runs.append((s0, x))
            s0 = None
    if s0 is not None: runs.append((s0, len(cols)))
    figs = []
    for (a, b) in runs[:4]:
        sub = im[:, a:b]; ys, xs = np.where(sub[..., 3] > 128); figs.append(sub[ys.min():ys.max() + 1, xs.min():xs.max() + 1].copy())
    k = m.TARGET_H / figs[0].shape[0]; print("suojaus: hahmoja", len(runs), "mittakaava", round(k, 3)); m.setdims("v_suojaus")
    cells = []
    for f in figs:
        full = np.zeros((f.shape[0] + 40, f.shape[1] + 40, 4), np.uint8); full[20:20 + f.shape[0], 20:20 + f.shape[1]] = f
        cells.append(m.cell(full, "low", k, f.shape[0] + 20))
    m.save(cells, f"{outs}/tony_suojaus.png"); print("suojaus", len(cells), "kuvaa")

if __name__ == "__main__":
    U = "C:/Users/marku/.claude/uploads/04292e93-ed49-4896-966e-72709d802ca8/"
    a = sys.argv[1:]
    main(a[0], a[1], a[2], a[3], a[4], a[5] if len(a) > 5 else "Assets/Sprites/Viholliset", a[6] if len(a) > 6 else None, a[7] if len(a) > 7 else None, a[8] if len(a) > 8 else None)
