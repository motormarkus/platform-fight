"""Pomon apuri (jääkiekkoilija): luistelu (silmukka), jarrutus (videosta) ja taklaus (kuvan 6 ruutua). Ruutu 1280 x 704, ppu 200, jalat 16 px ylös.
Ajo (projektin juuresta): python Tools/LatkaAssets/make_apuri_sheets.py <luistelu_jarrutus.mov> <taklaus.png> <ulos-kansio> [valikuvat.png]
  valikuvat: 4 kuvaa (2x2), lisätään taklauksen kuvien 1 ja 2 väliin (maila nousee)"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from apuri_key import key
from make_latka_sheets import feet, cell, save, rescale
CW, ANCH, FLOOR, SK_LO, SK_HI, BR_LO, BR_HI = 1280, 640, 700, 14, 92, 92, 144
REF_H = 613

def torso_x(f):
    a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
    t = a[ys.min() + int(h * 0.25):ys.min() + int(h * 0.6)]; _, x = np.where(t); return x.mean()

def main():
    video, tackle, out = sys.argv[1:4]; mid = sys.argv[4] if len(sys.argv) > 4 else None; os.makedirs(out, exist_ok=True)
    cap = cv2.VideoCapture(video); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        k = key(f); k[FLOOR + 3:, :, 3] = 0; fr.append(k)   # jään heijastus pois
    h0 = feet(fr[0])[2]; k = REF_H / h0; print("seisova korkeus", h0, "skaalaus", round(k, 3))
    sc = [rescale(f, k) for f in fr]; fl = round(FLOOR * k)
    tx = {i: torso_x(sc[i]) for i in range(len(sc))}
    C = lambda i: cell(sc[i], tx[i], fl, CW, ANCH)
    # luistelusilmukka: etsi parhaiten sulkeutuva jakso
    M = {i: (C(i)[..., 3] > 128).astype(np.float32) for i in range(SK_LO, SK_HI)}
    d, s, e = min((np.abs(M[s] - M[e]).mean(), s, e) for s in range(SK_LO, SK_HI - 8) for e in range(s + 8, min(SK_HI, s + 24)))
    print("luistelu silmukka", s, e, "ero", round(float(d), 4), e - s, "kuvaa")
    sets = {"apuri_luistelu": [C(i) for i in range(s, e)], "apuri_jarrutus": [C(i) for i in range(BR_LO, BR_HI + 1, 2)]}
    # taklaus: kuvan kuusi hahmoa (yhtenäiset alueet, rivi kerrallaan)
    im = cv2.imread(tackle); kk = key(im, largest=False)
    m = (kk[..., 3] > 128).astype(np.uint8); n, lab, st, _ = cv2.connectedComponentsWithStats(cv2.dilate(m, np.ones((9, 9), np.uint8)))
    comps = sorted(range(1, n), key=lambda i: (st[i][1] > im.shape[0] // 2, st[i][0]))
    figs = []
    for i in comps:
        x, y, w, h, _ = st[i]; f = np.zeros_like(kk); sel = cv2.dilate((lab == i).astype(np.uint8), np.ones((9, 9), np.uint8)) > 0
        f[sel] = kk[sel]; figs.append(f[max(0, y - 4):y + h + 4, max(0, x - 4):x + w + 4])
    # väri: taklauskuvat (ja välikuvat) sovitetaan videon luistelukuvien väreihin (LAB-keskiarvo ja hajonta), koska kuvan värit ovat tummemmat
    st = []
    for i in range(s, e):
        m_ = sc[i][..., 3] > 250; px = cv2.cvtColor(sc[i][..., :3], cv2.COLOR_RGB2LAB)[m_].astype(np.float32); st.append((px.mean(0), px.std(0)))
    rm = np.mean([x[0] for x in st], 0); rs = np.mean([x[1] for x in st], 0)
    def match(f):
        lab = cv2.cvtColor(f[..., :3], cv2.COLOR_RGB2LAB).astype(np.float32); px = lab[f[..., 3] > 250]
        lab = (lab - px.mean(0)) / (px.std(0) + 1e-3) * rs + rm
        g = f.copy(); g[..., :3] = cv2.cvtColor(lab.clip(0, 255).astype(np.uint8), cv2.COLOR_LAB2RGB); return g
    # mittakaava: ensimmäinen taklauskuva on luistelupose, vastaa videon luistelukuvaa
    ref = feet(sc[s])[2]; kt = ref / feet(figs[0])[2]; print("taklaus skaalaus", round(kt, 3))
    if mid:
        im2 = cv2.imread(mid); k2 = key(im2, largest=False)
        m2 = (k2[..., 3] > 128).astype(np.uint8); n2, lab2, st2, _ = cv2.connectedComponentsWithStats(cv2.dilate(m2, np.ones((9, 9), np.uint8)))
        cs2 = sorted(range(1, n2), key=lambda i: (st2[i][1] > im2.shape[0] // 2, st2[i][0]))
        mids = []
        for i in cs2:
            x, y, w, h, _ = st2[i]; f = np.zeros_like(k2); sel = cv2.dilate((lab2 == i).astype(np.uint8), np.ones((9, 9), np.uint8)) > 0
            f[sel] = k2[sel]; mids.append(f[max(0, y - 4):y + h + 4, max(0, x - 4):x + w + 4])
        # mittakaava pinta-alalla: ensimmäinen välikuva (maila alhaalla) = taklauksen ensimmäinen kuva; väri sovitetaan samaan (LAB-keskiarvo ja hajonta)
        area = lambda g: (g[..., 3] > 128).sum()
        kt2 = kt * np.sqrt(area(rescale(figs[0], kt)) / area(rescale(mids[0], kt))); print("välikuvien skaalaus", round(kt2, 3), len(mids), "kuvaa")
        mids = mids[1:]   # ensimmäinen välikuva on sama asento kuin taklauksen ensimmäinen: pois, jolloin maila nousee nopeammin
        MID = [(match(f), kt2) for f in mids]
    else: MID = []
    tk = []
    for f in figs:
        g = rescale(match(f), kt); fl_, _, _ = feet(g); tk.append(cell(g, torso_x(g), fl_, CW, ANCH))
    mid_cells = []
    for f, kk_ in MID:
        g = rescale(f, kk_); fl_, _, _ = feet(g); mid_cells.append(cell(g, torso_x(g), fl_, CW, ANCH))
    sets["apuri_taklaus"] = tk[:1] + mid_cells + tk[1:]
    for name, cells in sets.items():
        for i, c in enumerate(cells):
            a = c[..., 3] > 128; ys, xs = np.where(a)
            if xs.min() <= 0 or xs.max() >= CW - 1 or ys.min() <= 0: print("  HUOM leikkautuu:", name, i)
        save(cells, 4, os.path.join(out, name + ".png"))
main()
