"""Rokkimimmin kevyet tanssit (2 videota: 1920x1080 30 fps 10 s taustaväri vaihtuu, 1280x720 24 fps 10 s vihreä). Käyttö: python make_rokki_dance.py <ulostulo-kansio>
Tulos: rokkimimmi_tanssi_1.png (100 kuvaa, 10 fps), rokkimimmi_tanssi_2.png (80 kuvaa, 8 fps); ruutu 320 x 400, mittakaava ruudun 0 mukaan (korkeus 300 px)."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "HelviAssets"))
import make_helvi_sheets
from make_helvi_sheets import key, sheet, read_all
make_helvi_sheets.TARGET_H = 346.0   # Rokkimimmin tavallinen seisova korkeus pelissä (rokkari2_idle: 346 px); muut hahmot 300
from key_video import key_frame, border_color
U = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
SRC = ["62de4fc8-copy_F0626D82-85FE-48B3-8901-B849EB4649EE.mov", "4ca27a49-gemini_generated_video_B3A9F6C2.mp4"]

def clean_pink(k):
    # vaaleanpunainen/magenta taustaväri jää aukkoihin (käsivarren ja vartalon väli): ei kuulu hahmolle (iho oranssi, hiukset punamustat)
    r, g, b = [k[..., i].astype(int) for i in range(3)]
    pink = (r > 110) & (b > 0.5 * r) & (g < 0.5 * r) & (b - g > 35)
    k[pink, 3] = 0
    solid = (k[..., 3] > 128).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid, connectivity=8)
    if n > 2:
        big = st[1:, cv2.CC_STAT_AREA].max()
        keep = np.isin(lab, [i for i in range(1, n) if st[i, cv2.CC_STAT_AREA] >= 0.05 * big])
        k[~keep, 3] = 0
    return k

def key_any(f):
    # tumma taustaväri (nimiö 'laivastonsininen', ruudut ~2-5 videossa 1): farkut eivät saa kadota -> matala kynnys ja kaikki isot osat mukaan
    if float(border_color(f).mean()) < 45:
        return clean_pink(key_frame(f, t_in=12.0, t_out=40.0, multi=True))
    return clean_pink(key(f))

def main(out):
    os.makedirs(out, exist_ok=True)
    ref = None
    for n, f in enumerate(SRC, 1):
        keyed = [key_any(x) for x in read_all(U + f)[::3]]
        ys = np.where(keyed[0][..., 3] > 128)[0]; ref = float(ys.max() - ys.min()); print("ruudun 0 korkeus", ref)   # kummallakin videolla ruutu 0 on sama asento: oma mittakaava (eri resoluutio)
        print(n, len(keyed), "ruutua")
        sheet(keyed, ref, os.path.join(out, "rokkimimmi_tanssi_%d.png" % n), follow=True)
if __name__ == "__main__": main(sys.argv[1])
