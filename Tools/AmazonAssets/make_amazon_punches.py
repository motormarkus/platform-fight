"""Amatsoni: lyönnit (2 suoraa, 2 koukkua) ja väistö neljännestä videosta (720p, kiinteä kamera, 24 fps). Ruutu 768 korkea, jalkojen keskikohta 330 px vasemmalta, leveys 960.
Ajo: python Tools/AmazonAssets/make_amazon_punches.py <video.mp4> <ulos>"""
import os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
import amazon_key as K
from make_amazon_sheets import rescale, feet, cell, save, SC
CW_P, ANCHOR_P = 960, 330

CLIPS = {   # nimi: (ensimmäinen, viimeinen ruutu, sarakkeet)
    "amatsoni_suora1": (11, 24), "amatsoni_suora2": (197, 214),
    "amatsoni_koukku1": (110, 128), "amatsoni_koukku2": (174, 190), "amatsoni_vaisto": (130, 148),
}
def main():
    v, out = sys.argv[1:3]; os.makedirs(out, exist_ok=True)
    F = [rescale(f, 1.5 * SC) for f in K.load(v)]; fl, ax = feet(F[0])
    for name, (a, b) in CLIPS.items():
        cells = [cell(F[i], ax, fl, CW_P, ANCHOR_P) for i in range(a, b + 1)]
        save(cells, 4, os.path.join(out, name + ".png"))
if __name__ == "__main__":
    main()
