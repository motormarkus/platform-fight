"""Rex: suojaus. 2x2-kuva (läpinäkyvä tausta): ylhäällä vasen = vartiokanta, oikea = suojaus, alhaalla vasen = suojaus (syvempi), oikea = vartiokanta.
-> Assets/Sprites/Viholliset/rex_suojaus.png (4 kuvaa, ruutu 640 x 768, hahmo ~640 px, jalat 16 px ylös; sama mittakaava kuin rex_idle).
Ajo: python Tools/RexAssets/make_rex_block.py <kuva.png> [ulos-kansio]"""
import sys
from PIL import Image
import numpy as np
CW, CH, PIVX, FEET, TARGET = 640, 768, 320, 16, 640.0

def main(src, outs):
    im = Image.open(src).convert("RGBA"); W, H = im.size
    quads = [im.crop((0, 0, W // 2, H // 2)), im.crop((W // 2, 0, W, H // 2)), im.crop((0, H // 2, W // 2, H)), im.crop((W // 2, H // 2, W, H))]
    def bbox(q):
        a = np.array(q)[..., 3] > 20; ys, xs = np.where(a); return xs.min(), xs.max(), ys.min(), ys.max()
    b0 = bbox(quads[0]); k = TARGET / (b0[3] - b0[2])   # vartiokannan korkeus = 640 px kuten idlessä
    sheet = Image.new("RGBA", (CW * 4, CH), (0, 0, 0, 0))
    for i, q in enumerate(quads):
        x0, x1, y0, y1 = bbox(q)
        c = q.crop((x0, y0, x1 + 1, y1 + 1)); c = c.resize((max(1, round(c.width * k)), max(1, round(c.height * k))), Image.LANCZOS)
        px = PIVX - c.width // 2; py = CH - FEET - c.height   # jalat lattialla (alareuna samalla korkeudella)
        sheet.alpha_composite(c, (i * CW + px, py)); print("kuva", i, c.size)
    sheet.save(f"{outs}/rex_suojaus.png")

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
