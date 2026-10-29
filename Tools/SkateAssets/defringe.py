"""Rullalautakuvien reunojen puhdistus: puolilÃ¤pinÃ¤kyvien reunapikselien vÃ¤ri (vihreÃ¤/valkoinen vuoto videon taustasta) korvataan
lÃ¤himmÃ¤n peittÃ¤vÃ¤n pikselin vÃ¤rillÃ¤. Alfa sÃ¤ilyy. KÃ¤yttÃ¶: python defringe.py kuva.png ..."""
import sys, cv2, numpy as np

def defringe(path, solid=250, dark=0.55):
    im = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    a = im[..., 3]
    seed = (a >= solid)
    edge = (a > 0) & ~seed
    if not edge.any(): return 0
    src = (~seed).astype(np.uint8)   # 0 = lÃ¤hdepikseli
    _, lab = cv2.distanceTransformWithLabels(src, cv2.DIST_L2, 3, labelType=cv2.DIST_LABEL_PIXEL)
    ys, xs = np.nonzero(seed)
    lut = np.zeros((lab.max() + 1, 3), np.uint8)
    lut[lab[ys, xs]] = im[ys, xs, :3]
    im[edge, :3] = (lut[lab[edge]].astype(np.float32) * dark).astype(np.uint8)   # reunan väri: lähin sisäväri tummennettuna (ääriviiva)
    cv2.imwrite(path, im)
    return int(edge.sum())

if __name__ == "__main__":
    for p in sys.argv[1:]: print(p, defringe(p))
