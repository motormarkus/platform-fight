"""Wardenin sheetit: hento tumma ääriviiva (ulkoreuna 3 px, alfa 0,6) ja reunan sisäpuolen tummennus, ettei hahmo näytä irralliselta taustasta.
Lähtökuvat: Tools/PrisonAssets/alkuperaiset/warden_*.png (kopioidaan ensimmäisellä ajolla); tulos kirjoitetaan Assets/Sprites/Viholliset/warden_*.png:iin.
Ajo: python Tools/PrisonAssets/warden_outline.py"""
import os, glob, shutil, numpy as np
from PIL import Image, ImageFilter
S = "Assets/Sprites/Viholliset"; O = "Tools/PrisonAssets/alkuperaiset"
OUT_PX = 7        # MaxFilter-koko (7 = 3 px ulos)
IN_PX = 5         # MinFilter-koko (2 px sisään)
OUT_ALPHA = 0.6; IN_DARK = 0.28
os.makedirs(O, exist_ok=True)
for f in sorted(glob.glob(os.path.join(S, "warden_*.png"))):
    n = os.path.basename(f); bak = os.path.join(O, n)
    if not os.path.exists(bak): shutil.copy2(f, bak)
    im = Image.open(bak).convert("RGBA"); a = im.getchannel("A")
    dil = np.array(a.filter(ImageFilter.MaxFilter(OUT_PX)), float) / 255.0
    ero = np.array(a.filter(ImageFilter.MinFilter(IN_PX)), float) / 255.0
    A = np.array(a, float) / 255.0
    rgb = np.array(im, float)[:, :, :3]
    edge = np.clip(A - ero, 0, 1)[:, :, None]
    rgb = rgb * (1 - IN_DARK * edge)                                  # sisäreuna tummemmaksi
    ring = np.clip(dil - A, 0, 1)                                      # ulkorengas
    out_a = np.clip(A + ring * OUT_ALPHA * (1 - A), 0, 1)
    col = np.array([18.0, 16.0, 22.0])
    # väri: sprite päällä, rengas tummaa (alfa-painotettu)
    w_sp = (A / np.maximum(out_a, 1e-6))[:, :, None]
    res_rgb = rgb * w_sp + col * (1 - w_sp)
    res = np.concatenate([np.clip(res_rgb, 0, 255), (out_a * 255)[:, :, None]], axis=2).astype(np.uint8)
    Image.fromarray(res, "RGBA").save(f)
    print(n, "ok")
