"""Jumppatytön spritesheetit videoista (7 harjoitusliikettä, 1080x1440, 30 fps, 8 s).
Käyttö: python make_jumppa_sheets.py <kansio jossa 7 .mov-tiedostoa kansion järjestyksessä: lisätty-aikajärjestys> <ulostulo-kansio>
Tiedostojärjestys annetaan alla (SRC_ORDER, tiedostonimen loppuosa). Jokainen video -> jumppa_N.png (10 x 8 ruutua, 320 x 400 px,
joka kolmas ruutu = 10 fps). Hahmon korkeus normalisoidaan ruudusta 0 (seisova asento) 300 px:ksi, jalkojen kohta rivillä 384."""
import os
import sys
import cv2
import numpy as np
from key_video import frames, key_frame

SRC_ORDER = ["9FB3E258", "3D9329E7", "4835F436", "E6B2BA9E", "17C36BBD", "5DBCD6DE", "B2CFA0B9"]   # 1..7: askelkyykky, kyykky edestä->takaa, kyykky takaa, haara-hyppy, etutaivutus, polvi/potku, hölkkä
CELL_W, CELL_H, COLS, ROWS = 320, 400, 10, 8
TARGET_H, FEET_ROW, STEP = 300.0, 384, 3


def resize_premult(rgba, w, h):
    f = rgba.astype(np.float32)
    a = f[..., 3:4] / 255.0
    f[..., :3] *= a
    f = cv2.resize(f, (w, h), interpolation=cv2.INTER_AREA)
    a2 = f[..., 3:4] / 255.0
    f[..., :3] = np.where(a2 > 1e-3, f[..., :3] / np.maximum(a2, 1e-3), 0)
    return f.clip(0, 255).astype(np.uint8)


def main(src, out):
    os.makedirs(out, exist_ok=True)
    files = os.listdir(src)
    for n, key in enumerate(SRC_ORDER, 1):
        path = os.path.join(src, next(f for f in files if key in f and f.endswith(".mov")))
        keyed = [key_frame(f) for i, f in enumerate(frames(path)) if i % STEP == 0]
        ys, xs = np.where(keyed[0][..., 3] > 128)
        h0 = ys.max() - ys.min(); cx = (xs.min() + xs.max()) / 2; base0 = ys.max(); s0 = TARGET_H / h0
        # kuva kuvalta: pinta-alan välkyntä (videon koon heilunta) tasataan viiden kuvan mediaanilla,
        # jalkojen alareuna seuraa liukuvaa mediaania (kameran liuku pois, hypyt säilyvät)
        area = np.array([float((r[..., 3] > 128).sum()) for r in keyed])
        bottoms = np.array([float(np.where(r[..., 3] > 128)[0].max()) for r in keyed])
        def rolling_median(a, w):
            h = w // 2
            return np.array([np.median(a[max(0, i - h):i + h + 1]) for i in range(len(a))])
        mult = np.clip(np.sqrt(rolling_median(area, 5) / area), 0.93, 1.07)
        base = np.clip(rolling_median(bottoms, 9), base0 - 20, base0 + 20)
        pad = 600
        sheet = np.zeros((ROWS * CELL_H, COLS * CELL_W, 4), np.uint8)
        for i, r in enumerate(keyed[:COLS * ROWS]):
            si = s0 * mult[i]
            sw, sh = int(round(CELL_W / si)), int(round(CELL_H / si))
            x0 = int(round(cx - sw / 2)); y1 = int(round(base[i] + (CELL_H - FEET_ROW) / si)); y0 = y1 - sh
            big = cv2.copyMakeBorder(r, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=(0, 0, 0, 0))
            crop = big[y0 + pad:y1 + pad, x0 + pad:x0 + sw + pad]
            cell = resize_premult(crop, CELL_W, CELL_H)
            sheet[(i // COLS) * CELL_H:(i // COLS + 1) * CELL_H, (i % COLS) * CELL_W:(i % COLS + 1) * CELL_W] = cell
        cv2.imwrite(os.path.join(out, f"jumppa_{n}.png"), cv2.cvtColor(sheet, cv2.COLOR_RGBA2BGRA))
        print(n, len(keyed), "ruutua, mittakaava %.3f" % s0)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
