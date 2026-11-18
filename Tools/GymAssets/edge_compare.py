"""Vertailukuva: vanha (orig_edges) vasemmalla, uusi oikealla; ensimmäisen ruudun hahmon yläosa suurennettuna.
Ajo: python Tools/GymAssets/edge_compare.py <orig_edges-kansio> <arkki.png> <ruudun leveys> <ruudun korkeus> <ulos.png> [korkeus 330]"""
import sys
import numpy as np
from PIL import Image
src, name, cw, ch, out = sys.argv[1], sys.argv[2], int(sys.argv[3]), int(sys.argv[4]), sys.argv[5]
H = int(sys.argv[6]) if len(sys.argv) > 6 else 330
def fig(p):
    im = Image.open(p).convert("RGBA").crop((0, 0, cw, ch)); a = np.asarray(im)[..., 3]; ys, xs = np.nonzero(a > 20)
    return im.crop((xs.min() - 8, ys.min() - 8, xs.max() + 8, ys.min() + H))
a = fig(src + "/" + name); b = fig("Assets/Sprites/Viholliset/" + name)
s = 600 / max(a.height, b.height); a = a.resize((int(a.width * s), int(a.height * s)), Image.BICUBIC); b = b.resize((int(b.width * s), int(b.height * s)), Image.BICUBIC)
o = Image.new("RGBA", (a.width + b.width + 20, max(a.height, b.height)), (235, 220, 200, 255)); o.alpha_composite(a, (0, 0)); o.alpha_composite(b, (a.width + 20, 0)); o.save(out)
