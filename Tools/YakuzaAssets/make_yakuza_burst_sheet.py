"""Yakuza-pomon purkaus (hermostuminen): video 1280x720, 24 fps, vihreä tausta, kiinteä kamera, 121 ruutua.
  Käytetyt ruudut: 8-23 (kyykkyyn lataus), 31-98 (lyöntisarja ja potkut), 107-121 (nousu vartioon).
  Ohitetut: 24-30 ja 99-106 (hahmo hyppää ruudun yläreunan yli, kuva leikkautuu).
Ruudut skaalataan samaan kokoon kuin muut pomon iskut (seisova 629 px), ruutu 896 x 704, jalkojen keskikohta 300 px vasemmalta (pivot x 0,335), jalat 16 px ylös.
Peräkkäiset lähes samanlaiset ruudut (lataus pitää asentoa) yhdistetään; kestot (24 fps:n ruutujen määrä) tulostetaan ja kirjoitetaan .txt-tiedostoon.
Tulos: yakuzapomo_purkaus.png (9 saraketta) ja yakuzapomo_purkaus.txt (ruutujen kestot ruuduissa, pilkuin).
Ajo (projektin juuresta): python Tools/YakuzaAssets/make_yakuza_burst_sheet.py <video.mov> <ulos>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "AttendantAssets"))
from make_attendant_hd import key_frame

STAND_TO, CW, ANCHOR, COLS = 629.0, 896, 300, 9
USE = list(range(8, 24)) + list(range(31, 99)) + list(range(107, 122))   # 1-pohjaiset ruudut

CH, FOOT = 704, 16

def feet(f):
    a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
    low = a[ys.min() + int(h * 0.8):]; _, lx = np.where(low)
    return ys.max() + 1, lx.mean(), ys.max() - ys.min() + 1

def rescale(f, k):
    if abs(k - 1) < 1e-3: return f
    a = f.astype(np.float32); a[..., :3] *= a[..., 3:4] / 255.0
    r = cv2.resize(a, (round(f.shape[1] * k), round(f.shape[0] * k)), interpolation=cv2.INTER_AREA)
    al = r[..., 3:4] / 255.0; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    return r.clip(0, 255).astype(np.uint8)

def cell(f, ax, floor, cw, anchor_x):
    c = np.zeros((CH, cw, 4), np.uint8)
    x0 = int(round(ax)) - anchor_x; y0 = int(round(floor)) - (CH - FOOT)
    sy0, sx0 = max(0, y0), max(0, x0); sy1, sx1 = min(f.shape[0], y0 + CH), min(f.shape[1], x0 + cw)
    if sy1 > sy0 and sx1 > sx0: c[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = f[sy0:sy1, sx0:sx1]
    return c

def save(cells, cols, path):
    rows = (len(cells) + cols - 1) // cols; ch, cw = cells[0].shape[:2]
    img = np.zeros((rows * ch, cols * cw, 4), np.uint8)
    for n, c in enumerate(cells):
        r, k = divmod(n, cols); img[r * ch:(r + 1) * ch, k * cw:(k + 1) * cw] = c
    cv2.imwrite(path, cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA)); print(os.path.basename(path), len(cells), "kuvaa", img.shape[1], "x", img.shape[0])

def main():
    path, out = sys.argv[1:3]
    os.makedirs(out, exist_ok=True)
    cap = cv2.VideoCapture(path); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        fr.append(key_frame(f, keep_effects=True))
    floor0, ax0, h0 = feet(fr[0]); K = STAND_TO / h0
    print("ruutuja", len(fr), "lattia", floor0, "jalkojen keski", round(ax0), "korkeus", h0, "kerroin", round(K, 3))
    cells, durs, src, prev = [], [], [], None
    for n in USE:
        f = fr[n - 1]
        a = f[..., 3].astype(np.float32); small = cv2.resize(f[..., :3], (160, 90)).astype(np.float32)
        if prev is not None and np.abs(small - prev).mean() < 0.35 and durs[-1] < 6:
            durs[-1] += 1; continue
        prev = small
        r = rescale(f, K)
        c = cell(r, ax0 * K, floor0 * K, CW, ANCHOR)
        m = c[..., 3] > 128; yy, xx = np.where(m)
        if xx.min() <= 0 or xx.max() >= CW - 1 or yy.min() <= 0: print("  HUOM leikkautuu ruutu", n, xx.min(), xx.max(), yy.min())
        cells.append(c); durs.append(1); src.append(n)
    print(len(cells), "kuvaa; kestot", durs)
    save(cells, COLS, os.path.join(out, "yakuzapomo_purkaus.png"))
    open(os.path.join(out, "yakuzapomo_purkaus.txt"), "w").write(",".join(map(str, durs)) + "\n" + ",".join(map(str, src)) + "\n")

main()
