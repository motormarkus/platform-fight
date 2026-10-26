"""Lentokentän katukuvien (1-3) pystysiirto, jotta ne täsmäävät Thaitownin kadun viimeiseen kuvaan (thaitown_katu7): sisältö nousee SHIFT px.
Kuvat kootaan katkaistuista osista (_yla/_kav/_ala), siirretään, alareunaa jatketaan viimeisellä rivillä ja osat + ikkunamaskit kirjoitetaan uudelleen.
Ajo (projektin juuresta): python Tools/AirportAssets/shift_street.py [SHIFT]"""
import sys, os
import numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from make_airport_images import T, CUT, CURB, holes

SHIFT = int(sys.argv[1]) if len(sys.argv) > 1 else 8
for k in (1, 2, 3):
    base = os.path.join(T, "thaitown_lentokentta_katu_%d" % k)
    parts = [np.array(Image.open(base + s + ".png").convert("RGBA")) for s in ("_yla", "_kav", "_ala")]
    im = np.vstack(parts); h, w = im.shape[:2]
    out = np.zeros_like(im)
    out[:h - SHIFT] = im[SHIFT:]
    out[h - SHIFT:] = im[-1:]
    Image.fromarray(out).crop((0, 0, w, CUT)).save(base + "_yla.png")
    Image.fromarray(out).crop((0, CUT, w, CURB)).save(base + "_kav.png")
    Image.fromarray(out).crop((0, CURB, w, h)).save(base + "_ala.png")
    m = holes(out[:CUT, :, 3])
    mask = np.zeros((CUT, w, 4), np.uint8); mask[m > 0] = (255, 255, 255, 255)
    Image.fromarray(mask, "RGBA").save(base + "_ikkunat.png")
    print("katu", k, "siirretty", SHIFT, "px ylös")
