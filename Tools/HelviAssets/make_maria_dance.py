"""Marian tanssit festeille (3 videota 1080x1440, 30 fps, 8 s; kaikki alkavat samasta asennosta). Käyttö: python make_maria_dance.py <ulostulo-kansio>
Tulos: maria_tanssi_1..3.png (10 saraketta, ruutu 320 x 400, joka kolmas ruutu = 10 fps); mittakaava ruudun 0 mukaan (korkeus 300 px) kuten Helvi."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_helvi_sheets import key, sheet, read_all
U = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
SRC = ["5a1267e5-copy_DBF1077F-A560-458E-81E8-CFD58B8DAA07.mov", "89291e6f-copy_20DF1FCA-B35D-4C50-8EB0-B65B848323B5.mov", "111a97e0-copy_C21BF8A2-BF42-4541-B606-3678A4E99682.mov"]

def clean_blue(k):
    # Maria on punainen/ruskea/kulta/musta: sinertävä tai vihertävä pikseli on taustan jäännös (suljetut aukot käsivarsien ja jalkojen välissä)
    r, g, b = [k[..., i].astype(int) for i in range(3)]
    bad = (b > r + 12) | (g > r + 12)
    k[bad, 3] = 0
    solid = (k[..., 3] > 128).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid, connectivity=8)
    if n > 2:
        keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); solid = (lab == keep).astype(np.uint8)
    k[cv2.dilate(solid, np.ones((5, 5), np.uint8)) == 0, 3] = 0
    return k

def main(out):
    os.makedirs(out, exist_ok=True)
    ref = None
    for n, f in enumerate(SRC, 1):
        keyed = [clean_blue(key(x)) for x in read_all(U + f)[::3]]
        if ref is None:
            ys = np.where(keyed[0][..., 3] > 128)[0]; ref = float(ys.max() - ys.min()); print("ruudun 0 korkeus", ref)
        print(n, len(keyed), "ruutua")
        sheet(keyed, ref, os.path.join(out, "maria_tanssi_%d.png" % n), follow=True)
if __name__ == "__main__": main(sys.argv[1])
