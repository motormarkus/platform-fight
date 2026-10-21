"""Candin lyönti- ja potkujumppa (2 videota 1080x1920, 30 fps, 10 s). Käyttö: python make_candi_lyonti.py <ulostulo-kansio>
Tulos: candi_lyonti_1.png (vihreä tausta), candi_lyonti_2.png (vaaleansininen tausta), 10 x 10 ruutua 320 x 400 (joka kolmas ruutu = 10 fps)."""
import os, sys
import cv2, numpy as np
from key_video import frames, key_frame
U = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
SRC = [U + "3a25eb25-copy_002F469B-5BCC-4EE5-9D53-8AA0ED42794B.mov", U + "b36ba9d6-copy_BF4EBAAE-1AFC-4DF3-8DC1-3770BE4EAD67.mov"]
CELL_W, CELL_H, COLS, TARGET_H, FEET_ROW, STEP = 320, 400, 10, 300.0, 384, 3

def resize_premult(rgba, w, h):
    f = rgba.astype(np.float32); a = f[..., 3:4] / 255.0; f[..., :3] *= a
    f = cv2.resize(f, (w, h), interpolation=cv2.INTER_AREA); a2 = f[..., 3:4] / 255.0
    f[..., :3] = np.where(a2 > 1e-3, f[..., :3] / np.maximum(a2, 1e-3), 0)
    return f.clip(0, 255).astype(np.uint8)

def clean(k, floor=False):
    if floor:   # lattian varjo ja vaaleansininen jäännös (vaalea, vähän värikylläinen, sininen sävy): pois
        r, g, b = [k[..., i].astype(int) for i in range(3)]
        mx = np.maximum(np.maximum(r, g), b); mn = np.minimum(np.minimum(r, g), b)
        sat = (mx - mn) / np.maximum(mx, 1)
        shadow = (b >= r + 6) & (b >= g - 4) & (mx > 120) & (sat < 0.40) & (sat > 0.02)
        k[shadow, 3] = 0
        k[..., 3] = cv2.medianBlur(k[..., 3], 3)
    solid = (k[..., 3] > 128).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid, connectivity=8)
    if n > 2:
        keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); solid = (lab == keep).astype(np.uint8)
    allow = cv2.dilate(solid, np.ones((7, 7), np.uint8)); k[allow == 0, 3] = 0
    return k

def main(out):
    os.makedirs(out, exist_ok=True)
    for n, path in enumerate(SRC, 1):
        keyed = [clean(key_frame(f), n == 2) for i, f in enumerate(frames(path)) if i % STEP == 0]
        ys, xs = np.where(keyed[0][..., 3] > 128)
        s0 = TARGET_H / (ys.max() - ys.min()); cx = (xs.min() + xs.max()) / 2; base0 = ys.max()
        pad = 600; rows = (len(keyed) + COLS - 1) // COLS
        sheet = np.zeros((rows * CELL_H, COLS * CELL_W, 4), np.uint8)
        for i, r in enumerate(keyed):
            sw, sh = int(round(CELL_W / s0)), int(round(CELL_H / s0))
            x0 = int(round(cx - sw / 2)); y1 = int(round(base0 + (CELL_H - FEET_ROW) / s0)); y0 = y1 - sh
            big = cv2.copyMakeBorder(r, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=(0, 0, 0, 0))
            cell = resize_premult(big[y0 + pad:y1 + pad, x0 + pad:x0 + sw + pad], CELL_W, CELL_H)
            sheet[(i // COLS) * CELL_H:(i // COLS + 1) * CELL_H, (i % COLS) * CELL_W:(i % COLS + 1) * CELL_W] = cell
        cv2.imwrite(os.path.join(out, "candi_lyonti_%d.png" % n), cv2.cvtColor(sheet, cv2.COLOR_RGBA2BGRA))
        print(n, len(keyed), "ruutua")
if __name__ == "__main__": main(sys.argv[1])
