"""Lokin purkausliikkeet (nurkkapurkaus): kyynärpääisku (lokki_kyynarpaa) ja lennättävä tuplahyppypotku (lokki_tuplapotku) yhdestä vihreätaustaisesta videosta.
Video (24 fps, 240 kuvaa): 0-43 asento, 44-92 kyynärpää (osuma 52-66), 96-206 tuplahyppypotku (hyppy 102-124, vaakapotku 126-156, lasku 162-180, kyykky 186-196, nousu).
Mittakaava: seisova asento = 640 px kuten muilla Lokin kuvilla. Ajo: python Tools/LokkiAssets/make_lokki_burst.py <video.mp4> [ulos]"""
import os, sys, json
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE); sys.path.insert(0, os.path.join(HERE, "..", "NokkamiesAssets")); sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_nokkamies_sheets as nk
import make_king_sheets as m

TARGET_H = 640.0
nk.KEY_HI, nk.KEY_RANGE = 0.68, 0.56
nk.INK_DEPTH, nk.INK_ERODE = 2.2, 0.8

class Sheet(nk.Sheet):
    def save(self, outs, dims):
        super().save(outs, dims)
        os.replace(f"{outs}/nokkamies_{self.name}.png", f"{outs}/lokki_{self.name}.png")

def main(video, outs):
    V = nk.load(video)
    x0, x1, y0, y1 = m.bbox(V[0]); k = TARGET_H / (y1 - y0); floor = y1
    print("kuvia", len(V), "mittakaava", round(k, 3), "lattia", floor)
    dims = {}
    def sheet(name, idx):
        s = Sheet(name)
        for i in idx:
            if i < len(V): s.add(V[i], "low", k, floor)
        s.save(outs, dims); print("  ", name, "kuvat", list(idx)[0], "-", list(idx)[-1])
    sheet("kyynarpaa", range(44, 92, 2))        # osuma idx 4-10 (kuvat 52-64)
    nk.COLS = 8   # 56 kuvaa: 4 sarakkeella kuva olisi 10752 px korkea (Unityn tekstuuriraja 8192)
    sheet("tuplapotku", range(96, 208, 2))      # hyppy idx 3-14, vaakapotku idx 15-30 (kuvat 126-156)
    p = os.path.join(HERE, "dims.json")
    d = json.load(open(p)) if os.path.exists(p) else {}
    d.update(dims); json.dump(d, open(p, "w"), indent=1)

if __name__ == "__main__":
    a = sys.argv
    main(a[1], a[2] if len(a) > 2 else "Assets/Sprites/Viholliset")
