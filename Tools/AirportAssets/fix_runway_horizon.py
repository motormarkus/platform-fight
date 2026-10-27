"""Kiitotien kuvien saumat: maan yläreunan (horisontin) porras kuvien välillä. Kuvan 1 oikea reuna on rivillä ~563, kuvan 2 vasen ~572 (jne.),
joten taivas näkyy saumassa kapeana viirunä. Korjaus: kuvan 2 ja 3 vasemman reunan maaosaa venytetään ylöspäin niin, että yläreuna täsmää edellisen
kuvan oikeaan reunaan, ja venytys häipyy RAMP pikselin matkalla. Ajo (projektin juuresta, vain kerran; varmuuskopio kuvakansioon *.orig): python Tools/AirportAssets/fix_runway_horizon.py"""
import os, shutil
import numpy as np
from PIL import Image

T = "Assets/Sprites/Taustat/"
RAMP = 320      # px: venytys häipyy tällä matkalla
BAND = 70       # px: maan yläosan kaista, joka venytetään (alla oleva tie pysyy ennallaan)

def top_row(M, x):
    a = np.where(M[:, x, 3] > 128)[0]
    return int(a.min()) if len(a) else None

def main():
    paths = [T + "thaitown_kiitotie_%d.png" % i for i in (1, 2, 3)]
    for p in paths:
        if not os.path.exists(p + ".orig"): shutil.copyfile(p, p + ".orig")
    ims = [np.array(Image.open(p + ".orig").convert("RGBA")) for p in paths]
    for k in (1, 2):
        A, B = ims[k - 1], ims[k]
        target = top_row(A, A.shape[1] - 1)
        out = B.copy()
        for x in range(min(RAMP, B.shape[1])):
            tb = top_row(B, x)
            if tb is None or tb < 540: continue          # hangarin kohta: ei kosketa
            h = int(round((tb - target) * (1 - x / RAMP)))
            if h <= 0: continue
            src = B[tb:tb + BAND, x]                      # alkuperäinen kaista
            dst_n = BAND + h
            ys = np.linspace(0, BAND - 1, dst_n)
            col = np.stack([np.interp(ys, np.arange(BAND), src[:, c].astype(np.float32)) for c in range(4)], axis=1)
            out[tb - h:tb + BAND, x] = np.clip(col, 0, 255).astype(np.uint8)
        ims[k] = out
        print("sauma", k, "tavoite", target, "kuvan vasen reuna", top_row(B, 0), "->", top_row(out, 0))
    for p, im in zip(paths, ims):
        Image.fromarray(im, "RGBA").save(p)

main()
