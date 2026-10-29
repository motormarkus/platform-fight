"""Kaukalon pomo, kapteeni (jääkiekkopaita, nyrkkeilijä): 4 vihreätaustaista 720p-videota -> sheetit (ruutu 1280 x 768, ankkuri x=640, jalat 16 px ylös, seisova hahmo 640 px).
  osuma-video:  osuma, kaatuminen, maahan, nousu;  liike-video: kävely, juoksu;  isku-video: suora1, suora2, suora3, idle.
Ajo (projektin juuresta): python Tools/LatkaAssets/make_kapteeni_sheets.py <osuma.mp4> <liike.mp4> <iskut.mp4> <ulos-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from apuri_key import key
import make_latka_sheets
make_latka_sheets.CH = 768
from make_latka_sheets import feet, cell, save, rescale, torso_x
from make_apuri_nyrkki import smooth, dedupe, check, load as _load
CW, ANCH = 1280, 640
K = 640 / 541.0
r = lambda a, b, s=1: list(range(a, b + 1, s))

def loop(cells_fn, frames, lo, hi, mn, mx):
    M = {i: (cells_fn(i)[..., 3] > 128).astype(np.float32) for i in range(lo, hi)}
    d, s, e = min((np.abs(M[s] - M[e]).mean(), s, e) for s in range(lo, hi - mn) for e in range(s + mn, min(hi, s + mx)))
    return d, s, e

def main():
    v0, v1, v2, out = sys.argv[1:5]; os.makedirs(out, exist_ok=True); sets = {}
    # --- osuma-video
    f0 = _load(v0); fl, ax, _ = feet(f0[0]); C0 = lambda i: cell(rescale(f0[i], K), ax * K, fl * K, CW, ANCH)
    sets["kapteeni_osuma"] = smooth([C0(i) for i in r(22, 40)])
    sets["kapteeni_kaatuminen"] = smooth([C0(i) for i in r(40, 74)])
    sets["kapteeni_maahan"] = smooth([C0(i) for i in r(74, 100, 2)])
    sets["kapteeni_nousu"] = smooth([C0(i) for i in r(100, 208)])
    # --- liike-video: kävely ja juoksu silmukkana, hahmo keskitetty vartalon mukaan
    f1 = _load(v1); fl1 = float(np.median([feet(f1[i])[0] for i in range(0, 12)]))
    tx = {i: torso_x(f1[i]) for i in range(len(f1))}
    FL = {}
    C1 = lambda i: cell(rescale(f1[i], K), tx[i] * K, FL.get("v", fl1) * K, CW, ANCH)
    for name, lo, hi, mn, mx in (("kavely", 70, 150, 16, 44), ("juoksu", 164, 239, 10, 28)):
        FL["v"] = float(np.median([feet(f1[i])[0] for i in range(lo, hi)]))   # kävelyssä hahmo on hieman lähempänä kuin seistessä
        d, s, e = loop(C1, f1, lo, hi, mn, mx); print(name, "silmukka", s, e, "ero", round(float(d), 4), e - s, "kuvaa", "lattia", FL["v"])
        sets["kapteeni_" + name] = [C1(i) for i in range(s, e)]
    # --- isku-video
    f2 = _load(v2); fl2, ax2, _ = feet(f2[0]); C2 = lambda i: cell(rescale(f2[i], K), ax2 * K, fl2 * K, CW, ANCH)
    sets["kapteeni_suora1"] = smooth([C2(i) for i in r(29, 39)])
    sets["kapteeni_suora2"] = smooth([C2(i) for i in r(117, 128)])
    sets["kapteeni_suora3"] = smooth([C2(i) for i in r(159, 170)])
    d, s, e = loop(C2, f2, 176, 239, 30, 62); print("idle silmukka", s, e, "ero", round(float(d), 4), e - s)
    sets["kapteeni_idle"] = [C2(i) for i in range(s, e)]
    for name, cells in sets.items():
        check(name, cells); save(cells, 4, os.path.join(out, name + ".png"))
main()
