"""Pomon apuri nyrkkeilijänä (maila pudotettu): mailan pudotus + 2 suoraa (video 1, 1080p), osuma/horjunta/kaatuminen/maahan/nousu ilman mailaa (video 2, 720p), idle (video 3, 1080p).
Pudotettu maila poistetaan lattialta iskukuvista (staattinen maski: sama paikka kaikissa kuvissa). Ruutu 1280 x 768, ankkuri x=640 (ruudun keskellä), jalat 16 px ylös, kaksoiskuvat pois.
Ajo (projektin juuresta): python Tools/LatkaAssets/make_apuri_nyrkki.py <iskut.mov> <kaatuminen.mov> <idle.mov> <ulos-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from apuri_key import key
import make_latka_sheets
make_latka_sheets.CH = 768
from make_latka_sheets import feet, cell, save, rescale
CW, ANCH = 1280, 640
K1080 = 613 / 837.0   # seisova mailahahmo 1080p-videossa -> 613 px (sama kuin muissa apuri_-sheeteissä)
r = lambda a, b, s=1: list(range(a, b + 1, s))

def load(path, f=key):
    cap = cv2.VideoCapture(path); fr = []
    while True:
        ok, im = cap.read()
        if not ok: break
        fr.append(f(im))
    return fr

def static_stick(fr):
    A = np.stack([(f[..., 3] > 128) for f in fr[70:150]]); S = A.mean(0) > 0.85
    R = np.zeros_like(S); R[1016:] = S[1016:]; R[850:, 1380:] |= S[850:, 1380:]
    R = cv2.dilate(R.astype(np.uint8), np.ones((7, 7), np.uint8)) > 0
    R[:1000, :1380] = False
    R[850:, 1380:] = cv2.dilate(S.astype(np.uint8), np.ones((7, 7), np.uint8))[850:, 1380:] > 0
    return R

def dedupe(cells):
    out = [cells[0]]
    for c in cells[1:]:
        d = np.abs(c[..., :3].astype(int) - out[-1][..., :3].astype(int)).mean() + np.abs(c[..., 3].astype(int) - out[-1][..., 3].astype(int)).mean()
        if d > 0.25: out.append(c)
    return out

def _flow(a, b):
    g = lambda c: cv2.cvtColor((c[..., :3].astype(np.float32) * (c[..., 3:4] / 255.0) + 128 * (1 - c[..., 3:4] / 255.0)).astype(np.uint8), cv2.COLOR_RGB2GRAY)
    return cv2.calcOpticalFlowFarneback(g(a), g(b), None, 0.5, 4, 25, 5, 7, 1.5, 0)

def _warp(c, flow, t):
    h, w = c.shape[:2]; gx, gy = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
    p = c.astype(np.float32); p[..., :3] *= p[..., 3:4] / 255.0
    o = cv2.remap(p, gx - t * flow[..., 0], gy - t * flow[..., 1], cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT, borderValue=0)
    return o

def between(a, b, n):
    """n-1 välikuvaa kuvien a ja b väliin optisella virtauksella (premultiplied-sekoitus)"""
    fab, fba = _flow(a, b), _flow(b, a); res = []
    for j in range(1, n):
        t = j / n
        m = _warp(a, fab, t) if t < 0.5 else _warp(b, fba, 1 - t)   # vain lähempi kuva siirrettynä: ei haamukäsiä
        al = m[..., 3:4] / 255.0; o = np.zeros_like(m); o[..., :3] = np.where(al > 1e-3, m[..., :3] / np.maximum(al, 1e-3), 0); o[..., 3] = m[..., 3]
        res.append(o.clip(0, 255).astype(np.uint8))
    return res

def smooth(cells):
    """Videon kaksois-/kolmoiskuvat (epätasainen kuvausnopeus) täytetään välikuvilla: tasainen 30 fps, ei nykimistä."""
    keep = [(cells[0], 1)]
    for c in cells[1:]:
        d = np.abs(c[..., :3].astype(int) - keep[-1][0][..., :3].astype(int)).mean() + np.abs(c[..., 3].astype(int) - keep[-1][0][..., 3].astype(int)).mean()
        if d > 0.25: keep.append((c, 1))
        else: keep[-1] = (keep[-1][0], keep[-1][1] + 1)
    out = []
    for (c, n), nxt in zip(keep, keep[1:] + [None]):
        out.append(c)
        if nxt is not None and n > 1: out += between(c, nxt[0], n)
    return out

def check(name, cells):
    for i, c in enumerate(cells):
        al = c[..., 3] > 128; ys, xs = np.where(al)
        if xs.min() <= 0 or xs.max() >= CW - 1 or ys.min() <= 0 or ys.max() >= c.shape[0] - 1: print("  HUOM leikkautuu:", name, i, xs.min(), xs.max(), ys.min(), ys.max())

def main():
    v0, v1, v2, out = sys.argv[1:5]; os.makedirs(out, exist_ok=True)
    sets = {}
    # --- video 1: pudotus + iskut
    f0 = load(v0); R = static_stick(f0)
    cl = [f.copy() for f in f0]
    for i in range(46, len(cl)): cl[i][..., 3][R] = 0
    k = K1080
    stance = list(range(48, 56))
    fl = float(np.median([feet(cl[i])[0] for i in stance])); ax = float(np.mean([feet(cl[i])[1] for i in stance]))
    sc = lambda f: rescale(f, k)
    cell0 = lambda f: cell(sc(f), ax * k, fl * k, CW, ANCH)
    sets["apuri_pudotus"] = smooth([cell0(f0[i]) for i in r(22, 46)])
    WIN = [(54, 68), (66, 79)]   # videon 8 iskusta vain molemmat suorat (A: ikkuna 1, B: ikkuna 2); muut toistoa
    for n, (a, b) in enumerate(WIN, 1):
        sets[f"apuri_nyrkki_suora{n}"] = smooth([cell0(cl[i]) for i in r(a, b)])
    # --- video 2: osuma ... nousu (720p)
    f1 = load(v1); k1 = K1080 * 1080 / 720.0
    fl1, ax1, _ = feet(f1[0]); C1 = lambda i: cell(rescale(f1[i], k1), ax1 * k1, fl1 * k1, CW, ANCH)
    sets["apuri_nyrkki_osuma"] = [C1(i) for i in r(8, 26, 2)]
    sets["apuri_nyrkki_horjunta"] = [C1(i) for i in r(26, 66, 2)]
    sets["apuri_nyrkki_kaatuminen"] = [C1(i) for i in r(66, 92, 2)]
    sets["apuri_nyrkki_maahan"] = [C1(i) for i in r(90, 125, 3)]
    sets["apuri_nyrkki_nousu"] = [C1(i) for i in r(125, 192, 2)]
    # --- video 3: idle (silmukka)
    f2 = load(v2); fl2, ax2, _ = feet(f2[0])
    C2 = lambda i: cell(rescale(f2[i], K1080), ax2 * K1080, fl2 * K1080, CW, ANCH)
    M = {i: (C2(i)[..., 3] > 128).astype(np.float32) for i in range(0, len(f2))}
    d, s, e = min((np.abs(M[s] - M[e]).mean(), s, e) for s in range(0, 40) for e in range(s + 30, len(f2)))
    print("idle silmukka", s, e, "ero", round(float(d), 4), e - s, "kuvaa")
    sets["apuri_nyrkki_idle"] = [C2(i) for i in range(s, e - 1, 2)]
    for name, cells in sets.items():
        check(name, cells); save(cells, 4, os.path.join(out, name + ".png"))
if __name__ == "__main__": main()
