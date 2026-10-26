"""Korvaa lentokentän ensimmäisen katukuvan (muuri + portti) uudella kuvalla (läpinäkyvä RGBA 1672 x 941) ja tasaa saumat: ensin kadun viimeiseen kuvaan, sitten lentokentän kuviin 2 ja 3
(pystysiirto erikseen rakennukselle ja jalkakäytävälle + värisävyn tasaus). Ajo: python Tools/AirportAssets/use_new_gate.py <uusi_kuva.png|webp>"""
import sys, os
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
import fix_seams as f

def prof(im, cols, lo, hi): return im[lo:hi][:, cols, :3].astype(float).mean((1, 2))

def best(Lp, Rp):
    res = []
    for s in range(-25, 26):
        a = Lp[30:-30]; b = Rp[30 + s:len(Rp) - 30 + s]
        res.append((np.abs(np.diff(a) - np.diff(b)).sum(), s))
    return sorted(res)[0][1]

def align(prev, im):
    su = best(prof(prev, slice(-40, None), 310, f.CUT + 30), prof(im, slice(0, 40), 310, f.CUT + 30))
    sl = best(prof(prev, slice(-40, None), f.CUT - 30, 730), prof(im, slice(0, 40), f.CUT - 30, 730))
    new = np.vstack([f.vshift(im, su)[:f.CUT], f.vshift(im, sl)[f.CUT:]])
    return new, su, sl

def main(path):
    k7 = np.array(Image.open(os.path.join(f.T, "thaitown_katu7.png")).convert("RGBA"))
    new = np.array(Image.open(path).convert("RGBA"))
    assert new.shape[:2] == (941, 1672), new.shape
    prev = k7
    for k in (1, 2, 3):
        im = new if k == 1 else f.load_street(k)
        im, su, sl = align(prev, im)
        im = f.apply_gain(im, f.gain_rows(prev, im))
        f.save_street(k, im); prev = im
        print("katu", k, "rakennus", su, "jalkakäytävä", sl)

if __name__ == "__main__":
    main(sys.argv[1])
