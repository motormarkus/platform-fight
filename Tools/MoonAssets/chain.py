import numpy as np
from PIL import Image
def horizon(x, col): return int(np.argmax(x[:, col, 3] > 128))
def stitch(imgs, OV=120, TAPER=700, MAXSHIFT=40):
    C = imgs[0].astype(np.float32); H = C.shape[0]; rows = np.arange(H); offs = [0]
    for B in imgs[1:]:
        B = B.astype(np.float32)
        d = max(-MAXSHIFT, min(MAXSHIFT, horizon(B, 0) - horizon(C, C.shape[1] - 1)))   # kuinka paljon B:tä nostetaan
        B2 = B.copy()
        for x in range(min(TAPER, B.shape[1])):
            s = d * (1 - x / TAPER); src = np.clip(rows + s, 0, H - 1)
            lo = np.floor(src).astype(int); hi = np.clip(lo + 1, 0, H - 1); f = (src - lo)[:, None]
            B2[:, x] = B[lo, x] * (1 - f) + B[hi, x] * f
        start = C.shape[1] - OV
        t = np.linspace(0, 1, OV)[None, :, None]
        blend = C[:, start:] * (1 - t) + B2[:, :OV] * t
        C = np.concatenate([C[:, :start], blend, B2[:, OV:]], axis=1); offs.append(start)
    return C, offs
