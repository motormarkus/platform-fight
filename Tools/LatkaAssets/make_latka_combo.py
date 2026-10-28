"""Lätkäfanin lyöntikombo yhdestä jatkuvasta videosta (1080p, 30 fps, vihreä tausta): iskut ovat saman videon peräkkäisiä pätkiä, joten ne jatkuvat saumattomasti.
Ruudut 1024 x 704 (jalkojen keskikohta 400 px vasemmalta, ppu 200), hahmo skaalataan samaan korkeuteen kuin muut Lätkäfanin kuvat (613 px).
Ajo (projektin juuresta): python Tools/LatkaAssets/make_latka_combo.py <lyontikombo.mov> <ulos-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_latka_sheets import load, feet, cell, save, rescale, CW_ATK, ANCHOR_ATK

H_REF = 613
CLIPS = {   # nimi: (ensimmäinen ruutu, viimeinen ruutu, osumaruudut); pätkät ovat peräkkäisiä, seuraava alkaa edellisen viimeisestä ruudusta
    "jab": (0, 20, (7, 17)),            # kaksi osumaa (J + secondImpactFrame)
    "suora": (20, 41, (27, 34)),        # kaksi osumaa (S)
    "isku3": (41, 49, (45,)),           # K
    "vartalokoukku": (49, 72, (64,)),   # U
    "koukku": (77, 98, (88,)),          # D (sivukoukku)
}

def main():
    video, out = sys.argv[1:3]; os.makedirs(out, exist_ok=True)
    fr = load(video); floor0, ax0, h0 = feet(fr[0]); k = H_REF / h0
    print("ruutuja", len(fr), "lattia", floor0, "jalkojen keski", round(ax0), "korkeus", h0, "skaalaus", round(k, 3))
    sc = [rescale(f, k) for f in fr]; f0, a0, _ = feet(sc[0])
    for name, (a, b, imp) in CLIPS.items():
        idx = list(range(a, b + 1)); cells = [cell(sc[i], a0, f0, CW_ATK, ANCHOR_ATK) for i in idx]
        for i, c in zip(idx, cells):
            al = c[..., 3] > 128; ys, xs = np.where(al)
            if xs.min() <= 0 or xs.max() >= CW_ATK - 1 or ys.min() <= 0: print("  HUOM leikkautuu:", name, i, xs.min(), xs.max(), ys.min())
        save(cells, 4, os.path.join(out, "latkafani_" + name + ".png")); print("  osumakuva-indeksit", [i - a for i in imp], "kuvia", len(idx))
main()
