"""Kiitotie: kolme kuvaa (38 = A2-ovirakennus + hangaarit, 37 = hangaarit, 36 = tyhjä kiitotie) -> Assets/Sprites/Taustat/thaitown_kiitotie_1..3.png (1672 x 941, läpinäkyvä taivas).
Kuva 38:n tie on 30 px matalammalla kuin kuvissa 37 ja 36, joten 37 ja 36 siirretään 30 px alas (yläreuna läpinäkyvää, alareunan tietä leikkautuu).
Ajo: python Tools/AirportAssets/make_runway_images.py <kuvakansio (36.webp, 37.webp, 38.webp)>"""
import sys, os
from PIL import Image
import numpy as np
src = sys.argv[1]; out = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
SHIFT = 30
for n, name in ((38, 1), (37, 2), (36, 3)):
    im = Image.open(os.path.join(src, f"{n}.webp")).convert("RGBA")
    if n != 38:
        canvas = Image.new("RGBA", im.size, (0, 0, 0, 0)); canvas.paste(im, (0, SHIFT)); im = canvas
    if n != 38:   # 38:n tien yläreuna on rivillä 564, siirrettyjen kuvien rivillä 573: täytetään rako (vain kohdissa, joissa maan yläpuoli on läpinäkyvä)
        a = np.array(im); g = a[573].copy()
        for x in range(a.shape[1]):
            if a[573, x, 3] > 200 and a[572, x, 3] < 10:
                for y in range(564, 573): a[y, x] = g[x]
        im = Image.fromarray(a)
    im.save(os.path.join(out, f"thaitown_kiitotie_{name}.png"))
    print(name, im.size)
