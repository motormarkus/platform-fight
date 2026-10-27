"""Yakuza-pomon reaktiokuvat Codexin irrottamista videoruuduista (yakuza_001..181.png, 910 x 512, kiinteä kamera, 30 fps):
  1-6 vartioasento (hylätään: ei turhaa seisomista alussa), 6-60 osuma (nojaa taakse), 52-68 kaatuminen ilmassa, 69-100 maahan, lepo, 105-172 ylösnousu, 173-181 vartioasento (hylätään).
Ruudut skaalataan samaan kokoon kuin iskuvideon hahmo (seisova 443 px -> 629 px), ruutu 1024 x 736, jalkojen keskikohta 560 px vasemmalta, jalat 32 px ylös (pivot x 0,547, y 16/736 Unityssa).
Tulos: yakuzapomo_osuma.png (10), yakuzapomo_kaatuminen.png (22 = ilma 9 + maa 13), yakuzapomo_ylosnousu.png (34), yakuzapomo_heitetty.png (9).
Kuvien kestot (kiihtyvä kaatuminen, hidastuva osuma, hidas nousu) asetetaan editorivalikossa 285 (YakuzaDurations).
Ajo (projektin juuresta): python Tools/YakuzaAssets/make_yakuza_reaction_sheets.py <kansio jossa yakuza_NNN.png> <ulos>"""
import os, sys
import cv2, numpy as np
from PIL import Image
sys.path.insert(0, os.path.dirname(__file__))
from dust_clean import clean_dust   # kaatumisruutujen pöly ja savu pois (ruudut 66-104)

CW, CH, FOOT, ANCHOR, COLS = 1024, 736, 32, 560, 4   # makuukuvat ulottuvat lattiaviivan alle: 32 px marginaali; pivot y = 16/736
STAND_TO = 629.0

OSUMA = [6, 8, 10, 13, 17, 22, 28, 35, 43, 52]
ILMA = [52, 55, 58, 61, 63, 65, 66, 67, 68]
MAA = [69, 70, 72, 74, 76, 78, 80, 82, 85, 88, 91, 95, 100]
NOUSU = list(range(106, 173, 2))   # joka toinen ruutu (34 kuvaa): tasainen liike, ei portaita
HEITETTY = [10, 52, 58, 63, 67, 70, 75, 82, 95]

def load(folder, n):
    a = np.array(Image.open(os.path.join(folder, "yakuza_%03d.png" % n)).convert("RGBA"))
    if 66 <= n <= 104: a, _ = clean_dust(a)
    return a

def upscale(f, k):
    a = f.astype(np.float32); a[..., :3] *= a[..., 3:4] / 255.0
    r = cv2.resize(a, (round(f.shape[1] * k), round(f.shape[0] * k)), interpolation=cv2.INTER_CUBIC)
    al = np.clip(r[..., 3:4], 0, 255) / 255.0; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    r[..., 3] = np.clip(r[..., 3], 0, 255)
    return r.clip(0, 255).astype(np.uint8)

def main():
    folder, out = sys.argv[1], sys.argv[2]
    os.makedirs(out, exist_ok=True)
    f1 = load(folder, 1); a = f1[..., 3] > 128; ys, xs = np.where(a)
    floor = ys.max() + 1; h = ys.max() - ys.min() + 1
    low = a[ys.min() + int(h * 0.8):]; _, lx = np.where(low); ax = lx.mean(); K = STAND_TO / h
    print("ruutu 1: lattia", floor, "jalkojen keski", round(ax), "korkeus", h, "kerroin", round(K, 3))
    def cell(n):
        f = upscale(load(folder, n), K)
        c = np.zeros((CH, CW, 4), np.uint8)
        x0 = int(round(ax * K)) - ANCHOR; y0 = int(round(floor * K)) - (CH - FOOT)
        sy0, sx0 = max(0, y0), max(0, x0); sy1, sx1 = min(f.shape[0], y0 + CH), min(f.shape[1], x0 + CW)
        c[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = f[sy0:sy1, sx0:sx1]
        m = c[..., 3] > 128; yy, xx = np.where(m)
        if xx.min() <= 0 or xx.max() >= CW - 1 or yy.min() <= 0 or yy.max() >= CH - 1: print("  HUOM leikkautuu ruutu", n, xx.min(), xx.max(), yy.min(), yy.max())
        return c
    def save(name, nums):
        cells = [cell(n) for n in nums]; rows = (len(cells) + COLS - 1) // COLS
        img = np.zeros((rows * CH, COLS * CW, 4), np.uint8)
        for i, c in enumerate(cells):
            r, k = divmod(i, COLS); img[r * CH:(r + 1) * CH, k * CW:(k + 1) * CW] = c
        cv2.imwrite(os.path.join(out, name + ".png"), cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA)); print(name, len(cells), "kuvaa", img.shape[1], "x", img.shape[0])
    save("yakuzapomo_osuma", OSUMA)
    save("yakuzapomo_kaatuminen", ILMA + MAA)
    save("yakuzapomo_ylosnousu", NOUSU)
    save("yakuzapomo_heitetty", HEITETTY)

main()
