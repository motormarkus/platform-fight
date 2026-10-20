"""Candin kuvien siivous (jälkikäsittely valmiille arkeille): vihreän/keltavihreän taustan jäänteet pois, pienet irtonaiset palat pois,
puoliläpinäkyvien reunojen väri korvataan lähimmillä peitteisillä väreillä (tukan hohto pois) ja reuna terävöitetään hieman.
Käyttö: python clean_candi.py <kansio jossa candi_1..3.png>  (ruutu 320 x 400, 10 saraketta)"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "GustavAssets"))
from split_sheets import defringe
CW, CH, COLS = 320, 400, 10

def clean_cell(c):
    a = c[..., 3]
    if a.max() == 0: return c
    hsv = cv2.cvtColor(np.ascontiguousarray(c[..., :3]), cv2.COLOR_BGR2HSV).astype(np.float32)   # H 0..180
    hue, sat = hsv[..., 0] * 2.0, hsv[..., 1] / 255.0
    bad = (a > 0) & (hue >= 45) & (hue <= 110) & (sat > 0.18)                                      # keltavihreä / vihreä (Candilla ei ole)
    b, g, r = [c[..., i].astype(int) for i in range(3)]
    bad |= (a > 0) & (a < 250) & (g > r + 4) & (g > b + 4)                                          # reunan vihreä sävy
    out = c.copy(); out[bad, 3] = 0
    m = (out[..., 3] > 20).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(cv2.dilate(m, np.ones((3, 3), np.uint8)), connectivity=8)
    if n > 2:
        big = st[1:, cv2.CC_STAT_AREA].max()
        for i in range(1, n):
            if st[i, cv2.CC_STAT_AREA] < big * 0.02: out[lab == i, 3] = 0                        # irtonaiset roskat
    out = defringe(out, solid=235)                                                                  # reunojen väri sisäpuolelta
    a2 = out[..., 3].astype(np.float32)
    out[..., 3] = np.clip((a2 - 40.0) / (255.0 - 40.0) * 255.0, 0, 255).astype(np.uint8)         # reuna terävämmäksi (pehmeä hohto pois)
    return out

def main(folder):
    for n in (1, 2, 3):
        p = os.path.join(folder, f"candi_{n}.png")
        im = cv2.imread(p, cv2.IMREAD_UNCHANGED); H, W = im.shape[:2]
        out = im.copy()
        for y in range(0, H, CH):
            for x in range(0, W, CW):
                out[y:y + CH, x:x + CW] = clean_cell(im[y:y + CH, x:x + CW])
        cv2.imwrite(p, out); print(p, im.shape)

if __name__ == "__main__":
    main(sys.argv[1])
