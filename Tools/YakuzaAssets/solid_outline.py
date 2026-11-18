"""Jakuzan (vihu, Assets/Sprites/yakuza_*.png) ääriviivat: vanhat reunat olivat pehmeät ja osittain läpinäkyvät -> kiinteä tumma ääriviiva.
Alfa kynnetään (>=128 = hahmo), hahmon ympärille tulee LINE px tasainen tumma ääriviiva (täysin peittävä), ulkoreuna pehmennetään vain 1 px (3x suurennos).
Alkuperäiset: Tools/YakuzaAssets/orig_outline/. Ajo: python Tools/YakuzaAssets/solid_outline.py [nimi.png ...]"""
import os, sys, glob, shutil
import numpy as np, cv2
from PIL import Image
SRC = "Assets/Sprites"; BAK = "Tools/YakuzaAssets/orig_outline"; PEEL = 2; DARK_LUMA = 13
os.makedirs(BAK, exist_ok=True)
files = sys.argv[1:] or [os.path.basename(f) for f in sorted(glob.glob(SRC + "/yakuza_*.png"))]
for n in files:
    p = os.path.join(SRC, n); b = os.path.join(BAK, n)
    if not os.path.exists(b): shutil.copy(p, b)
    im = np.array(Image.open(b).convert("RGBA")); H, W = im.shape[:2]
    # ohennus: alkuperäisen reunan musta halo (~4 px, epätarkka) kuoritaan 2 px:ksi: kuoritaan vain lähes mustia reunapikseleitä (luma < DARK_LUMA; vaatteet ja ihon varjot ovat vaaleampia)
    lum = im[..., :3].astype(np.float32) @ np.array([.299, .587, .114], np.float32)
    m = (im[..., 3] >= 128).astype(np.uint8); dark = (lum < DARK_LUMA)
    K3 = np.ones((3, 3), np.uint8)
    for _ in range(PEEL):
        edge = m - cv2.erode(m, K3); rem = (edge > 0) & dark; m[rem] = 0
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, K3); m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, K3)
    rgb = im[..., :3].copy()
    big = cv2.resize(m.astype(np.float32), (W * 3, H * 3), interpolation=cv2.INTER_CUBIC); big = cv2.GaussianBlur(big, (0, 0), 2.4)
    a = cv2.resize(np.clip((big - 0.5) * 2.6 + 0.5, 0, 1), (W, H), interpolation=cv2.INTER_AREA)
    tmp = os.path.join(os.environ.get("TEMP", "."), "so_" + n); Image.fromarray(np.dstack([rgb, (a * 255).astype(np.uint8)]), "RGBA").save(tmp)
    for _ in range(5):   # Unityn tuonti voi lukita tiedoston hetkeksi
        try: shutil.copyfile(tmp, p); break
        except OSError: import time; time.sleep(1)
    print(n, "ok")
