"""Thaitownin hotellin hissi ja kattohuoneistokäytävä: Assets/ArtSources/Hotel/hotelli_*.png (alkuperäiset) -> Assets/Sprites/Taustat/.
Käytävä on peilikuvana molempiin suuntiin hissistä: [peilattu pääty, peilattu ovet, hissi, ovet, pääty] -> thaitown_kattokayt_1..5.png.
- pään ikkunalasit (valkoiset) läpinäkyviksi (ikkunanäkymä näkyy läpi), kuten salin kuvissa (Tools/GymAssets/make_gym_images.py)
- huoneen numerolaatat korjataan oikeinpäin peilatussa kuvassa; kuvien 2–5 vasen reuna häivytetään OVERLAP px:n matkalla (sama arvo BeatEmUpSetup.cs:ssä)
- hissin sisäkuva: thaitown_hissi_sisa.png
Käyttö: python Tools/HotelAssets/make_penthouse_images.py"""
import os, sys
import cv2
import numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
from make_gym_images import pane_mask

SRC, DST = "Assets/ArtSources/Hotel", "Assets/Sprites/Taustat"
OVERLAP = 48
PLATES = [(562, 594), (1046, 1078), (1530, 1562)]   # huonenumerolaatat kuvassa "ovet" (x-alueet; rivit 283–315)


def load(name):
    return cv2.cvtColor(cv2.imread(os.path.join(SRC, f"hotelli_{name}.png"), cv2.IMREAD_UNCHANGED), cv2.COLOR_BGR2RGBA)


def with_panes(im):
    out = im.copy(); out[..., 3] = 255
    out[pane_mask(im), 3] = 0
    return out


def fade_left(im):
    a = im[..., 3].astype(np.float32)
    t = np.clip(np.arange(im.shape[1]) / float(OVERLAP), 0, 1); t = t * t * (3 - 2 * t)
    out = im.copy(); out[..., 3] = (a * t[None, :]).astype(np.uint8)
    return out


def main():
    end = with_panes(load("kattokayt_paaty"))
    doors = load("kattokayt_ovet"); doors[..., 3] = 255
    lift = load("hissi_ulko"); lift[..., 3] = 255
    mdoors = doors[:, ::-1].copy()
    W = doors.shape[1]
    for x0, x1 in PLATES:                                  # numerot oikeinpäin peilikuvassakin
        mdoors[281:318, W - x1:W - x0] = doors[281:318, x0:x1]
    mend = end[:, ::-1].copy()
    seq = [mend, mdoors, lift, doors, end]
    for i, im in enumerate(seq, 1):
        out = fade_left(im) if i > 1 else im
        cv2.imwrite(os.path.join(DST, f"thaitown_kattokayt_{i}.png"), cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
    inside = load("hissi_sisa"); inside[..., 3] = 255
    cv2.imwrite(os.path.join(DST, "thaitown_hissi_sisa.png"), cv2.cvtColor(inside, cv2.COLOR_RGBA2BGRA))
    print("ok", [round(100.0 * (s[..., 3] == 0).mean(), 1) for s in seq])


if __name__ == "__main__":
    main()
