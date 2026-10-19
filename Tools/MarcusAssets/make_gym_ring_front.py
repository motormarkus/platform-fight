"""Salin kuvan 3 kehän etuköydet ja etutolpat omaksi kuvakseen (sali_boxing_3_etu.png, sama koko 1536 x 1024), jotta salin kehässä
varjonyrkkeilevä Marcus jää köysien taakse. Alueet mitattu kuvasta."""
import cv2
import numpy as np
SRC = "Assets/Sprites/Taustat/sali_boxing_3.png"
DST = "Assets/Sprites/Taustat/sali_boxing_3_etu.png"
im = cv2.imread(SRC, cv2.IMREAD_UNCHANGED)[..., :3]
H, W = im.shape[:2]
m = np.zeros((H, W), np.float32)

def rect(x0, y0, x1, y1, r=0):
    t = np.zeros((H, W), np.float32)
    cv2.rectangle(t, (x0 + r, y0), (x1 - r, y1), 1.0, -1)
    cv2.rectangle(t, (x0, y0 + r), (x1, y1 - r), 1.0, -1)
    for cx, cy in ((x0 + r, y0 + r), (x1 - r, y0 + r), (x0 + r, y1 - r), (x1 - r, y1 - r)):
        cv2.circle(t, (cx, cy), r, 1.0, -1)
    return t

m = np.maximum(m, rect(474, 293, 507, 494, 3))        # vasen etutolppa (punainen)
m = np.maximum(m, rect(1441, 293, 1473, 494, 3))      # oikea etutolppa (sininen)
# köydet (rivit) ja tyynyt (rivit); tyynyt vasemmalla x 539-578, oikealla x 1391-1432
for (r0, r1), (c0, c1) in (((320, 328), (311, 334)), ((372, 381), (366, 387)), ((429, 437), (421, 443))):
    m = np.maximum(m, rect(570, r0, 1400, r1, 0))
    m = np.maximum(m, rect(539, c0, 578, c1, 8))
    m = np.maximum(m, rect(1391, c0, 1432, c1, 8))
a = cv2.GaussianBlur(m, (0, 0), 0.8)
cv2.imwrite(DST, np.dstack([im, (a * 255).astype(np.uint8)]))
bg = np.full(im.shape, (255, 0, 255), np.uint8)
pv = (im * a[..., None] + bg * (1 - a[..., None])).astype(np.uint8)
cv2.imwrite(r"C:/Users/marku/AppData/Local/Temp/claude/C--Users-marku--claude-spares-spare-HJa4I0/3a0bc6ca-2c02-49e6-9370-0e4a65bef55c/scratchpad/gym_front_prev.jpg", cv2.resize(pv[250:520], None, fx=0.7, fy=0.7))
print("ok")
