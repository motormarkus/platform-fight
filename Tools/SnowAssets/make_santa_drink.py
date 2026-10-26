"""Joulupukin ryyppy (kolpakko suuhun, juo, laskee, voimailee) -> Assets/Sprites/Joulupukki/pukki_juoma.png (8 saraketta, 34 ruutua)
Video 1920x1080 @30 fps, mittakaava 0.5. Ruudut: nosto 0-15 | juonti 16-23 (berserk alkaa ruudusta 18) | lasku ja voimailu 24-33.
Ajo: python make_santa_drink.py <ruutukansio f_%03d.png>"""
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
src = sys.argv[1]
IDX = list(range(20, 52, 2)) + [54, 58, 62, 66, 70, 74, 78, 82] + [85, 88, 91, 94, 97, 100, 104, 108, 112, 116]
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
cv2.imwrite(os.path.join(OUT, "pukki_juoma.png"), s)
print("ruutuja", len(cells), "solu", cw, "x", ch, "sheet", s.shape[1], "x", s.shape[0])
