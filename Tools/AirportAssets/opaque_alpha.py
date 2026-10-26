"""Taustakuvien runko-alfa: monissa kuvissa peittävä alue on alfalla 250-254 (ei 255), jolloin parallaksi ja taivas kuultavat heikosti läpi.
Asettaa alfan >= 245 arvoon 255 kuvissa, joissa tällaisia pikseleitä on yli 1 %. Vain thaitown_*-kuvat. Ajo: python Tools/AirportAssets/opaque_alpha.py [--dry]"""
import glob, os, sys, numpy as np, cv2
d = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
dry = "--dry" in sys.argv; n = 0
for f in sorted(glob.glob(os.path.join(d, "thaitown_*.png"))):
    im = cv2.imread(f, cv2.IMREAD_UNCHANGED)
    if im is None or im.ndim < 3 or im.shape[2] != 4: continue
    al = im[..., 3]; m = (al >= 245) & (al < 255)
    if m.mean() < 0.01: continue
    n += 1; print(os.path.basename(f), round(float(m.mean()) * 100, 1), "%")
    if not dry: al[m] = 255; cv2.imwrite(f, im)
print("tiedostoja:", n)
