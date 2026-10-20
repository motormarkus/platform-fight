"""Roccon uudet iskut ja alasväistö videoruuduista (Rocco_170_frames.zip, 910 x 512, 30 fps, läpinäkyvä tausta).
Käyttö: python Tools/RoccoAssets/make_rocco_sheets.py <kansio jossa rocco_0001.png ... rocco_0170.png>
Tulos: Assets/Sprites/rocco_etukoukku.png, rocco_takasivukoukku.png, rocco_etusivukoukku.png, rocco_alasvaisto.png
(4 saraketta, ruutu 512 x 384 kuten muut Roccon kuvat, ylärivi ensin). Hahmon korkeus normalisoidaan Roccon vartioasentoon (332 px),
jalkojen alareuna ruudun riville 374, x = jalkojen/lantion keskikohta (pehmennetty) ruudun x 256.
Ruudut (zipin numerointi): D alasväistö 73–95, G etukäden koukku 129–149, H takakäden sivukoukku 149–165, I etukäden sivukoukku 165–170 (+ paluu vartioon)."""
import os
import sys
import cv2
import numpy as np

CELL_W, CELL_H, COLS = 512, 384, 4
GUARD_H, FEET_ROW, ANCHOR_X = 332.0, 374, 256.0

ANIMS = [
    ("rocco_alasvaisto",      [73, 75, 77, 79, 81, 83, 85, 87, 89, 91, 93, 95]),
    ("rocco_etukoukku",       [129, 131, 133, 135, 137, 139, 141, 143, 145, 147, 149]),
    ("rocco_takasivukoukku",  [149, 151, 153, 155, 157, 159, 161, 163, 165, 167]),
    ("rocco_etusivukoukku",   [165, 166, 167, 168, 169, 170, 1]),   # viimeinen = paluu vartioasentoon
]


def resize_premult(rgba, w, h):
    f = rgba.astype(np.float32)
    a = f[..., 3:4] / 255.0
    f[..., :3] *= a
    f = cv2.resize(f, (w, h), interpolation=cv2.INTER_AREA)
    a2 = f[..., 3:4] / 255.0
    f[..., :3] = np.where(a2 > 1e-3, f[..., :3] / np.maximum(a2, 1e-3), 0)
    return f.clip(0, 255).astype(np.uint8)


def load(folder, n):
    im = cv2.imread(os.path.join(folder, "rocco_%04d.png" % n), cv2.IMREAD_UNCHANGED)
    return cv2.cvtColor(im, cv2.COLOR_BGRA2RGBA)


def bbox(im):
    ys, xs = np.where(im[..., 3] > 40)
    return xs.min(), xs.max() + 1, ys.min(), ys.max() + 1


def hip_x(im):
    """Alavartalon (jalat ja lantio) keskikohta: pysyy ruudun keskellä, joten iskut eivät liu'u sivusuunnassa."""
    x0, x1, y0, y1 = bbox(im)
    a = im[..., 3] > 128
    h = y1 - y0
    ys, xs = np.where(a[int(y0 + h * 0.55):y1])
    return xs.mean()


def main(folder, out_dir="Assets/Sprites"):
    ref = load(folder, 1)
    rx0, rx1, ry0, ry1 = bbox(ref)
    scale = GUARD_H / (ry1 - ry0)
    ground = ry1
    for name, frames in ANIMS:
        ims = [load(folder, n) for n in frames]
        hx = np.array([hip_x(im) for im in ims])
        if len(hx) >= 5:
            hx = np.array([np.median(hx[max(0, i - 2):i + 3]) for i in range(len(hx))])
        rows = (len(ims) + COLS - 1) // COLS
        sheet = np.zeros((rows * CELL_H, COLS * CELL_W, 4), np.uint8)
        for n, im in enumerate(ims):
            x0, x1, y0, y1 = bbox(im)
            crop = im[y0:y1, x0:x1]
            sm = resize_premult(crop, max(1, round(crop.shape[1] * scale)), max(1, round(crop.shape[0] * scale)))
            left = int(round(ANCHOR_X - (hx[n] - x0) * scale))
            bottom = FEET_ROW + int(round((y1 - ground) * scale))
            top = bottom - sm.shape[0]
            cell = np.zeros((CELL_H, CELL_W, 4), np.uint8)
            sx0, sy0 = max(0, -left), max(0, -top)
            dx0, dy0 = max(0, left), max(0, top)
            w = min(sm.shape[1] - sx0, CELL_W - dx0); h = min(sm.shape[0] - sy0, CELL_H - dy0)
            if w > 0 and h > 0:
                cell[dy0:dy0 + h, dx0:dx0 + w] = sm[sy0:sy0 + h, sx0:sx0 + w]
            if left < 0 or left + sm.shape[1] > CELL_W or top < 0:
                print("  VAROITUS: ruutu", frames[n], "leikkautuu (", left, top, sm.shape[1], sm.shape[0], ")")
            r, c = divmod(n, COLS)
            sheet[r * CELL_H:(r + 1) * CELL_H, c * CELL_W:(c + 1) * CELL_W] = cell
        cv2.imwrite(os.path.join(out_dir, name + ".png"), cv2.cvtColor(sheet, cv2.COLOR_RGBA2BGRA))
        print(name, len(ims), "ruutua, mittakaava %.3f" % scale)


if __name__ == "__main__":
    main(sys.argv[1])
