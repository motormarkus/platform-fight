"""Tanyan kombo, kuva 19 (indeksi 18): potkaisevan jalan alta alaspäin sojottava musta läiskä (videon liike-epäterävyys) pois.
Leikataan jalan alareunan linjasta (y ~161 ruudussa) alaspäin x 300-365, reuna pehmennetään. Tehdään sekä Assets-kuvaan että orig_tanya-kopioon
(peel_outline.py:n uudelleenajo lähtee orig_tanyasta). Ajo: python Tools/EdgeBatch/fix_tanya_kick.py"""
import numpy as np, cv2
from PIL import Image
CW, CH, IDX = 512, 384, 18
for p in ("Assets/Sprites/Tanya/tanya_kombo_00.png", "Tools/EdgeBatch/orig_tanya/tanya_kombo_00.png"):
    im = np.array(Image.open(p).convert("RGBA")); r, c = divmod(IDX, 4)
    cell = im[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
    yy, xx = np.mgrid[:CH, :CW]
    cut = (xx >= 300) & (xx <= 365) & (yy > 161 + (365 - xx) * 0.02)
    a = cell[..., 3].astype(np.float32); a[cut] = 0
    # pehmeä reuna leikkauskohtaan
    band = (xx >= 296) & (xx <= 369) & (yy >= 156) & (yy <= 168)
    sm = cv2.GaussianBlur(a, (0, 0), 0.9); a[band] = np.minimum(a[band], sm[band])
    cell[..., 3] = a.astype(np.uint8); cell[cell[..., 3] == 0] = 0
    im[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = cell
    Image.fromarray(im, "RGBA").save(p); print(p, "ok")
