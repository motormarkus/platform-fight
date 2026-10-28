"""Lätkäfanin heitto (nosto ja heitto, Enemyn grabSprites 8 kuvaa): 0 kurkotus, 1-4 nosto, 5 heilautus, 6 heitto, 7 palautuminen. Video 1440 x 1080, 30 fps, vihreä tausta.
Videon pitkä lataus yläasennossa (ruudut 30-107) ja generaattorin piirtämä pallo lopussa (ruudut 108-110) jätetään pois. Ruutu 1024 x 896, jalkojen keskikohta ruudun keskellä, ppu 200.
Ajo: python Tools/LatkaAssets/make_latka_heitto.py <heitto.mov> <potkut.mov> <ulos-kansio>"""
import os, sys
import numpy as np
import cv2
sys.path.insert(0, os.path.dirname(__file__))
from make_latka_sheets import load, feet, cell, save, rescale
from make_latka_kicks import mix
CW, CHH, A = 1024, 896, 512
import make_latka_sheets as M
M.CH = CHH   # korkeampi ruutu: kädet ylhäällä ylittävät 704 px
PICK = [4, 12, 18, 24, 30, 107, 113, 116]   # videon ruudut kuville 0-7: 113 = heitto (kädet ojennettuina, liike-epätarkka -> terävöitetään), 116 = seuranta
SHARPEN = (113, 116)
def main():
    throw, kicks, out = sys.argv[1:4]; os.makedirs(out, exist_ok=True)
    kv = load(kicks); k0 = 613 / feet(kv[0])[2]; s0f = rescale(kv[0], k0); sf, sa, _ = feet(s0f); s0 = cell(s0f, sa, sf, CW, A)
    fr = load(throw); fl0, fa0, h0 = feet(fr[0]); k = 613 / h0; print("skaalaus", round(k, 3), "korkeus", h0)
    def sharpen(f, amount=1.6, radius=2.2):   # epäterävän (liike-epätarkan) ruudun terävöitys: unsharp mask vain väreille
        rgb = f[..., :3].astype(np.float32); bl = cv2.GaussianBlur(rgb, (0, 0), radius)
        out = f.copy(); out[..., :3] = np.clip(rgb + amount * (rgb - bl), 0, 255).astype(np.uint8); return out
    fr = [sharpen(f) if i in SHARPEN else f for i, f in enumerate(fr)]
    sc = [rescale(f, k) for f in fr]; ff, aa, _ = feet(sc[0])
    cells = [cell(sc[i], aa, ff, CW, A) for i in PICK]
    for i, c in enumerate(cells):
        al = c[..., 3] > 128; ys, xs = np.where(al)
        if xs.min() <= 0 or xs.max() >= CW - 1 or ys.min() <= 0: print("  HUOM leikkautuu:", i, xs.min(), xs.max(), ys.min())
    save(cells, 4, os.path.join(out, "latkafani_heitto.png"))
main()
