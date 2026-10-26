"""Taustakuvien alfa-auditointi: kuinka paljon kuvan sisällä on läpinäkyviä aukkoja (taustan/parallaksin näkyminen läpi) ja puoliläpinäkyviä pikseleitä.
Ajo: python Tools/AirportAssets/audit_alpha.py [--fix kuva.png [maxAla]]   (--fix täyttää alle maxAla px aukot ympäristön värillä ja tekee sisäosan puoliläpinäkyvistä pikseleistä peittäviä)"""
import sys, os, glob, numpy as np, cv2
d = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
SKIP = ("kone", "ikkunat", "tiski", "maski", "junamatka", "loop")
def analyze(path, fix=False, max_area=8000):
    im = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    if im is None or im.ndim < 3 or im.shape[2] != 4: return None
    al = im[..., 3]; H, W = al.shape
    hole = (al < 200).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(hole, connectivity=4)
    border = set(np.unique(np.concatenate([lab[0], lab[-1], lab[:, 0], lab[:, -1]])))
    enclosed = [(st[i, cv2.CC_STAT_AREA], i) for i in range(1, n) if i not in border]
    outside = np.isin(lab, [0] + list(border)) & (hole > 0)
    near_out = cv2.dilate(outside.astype(np.uint8), np.ones((9, 9), np.uint8)) > 0   # kuvan reunan alfareuna (sallittu)
    semi = ((al >= 10) & (al < 255) & ~near_out)
    small = [(a, i) for a, i in enclosed if a <= max_area]
    if fix and (small or semi.any()):
        mask = np.zeros((H, W), np.uint8)
        for a, i in small: mask[lab == i] = 255
        mask = cv2.dilate(mask, np.ones((3, 3), np.uint8))
        if mask.any():
            rgb = cv2.inpaint(np.ascontiguousarray(im[..., :3]), mask, 5, cv2.INPAINT_TELEA); im[..., :3] = rgb
            im[..., 3][mask > 0] = 255
        im[..., 3][semi] = 255
        cv2.imwrite(path, im)
    big = sorted([a for a, _ in enclosed if a > max_area], reverse=True)[:4]
    return len(small), big, int(semi.sum())
if __name__ == "__main__":
    if len(sys.argv) > 2 and sys.argv[1] == "--fix":
        p = sys.argv[2] if os.path.isabs(sys.argv[2]) or os.path.exists(sys.argv[2]) else os.path.join(d, sys.argv[2])
        print(analyze(p, True, int(sys.argv[3]) if len(sys.argv) > 3 else 8000)); sys.exit()
    for f in sorted(glob.glob(os.path.join(d, "*.png"))):
        b = os.path.basename(f)
        if any(s in b for s in SKIP): continue
        r = analyze(f)
        if r and (r[0] or r[2] > 50): print(f"{b:50s} pienet aukot {r[0]:4d}  isot {r[1]}  puoliläpinäkyvää sisällä {r[2]}")
