# Kuun pinta: kuva 1 (kraatteri, laatikot) + kuva 2 (asema-ovi oikealla) -> yksi kuva korkeudella 941; parallaksit skaalataan korkeuteen 941.
# Käyttö: python make_moon_surface.py <jatko2> <jatko1> <kuva1> <kuva2> <kaukainen> <tahdet> [out]
import sys, numpy as np
from PIL import Image
af, ast = sys.argv[5:7]
out = sys.argv[7] if len(sys.argv) > 7 else 'Assets/Sprites/Taustat/'
sys.path.insert(0, __import__('os').path.dirname(__file__))
from chain import stitch
# kuvat vasemmalta oikealle: [jatko 2, jatko 1, kuva 1, kuva 2 (ovi oikealla)]
imgs = [np.array(Image.open(f).convert('RGBA')) for f in sys.argv[1:5]]
H = min(i.shape[0] for i in imgs); imgs = [i[:H] for i in imgs]
C, offs = stitch(imgs, OV=120)
w = C.shape[1]
im = Image.fromarray(np.clip(C, 0, 255).astype(np.uint8), 'RGBA')
s = 941 / H
im = im.resize((round(w * s), 941), Image.LANCZOS); im.save(out + 'muuri_kuupinta.png')   # (jaa tämän jälkeen kahtia: muuri_kuupinta_a/_b, koska Unityn tekstuurin enimmäisleveys on 8192)
print('pinta', im.size, 'kuvan 2 alku px:', offs[3] * s, 'ovi (1617):', (offs[3] + 1617) * s, 'kavely max (1740):', (offs[3] + 1740) * s)
for src, name in [(af, 'muuri_kuupinta_far.png'), (ast, 'muuri_kuupinta_tahdet.png')]:
    p = Image.open(src).convert('RGBA' if name.endswith('far.png') else 'RGB')
    p = p.resize((round(p.width * 941 / p.height), 941), Image.LANCZOS); 
    if name.endswith('tahdet.png'):   # kamera nousee korkeissa hypyissä: peilattu jatko ylös
        from PIL import ImageOps
        q = Image.new('RGB', (p.width, p.height * 2)); q.paste(ImageOps.flip(p), (0, 0)); q.paste(p, (0, p.height)); p = q
    p.save(out + name); print(name, p.size)
