"""Helvin yksin-tanssit (5 videota 1920x1080, 30 fps). Käyttö: python make_helvi_dance.py <ulostulo-kansio>
Tulos: helvi_tanssi_1..5.png (10 saraketta, ruutu 320 x 400, joka kolmas ruutu = 10 fps). Koko sama kuin helvi_idle (seisova korkeus 300 px ruudun 0 mukaan)."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_helvi_sheets import key, sheet, read_all
U = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
SRC = ["10745e11-copy_75A922DB-56E7-4740-ACCE-EF4D9117F38F.mov", "d3a81821-copy_054E572B-1C92-4598-9031-5B965930732B.mov",
       "79265152-copy_6001A6DF-84DD-4502-8514-998BF98F96D8.mov", "23ffbab9-copy_2F977240-F8F8-4D07-BA0A-A89745933A8E.mov",
       "c51b7382-copy_8F525855-B8E0-45D5-BAF8-93D8BBA8E110.mov"]
STEP = 3

def main(out):
    os.makedirs(out, exist_ok=True)
    for n, f in enumerate(SRC, 1):
        fr = read_all(U + f)[::STEP]
        keyed = [key(x) for x in fr]
        ys = np.where(keyed[0][..., 3] > 128)[0]
        ref_h = float(ys.max() - ys.min())
        print(n, len(keyed), "ruutua, ruudun 0 korkeus", ref_h)
        sheet(keyed, ref_h, os.path.join(out, "helvi_tanssi_%d.png" % n), follow=True)

if __name__ == "__main__": main(sys.argv[1])
