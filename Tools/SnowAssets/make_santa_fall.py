"""Joulupukin osuma, kaatuminen, heitettynä (ilmassa), makuu ja nousu (video 1280x720, 24 fps) -> Assets/Sprites/Joulupukki/pukki_osuma/kaato/heitto/makaa/nousu.png
Sama kiinteä rajaus kaikille, mittakaava 0.75 (vastaa idleä, kävelyä ja iskuja).
Ajo: python make_santa_fall.py <ruutukansio f_%03d.png>"""
import os, sys, cv2, numpy as np
OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Joulupukki")
src = sys.argv[1]; SC = 0.75
MOVES = {
    "osuma": list(range(8, 32, 2)),         # lyönti kasvoihin, huojahdus
    "kaato": list(range(30, 54, 2)),        # kaatuu ja lyö maahan
    "heitto": [34, 36, 38, 40, 42],         # ilmassa (heitetyn asento)
    "makaa": [60],                          # makuulla
    "nousu": list(range(88, 142, 3)),       # ylös ja takaisin vartiointiin
}
def key(im):
    f = im.astype(np.float32); g = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = cv2.GaussianBlur(np.clip((70.0 - g) / 50.0, 0, 1), (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4)
    return np.dstack([f, a * 255]).clip(0, 255).astype(np.uint8)
X0, X1, Y0, Y1 = 150, 1050, 0, 730
prev = []
for name, idx in MOVES.items():
    cells = [cv2.resize(key(cv2.imread(os.path.join(src, "f_%03d.png" % i)))[Y0:Y1, X0:X1], None, fx=SC, fy=SC, interpolation=cv2.INTER_AREA) for i in idx]
    ch, cw = cells[0].shape[:2]; cols = 8; rows = (len(cells) + cols - 1) // cols
    s = np.zeros((rows * ch, min(cols, len(cells)) * cw, 4), np.uint8)
    for k, c in enumerate(cells): s[(k // cols) * ch:(k // cols + 1) * ch, (k % cols) * cw:(k % cols + 1) * cw] = c
    cv2.imwrite(os.path.join(OUT, "pukki_" + name + ".png"), s); print(name, len(cells), "ruutua, solu", cw, "x", ch)
    prev += [cells[0], cells[len(cells) // 2]] if len(cells) > 1 else cells
row = np.hstack(prev[:10]); m = row[..., 3:4] / 255.0
cv2.imwrite(os.path.join(src, "preview.png"), cv2.resize((np.full(row.shape[:2] + (3,), (40, 120, 40), np.uint8) * (1 - m) + row[..., :3] * m).astype(np.uint8), None, fx=0.5, fy=0.5))
