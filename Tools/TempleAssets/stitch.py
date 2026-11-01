"""Temppelin sisäkuvien 1 ja 2 saumaton liitos: lattiaviiva sovitetaan (pystysuora venytys) ja sauma ristiinhäivytetään.
Käyttö: python stitch.py kuva1 kuva2 ulos1 ulos2"""
import sys, numpy as np, cv2
a = cv2.imread(sys.argv[1]).astype(np.float32); b = cv2.imread(sys.argv[2]).astype(np.float32)
h, w = a.shape[:2]
FA, FB = 730.0, 648.0      # lattian yläreuna kuvissa 1 ja 2
RAMP = 560                 # leveys, jolla kuvan 1 lattia nousee FA -> FB
L = 110                    # häivytyksen puolileveys sauman kummallakin puolella

ys = np.arange(h, dtype=np.float32)
src = np.interp(ys, [0, FB, h - 1], [0, FA, h - 1]).astype(np.float32)   # tulos-y -> lähde-y
xs = np.arange(w, dtype=np.float32)
t = np.clip((xs - (w - RAMP)) / RAMP, 0, 1); t = t * t * (3 - 2 * t)
mx = np.tile(xs, (h, 1))
my = ys[:, None] * (1 - t[None, :]) + src[:, None] * t[None, :]
a = cv2.remap(a, mx, my.astype(np.float32), cv2.INTER_CUBIC, borderMode=cv2.BORDER_REPLICATE)

def col(img, x): return img[:, int(np.clip(x, 0, w - 1))]
outA, outB = a.copy(), b.copy()
for d in range(0, L + 1):
    # asema p = -d (kuvan 1 puolella) ja p = +d (kuvan 2 puolella); alpha = paino kuvan 2 sisällölle
    for p in (-d, d):
        s = (p + L) / (2.0 * L); al = s * s * (3 - 2 * s)
        c1 = col(a, w - 1 - abs(p)) if p < 0 else col(a, w - 1 - p)   # kuvan 1 sisältö (peilattu sauman yli)
        c2 = col(b, abs(p))                                            # kuvan 2 sisältö (peilattu sauman yli)
        mix = c1 * (1 - al) + c2 * al
        if p <= 0: outA[:, w - 1 - abs(p)] = mix
        if p >= 0: outB[:, p] = mix
cv2.imwrite(sys.argv[3], np.clip(outA, 0, 255).astype(np.uint8)); cv2.imwrite(sys.argv[4], np.clip(outB, 0, 255).astype(np.uint8))
