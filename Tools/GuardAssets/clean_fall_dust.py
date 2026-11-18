"""Vartijan kaatumissarjan kuvista 9 ja 10 (indeksit 8, 9) pölyn valkoiset jäämät pois: lähes valkoiset pikselit läpinäkyviksi ja pienet irralliset alueet pois.
Ajo: python Tools/GuardAssets/clean_fall_dust.py [sheet.png] (alkuperäinen tallessa: vartija_kaatuminen_alkuperainen.png)"""
import sys
import cv2, numpy as np
from PIL import Image
CW, CH, COLS = 1152, 1024, 4
FRAMES = (8, 9, 10)
GREY = (10,)   # harmaa pölypilvi vain tässä kuvassa (muualla vie paidan sävyjä)

def clean(cell, grey=False):
    """Vain hahmon alaosan kaistalla (pöly lattialla); silmät ja hampaat ylempänä säilyvät."""
    ys, xs = np.where(cell[..., 3] > 10); y0, y1 = ys.min(), ys.max()
    band = np.zeros(cell.shape[:2], bool); band[int(y1 - 0.20 * (y1 - y0)):, :] = True
    r, g, b = [cell[..., i].astype(int) for i in range(3)]
    mx = np.maximum(np.maximum(r, g), b); mn = np.minimum(np.minimum(r, g), b)
    light = (mx > 140) & (mx - mn < 60)       # valkoinen/vaaleanharmaa pöly ja sen reunat
    cell[light & band, 3] = 0
    # ohuet pölyviivat (uusi avainnus säilyttää ne): alakaistalla avaus 7x7 -> alle ~6 px paksut vaaleahkot rakenteet pois
    solid = (cell[..., 3] > 60).astype(np.uint8)
    opened = cv2.dilate(cv2.morphologyEx(solid, cv2.MORPH_OPEN, np.ones((7, 7), np.uint8)), np.ones((3, 3), np.uint8))
    thin = (solid > 0) & (opened == 0) & band & (mx > 90) & (mx - mn < 80)
    cell[thin, 3] = 0
    if grey: cell[band & (mx > 70) & (mx < 210) & (mx - mn < 22), 3] = 0   # harmaa pölypilvi (lähes väritön)
    a = (cell[..., 3] > 10).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(a, 8)
    if n > 2:
        big = st[1:, cv2.CC_STAT_AREA].max()
        for j in range(1, n):
            if st[j, cv2.CC_STAT_AREA] < 0.03 * big: cell[lab == j, 3] = 0
    return cell

def main(path):
    a = np.array(Image.open(path).convert("RGBA"))
    for f in FRAMES:
        c, r = f % COLS, f // COLS
        sl = (slice(r * CH, (r + 1) * CH), slice(c * CW, (c + 1) * CW))
        a[sl] = clean(a[sl].copy(), f in GREY)
    Image.fromarray(a).save(path); print("siivottu", FRAMES)

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "Assets/Sprites/Viholliset/vartija_kaatuminen.png")
