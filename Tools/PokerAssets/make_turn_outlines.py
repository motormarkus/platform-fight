#!/usr/bin/env python3
"""Tekee vuorossa olevan hahmon kultaisen ääriviivan (TurnOutline1..4.png).

Pohjana on TableOpponentsKeyed.png:n alpha. Ääriviiva rajataan hahmokohtaiseen
alueeseen, jotta pöydän reuna ei saa kehystä. Järjestys = pelipaikat 1..4
(Yakuza, prätkäjätkä, puliukko, rokkimimmi). Käyttö: python3 Tools/PokerAssets/make_turn_outlines.py
"""
import numpy as np
from PIL import Image
ART = "Assets/Resources/Poker/Artwork/"
# (x0, y0, x1, y1) lähdekuvan 1280x720 pikseleinä
REGIONS = {1: (262, 30, 600, 262), 2: (690, 30, 1012, 262), 3: (0, 110, 296, 400), 4: (1025, 110, 1280, 400)}
GOLD = np.array([1.0, .78, .18])
src = np.asarray(Image.open(ART + "TableOpponentsKeyed.png"))
a = src[..., 3] > 128
K = src.shape[1] // 1280   # kuva voi olla suurennettu; alueet ja säteet skaalataan
def dil(m, n):
    for _ in range(n):
        d = m.copy(); d[1:] |= m[:-1]; d[:-1] |= m[1:]; d[:, 1:] |= m[:, :-1]; d[:, :-1] |= m[:, 1:]; m = d
    return m
ring = {r: dil(a, r * K) for r in range(1, 11)}
alpha = np.zeros(a.shape, np.float32)
alpha[ring[3] & ~a] = 1.0                           # kiinteä ääriviiva
for r in range(4, 11): alpha[(ring[r] & ~ring[r - 1]) & (alpha == 0)] = 0.55 * (1 - (r - 4) / 7)  # pehmeä hehku
for i, (x0, y0, x1, y1) in REGIONS.items():
    x0, y0, x1, y1 = x0 * K, y0 * K, x1 * K, y1 * K
    m = np.zeros(a.shape, bool); m[y0:y1, x0:x1] = True
    al = alpha * m
    img = np.zeros(a.shape + (4,), np.float32); img[..., :3] = GOLD; img[..., 3] = al
    Image.fromarray((img * 255 + .5).astype(np.uint8), "RGBA").save(ART + f"TurnOutline{i}.png")
    print(i, int((al > 0).sum()))
