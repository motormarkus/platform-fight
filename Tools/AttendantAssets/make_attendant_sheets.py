"""Lentoemäntä (NPC): yhdestä videosta (idle ruudut 0-44, kävely 60-167, katsoo oikealle) kuvasarjat
Assets/Sprites/Viholliset/lentoemanta_idle.png (ping-pong 39 kuvaa) ja lentoemanta_kavely.png (35 kuvan askelsilmukka); ruutu 768 x 448, hahmo 300 px, jalat 8 px ylös.
Puhevideo (valinnainen 3. argumentti): lentoemanta_puhe.png (joka 4. ruutu, 42 kuvaa; alku ja loppu ovat samaa seisoma-asentoa).
Ajo: python Tools/AttendantAssets/make_attendant_sheets.py <video.mp4> <ulos> [<puhevideo.mp4>]"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "PilotAssets"))
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
import make_pilot_sheets as P
from key_video import key_frame
src, out = sys.argv[1], sys.argv[2]
def load(path):
    cap = cv2.VideoCapture(path); out_f = []
    while True:
        ok, fr = cap.read()
        if not ok: break
        out_f.append(key_frame(fr))
    return out_f
frames = load(src)
ys, xs = np.where(frames[0][..., 3] > 128); FLOOR = ys.max() + 1; K = P.TARGET_H / 590.0
idle_idx = list(range(0, 41, 2)); idle_idx = idle_idx + idle_idx[-2:0:-1]   # edestakaisin
walk_idx = list(range(72, 107))
def sheet(idx, mode, name, fr=None):
    fr = fr or frames; items = []
    for i in idx:
        im = fr[i]; a = np.where(im[..., 3] > 128); y0, y1, x0, x1 = a[0].min(), a[0].max() + 1, a[1].min(), a[1].max() + 1
        crop = im[y0:y1, x0:x1]
        r = P.resize_premult(crop, K); ax = P.anchor_x(crop, mode) * K
        items.append((r, ax, (FLOOR - y0) * K))
    below = max(r.shape[0] - foot for r, _, foot in items)
    lift = max(0, int(np.ceil(below - P.FOOT_MARGIN + 2)))
    P.save_sheet(items, lift, os.path.join(out, name)); print(name, len(items), "kuvaa nosto", lift)
os.makedirs(out, exist_ok=True)
sheet(idle_idx, "low", "lentoemanta_idle.png")
sheet(walk_idx, "torso", "lentoemanta_kavely.png")
if len(sys.argv) > 3:
    talk = load(sys.argv[3])
    sheet(list(range(0, len(talk), 4)), "low", "lentoemanta_puhe.png", talk)
