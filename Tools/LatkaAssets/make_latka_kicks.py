"""Lätkäfanin potkut yhdestä videosta (1080p, 30 fps, vihreä tausta): polvipotku, etupotku ja roundhouse omina sarjoinaan (ruutu 896 x 704, jalkojen keskikohta 300 px vasemmalta, ppu 200).
Etupotku ja roundhouse alkavat ja päättyvät videossa käännettyyn asentoon, joten alkuun ja loppuun lisätään 3 häivytyskuvaa perusasennosta (ruutu 0) ja takaisin.
Ajo (projektin juuresta): python Tools/LatkaAssets/make_latka_kicks.py <potkut.mov> <ulos-kansio>"""
import os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_latka_sheets import load, feet, cell, save, rescale
CW, ANCH, BLEND = 896, 300, 3
CLIPS = {"polvipotku": (0, 24, 15, False), "etupotku": (26, 40, 30, True), "roundhouse": (40, 57, 44, True)}   # alku, loppu, osumaruutu, häivytys

def mix(a, b, t):
    pa = a.astype(np.float32); pb = b.astype(np.float32)
    pa[..., :3] *= pa[..., 3:4] / 255; pb[..., :3] *= pb[..., 3:4] / 255
    r = pa * (1 - t) + pb * t
    al = r[..., 3:4] / 255; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    return r.clip(0, 255).astype(np.uint8)

def main():
    video, out = sys.argv[1:3]; os.makedirs(out, exist_ok=True)
    fr = load(video); f0, a0, h0 = feet(fr[0]); k = 613 / h0
    sc = [rescale(f, k) for f in fr]; ff, aa, _ = feet(sc[0])
    C = lambda i: cell(sc[i], aa, ff, CW, ANCH)
    s0 = C(0)
    for name, (a, b, imp, blend) in CLIPS.items():
        cells = [C(i) for i in range(a, b + 1)]
        pre = 0
        if blend:
            ins = [mix(s0, cells[0], (j + 1) / (BLEND + 1)) for j in range(BLEND)]; pre = BLEND
            outs = [mix(cells[-1], s0, (j + 1) / (BLEND + 1)) for j in range(BLEND)]
            cells = ins + cells + outs
        for i, c in enumerate(cells):
            al = c[..., 3] > 128; ys, xs = np.where(al)
            if xs.min() <= 0 or xs.max() >= CW - 1 or ys.min() <= 0: print("  HUOM leikkautuu:", name, i)
        save(cells, 4, os.path.join(out, "latkafani_" + name + ".png")); print("  osumakuva-indeksi", imp - a + pre, "kuvia", len(cells))
if __name__ == "__main__": main()
