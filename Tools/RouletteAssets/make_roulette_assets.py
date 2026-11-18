"""Rulettikuvat -> Assets/Resources/Roulette/: panel.png (pöytä+napit, ruletti_lahde.png:stä), wheel_frame.png ja wheel_disc.png (pyörä suoraan ylhäältä,
output/imagegen/vegas-rulettirulla-ylhaalta), chip_*.png (ei käytössä tällä hetkellä)."""
import os
import numpy as np, cv2
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = os.path.join(HERE, "..", "..", "Assets", "Resources", "Roulette")
os.makedirs(OUT, exist_ok=True)
src = Image.open(os.path.join(HERE, "ruletti_lahde.png")).convert("RGBA")
panel = src.crop((1636, 446, 3744, 1674)); panel.save(os.path.join(OUT, "panel.png"))
# pyörä ylhäältä (käyttäjän uudet kuvat): kiinteä runko + pyörivä numerolevy (keskipiste 767,5, ulkosäde 539 px)
import shutil
WH = os.path.join(HERE, "..", "..", "output", "imagegen", "vegas-rulettirulla-ylhaalta")
shutil.copy(os.path.join(WH, "rulettirulla-kiintea-runko.png"), os.path.join(OUT, "wheel_frame.png"))
shutil.copy(os.path.join(WH, "rulettirulla-pyoriva-numerolevy.png"), os.path.join(OUT, "wheel_disc.png"))
# pelimerkit (keskipisteet paneelikuvan pikseleinä)
p = np.array(panel)
for name, cx in (("1", 169), ("5", 326), ("25", 486), ("100", 643)):
    cy, r = 1063, 58
    c = p[cy - r:cy + r, cx - r:cx + r].copy()
    yy, xx = np.mgrid[0:2 * r, 0:2 * r].astype(np.float32)
    d = np.sqrt((xx - r + .5) ** 2 + (yy - r + .5) ** 2)
    c[..., 3] = (np.clip((r - 2.5 - d) / 2.0, 0, 1) * 255).astype(np.uint8)
    Image.fromarray(c, "RGBA").save(os.path.join(OUT, "chip_%s.png" % name))
print("ok", os.listdir(OUT))
