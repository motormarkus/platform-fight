"""Lätkäfanin pyörähdyspotku (erikoisliike) videosta (1080p, 30 fps): latkafani_pyorahdys.png (ruudukko, ruutu 1536 x 704, jalkojen keskikohta ruudun keskellä, ppu 200).
Videosta otetaan kaikki ruudut 0-59 ilman toistuvia ruutuja (alun odotus 0-24 on puheen paikka); alkuun ja loppuun lisätään 3 häivytyskuvaa perusasennosta (ks. make_latka_kicks.py, ruutu 0 potkuvideosta) ja takaisin.
Osumakuvat tulostuvat ajossa (potku eteen, taakse, eteen). Ajo: python Tools/LatkaAssets/make_latka_spin.py <pyorahdys.mov> <potkut.mov> <ulos-kansio>"""
import os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_latka_sheets import load, feet, cell, save, rescale
from make_latka_kicks import mix
CW, A, BLEND = 1536, 768, 3
KEEP = [i for i in range(0, 60) if i not in (25, 28, 40, 43)]   # alku (ruudut 0-24: odotus, tähän tulee puhe) mukana; videon toistuvat ruudut pois
HITS = (33, 38, 42)
def main():
    spin, kicks, out = sys.argv[1:4]; os.makedirs(out, exist_ok=True)
    kv = load(kicks); f0, a0, h0 = feet(kv[0]); k0 = 613 / h0; s0f = rescale(kv[0], k0); sf, sa, _ = feet(s0f)
    s0 = cell(s0f, sa, sf, CW, A)
    fr = load(spin); f1, a1, h1 = feet(fr[0]); k = 613 / h1; print("skaalaus", round(k, 3), "korkeus", h1)
    sc = [rescale(f, k) for f in fr]; ff, aa, _ = feet(sc[0])
    cells = [cell(sc[i], aa, ff, CW, A) for i in KEEP]
    for i, c in enumerate(cells):
        al = c[..., 3] > 128; ys, xs = np.where(al)
        if xs.min() <= 0 or xs.max() >= CW - 1 or ys.min() <= 0: print("  HUOM leikkautuu:", i + 23, xs.min(), xs.max(), ys.min())
    ins = [mix(s0, cells[0], (j + 1) / (BLEND + 1)) for j in range(BLEND)]; outs = [mix(cells[-1], s0, (j + 1) / (BLEND + 1)) for j in range(BLEND)]
    cells = ins + cells + outs
    print("osumakuvat", [KEEP.index(h) + BLEND for h in HITS], "kuvia", len(cells))
    save(cells, 3, os.path.join(out, "latkafani_pyorahdys.png"))
main()
