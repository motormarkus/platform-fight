"""Sohvalle istuminen: vihreätaustaisista videoista (sini istuu sohvalle.mp4, rocco istuu sohvallle.mp4) kuvasarjat
Assets/Resources/Kampa/sini_sit_NN ja rocco_sit_NN. Mittakaava: Sinin seisova hahmo = peli-Sinin idle-kuvan korkeus (361 px),
Rocco samalla kertoimella (videoiden keskinäinen suhde säilyy). Pivot = seisovan hahmon jalkojen keskikohta (tulostetaan).

Ajo: python Tools/FlatAssets/make_sofa_frames.py "<sini.mp4>" "<rocco.mp4>"
"""
import sys, os
import numpy as np
import cv2
import av
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Resources", "Kampa")
GAME_SINI_PX = 361.0
JOBS = [("sini", 180, 3), ("rocco", 96, 2)]    # nimi, viimeinen kuva, otanta


def key(rgb):
    f = rgb.astype(np.float32)
    d = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = np.clip(1.0 - (d - 30.0) / 60.0, 0.0, 1.0)
    a = cv2.GaussianBlur(cv2.erode(a, np.ones((3, 3), np.uint8)), (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4.0)
    return np.concatenate([f * a[..., None], a[..., None]], 2)


def bbox(a):
    ys, xs = np.where(a > 0.5)
    return xs.min(), xs.max(), ys.min(), ys.max()


def main(paths):
    os.makedirs(OUT, exist_ok=True)
    fr = {n: [key(f.to_ndarray(format="rgb24")) for f in av.open(p).decode(video=0)] for (n, _, _), p in zip(JOBS, paths)}
    x0, x1, y0, y1 = bbox(fr["sini"][0][..., 3])
    k = GAME_SINI_PX / float(y1 - y0)
    print("kerroin", round(k, 4))
    for name, last, step in JOBS:
        seq = fr[name][: last + 1: step]
        bx0, bx1, by0, by1 = bbox(fr[name][0][..., 3])
        ux0 = min(bbox(f[..., 3])[0] for f in seq); ux1 = max(bbox(f[..., 3])[1] for f in seq)
        uy0 = min(bbox(f[..., 3])[2] for f in seq); uy1 = max(bbox(f[..., 3])[3] for f in seq)
        m = 6
        cx0, cx1, cy0, cy1 = max(0, ux0 - m), min(1279, ux1 + m), max(0, uy0 - m), min(719, uy1 + m)
        for f in os.listdir(OUT):
            if f.startswith(name + "_sit_"):
                os.remove(os.path.join(OUT, f))
        for i, fa in enumerate(seq):
            c = fa[cy0:cy1 + 1, cx0:cx1 + 1]
            sm = cv2.resize(c, (int(c.shape[1] * k), int(c.shape[0] * k)), interpolation=cv2.INTER_AREA)
            a = sm[..., 3:4]
            rgb = np.where(a > 1e-3, sm[..., :3] / np.maximum(a, 1e-3), 0)
            Image.fromarray(np.concatenate([np.clip(rgb, 0, 255), a * 255], 2).astype(np.uint8), "RGBA").save(os.path.join(OUT, "%s_sit_%02d.png" % (name, i)))
        w, h = int((cx1 - cx0 + 1) * k), int((cy1 - cy0 + 1) * k)
        px = ((bx0 + bx1) / 2.0 - cx0) * k / w
        py = 1.0 - (by1 - cy0) * k / h
        print(name, len(seq), "kuvaa", w, "x", h, "pivot", round(px, 4), round(py, 4), "seisova korkeus px", round((by1 - by0) * k))


if __name__ == "__main__":
    main(sys.argv[1:3])
