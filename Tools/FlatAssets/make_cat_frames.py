"""Kämpän kissa: vihreätaustaisista videoista (kissa idle.mp4, kissa.mp4) kuvasarjat Assets/Resources/Kissa/.

Ajo: python Tools/FlatAssets/make_cat_frames.py "<kissa idle.mp4>" "<kissa.mp4>"
Tulos: idle_NN (siisti/nuoleskelu, silmukka), start_NN (istuu -> nousee kävelylle), walk_NN (paikallaan kävely, silmukka),
stop_NN (kävely -> istuu). Kaikki samassa rajauksessa, joten siirtymät osuvat kohdalleen; pivot = kissan istumapaikka.
"""
import sys, os
import numpy as np
import cv2
import av
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Resources", "Kissa")
BOX = (140, 40, 1000, 700)            # x0, y0, x1, y1 lähdekuvassa (1280x720)
SCALE = 0.3
WALK = (56, 135)                      # tasainen kävelyjakso (kuvat), loppu sekoitetaan alkuun
STOP_FROM = 144
XFADE = 0                           # sekoitus jätti haamujalat -> kova leikkaus


def key(rgb):
    f = rgb.astype(np.float32)
    d = f[..., 1] - np.maximum(f[..., 0], f[..., 2])
    a = np.clip(1.0 - (d - 30.0) / 60.0, 0.0, 1.0)
    a = cv2.GaussianBlur(cv2.erode(a, np.ones((3, 3), np.uint8)), (3, 3), 0)
    f[..., 1] = np.minimum(f[..., 1], np.maximum(f[..., 0], f[..., 2]) + 4.0)
    return np.concatenate([f * a[..., None], a[..., None]], 2)       # esikerrottu RGB + alfa


def load(path):
    x0, y0, x1, y1 = BOX
    return [key(f.to_ndarray(format="rgb24")[y0:y1, x0:x1]) for f in av.open(path).decode(video=0)]


def save(fa, name):
    sm = cv2.resize(fa, (int((BOX[2] - BOX[0]) * SCALE), int((BOX[3] - BOX[1]) * SCALE)), interpolation=cv2.INTER_AREA)
    a = sm[..., 3:4]
    rgb = np.where(a > 1e-3, sm[..., :3] / np.maximum(a, 1e-3), 0)
    im = np.concatenate([np.clip(rgb, 0, 255), a * 255], 2).astype(np.uint8)
    Image.fromarray(im, "RGBA").save(os.path.join(OUT, name + ".png"))


def main(idle_path, walk_path):
    os.makedirs(OUT, exist_ok=True)
    for f in os.listdir(OUT):
        if f.endswith(".png"):
            os.remove(os.path.join(OUT, f))
    idle = load(idle_path)
    for i, j in enumerate(range(0, len(idle), 3)):
        save(idle[j], "idle_%02d" % i)
    w = load(walk_path)
    for i, j in enumerate(range(0, WALK[0], 2)):
        save(w[j], "start_%02d" % i)
    seg = list(range(WALK[0], WALK[1], 2))
    for i, j in enumerate(seg):
        fa = w[j]
        k = i - (len(seg) - XFADE)
        if k >= 0:                         # silmukan saumaan: loppu liukuu alun kuviin
            t = (k + 1) / (XFADE + 1)
            fa = fa * (1 - t) + w[WALK[0] + 2 * k] * t
        save(fa, "walk_%02d" % i)
    for i, j in enumerate(range(STOP_FROM, len(w), 2)):
        save(w[j], "stop_%02d" % i)
    print("ok", len(os.listdir(OUT)), "kuvaa")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
