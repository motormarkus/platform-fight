"""Alppikylän panttilainaamon sisätila: kaksi kuvaa (vasen, oikea) limitetään yhdeksi nauhaksi (SIFT-kohdistus) -> Assets/Sprites/Taustat/alppi_pantti_koko.png,
ja tiskin etuosa leikataan omaksi kuvakseen alppi_pantti_tiski.png (piirretään Helvin eteen). Tulostaa ovien/tiskin paikat nauhalla (px).
Ajo: python Tools/SnowAssets/make_alpine_pawn.py <vasen.png> <oikea.png>"""
import os, sys
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(__file__))
from make_alpine_terminal import stitch

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
DOOR_X, COUNTER = 1005.0, (38, 456, 862, 668)      # oikean kuvan oven keskikohta ja tiskin suorakulmio (x0, y0, x1, y1)


def save(name, img):
    for _ in range(6):
        try:
            cv2.imwrite(os.path.join(OUT, name), img); return
        except Exception:
            import time; time.sleep(1)
    cv2.imwrite(os.path.join(OUT, name), img)


def main(left_p, right_p):
    ims = []
    for p in (left_p, right_p):
        im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
        if im.shape[2] == 3: im = np.dstack([im, np.full(im.shape[:2], 255, np.uint8)])
        if im.shape[:2] != (941, 1672): im = cv2.resize(im, (1672, 941), interpolation=cv2.INTER_CUBIC)
        ims.append(im)
    strip, T, info = stitch(ims, [700], forced=None)
    for i, n, sc, dx in info: print("liitos", i, "inlierit", n, "skaala", round(sc, 4), "siirto", round(dx, 1))
    save("alppi_pantti_koko.png", strip)
    M = T[1]
    door = float((M @ np.array([DOOR_X, 0, 1]))[0])
    x0, y0, x1, y1 = [float(v) for v in COUNTER]
    cx0 = int(round((M @ np.array([x0, 0, 1]))[0])); cx1 = int(round((M @ np.array([x1, 0, 1]))[0]))
    crop = strip[int(y0):int(y1), cx0:cx1].copy()
    save("alppi_pantti_tiski.png", crop)
    print("nauha", strip.shape, "ovi x", round(door), "tiski x", cx0, "-", cx1, "y", int(y0), "-", int(y1))


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
