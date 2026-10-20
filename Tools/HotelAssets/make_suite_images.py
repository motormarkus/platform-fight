"""Hotellin sviitti (Thaitown): Assets/ArtSources/Hotel/hotelli_sviitti_1.png (sisäänkäynti, vasen) ja _2.png (sänky ja ikkunat, oikea)
-> Assets/Sprites/Taustat/thaitown_sviitti_1.png ja _2.png. Kuvat ovat peräkkäisiä leikkeitä (sohva ja pöytä jatkuvat saumattomasti),
joten ne asetetaan suoraan vierekkäin ilman limitystä. Kuvan 2 (hotelli_sviitti_2.png) on toimitettu läpinäkyvillä ikkunoilla (alfa): ikkunanäkymä näkyy läpi. Reunojen punertava rajapinta siivotaan.
Käyttö: python Tools/HotelAssets/make_suite_images.py"""
import os, sys
import cv2
import numpy as np

SRC, DST = "Assets/ArtSources/Hotel", "Assets/Sprites/Taustat"


def load(name):
    return cv2.cvtColor(cv2.imread(os.path.join(SRC, f"hotelli_{name}.png"), cv2.IMREAD_UNCHANGED), cv2.COLOR_BGR2RGBA)


def main():
    a = load("sviitti_1"); a[..., 3] = 255
    b = load("sviitti_2")
    clear = b[..., 3] < 140
    clear = cv2.erode(clear.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)   # reuna 1 px lasin sisään
    # reunapikselit saavat lähimmän tumman (kehyksen) värin: ei punertavaa/vaaleaa rajaa
    band = cv2.dilate(clear.astype(np.uint8), np.ones((7, 7), np.uint8)).astype(bool) & ~clear
    darkest = cv2.erode(b[..., :3], np.ones((5, 5), np.uint8))
    b[..., :3] = np.where(band[..., None], darkest, b[..., :3])
    b[..., 3] = np.where(clear, 0, 255).astype(np.uint8)
    cv2.imwrite(os.path.join(DST, "thaitown_sviitti_1.png"), cv2.cvtColor(a, cv2.COLOR_RGBA2BGRA))
    cv2.imwrite(os.path.join(DST, "thaitown_sviitti_2.png"), cv2.cvtColor(b, cv2.COLOR_RGBA2BGRA))
    print("ok, lasia %.1f %% kuvasta 2" % (100.0 * clear.mean()))


if __name__ == "__main__":
    main()
