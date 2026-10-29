"""Pomon apuri, osuma + kaatuminen + maahan + nousu yhdestä videosta (1080p, 30 fps, vihreä tausta, maila kädessä). Ruutu 1280 x 768, ppu 200, jalat 16 px ylös,
paikallaan seisovan hahmon ankkuri x=640 (ruudun keskellä), skaala = seisova hahmo 613 px.
Ajo (projektin juuresta): python Tools/LatkaAssets/make_apuri_kaatuminen.py <kaatuminen.mov> <ulos-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from apuri_key import key
import make_latka_sheets
make_latka_sheets.CH = 768   # korkeampi ruutu: pystyyn kohoava osuma ylittää 704 px
from make_latka_sheets import feet, cell, save, rescale
CW, ANCH, REF_H = 1280, 640, 613
r = lambda a, b, s=1: list(range(a, b + 1, s))
CLIPS = {"apuri_osuma": r(54, 76), "apuri_kaatuminen": r(94, 140), "apuri_maahan": r(138, 164, 2), "apuri_nousu": r(164, 236, 2)}

def main():
    video, out = sys.argv[1:3]; os.makedirs(out, exist_ok=True)
    cap = cv2.VideoCapture(video); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        fr.append(key(f))
    fl0, ax0, h0 = feet(fr[0]); k = REF_H / h0; print("seisova korkeus", h0, "skaala", round(k, 3), "lattia", fl0, "ankkuri", round(ax0))
    sc = {i: rescale(fr[i], k) for i in sorted({i for v in CLIPS.values() for i in v if i < len(fr)})}
    ff, aa, _ = feet(rescale(fr[0], k))
    for name, idx in CLIPS.items():
        cells = [cell(sc[i], aa, ff, CW, ANCH) for i in idx if i in sc]
        for i, c in zip(idx, cells):
            al = c[..., 3] > 128; ys, xs = np.where(al)
            if xs.min() <= 0 or xs.max() >= CW - 1 or ys.min() <= 0 or ys.max() >= c.shape[0] - 1: print("  HUOM leikkautuu:", name, i, xs.min(), xs.max(), ys.min(), ys.max())
        save(cells, 4, os.path.join(out, name + ".png"))
main()
