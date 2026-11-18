"""Mainarin kaatumiskuvien harmaanvihreä roska (kuvan avainnuksen jäämiä käden takana ja maassa makaavissa kuvissa) pois.
Roska = harmaanvihertävät pikselit (g - r >= 14, g >= b - 8), jotka muodostavat alueen, joka koskettaa läpinäkyvää reunaa: poistetaan alfasta (+ 2 px reunan syönti).
Alkuperäiset: Tools/MinerAssets/orig_junk/. Ajo: python Tools/MinerAssets/clean_gray_junk.py mainari_kaatuminen.png mainari_maahan.png ..."""
import os, sys, shutil
import numpy as np, cv2
from PIL import Image
SRC = "Assets/Sprites/Viholliset"; BAK = "Tools/MinerAssets/orig_junk"
for n in sys.argv[1:]:
    b = os.path.join(BAK, n)
    if not os.path.exists(b): shutil.copy(os.path.join(SRC, n), b)
    im = np.array(Image.open(b).convert("RGBA")); rgb = im[..., :3].astype(np.int16); a = im[..., 3]
    r, g, bl = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    junk = ((((g - r) >= 14) & (g >= bl - 8)) | (((g - bl) >= 24) & ((r - g) <= 10) & (g >= r - 8)) | ((g >= r) & (g >= bl - 4) & ((rgb.max(2) - rgb.min(2)) < 40) & ((0.299*r + 0.587*g + 0.114*bl) > 48) & ((0.299*r + 0.587*g + 0.114*bl) < 112))) & (a > 20)   # harmaanvihreä tai oliivi (maassa pölyä)
    junk = cv2.morphologyEx(junk.astype(np.uint8), cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    transp = (a < 128).astype(np.uint8)
    k, lab, st, _ = cv2.connectedComponentsWithStats(junk, 8)
    near = cv2.dilate(transp, np.ones((9, 9), np.uint8)) > 0
    kill = np.zeros_like(junk)
    for i in range(1, k):
        comp = lab == i
        if st[i, cv2.CC_STAT_AREA] >= 40 and (comp & near).any(): kill |= comp.astype(np.uint8)
    kill = cv2.dilate(kill, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5)))
    out = im.copy(); out[..., 3] = np.where(kill > 0, 0, a)
    # irralliset pikku roiskeet (alle 700 px) pois: hahmo on aina suuri yhtenäinen alue
    ka, kl, ks, _ = cv2.connectedComponentsWithStats((out[..., 3] > 30).astype(np.uint8), 8)
    for i in range(1, ka):
        if ks[i, cv2.CC_STAT_AREA] < 700: out[..., 3][kl == i] = 0
    # pehmeä reuna poistetun alueen ympärillä
    af = cv2.GaussianBlur(out[..., 3].astype(np.float32), (0, 0), 0.8)
    out[..., 3] = np.where(kill > 0, 0, np.maximum(af, 0)).astype(np.uint8) if False else out[..., 3]
    tmp = os.path.join(os.environ.get("TEMP", "."), "cj_" + n); Image.fromarray(out, "RGBA").save(tmp)
    import time
    for _ in range(10):   # Unityn tuonti voi lukita tiedoston hetkeksi
        try: shutil.copyfile(tmp, os.path.join(SRC, n)); break
        except OSError: time.sleep(1.5)
    print(n, "poistettu px:", int(((kill > 0) & (a > 0)).sum()))
