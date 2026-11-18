"""Khanin lohikäärmeenhenkäyksen liekkipalat: videon C (kuvat 100-170) suusta oikealle lentävästä tulesta pehmeäreunaisia läiskiä (khan_liekki_N.png).
Alfa = oranssi/keltainen/valkoinen vs vihreä tausta (r - 0.8 g), tumma savu ja vihreä hohto jäävät pois; reuna häivytetään ellipsillä, jotta palat sulautuvat toisiinsa.
Ajo: python Tools/KhanAssets/make_khan_fx.py <C.mp4>"""
import os, sys, glob
import av, numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Khan")
SCALE = 0.75; X0, X1, Y0, Y1 = 990, 1275, 190, 545
raw = [f.to_ndarray(format="rgb24") for f in av.open(sys.argv[1]).decode(video=0)]
for old in glob.glob(os.path.join(OUT, "khan_liekki_*.png")): os.remove(old)
n = 0
for fi in range(100, 170, 8):
    a = raw[fi][Y0:Y1, X0:X1].astype(np.float32); r, g, b = a[..., 0], a[..., 1], a[..., 2]
    al = np.clip((r - 0.8 * g + 25) / 85.0, 0, 1)
    h, w = al.shape; yy, xx = np.mgrid[0:h, 0:w]
    d = ((xx - w / 2) / (w / 2)) ** 2 + ((yy - h / 2) / (h / 2)) ** 2
    mask = np.clip((1.0 - d) / 0.45, 0, 1); mask = mask * mask * (3 - 2 * mask)
    al = al * mask
    gg = np.minimum(g, r * 0.95); b = np.minimum(b, gg * 0.62)
    rgba = np.dstack([r, gg, b, al * 255]).clip(0, 255).astype(np.uint8)
    im = Image.fromarray(rgba, "RGBA"); im = im.resize((int(w * SCALE) // 4 * 4, int(h * SCALE) // 4 * 4), Image.LANCZOS)
    im.save(os.path.join(OUT, "khan_liekki_%d.png" % n)); n += 1
print(n, "liekkipalaa", im.size)
