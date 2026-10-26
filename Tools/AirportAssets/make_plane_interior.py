"""Lentokoneen sisätila: neljä kuvaa (51 = takakeittiö, 50 = penkit, 49 = penkit + ovi, 48 = ohjaamo) -> Assets/Sprites/Taustat/thaitown_kone_sisa_1..4.png.
Ikkunat ja ohjaamon lasit ovat alkuperäisessä kuvassa (osittain) läpinäkyviä: ne tehdään täysin läpinäkyviksi, kuvan 49 shakkiruutuikkunat tunnistetaan väristä, jolloin niistä näkyy taivas.
Ajo: python Tools/AirportAssets/make_plane_interior.py <kuvakansio (48-51.webp)>"""
import sys, os, numpy as np, cv2
src = sys.argv[1]; out = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
from PIL import Image
for n, k in ((51, 1), (50, 2), (49, 3), (48, 4)):
    im = np.array(Image.open(os.path.join(src, f"{n}.webp")).convert("RGBA"))
    rgb = im[..., :3].astype(int); lum = rgb.mean(2); sat = rgb.max(2) - rgb.min(2)
    H, W = lum.shape
    a0 = im[..., 3].astype(int)
    keep = np.zeros((H, W), bool)
    if n == 49:   # kuvan ikkunoissa on shakkiruutu "läpinäkyvyyden" tilalla: tunnistetaan vaalea, vähäsaturaatioinen ikkunaovaali
        m = ((lum > 195) & (sat < 14)).astype(np.uint8)
        m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, np.ones((9, 9), np.uint8))
        cnt, lab, st, _ = cv2.connectedComponentsWithStats(m, connectivity=4)
        for k2 in range(1, cnt):
            x, y, w, h, a = st[k2]
            if 3000 < a < 40000 and 1.1 < h / max(w, 1) < 2.4 and 300 < y < 480: keep |= lab == k2
        keep = cv2.dilate(keep.astype(np.uint8), np.ones((3, 3), np.uint8)) > 0
    al = np.where(a0 >= 130, 255, a0)       # runko peittäväksi, ikkunat ja ohjaamon lasit (alkuperäinen alfa) läpinäkyviksi
    al = np.where(a0 < 130, 0, al)
    al = np.where(keep, 0, al).astype(np.uint8)
    keep |= a0 < 130
    im[..., 3] = al
    Image.fromarray(im).save(os.path.join(out, f"thaitown_kone_sisa_{k}.png"))
    print(k, "ikkunapikseleitä", int(keep.sum()))
