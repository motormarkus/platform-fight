"""Skaalaa yksittäisiä ruutuja spritesheetissä ankkuripisteen (ruudun alakeskikohta) ympäri: python rescale_frames.py <tiedosto.png> <ruutuLev> <ruutuKork> <kertoimet pilkulla, ruudusta 0 alkaen>
Esim. Rokkimimmin kaatumiskuvat: ruudut 2-8 kasvatetaan 1,2-kertaisiksi (video kutisti hahmon kaatuessa). Alkuperäinen tallennetaan BACKUP_DIR-kansioon."""
import sys, os, shutil
import cv2, numpy as np

def main(path, cw, ch, factors):
    im = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    H, W = im.shape[:2]; cols = W // cw
    bak = os.environ.get("BACKUP_DIR")
    if bak: os.makedirs(bak, exist_ok=True); shutil.copy(path, os.path.join(bak, os.path.basename(path)))
    out = im.copy()
    for i, f in enumerate(factors):
        if abs(f - 1.0) < 1e-3: continue
        r, c = divmod(i, cols)
        cell = im[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw].astype(np.float32)
        cell[..., :3] *= cell[..., 3:4] / 255.0
        M = np.array([[f, 0, cw * 0.5 * (1 - f)], [0, f, ch * (1 - f)]], np.float32)
        w = cv2.warpAffine(cell, M, (cw, ch), flags=cv2.INTER_CUBIC, borderValue=(0, 0, 0, 0))
        a = np.clip(w[..., 3:4], 0, 255)
        rgb = np.where(a > 1, w[..., :3] / np.maximum(a / 255.0, 1e-3), 0)
        res = np.dstack([rgb.clip(0, 255), a]).astype(np.uint8)
        ys, xs = np.where(res[..., 3] > 128)
        print(os.path.basename(path), "ruutu", i, "x%.2f" % f, "bbox", xs.min(), xs.max(), ys.min(), ys.max(), "(ruutu %dx%d)" % (cw, ch))
        out[r * ch:(r + 1) * ch, c * cw:(c + 1) * cw] = res
    cv2.imwrite(path, out)

if __name__ == "__main__": main(sys.argv[1], int(sys.argv[2]), int(sys.argv[3]), [float(x) for x in sys.argv[4].split(",")])
