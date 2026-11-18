"""Liian paksu / sumea tumma ääriviiva ohuemmaksi: reunalta kuoritaan tummia ja puoliläpinäkyviä pikseleitä (enintään PEEL kierrosta),
maski siistitään ja tilalle tasainen LINE px tumma viiva, reuna pehmennetään 3x suurennoksessa. Sinertävä/vihertävä hehku lähtee kuorinnan mukana.
Ajo: python Tools/EdgeBatch/peel_outline.py <alkuperäisten kansio> <kohdekansio> <PEEL> <LINE> tiedosto1.png ..."""
import os, sys
import numpy as np, cv2
from PIL import Image
K3 = np.ones((3, 3), np.uint8)
def run(src, dst, names, peel=3, line=1.6, dark=40, soft=2.2, sharpen=0.0):   # dark 40: vain lähes musta ääriviiva kuoritaan (tummat hanskat/ranneke säilyvät)
    for n in names:
        im = np.array(Image.open(os.path.join(src, n)).convert("RGBA"))
        rgb = im[..., :3]; luma = rgb.astype(np.float32) @ np.array([0.299, 0.587, 0.114], np.float32)
        m = (im[..., 3] >= 110).astype(np.uint8)
        for _ in range(int(peel)):
            edge = (m - cv2.erode(m, K3)) > 0
            rem = edge & ((luma < dark) | (im[..., 3] < 230))
            if not rem.any(): break
            m[rem] = 0
        m = cv2.morphologyEx(m, cv2.MORPH_OPEN, K3)
        k, lab, st, _ = cv2.connectedComponentsWithStats(m, 8)
        for i in range(1, k):
            if st[i, cv2.CC_STAT_AREA] < 60: m[lab == i] = 0
        H, W = m.shape
        big = cv2.GaussianBlur(cv2.resize(m.astype(np.float32), (W * 3, H * 3), interpolation=cv2.INTER_CUBIC), (0, 0), soft)
        inner = (big >= 0.5).astype(np.uint8)
        r = max(1, int(round(line * 3)))
        full = cv2.dilate(inner, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * r + 1, 2 * r + 1)))
        fa = cv2.resize(cv2.GaussianBlur(full.astype(np.float32), (0, 0), 1.2), (W, H), interpolation=cv2.INTER_AREA)
        ia = cv2.resize(cv2.GaussianBlur(inner.astype(np.float32), (0, 0), 1.2), (W, H), interpolation=cv2.INTER_AREA)
        alpha = np.clip((fa - 0.5) * 2.2 + 0.5, 0, 1)
        w_in = np.clip((ia - 0.5) * 2.2 + 0.5, 0, 1)
        core = (m > 0) & (cv2.erode(m, K3) > 0)
        _, lb = cv2.distanceTransformWithLabels((~core).astype(np.uint8), cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL)
        ys, xs = np.nonzero(core); lut = np.zeros((lb.max() + 1, 3), np.uint8); lut[lb[ys, xs]] = rgb[ys, xs]
        base = np.where(core[..., None], rgb, lut[lb]).astype(np.float32)
        if sharpen > 0:   # epätarkka lähdekuva: kevyt epäterävän maskin terävöitys sisäväreihin
            bl = cv2.GaussianBlur(base, (0, 0), 1.2)
            base = np.clip(base + sharpen * (base - bl), 0, 255)
        lc = np.array([22, 16, 18], np.float32)
        out = base * w_in[..., None] + lc * (1 - w_in[..., None])
        res = np.dstack([out.clip(0, 255), alpha * 255]).astype(np.uint8)
        res[alpha < 0.01] = 0
        import time
        for t in range(10):   # Unity voi lukita tiedoston hetkeksi
            try: Image.fromarray(res, "RGBA").save(os.path.join(dst, n)); break
            except OSError: time.sleep(1.0)
        print(n, "ok")
if __name__ == "__main__":
    run(sys.argv[1], sys.argv[2], sys.argv[5:], float(sys.argv[3]), float(sys.argv[4]))
