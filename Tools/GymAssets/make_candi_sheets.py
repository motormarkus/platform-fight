"""Candin (Violetin jumppakaveri) spritesheetit videoista: candi_N.png, 10 saraketta x N riviä, ruutu 320 x 400 px, joka kolmas ruutu (10 fps).
Sama mitoitus kuin make_jumppa_sheets.py (hahmon korkeus ruudusta 0 -> 300 px, jalat rivillä 384). Avainnus: key_video.key_frame(cyan_safe=True)
(turkoosit raidat eivät saa kadota vihreän taustan mukana).
Käyttö: python make_candi_sheets.py <ulostulo-kansio>"""
import glob, os, sys
import cv2
import numpy as np
from key_video import frames, key_frame
from make_jumppa_sheets import resize_premult, CELL_W, CELL_H, COLS, TARGET_H, FEET_ROW, STEP

UP = "C:/Users/marku/.claude/uploads/6d277d2e-0dfe-4331-9679-b4155f74f179/"
SRC = {1: "1ba714c7", 2: "c5db65e3", 3: "9f9e89e8"}   # 1 askelhölkkä, 2 kyykky edestä -> selin, 3 käsienheilutus/hypyt/taivutus

def main(out):
    os.makedirs(out, exist_ok=True)
    for n, key in SRC.items():
        path = glob.glob(UP + key + "*.mov")[0]
        keyed = [key_frame(f, cyan_safe=True) for i, f in enumerate(frames(path)) if i % STEP == 0]
        ys, xs = np.where(keyed[0][..., 3] > 128)
        h0 = ys.max() - ys.min(); cx = (xs.min() + xs.max()) / 2; base0 = ys.max(); s0 = TARGET_H / h0
        area = np.array([float((r[..., 3] > 128).sum()) for r in keyed])
        bottoms = np.array([float(np.where(r[..., 3] > 128)[0].max()) for r in keyed])
        def rolling_median(a, w):
            h = w // 2
            return np.array([np.median(a[max(0, i - h):i + h + 1]) for i in range(len(a))])
        mult = np.clip(np.sqrt(rolling_median(area, 5) / area), 0.93, 1.07)
        base = np.clip(rolling_median(bottoms, 9), base0 - 20, base0 + 20)
        pad = 600
        rows = (len(keyed) + COLS - 1) // COLS
        sheet = np.zeros((rows * CELL_H, COLS * CELL_W, 4), np.uint8)
        for i, r in enumerate(keyed):
            si = s0 * mult[i]
            sw, sh = int(round(CELL_W / si)), int(round(CELL_H / si))
            x0 = int(round(cx - sw / 2)); y1 = int(round(base[i] + (CELL_H - FEET_ROW) / si)); y0 = y1 - sh
            big = cv2.copyMakeBorder(r, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=(0, 0, 0, 0))
            crop = big[y0 + pad:y1 + pad, x0 + pad:x0 + sw + pad]
            cell = resize_premult(crop, CELL_W, CELL_H)
            sheet[(i // COLS) * CELL_H:(i // COLS + 1) * CELL_H, (i % COLS) * CELL_W:(i % COLS + 1) * CELL_W] = cell
        cv2.imwrite(os.path.join(out, f"candi_{n}.png"), cv2.cvtColor(sheet, cv2.COLOR_RGBA2BGRA))
        print(n, len(keyed), "ruutua, mittakaava %.3f" % s0)

if __name__ == "__main__":
    main(sys.argv[1])
