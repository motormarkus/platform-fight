"""Yakuza-pomon juoksu: video 1280x720, 24 fps, vihreä tausta, 72 ruutua (hahmo juoksee paikallaan, kamera seuraa).
  juoksualku: ruudut 12-36 (vartiosta etukumaraan ja vauhtiin), juoksu: ruudut 37-55 (yksi täysi askelkierros, silmukka; paras sauma etsitty maskien erolla).
Ruudut 640 x 704, hahmon ylävartalon keskikohta ruudun keskellä (pivot 0,5), lattia kiinteä (678 -> jalat 16 px ylös), mittakaava sama kuin muut pomon kuvat (K = 1).
Ajo (projektin juuresta): python Tools/YakuzaAssets/make_yakuza_run_sheets.py <video.mov> <ulos>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "AttendantAssets"))
from make_attendant_hd import key_frame

CW, CH, FOOT, FL, COLS = 640, 704, 16, 678, 6
START = list(range(12, 37)); LOOP = list(range(37, 56))   # 1-pohjaiset ruudut

def tx(f):
    a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
    t = a[ys.min() + int(h * .25):ys.min() + int(h * .6)]; _, x = np.where(t); return x.mean()

def cell(f):
    c = np.zeros((CH, CW, 4), np.uint8)
    x0 = int(round(tx(f))) - CW // 2; y0 = FL - (CH - FOOT)
    sy0, sx0 = max(0, y0), max(0, x0); sy1, sx1 = min(f.shape[0], y0 + CH), min(f.shape[1], x0 + CW)
    c[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = f[sy0:sy1, sx0:sx1]
    m = c[..., 3] > 128; yy, xx = np.where(m)
    if xx.min() <= 0 or xx.max() >= CW - 1 or yy.min() <= 0: print("  HUOM leikkautuu", xx.min(), xx.max(), yy.min())
    return c

def save(cells, path):
    rows = (len(cells) + COLS - 1) // COLS
    img = np.zeros((rows * CH, COLS * CW, 4), np.uint8)
    for n, c in enumerate(cells):
        r, k = divmod(n, COLS); img[r * CH:(r + 1) * CH, k * CW:(k + 1) * CW] = c
    cv2.imwrite(path, cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA)); print(os.path.basename(path), len(cells), "kuvaa", img.shape[1], "x", img.shape[0])

def main():
    path, out = sys.argv[1:3]
    os.makedirs(out, exist_ok=True)
    cap = cv2.VideoCapture(path); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        fr.append(key_frame(f))
    save([cell(fr[n - 1]) for n in START], os.path.join(out, "yakuzapomo_juoksualku.png"))
    save([cell(fr[n - 1]) for n in LOOP], os.path.join(out, "yakuzapomo_juoksu.png"))
main()
