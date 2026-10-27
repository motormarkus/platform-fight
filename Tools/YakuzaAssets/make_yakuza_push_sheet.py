"""Yakuza-pomon tönäisy: video 1920x1080, ~26,5 fps, vihreä tausta, kiinteä kamera, 30 ruutua.
  Käytetyt ruudut: 4-16 (asento vartiosta, kädet eteen, tönäisy ojennuksessa). Ruudut 17-30 ovat samaa ojennusta (ohitetaan);
  palautus tehdään koodissa soittamalla aiempia ruutuja takaperin.
Ruudut skaalataan samaan kokoon kuin muut pomon iskut (seisova 629 px), ruutu 896 x 704, jalkojen keskikohta 300 px vasemmalta, jalat 16 px ylös.
Tulos: yakuzapomo_tonaisy.png (4 saraketta). Ajo (projektin juuresta): python Tools/YakuzaAssets/make_yakuza_push_sheet.py <video.mov> <ulos>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "AttendantAssets"))
from make_attendant_hd import key_frame

STAND_TO, CW, CH, FOOT, ANCHOR, COLS = 629.0, 896, 704, 16, 300, 4
USE = list(range(4, 17))   # 1-pohjaiset ruudut

def feet(f):
    a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
    low = a[ys.min() + int(h * 0.8):]; _, lx = np.where(low)
    return ys.max() + 1, lx.mean(), ys.max() - ys.min() + 1

def front_foot(f):
    """Alin pikseli ja etujalan oikeanpuoleisin kohta (jalat pysyvät paikallaan; videon rajaus heilahtaa ruuduissa 15-16)."""
    a = f[..., 3] > 128; ys, xs = np.where(a); low = a[ys.max() - 40:]; _, lx = np.where(low)
    return ys.max() + 1, lx.max()

def rescale(f, k):
    a = f.astype(np.float32); a[..., :3] *= a[..., 3:4] / 255.0
    r = cv2.resize(a, (round(f.shape[1] * k), round(f.shape[0] * k)), interpolation=cv2.INTER_AREA)
    al = r[..., 3:4] / 255.0; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    return r.clip(0, 255).astype(np.uint8)

def cell(f, ax, floor):
    c = np.zeros((CH, CW, 4), np.uint8)
    x0 = int(round(ax)) - ANCHOR; y0 = int(round(floor)) - (CH - FOOT)
    sy0, sx0 = max(0, y0), max(0, x0); sy1, sx1 = min(f.shape[0], y0 + CH), min(f.shape[1], x0 + CW)
    c[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = f[sy0:sy1, sx0:sx1]
    return c

def main():
    path, out = sys.argv[1:3]
    os.makedirs(out, exist_ok=True)
    cap = cv2.VideoCapture(path); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        fr.append(key_frame(f[110:971, 194:1727]))   # videossa mustat reunat: vain vihreä alue
    floor0, ax0, h0 = feet(fr[0]); K = STAND_TO / h0
    print("ruutuja", len(fr), "lattia", floor0, "jalkojen keski", round(ax0), "korkeus", h0, "kerroin", round(K, 3))
    cells = []
    fl0, fx0 = front_foot(fr[USE[0] - 1])
    for n in USE:
        fl, fx = front_foot(fr[n - 1])   # sijoitus etujalan ja lattian mukaan: ei hyppäyksiä
        c = cell(rescale(fr[n - 1], K), (ax0 + fx - fx0) * K, (floor0 + fl - fl0) * K)
        m = c[..., 3] > 128; yy, xx = np.where(m)
        if xx.min() <= 0 or xx.max() >= CW - 1 or yy.min() <= 0: print("  HUOM leikkautuu ruutu", n, xx.min(), xx.max(), yy.min())
        cells.append(c)
    rows = (len(cells) + COLS - 1) // COLS
    img = np.zeros((rows * CH, COLS * CW, 4), np.uint8)
    for i, c in enumerate(cells):
        r, k = divmod(i, COLS); img[r * CH:(r + 1) * CH, k * CW:(k + 1) * CW] = c
    cv2.imwrite(os.path.join(out, "yakuzapomo_tonaisy.png"), cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA))
    print("yakuzapomo_tonaisy.png", len(cells), "kuvaa", img.shape[1], "x", img.shape[0])
main()
