"""Roccon lumilautailu, uudet videot (vihreä tausta): idle + kaksi temppua.
Ajo: python Tools/SnowAssets/make_board_tricks.py <idle.mp4> <temppu1.mp4> <temppu2.mp4> <ulos-kansio>
Tuottaa rocco_lautailu_idle|temppu1|temppu2.png (4 saraketta, ruutu 768 x 448, maan linja 32 px ruudun alareunasta kaikissa, hahmo katsoo oikealle).
Ruudun ankkuri: idle = laudan pohja jalkalinjalla; tempuissa vartalon painopiste (video liikkuu itse ylös/alas, laudan pohja kiertyy), maalinja videon alkuruudun laudan pohja.
360p-video (temppu 2) suurennetaan 2 x, 720p-videot normaalisti."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "PilotAssets"))
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
import make_pilot_sheets as P
from key_video import key_frame

K720 = P.TARGET_H / 590.0
LIFT = 24   # maan linja = FOOT_MARGIN + LIFT = 32 px ruudun alareunasta


def load(path, a, b):
    cap = cv2.VideoCapture(path); fr = []; i = 0
    h = int(cap.get(4))
    while True:
        ok, f = cap.read()
        if not ok: break
        if a <= i <= b: fr.append(key_frame(f, cyan_safe=True))
        i += 1
    return fr, (2.0 if h < 500 else 1.0) * K720


def ymax(im): return np.where(im[..., 3] > 128)[0].max()


def item(im, floor, k, mode):
    a = np.where(im[..., 3] > 128); y0, y1, x0, x1 = a[0].min(), a[0].max() + 1, a[1].min(), a[1].max() + 1
    crop = im[y0:y1, x0:x1]
    r = P.resize_premult(crop, k); ax = P.anchor_x(crop, mode) * k
    return (r, ax, (floor - y0) * k)


def sheet(items, name, out):
    below = max(r.shape[0] - foot for r, _, foot in items)
    assert below < P.FOOT_MARGIN + LIFT, (name, below)
    P.save_sheet(items, LIFT, os.path.join(out, name)); print(name, len(items), "kuvaa, alin kohta", round(below, 1))


def main(vi, vt1, vt2, out):
    os.makedirs(out, exist_ok=True)
    fr, k = load(vi, 0, 119)
    floor = np.median([ymax(f) for f in fr])
    sheet([item(fr[i], floor, k, "low") for i in range(0, 120, 3)], "rocco_lautailu_idle.png", out)
    for path, a, b, nm in ((vt1, 4, 96, "temppu1"), (vt2, 12, 92, "temppu2")):
        fr, k = load(path, a, b)
        floor = np.median([ymax(f) for f in fr[:3]])
        sheet([item(fr[i], floor, k, "com") for i in range(0, len(fr), 2)], "rocco_lautailu_%s.png" % nm, out)


if __name__ == "__main__":
    main(*sys.argv[1:5])
