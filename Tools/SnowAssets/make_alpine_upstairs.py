"""Alppikylän afterski-baarin yläkerta: 2 kuvaa (vasen: portaat, oikea: lava) liitetään vierekkäin yhdeksi nauhaksi -> Assets/Sprites/Taustat/alppi_ylakerta_koko.png
Ikkunoissa on läpinäkyvät aukot (parallaksi näkyy läpi): alfareunan väri levitetään (bleed), ettei punaista reunaa jää.
Portaiden paikka nauhalla: x ~3072 (vasemman kuvan x 1400 + 1672).
Ajo: python make_alpine_upstairs.py <vasen kuva> <oikea kuva>"""
import os, sys
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(__file__))
from make_alpine_terminal import bleed, clean_holes

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")

def main(paths):
    ims = []
    for p in paths:
        im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
        if im.shape[2] == 3: im = np.dstack([im, np.full(im.shape[:2], 255, np.uint8)])
        else: im = clean_holes(bleed(im))
        if im.shape[:2] != (941, 1672): im = cv2.resize(im, (1672, 941), interpolation=cv2.INTER_CUBIC)
        ims.append(im)
    # kuvat eivät limity: kuvat liitetään suoraan vierekkäin (ikkunallinen seinä jatkuu), järjestys: oikea kuva (lava) vasemmalle, vasen kuva (portaikko) oikealle
    strip = np.hstack(ims[::-1])        # lava vasemmalle, portaikko oikeaan päähän
    for _ in range(6):
        try:
            cv2.imwrite(os.path.join(OUT, "alppi_ylakerta_koko.png"), strip); break
        except Exception:
            import time; time.sleep(1)
    print("nauha", strip.shape, "aukkoja", round(float((strip[..., 3] < 10).mean()), 3), "portaat x ~3072")

if __name__ == "__main__":
    main(sys.argv[1:3])
