"""Joulupukin viskaus (tartu -> pito -> heilautus -> heitto) -> Assets/Sprites/Joulupukki/pukki_viskaus.png (8 saraketta)
Video 1920x1080 @30 fps, mittakaava 0.5 (kuten idle/kävely). Ruutujärjestys: TARTU 0-8 | PITO 9-11 (toistetaan kun uhri on kädessä) | HEILAUTUS 12-21 | HEITTO 22-33.
Ruutuvälit (ehdotus, kiihtyy loppua kohden): tartu 0.06, pito 0.10, heilautus 0.07 -> 0.03, heitto 0.03 -> 0.05.
Ajo: python make_santa_throw.py <ruutukansio f_%03d.png>"""
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
src = sys.argv[1]
GRAB = list(range(30, 47, 2)); HOLD = [50, 58, 66]
WIND = [70, 73, 76, 79, 82, 85, 88, 91, 94, 96]; THROW = [98, 99, 100, 101, 102, 104, 106, 108, 110, 112, 115, 118]
IDX = GRAB + HOLD + WIND + THROW
X0, X1, Y0, Y1 = 470, 1560, 0, 1070
def key(im):
    f = im.astype(np.float32); g = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = cv2.GaussianBlur(np.clip((70.0 - g) / 50.0, 0, 1), (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4)
    return np.dstack([f, a * 255]).clip(0, 255).astype(np.uint8)
cells = [cv2.resize(key(cv2.imread(os.path.join(src, "f_%03d.png" % i)))[Y0:Y1, X0:X1], None, fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA) for i in IDX]
ch, cw = cells[0].shape[:2]; cols = 8; rows = (len(cells) + cols - 1) // cols
s = np.zeros((rows * ch, cols * cw, 4), np.uint8)
for k, c in enumerate(cells): s[(k // cols) * ch:(k // cols + 1) * ch, (k % cols) * cw:(k % cols + 1) * cw] = c
cv2.imwrite(os.path.join(OUT, "pukki_viskaus.png"), s)
print("ruutuja", len(cells), "solu", cw, "x", ch, "| tartu", len(GRAB), "pito", len(HOLD), "heilautus", len(WIND), "heitto", len(THROW))
