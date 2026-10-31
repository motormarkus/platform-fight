"""Amatsoni: jabi ja takasuora (vihreätaustainen 1080p-video, 30 fps). Korvaa vanhat suorat ja koukut.
Ruutu 960 x 768, jalkojen keskikohta 330 px vasemmalta, hahmo skaalataan samaan korkeuteen kuin muut (n. 650 px).
Jabi 9 kuvaa, takasuora 13 kuvaa (osumahetki kuva 3).
Ajo: python Tools/AmazonAssets/make_amazon_jab_cross.py <video.mov> <ulos>"""
import os, sys
import numpy as np
sys.path.insert(0, os.path.dirname(__file__))
import amazon_key as K
from make_amazon_sheets import rescale, feet, cell, save
CW_P, ANCHOR_P, TARGET_H = 960, 330, 650.0
# osumahetki molemmissa kuva 3; pidetyt ojennusruudut jätetty pois, jotta isku näyttää nopealta
CLIPS = {"amatsoni_jabi": [27, 28, 29, 30, 31, 32, 35, 36, 37], "amatsoni_suora2": [37, 38, 39, 40, 41, 42, 47, 48, 49, 50, 51, 52, 53]}
def main():
    v, out = sys.argv[1:3]; os.makedirs(out, exist_ok=True)
    raw = K.load(v)
    ys, _ = np.where(raw[0][..., 3] > 128); k = TARGET_H / float(ys.max() - ys.min())
    print("hahmon korkeus", ys.max() - ys.min(), "kerroin", round(k, 3))
    F = [rescale(f, k) for f in raw]; fl, ax = feet(F[0])
    for name, idx in CLIPS.items():
        save([cell(F[i], ax, fl, CW_P, ANCHOR_P) for i in idx], 4, os.path.join(out, name + ".png"))

# --- koukku (lennättävä nostokoukku): python make_amazon_jab_cross.py hook <video.mov> <ulos> ---
HOOK = [19, 20, 22, 24, 26, 28, 30, 32, 34, 36, 39, 41, 42, 43, 44, 45]   # kuvaväli kasvaa: 1,2,2,3 ruutua/kuva = isku kiihtyy; osumahetki ruutu 39 = kuva 10
def hook(v, out):
    raw = K.load(v)
    ys, _ = np.where(raw[0][..., 3] > 128); k = 0.96 * TARGET_H / float(ys.max() - ys.min())   # 4 % pienemmäksi: käsi ylhäällä mahtuu ruutuun
    F = [rescale(f, k) for f in raw]; fl, ax = feet(F[0])
    save([cell(F[i], ax, fl, CW_P, ANCHOR_P) for i in HOOK], 4, os.path.join(out, "amatsoni_koukku.png"))

if __name__ == "__main__":
    if sys.argv[1] == "hook": hook(sys.argv[2], sys.argv[3])
    else: main()
