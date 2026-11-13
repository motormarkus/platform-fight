"""Warden: heittokuvat (10 läpinäkyvää ruutua 512x464, kiinteä kamera) -> Assets/Sprites/Viholliset/warden_heitto.png (ruutu 1024x896, ankkuri keskellä, jalat 16 px ylös).
Ruudut: 1 vartioasento, 2-6 nosto/ojennus (kädet ylös, 4-6 korkeimmillaan), 7-9 ojennus eteen (tarttuminen), 10 paluu. Mittakaava: seisova hahmo 348 px -> 640 px.
Ajo: python Tools/PrisonAssets/make_warden_heitto.py <kansio jossa warden-heitto-NN.png> [ulos-kansio]"""
import os, sys, glob
import cv2, numpy as np
CW, CH, FOOT = 1024, 896, 16
SRC_W, SRC_FLOOR, SRC_H = 512, 436.0, 348.0
def main(src, outs):
    k = 640.0 / SRC_H; cells = []
    for f in sorted(glob.glob(os.path.join(src, "warden-heitto-*.png"))):
        im = cv2.imread(f, cv2.IMREAD_UNCHANGED).astype(np.float32)   # BGRA
        pm = im.copy(); pm[..., :3] *= pm[..., 3:4] / 255.0           # esikerrottu: ei reunahalot
        M = np.float32([[k, 0, CW / 2 - SRC_W / 2 * k], [0, k, CH - FOOT - SRC_FLOOR * k]])
        o = cv2.warpAffine(pm, M, (CW, CH), flags=cv2.INTER_LANCZOS4, borderValue=(0, 0, 0, 0))
        a = np.clip(o[..., 3:4], 0, 255); rgb = o[..., :3] / np.maximum(a / 255.0, 1e-3)
        bl = cv2.GaussianBlur(rgb, (0, 0), 1.6); rgb = rgb + 0.6 * (rgb - bl)      # kevyt teroitus
        out = np.dstack([np.clip(rgb, 0, 255), a]).astype(np.uint8); out[out[..., 3] < 6] = 0
        cells.append(out)
    bad = [n + 1 for n, c in enumerate(cells) if (c[0, :, 3] > 10).any() or (c[-1, :, 3] > 10).any() or (c[:, 0, 3] > 10).any() or (c[:, -1, 3] > 10).any()]
    COLS = 4; rows = (len(cells) + COLS - 1) // COLS
    img = np.zeros((rows * CH, COLS * CW, 4), np.uint8)
    for n, c in enumerate(cells): img[(n // COLS) * CH:(n // COLS + 1) * CH, (n % COLS) * CW:(n % COLS + 1) * CW] = c
    cv2.imwrite(os.path.join(outs, "warden_heitto.png"), img)
    print("heitto", len(cells), "kuvaa k", round(k, 3), "leikkautuvat", bad)
if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset")
