"""Hector: ylösnousu kahdesta 2 x 2 -kuvasta (läpinäkyvä tausta; 1. osa: kyljellään, nojaa kyynärpäähän, konttaus, polvi; 2. osa: polvillaan, kumara, melkein pystyssä, vartiokanta)
-> Assets/Sprites/Viholliset/hector_nousu.png (8 kuvaa, 4 saraketta, ruutu 1152 x 768, pivot keskellä, hahmo ~640 px seisoessa).
Mittakaava hiusten pinta-alasta (kuten kaatumisessa), jokainen kuva asetetaan omalla alareunallaan lattialle ja keskitetään.
Ajo: python Tools/PrisonAssets/make_hector_nousu.py <osa1.png> <osa2.png> <osuma.png (mittakaavan vertailu)> [ulos-kansio]"""
import os, sys
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, HERE); sys.path.insert(0, os.path.join(HERE, "..", "RexAssets"))
import make_hector_react as R
import make_rex_kicks2 as k2

def main(p1, p2, p_hit, outs):
    ih = Image.open(p_hit); a_h = np.array(ih.convert("RGBA")); gh = R.grid(ih, 4, 1)
    stance = a_h[gh[0][1]:gh[0][3], gh[0][0]:gh[0][2]]; ys, xs = np.where(stance[..., 3] > 20)
    k_hit = k2.TARGET / (ys.max() - ys.min() + 1); sh_h = np.mean([R.shoes(a_h[c[1]:c[3], c[0]:c[2]]) for c in gh])
    # 2. osa: viimeinen kuva on vartiokanta -> mittakaava suoraan; 1. osa: lenkkareiden pinta-ala suhteessa osuma-arkkiin ja 2. osaan (keskiarvo)
    im2 = Image.open(p2); a2 = np.array(im2.convert("RGBA")); cells2 = R.split_cells(im2, 2, 2); cr2 = [a2[c[1]:c[3], c[0]:c[2]] for c in cells2]
    b2 = k2.bbox(cr2[3]); k2s = k2.TARGET / (b2[3] - b2[2]); sh2 = np.mean([R.shoes(c) for c in cr2])
    im1 = Image.open(p1); a1 = np.array(im1.convert("RGBA")); cells1 = R.split_cells(im1, 2, 2); cr1 = [a1[c[1]:c[3], c[0]:c[2]] for c in cells1]
    sh1 = np.mean([R.shoes(c) for c in cr1])
    k1s = 0.5 * (k_hit * (sh_h / sh1) ** 0.5 + k2s * (sh2 / sh1) ** 0.5)
    print("mittakaavat: osa 1", round(k1s, 3), "osa 2", round(k2s, 3))
    frames = [(c, k1s) for c in cr1] + [(c, k2s) for c in cr2]
    fix = [0.88, 0.91, 0.95, 1.0]   # 1. osan makuukuvat pienemmiksi (kuten kaatuminen), kohti seisovaa asentoa kerroin 1
    cw, ch, cols = 1152, 768, 4
    sheet = Image.new("RGBA", (cw * cols, ch * 2), (0, 0, 0, 0))
    for i, (c, k) in enumerate(frames):
        x0, x1, y0, y1 = k2.bbox(c)
        im = Image.fromarray(c[y0:y1, x0:x1]).resize((max(1, round((x1 - x0) * k)), max(1, round((y1 - y0) * k))), Image.LANCZOS)
        px = int(round(cw / 2 - im.width / 2)); py = int(round(ch - 16 - im.height))
        if px < 0 or px + im.width > cw or py < 0: print("  HUOM: leikkautuu", i, px, py, im.width, im.height)
        cell = Image.new("RGBA", (cw, ch), (0, 0, 0, 0)); cell.alpha_composite(im, (max(0, px), max(0, py)))
        sheet.alpha_composite(cell, ((i % cols) * cw, (i // cols) * ch)); print("  kuva", i, im.size)
    sheet.save(f"{outs}/hector_nousu.png")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset")
