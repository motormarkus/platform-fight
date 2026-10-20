"""Gustav: videoruutujen taustan poisto (vihreä/valkoinen tausta). Tulos: PNG-ruudut kansioon <out>/<nimi>/NNN.png (täysi 1920x1080, RGBA)
ja yhteenveto hahmon rajauslaatikoista (koon tarkistus)."""
import os, sys, glob
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
from key_video import key_frame
UP = "C:/Users/marku/.claude/uploads/6d277d2e-0dfe-4331-9679-b4155f74f179/"
VIDEOS = {"idle": "65fb80f7", "juoksu": "523f882d", "kavely": "6f95f874", "kaatuminen": "3c7dcd53", "potkut": "8ffa0211", "uppercut": "6c57f24a", "lyonnit": "7d4213ed"}
out = sys.argv[1]
ONLY = sys.argv[2].split(",") if len(sys.argv) > 2 else None
for name, pre in VIDEOS.items():
    if ONLY and name not in ONLY: continue
    cap = cv2.VideoCapture(glob.glob(UP + pre + "*")[0]); i = 0
    os.makedirs(f"{out}/{name}", exist_ok=True)
    while True:
        ok, f = cap.read()
        if not ok: break
        rgba = key_frame(f)
        cv2.imwrite(f"{out}/{name}/{i:03d}.png", cv2.cvtColor(rgba, cv2.COLOR_RGBA2BGRA))
        ys, xs = np.where(rgba[..., 3] > 128)
        print(name, i, xs.min(), xs.max(), ys.min(), ys.max(), ys.max() - ys.min())
        i += 1
