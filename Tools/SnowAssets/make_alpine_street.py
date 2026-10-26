"""Alppikylän kadun kuvat (peräkkäin vasemmalta oikealle) -> Assets/Sprites/Taustat/alppi_katu_1..N.png.
Läpinäkyvän taivaan reunoilta poistetaan musta hohto. Ajo: python Tools/SnowAssets/make_alpine_street.py <kuva1> [<kuva2> ...]"""
import os, sys
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(__file__))
from make_alpine_assets import bleed, save

for i, p in enumerate(sys.argv[1:], 1):
    im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
    if im.shape[2] == 4: im = bleed(im)
    else: im = np.dstack([im, np.full(im.shape[:2], 255, np.uint8)])
    if im.shape[:2] != (941, 1672): im = cv2.resize(im, (1672, 941), interpolation=cv2.INTER_CUBIC)
    save("alppi_katu_%d.png" % i, im); print(i, im.shape)
