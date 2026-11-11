"""Rex: lennättävä yläpotku (4 x 2 -kuva, 8 kuvaa) ja matala potku (5 kuvaa vierekkäin) valmiista läpinäkyvistä kuvista
-> Assets/Sprites/Viholliset/rex_ylapotku.png ja rex_matalapotku.png (4 saraketta, hahmo ~640 px seisoessa, jalat 16 px ylös, tukijalka pivotissa 320).
Kuvat on piirretty valmiiksi samaan kohtaan ruutuun, joten ruutujen keskinäinen asemointi säilytetään (sama siirtymä kaikille kuville).
Ajo: python Tools/RexAssets/make_rex_kicks2.py <ylapotku.png> <matalapotku.png> [ulos-kansio]"""
import sys
import numpy as np
from PIL import Image

CH, PIVX, FEET, TARGET, COLS = 768, 320, 16, 640.0, 4

def bbox(a):
    ys, xs = np.where(a[..., 3] > 20); return xs.min(), xs.max() + 1, ys.min(), ys.max() + 1

def build(img, cells, stance, out, name):
    """cells: [(x0, y0, x1, y1)] ruudun alueet kuvassa; stance: seisova kuva, josta mittakaava ja tukijalan paikka."""
    a = np.array(img.convert("RGBA"))
    crops = [a[y0:y1, x0:x1] for (x0, y0, x1, y1) in cells]
    sx0, sx1, sy0, sy1 = bbox(crops[stance]); k = TARGET / (sy1 - sy0)
    # tukijalka: seisovan kuvan alin 12 % -> keskimääräinen x
    ys, xs = np.where(crops[stance][..., 3] > 20); low = ys > ys.min() + 0.88 * (ys.max() - ys.min()); ax = xs[low].mean()
    bbs = [bbox(c) for c in crops]
    ux0, ux1, uy0, uy1 = min(b[0] for b in bbs), max(b[1] for b in bbs), min(b[2] for b in bbs), max(b[3] for b in bbs)   # kaikkien kuvien yhteinen alue
    cw = int(np.ceil((PIVX + (ux1 - ax) * k + 32) / 64.0) * 64); ch = CH
    need_h = (uy1 - uy0) * k + FEET
    print(name, "k", round(k, 3), "ruutu", cw, "x", ch, "tarvittava korkeus", round(need_h), "pivot x", PIVX / cw)
    if need_h > ch: ch = int(np.ceil(need_h / 128.0) * 128); print("  korkeus kasvatettu:", ch)
    rows = (len(crops) + COLS - 1) // COLS
    sheet = Image.new("RGBA", (cw * COLS, ch * rows), (0, 0, 0, 0))
    px = int(round(PIVX - (ax - ux0) * k)); py = int(round(ch - FEET - (uy1 - uy0) * k))
    if px < 0 or py < 0: print("  HUOM: leikkautuu", px, py)
    for i, c in enumerate(crops):
        im = Image.fromarray(c[uy0:uy1, ux0:ux1]).resize((max(1, round((ux1 - ux0) * k)), max(1, round((uy1 - uy0) * k))), Image.LANCZOS)
        cell = Image.new("RGBA", (cw, ch), (0, 0, 0, 0)); cell.alpha_composite(im, (max(0, px), max(0, py)))
        sheet.alpha_composite(cell, ((i % COLS) * cw, (i // COLS) * ch))
    sheet.save(f"{out}/{name}.png")

def main(p1, p2, outs):
    i1 = Image.open(p1); W, H = i1.size
    cw, chh = W // 4, H // 2
    cells1 = [((c * cw), (r * chh), (c + 1) * cw, (r + 1) * chh) for r in range(2) for c in range(4)]
    build(i1, cells1, 7, outs, "rex_ylapotku")
    i2 = Image.open(p2); W2, H2 = i2.size
    cells2 = [(c * (W2 // 5), 0, (c + 1) * (W2 // 5), H2) for c in range(5)]
    build(i2, cells2, 0, outs, "rex_matalapotku")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3] if len(sys.argv) > 3 else "Assets/Sprites/Viholliset")
