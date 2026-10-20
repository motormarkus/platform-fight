"""Tomin (salin puntti) videoruutujen taustan poisto. Vihreä tausta: Tools/GymAssets/key_video.key_frame; tomin_pose-video muuttuu
vihreästä mustaksi (ruutu 7 alkaen): musta tausta poistetaan reunoista (hahmon mustaa ääriviivaa ei saa syödä).
Tulos: PNG-ruudut <out>/<nimi>/NNN.png (RGBA, alkuperäinen koko)."""
import os, sys, glob
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GymAssets"))
from key_video import key_frame
UP = "C:/Users/marku/.claude/uploads/6d277d2e-0dfe-4331-9679-b4155f74f179/"
VIDEOS = {"idle_pose": "3814d928", "lyonnit": "9d81fb80", "kavely": "1422af72"}

def key_black(bgr):
    """Musta tausta: reunoista yhteydessä oleva lähes musta alue pois; hahmon ääriviiva säilyy 2 px:n reunalla."""
    g = bgr.max(axis=2)
    near = (g < 28).astype(np.uint8)
    n, lab = cv2.connectedComponents(near, connectivity=4)
    edge = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])).tolist()) - {0}
    bg = np.isin(lab, list(edge)).astype(np.uint8)
    # paksut mustat alueet hahmon sisällä (käsivarren ja vartalon välissä) ovat taustaa; ohut ääriviiva ei kestä avausta
    thick = cv2.morphologyEx(near, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (11, 11)))
    bg |= cv2.dilate(thick, np.ones((3, 3), np.uint8))
    # ääriviivan kapea reuna takaisin: bg-alue kutistetaan 2 px
    bg = cv2.erode(bg, np.ones((3, 3), np.uint8), iterations=2)
    # vain suurin yhtenäinen hahmo
    fg = (1 - bg).astype(np.uint8)
    n2, lab2, st, _ = cv2.connectedComponentsWithStats(fg, connectivity=8)
    k = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
    a = (lab2 == k).astype(np.float32)
    a = cv2.GaussianBlur(a, (0, 0), 0.8)
    return np.dstack([bgr[..., 2], bgr[..., 1], bgr[..., 0], (a * 255).astype(np.uint8)])

def is_green(bgr):
    b = np.concatenate([bgr[:8].reshape(-1, 3), bgr[-8:].reshape(-1, 3)]); m = np.median(b, axis=0)
    return m[1] > 120 and m[1] - max(m[0], m[2]) > 60

if __name__ == "__main__":
    out = sys.argv[1]; only = sys.argv[2].split(",") if len(sys.argv) > 2 else None
    for name, pre in VIDEOS.items():
        if only and name not in only: continue
        cap = cv2.VideoCapture(glob.glob(UP + pre + "*")[0]); i = 0
        os.makedirs(f"{out}/{name}", exist_ok=True)
        while True:
            ok, f = cap.read()
            if not ok: break
            rgba = key_frame(f, cyan_safe=True) if is_green(f) else key_black(f)
            cv2.imwrite(f"{out}/{name}/{i:03d}.png", cv2.cvtColor(rgba, cv2.COLOR_RGBA2BGRA))
            ys, xs = np.where(rgba[..., 3] > 128)
            print(name, i, xs.min(), xs.max(), ys.min(), ys.max())
            i += 1
