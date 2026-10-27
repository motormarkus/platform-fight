"""Yakuza-pomon väistö: yksi kuva (1536 x 1024, läpinäkyvä tausta), 5 asentoa: vartio, takaisinpäin nojaus, syvä taivutus, nousu, vartio.
Kuvat järjestetään riveittäin vasemmalta oikealle (ylärivi 3, alarivi 2). Alarivin hahmot on piirretty ~9 % isompina: skaalataan yhteen kokoon.
Ruutu 1024 x 736 (reaktiokuvien koko), jalkojen keskikohta 560 px vasemmalta, jalat 16 px ylös; sijoitus etujalan ja lattian mukaan.
Tulos: yakuzapomo_vaisto.png (4 saraketta). Ajo (projektin juuresta): python Tools/YakuzaAssets/make_yakuza_dodge_sheet.py <kuva.png> <ulos>"""
import os, sys
import cv2, numpy as np

STAND_TO, CW, CH, FOOT, ANCHOR, COLS = 629.0, 1024, 736, 16, 560, 4

def front_foot(f, rows=40):
    a = f[..., 3] > 128; ys, xs = np.where(a); low = a[ys.max() - rows:]; _, lx = np.where(low)
    return ys.max() + 1, lx.max()

def rescale(f, k):
    a = f.astype(np.float32); a[..., :3] *= a[..., 3:4] / 255.0
    r = cv2.resize(a, (round(f.shape[1] * k), round(f.shape[0] * k)), interpolation=cv2.INTER_AREA)
    al = r[..., 3:4] / 255.0; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    return r.clip(0, 255).astype(np.uint8)

def main():
    path, out = sys.argv[1:3]
    os.makedirs(out, exist_ok=True)
    im = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    a = (im[..., 3] > 40).astype(np.uint8)
    n, lab = cv2.connectedComponents(cv2.dilate(a, np.ones((13, 13), np.uint8)))
    boxes = []
    for i in range(1, n):
        ys, xs = np.where((lab == i) & (a > 0))
        boxes.append((xs.min(), xs.max(), ys.min(), ys.max()))
    assert len(boxes) == 5, len(boxes)
    boxes.sort(key=lambda b: (0 if b[2] < 500 else 1, b[0]))
    frames = []
    for (x0, x1, y0, y1) in boxes:
        f = np.zeros((y1 - y0 + 21, x1 - x0 + 21, 4), np.uint8)
        f[10:10 + y1 - y0 + 1, 10:10 + x1 - x0 + 1] = im[y0:y1 + 1, x0:x1 + 1]
        frames.append((f, y0 >= 500))
    h0 = boxes[0][3] - boxes[0][2] + 1
    K = STAND_TO / h0
    scales = [K * (0.918 if low else 1.0) for _, low in frames]
    cells = []
    for (f, _), s in zip(frames, scales):
        r = rescale(f, s); fl, fx = front_foot(r, int(65 * s))
        # etujalan kärki samaan kohtaan kuin ruudussa 0: kärki = jalkojen keskikohta + 154 px
        ax = ANCHOR + 154; y0 = int(round(fl)) - (CH - FOOT); x0 = int(round(fx)) - ax
        c = np.zeros((CH, CW, 4), np.uint8)
        sy0, sx0 = max(0, y0), max(0, x0); sy1, sx1 = min(r.shape[0], y0 + CH), min(r.shape[1], x0 + CW)
        c[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = r[sy0:sy1, sx0:sx1]
        m = c[..., 3] > 128; yy, xx = np.where(m)
        print("kuva", len(cells), "x", xx.min(), xx.max(), "y", yy.min(), yy.max(), "korkeus", yy.max() - yy.min() + 1)
        cells.append(c)
    rows = (len(cells) + COLS - 1) // COLS
    img = np.zeros((rows * CH, COLS * CW, 4), np.uint8)
    for i, c in enumerate(cells):
        r, k = divmod(i, COLS); img[r * CH:(r + 1) * CH, k * CW:(k + 1) * CW] = c
    cv2.imwrite(os.path.join(out, "yakuzapomo_vaisto.png"), img)
    print("yakuzapomo_vaisto.png", len(cells), "kuvaa")
main()
