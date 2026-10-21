"""Sohvin seisovat idle-kuvat ulkokahvilaan (kaksi videota, 1080 x 1440). Ruutu 320 x 400, 10 saraketta, hahmo 300 px, jalat rivillä 384.
Vain videoiden yksivärisen taustan jaksot (ruudut, joissa tausta on musta): s1 200-239 (rauhallinen idle), s2 50-146 (hiusten sipaisu).
Käyttö: python make_sohvi_sheets.py <ulostulo-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "HelviAssets"))
from make_helvi_sheets import key, sheet, read_all
UP = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
S1 = UP + "4b6d70d4-copy_0068BADA-0215-409A-982F-88C32989C801.mov"
S2 = UP + "4b38bcf6-copy_13F84596-85C0-449D-9451-AA81B0C60313.mov"

def main(out):
    os.makedirs(out, exist_ok=True)
    v1 = read_all(S1); v2 = read_all(S2)
    idle = [key(v1[i], dark=True) for i in range(200, 240, 2)]
    ref_h = float(np.ptp(np.where(idle[0][..., 3] > 128)[0]))
    sheet(idle, ref_h, os.path.join(out, 'sohvi_seisoo_idle.png'), follow=False)
    hair = [key(v2[i], dark=True) for i in range(50, 138, 3)]
    sheet(hair, ref_h, os.path.join(out, 'sohvi_seisoo_hiukset.png'), follow=False)

if __name__ == "__main__":
    main(sys.argv[1])
