"""Sohvin tanssit festeille (2 videota 1080x1440, 30 fps, 10 s). Käyttö: python make_sohvi_dance.py <ulostulo-kansio>
Tulos: sohvi_tanssi_1.png, _2.png (10 saraketta x 10 riviä, ruutu 320 x 400, joka kolmas ruutu = 10 fps); mittakaava ruudun 0 mukaan (korkeus 300 px).
Video 1: vihreä tausta (key_frame). Video 2: tumma sinivihreä himmenevä tausta -> värisääntö (sinivihreä pois)."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "HelviAssets"))
from key_video import key_frame
from make_helvi_sheets import key, sheet, read_all
U = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
SRC = ["b1fc908e-copy_EEFD9A93-EF10-40B1-BDA1-A5E9E7FFA198.mov", "c9016a65-copy_1419315B-D09F-4E69-B404-4144E4040509.mov"]

def key_teal(bgr):
    r, g, b = bgr[..., 2].astype(int), bgr[..., 1].astype(int), bgr[..., 0].astype(int)
    bg = (g - r > 10) & (b - r > 6)
    alpha = np.where(bg, 0, 255).astype(np.uint8)
    solid = cv2.morphologyEx((alpha > 128).astype(np.uint8), cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid, connectivity=8)
    if n > 2:
        keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); solid = (lab == keep).astype(np.uint8)
    alpha = np.where(cv2.dilate(solid, np.ones((5, 5), np.uint8)) > 0, alpha, 0).astype(np.uint8)
    alpha = cv2.GaussianBlur(alpha, (3, 3), 0)
    rgba = np.dstack([bgr[..., 2], bgr[..., 1], bgr[..., 0], alpha])
    return rgba

def main(out):
    os.makedirs(out, exist_ok=True)
    ref = None
    for n, f in enumerate(SRC, 1):
        frames = read_all(U + f)[::3]
        keyed = [key(x) if n == 1 else key_teal(x) for x in frames]
        if n == 2: keyed[0] = key(frames[0])   # ruutu 0 on vihreällä taustalla
        if ref is None:
            ys = np.where(keyed[0][..., 3] > 128)[0]; ref = float(ys.max() - ys.min()); print("ruudun 0 korkeus", ref)
        print(n, len(keyed), "ruutua")
        sheet(keyed, ref, os.path.join(out, "sohvi_tanssi_%d.png" % n), follow=True)
if __name__ == "__main__": main(sys.argv[1])
