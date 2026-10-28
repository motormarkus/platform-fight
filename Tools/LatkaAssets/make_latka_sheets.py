"""Lätkäfani (jääkiekkopaita, vihreätaustaiset videot): täysresoluutiokuvat (ppu 200), samat ruutukoot kuin Yakuza-pomolla.
  video 1: hengitys (juoksun jälkeen, ruudut 0-58), idle (56-96 silmukka), kävely, juoksu;  video 2: iskut ja potku;  kuva: suojaus (3 x 2 ruudukko, ruudut 1-4).
Ruudut: idle/kävely/hengitys/suoja 512 x 704 ja juoksu 640 x 704 (hahmo keskellä), iskut 1024 x 704 (jalkojen keskikohta 400 px vasemmalta), jalat 16 px ylös.
Ajo (projektin juuresta): python Tools/LatkaAssets/make_latka_sheets.py <idle_kavely_juoksu.mp4> <iskut.mp4> <suoja.png> <ulos>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "AttendantAssets"))
from make_attendant_hd import key_frame

CH, FOOT = 704, 16
CW_IDLE, CW_RUN, CW_ATK, ANCHOR_ATK = 512, 640, 1024, 400

def load(path):
    cap = cv2.VideoCapture(path); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        fr.append(key_frame(f))
    return fr

def feet(f):
    a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
    low = a[ys.min() + int(h * 0.8):]; _, lx = np.where(low)
    return ys.max() + 1, lx.mean(), ys.max() - ys.min() + 1

def rescale(f, k):
    if abs(k - 1) < 1e-3: return f
    a = f.astype(np.float32); a[..., :3] *= a[..., 3:4] / 255.0
    r = cv2.resize(a, (round(f.shape[1] * k), round(f.shape[0] * k)), interpolation=cv2.INTER_AREA)
    al = r[..., 3:4] / 255.0; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    return r.clip(0, 255).astype(np.uint8)

def cell(f, ax, floor, cw, anchor_x):
    c = np.zeros((CH, cw, 4), np.uint8)
    x0 = int(round(ax)) - anchor_x; y0 = int(round(floor)) - (CH - FOOT)
    sy0, sx0 = max(0, y0), max(0, x0); sy1, sx1 = min(f.shape[0], y0 + CH), min(f.shape[1], x0 + cw)
    if sy1 > sy0 and sx1 > sx0: c[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = f[sy0:sy1, sx0:sx1]
    return c

def save(cells, cols, path):
    rows = (len(cells) + cols - 1) // cols; ch, cw = cells[0].shape[:2]
    img = np.zeros((rows * ch, cols * cw, 4), np.uint8)
    for n, c in enumerate(cells):
        r, k = divmod(n, cols); img[r * ch:(r + 1) * ch, k * cw:(k + 1) * cw] = c
    cv2.imwrite(path, cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA)); print(os.path.basename(path), len(cells), "kuvaa", img.shape[1], "x", img.shape[0])

def torso_x(f):
    a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
    t = a[ys.min() + int(h * 0.25):ys.min() + int(h * 0.6)]; _, x = np.where(t); return x.mean()

def find_loop(frames, lo, hi, minlen, maxlen, floor):
    tx = {i: torso_x(frames[i]) for i in range(lo, hi)}
    def mask(i):
        return (cell(frames[i], tx[i], floor, CW_IDLE, CW_IDLE // 2)[..., 3] > 128).astype(np.float32)
    M = {i: mask(i) for i in range(lo, hi)}
    best = min(((np.abs(M[s] - M[e]).mean(), s, e) for s in range(lo, hi - minlen) for e in range(s + minlen, min(hi, s + maxlen))))
    return best, tx

def main():
    v1, v2, guard_png, out = sys.argv[1:5]
    os.makedirs(out, exist_ok=True)
    f1 = load(v1); fl = [feet(f)[0] for f in f1[:120]]; floor = int(np.median(fl)); a0 = feet(f1[0])[1]
    h_idle = feet(f1[0])[2]
    print("video 1: lattia", floor, "jalkojen keski", round(a0), "korkeus", h_idle)
    # hengitys (0-58 joka 2.), idle (56-96 silmukka, joka 2.): kiinteä ankkuri
    save([cell(f1[i], a0, floor, CW_IDLE, CW_IDLE // 2) for i in range(0, 60, 2)], 8, os.path.join(out, "latkafani_hengitys.png"))
    save([cell(f1[i], a0, floor, CW_IDLE, CW_IDLE // 2) for i in range(56, 98, 2)], 8, os.path.join(out, "latkafani_idle.png"))
    # kävely (124-204) ja juoksu (206-240): silmukka, hahmo keskitetään vartalon mukaan
    for name, lo, hi, mn, mx in (("kavely", 150, 204, 20, 56), ("juoksu", 206, 240, 12, 28)):
        (d, s, e), tx = find_loop(f1, lo, hi, mn, mx, floor)
        idx = list(range(s, e)); print(name, "silmukka", s, e, "ero", round(float(d), 4), len(idx), "kuvaa")
        cw_ = CW_RUN if name == "juoksu" else CW_IDLE
        save([cell(f1[i], tx[i], floor, cw_, cw_ // 2) for i in idx], 8, os.path.join(out, "latkafani_" + name + ".png"))
    # iskut
    f2 = load(v2); floor0, ax0, h0 = feet(f2[0]); print("video 2: lattia", floor0, "jalkojen keski", round(ax0), "korkeus", h0)
    r = lambda a, b, s=1: list(range(a, b + 1, s))
    CLIPS = {   # nimi: (ruudut, osumakuva-indeksit)
        "jab": (r(6, 16), (4,)),
        "suora": (r(18, 27), (3,)),
        "suora2": (r(26, 34), (3,)),
        "isku3": (r(46, 56), (3, 4)),
        "isku4": (r(56, 67), (5,)),
        "tupla": (r(70, 87), (2, 10)),
        "lyonti": (r(89, 97), (2, 3)),
        "koukku": (r(128, 148), (11,)),
        "potku": (r(168, 204, 2), (9,)),
        "vartalokoukku": (r(100, 122), (12,)),
    }
    for name, (idx, imp) in CLIPS.items():
        cells = [cell(f2[i], ax0, floor0, CW_ATK, ANCHOR_ATK) for i in idx]
        for i, c in zip(idx, cells):
            a = c[..., 3] > 128; ys, xs = np.where(a)
            if xs.min() <= 0 or xs.max() >= CW_ATK - 1 or ys.min() <= 0: print("  HUOM leikkautuu:", name, i, xs.min(), xs.max(), ys.min())
        save(cells, 4, os.path.join(out, "latkafani_" + name + ".png")); print("  osumakuvat", imp)
    # suojaus: kuvan ruudut 1-4 (ruudukko 3 x 2, ruudut 557 x 470), skaalataan videon hahmon korkeuteen
    g = cv2.imread(guard_png); gh, gw = g.shape[:2]; cw, chh = gw // 3, gh // 2
    cells_g = [key_frame(g[r_ * chh:(r_ + 1) * chh, c_ * cw:(c_ + 1) * cw]) for r_ in range(2) for c_ in range(3)]
    k = h_idle / feet(cells_g[0])[2]; print("suoja: skaalaus", round(k, 3))
    outg = []
    for n in (0, 1, 2, 3, 4, 5):
        s = rescale(cells_g[n], k); fl_, ax_, _ = feet(s); outg.append(cell(s, ax_, fl_, CW_IDLE, CW_IDLE // 2))
    save(outg, 6, os.path.join(out, "latkafani_suoja.png"))

if __name__ == "__main__": main()
