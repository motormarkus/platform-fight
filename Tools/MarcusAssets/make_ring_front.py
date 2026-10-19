"""Kehän etuköydet ja etutolpat omaksi kuvakseen (sali_kehakuva_etu.png, sama koko kuin sali_kehakuva.png), jotta ne piirtyvät
hahmojen EDESSÄ (hahmot ovat kehän sisällä etuköysien takana). Alueet mitattu kuvasta (1672 x 941)."""
import cv2
import numpy as np

SRC = "Assets/Sprites/Taustat/sali_kehakuva.png"
DST = "Assets/Sprites/Taustat/sali_kehakuva_etu.png"
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

# etutolpat (vasen punainen, oikea sininen)
m = np.maximum(m, rect(49, 402, 105, 832, 3))
m = np.maximum(m, rect(1567, 402, 1628, 832, 3))
# kolme etuköyttä: (köyden rivit, tyynyjen rivit)
for (r0, r1), (c0, c1) in (((439, 454), (425, 468)), ((524, 537), (507, 551)), ((605, 620), (589, 634))):
    m = np.maximum(m, rect(100, r0, 1570, r1, 0))
    m = np.maximum(m, rect(103, c0, 183, c1, 9))        # vasen tyyny
    m = np.maximum(m, rect(1490, c0, 1566, c1, 9))      # oikea tyyny
a = cv2.GaussianBlur(m, (0, 0), 0.8)
out = np.dstack([im, (a * 255).astype(np.uint8)])
cv2.imwrite(DST, out)
bg = np.full(im.shape, (255, 0, 255), np.uint8)
pv = (im * a[..., None] + bg * (1 - a[..., None])).astype(np.uint8)
cv2.imwrite(r"C:/Users/marku/AppData/Local/Temp/claude/C--Users-marku--claude-spares-spare-HJa4I0/3a0bc6ca-2c02-49e6-9370-0e4a65bef55c/scratchpad/ring_front_prev.jpg", cv2.resize(pv, None, fx=0.6, fy=0.6))
print("ok", DST)
