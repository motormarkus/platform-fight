"""Alppikylän afterski-baarin alakerta: kolme kuvaa (vasemmalta oikealle) limitetään yhdeksi nauhaksi (SIFT-kohdistus) -> Assets/Sprites/Taustat/alppi_baari_koko.png.
Tulostaa ovien paikat nauhalla (px): katuovi (kuva 1, x 322) ja portaiden alapää (kuva 2, x 1180).
Ajo: python Tools/SnowAssets/make_alpine_bar.py <kuva1> <kuva2> <kuva3>"""
import os, sys
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(__file__))
from make_alpine_terminal import stitch

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")


def main(paths):
    ims = []
    for p in paths:
        im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
        if im.shape[2] == 3: im = np.dstack([im, np.full(im.shape[:2], 255, np.uint8)])
        if im.shape[:2] != (941, 1672): im = cv2.resize(im, (1672, 941), interpolation=cv2.INTER_CUBIC)
        ims.append(im)
    strip, T, info = stitch(ims, [700, 700], forced=None, no_rot=True)
    for i, n, sc, dx in info: print("liitos", i, "inlierit", n, "skaala", round(sc, 4), "siirto", round(dx, 1))
    for _ in range(6):
        try:
            cv2.imwrite(os.path.join(OUT, "alppi_baari_koko.png"), strip); break
        except Exception:
            import time; time.sleep(1)
    # baaritiskin etuosa (x 1392-2472, y 398-608) omaksi kuvakseen: piirretään Sohvin eteen
    crop = strip[398:608, 1392:2472].copy()
    cv2.imwrite(os.path.join(OUT, "alppi_baari_tiski.png"), crop)
    door = float((T[0] @ np.array([322, 0, 1]))[0]); stairs = float((T[1] @ np.array([1180, 0, 1]))[0])
    print("nauha", strip.shape, "katuovi x", round(door), "portaat x", round(stairs))


if __name__ == "__main__":
    main(sys.argv[1:4])
