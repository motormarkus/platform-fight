"""Joulupukin tärähdys (kyykky -> ponnahdus -> paineaalto molemmille puolille -> voimailu -> paluu) -> Assets/Sprites/Joulupukki/pukki_aalto.png
Video 1920x1080 @30 fps, mittakaava 0.5 (sama kuin idle/kävely). Paineaalto ja pöly ovat mukana ruuduissa (läpikuultavina).
Ajo: python make_santa_shock.py <ruutukansio f_%03d.png>"""
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
src = sys.argv[1]
IDX = [17, 21, 25, 29, 37, 45, 49, 51] + list(range(53, 69)) + [70, 74, 80, 88, 96] + list(range(101, 120, 2))
X0, X1, Y0, Y1 = 430, 1580, 0, 1080
def key(im):
    f = im.astype(np.float32); g = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    bg = np.median(g[:30, :30])                                  # taustan vihreys vaihtelee ruuduittain
    a = np.clip((bg * 0.65 - g) / (bg * 0.45), 0, 1)
    a = cv2.GaussianBlur(a, (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4)
    return np.dstack([f, a * 255]).clip(0, 255).astype(np.uint8)
cells = [cv2.resize(key(cv2.imread(os.path.join(src, "f_%03d.png" % i)))[Y0:Y1, X0:X1], None, fx=0.5, fy=0.5, interpolation=cv2.INTER_AREA) for i in IDX]
ch, cw = cells[0].shape[:2]; cols = 8; rows = (len(cells) + cols - 1) // cols
s = np.zeros((rows * ch, cols * cw, 4), np.uint8)
for k, c in enumerate(cells): s[(k // cols) * ch:(k // cols + 1) * ch, (k % cols) * cw:(k % cols + 1) * cw] = c
cv2.imwrite(os.path.join(OUT, "pukki_aalto.png"), s)
pv = np.hstack([cells[i] for i in (0, 6, 11, 14, 17, 20)]); m = pv[..., 3:4] / 255.0
cv2.imwrite(os.path.join(src, "preview.png"), cv2.resize((np.full(pv.shape[:2] + (3,), (40, 120, 40), np.uint8) * (1 - m) + pv[..., :3] * m).astype(np.uint8), None, fx=0.6, fy=0.6))
print("ruutuja", len(cells), "solu", cw, "x", ch)
