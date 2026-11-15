"""Lentoemännän kuvasarjojen siivous (lentoemanta_idle / kavely / puhe, ruutu 512 x 640, 8 saraketta): vihreä reunakehä pois (despill), rosoinen/pehmeä reuna sileäksi + hento
tumma ääriviiva (sama "mustetus" kuin Lokilla: Tools/NokkamiesAssets soften_outline) ja kevyt terävöitys väriin (lähde oli epätarkka, suurennettu video).
Alkuperäiset varmuuskopioidaan kansioon Tools/AttendantAssets/orig/. Ajo: python Tools/AttendantAssets/clean_attendant_sheets.py"""
import os, sys, shutil
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "NokkamiesAssets")); sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_nokkamies_sheets as nk
nk.INK_DEPTH, nk.INK_ERODE = 2.0, 0.7
CW, CH, COLS = 512, 640, 8
SRC = os.path.join(HERE, "..", "..", "Assets", "Sprites", "Viholliset")

def clean_cell(c):
    a = c[..., 3].astype(np.float32) / 255.0
    if a.max() <= 0: return c
    rgb = c[..., :3].astype(np.float32)
    solid = (a > 0.5).astype(np.uint8)
    # reunavyöhyke ~4 px: despill (vihreä ei saa ylittää punaista/sinistä) ja hento tummennus
    band = cv2.dilate((a < 0.98).astype(np.uint8), np.ones((9, 9), np.uint8)) > 0
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    g2 = np.where(band, np.minimum(g, np.maximum(r, b) + 3.0), g)
    rgb = np.dstack([r, g2, b]) * np.where(band, 0.94, 1.0)[..., None]
    # terävöitys (unsharp mask) vain hahmon sisällä; läpinäkyvä alue täytetään lähimmällä näkyvällä värillä, jottei reuna tummu
    dist, lab = cv2.distanceTransformWithLabels(1 - cv2.erode(solid, np.ones((3, 3), np.uint8)), cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL)
    ys, xs = np.nonzero(cv2.erode(solid, np.ones((3, 3), np.uint8)))
    if len(ys):
        idx = np.zeros(lab.max() + 1, np.int64); idx[lab[ys, xs]] = np.arange(len(ys))
        filled = rgb[ys[idx[lab]], xs[idx[lab]]]
    else: filled = rgb
    blur = cv2.GaussianBlur(filled, (0, 0), 1.4)
    sharp = np.clip(filled + 0.75 * (filled - blur), 0, 255)
    rgb = np.where((a > 0.5)[..., None], sharp, filled)
    out = np.dstack([rgb, a * 255.0]).clip(0, 255).astype(np.uint8)
    return nk.soften_outline(out, thick=1.4, ink=True)

def process(name):
    path = os.path.join(SRC, f"lentoemanta_{name}.png")
    bak = os.path.join(HERE, "orig"); os.makedirs(bak, exist_ok=True)
    bp = os.path.join(bak, f"lentoemanta_{name}.png")
    if not os.path.exists(bp): shutil.copy(path, bp)
    im = cv2.cvtColor(cv2.imread(bp, cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)   # aina alkuperäisestä (ajo on toistettavissa)
    H, W = im.shape[:2]; out = np.zeros_like(im); n = 0
    for r in range(H // CH):
        for c in range(W // CW):
            cell = im[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
            if cell[..., 3].max() == 0: continue
            out[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = clean_cell(cell); n += 1
    cv2.imwrite(path, cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA)); print(name, n, "kuvaa")

if __name__ == "__main__":
    for n in ("idle", "kavely", "puhe"): process(n)
