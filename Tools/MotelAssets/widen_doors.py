"""Motellin ovet leveämmiksi: jokainen huoneen ovi (karmeineen) levennetään kertoimella FACTOR ja oven oikealla puolella oleva seinä/ikkuna/lamppu
puristetaan vastaavasti kasaan (pilarit ja kaikki muu pysyvät paikallaan). Kerroksittain: yläkerran nauha (y 955-1745) ja alakerran nauha (y 1805-3029).
Lähde: output/imagegen/vegas-motelli (alkuperäiset), tulos: Assets/Sprites/Taustat/vegas_motelli_*.png.
Ajo: python Tools/MotelAssets/widen_doors.py [esikatselu.png]"""
import sys, os
import numpy as np, cv2
from PIL import Image
SRC = "output/imagegen/vegas-motelli/"
OUT = "Assets/Sprites/Taustat/"
FACTOR = 1.38
FRAME = 22   # karmin leveys teal-alueen ulkopuolella
PILLARS = [2800, 4471, 5990, 7500, 9000]   # pilarien keskikohdat (koko kuva 10240 leveä)
BANDS = [(955, 1745, 1041, 1720), (1805, 3029, 2040, 2871)]   # (nauha y0, y1, oven teal-alue y0, y1)

def compose():
    c = Image.new("RGBA", (10240, 3688), (0, 0, 0, 0))
    for f, x, y in [("03-yla-vasen", 0, 0), ("04-yla-oikea", 5120, 0), ("01-katu-vasen", 0, 1844), ("02-katu-oikea", 5120, 1844)]:
        c.alpha_composite(Image.open(SRC + f + "-5120x1844.png").convert("RGBA"), (x, y))
    return np.array(c)

def doors(a, ty0, ty1):
    r, g, b = [a[..., i].astype(int) for i in range(3)]
    teal = (g > r + 20) & (b > r + 5) & (a[..., 3] > 200)
    cnt = teal[ty0:ty1].sum(0); thr = 0.45 * (ty1 - ty0)
    out = []; s = None
    for x, v in enumerate(cnt > thr):
        if v and s is None: s = x
        if not v and s is not None:
            if x - s >= 120: out.append((s, x))
            s = None
    return out

def main(prev=None):
    a = compose(); out = a.copy()
    for (y0, y1, ty0, ty1) in BANDS:
        xs = np.arange(10240, dtype=np.float32)
        for (tl, tr) in doors(a, ty0, ty1):
            dl, dr = tl - FRAME, tr + FRAME
            br = min(p for p in PILLARS if p > dr + 200) - 120
            w2 = (dr - dl) * FACTOR
            xo = np.arange(dl, br + 1, dtype=np.float32)
            xs[dl:br + 1] = np.interp(xo, [dl, dl + w2, br], [dl, dr, br])
            print("nauha", y0, "ovi", dl, dr, "->", int(dl + w2), "puristus", dr, "-", br)
        mapx = np.tile(xs, (y1 - y0, 1)); mapy = np.tile(np.arange(y0, y1, dtype=np.float32)[:, None], (1, 10240))
        out[y0:y1] = cv2.remap(a, mapx, mapy, cv2.INTER_CUBIC, borderMode=cv2.BORDER_REPLICATE)
    im = Image.fromarray(out)
    if prev:
        im.save(prev); return
    for f, x, y in [("vegas_motelli_yla1", 0, 0), ("vegas_motelli_yla2", 5120, 0), ("vegas_motelli_katu1", 0, 1844), ("vegas_motelli_katu2", 5120, 1844)]:
        im.crop((x, y, x + 5120, y + 1844)).save(OUT + f + ".png")
    print("tallennettu", OUT)

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else None)
