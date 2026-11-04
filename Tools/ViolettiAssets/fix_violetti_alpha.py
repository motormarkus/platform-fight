"""Violetin sheettien korjaus: täyttää hahmon sisäiset läpinäkyvät/puoliläpinäkyvät laikut (farkut) ja poistaa reunan valkoisen hapsun
(reunapikselien väri korvataan sisäpuolen väreillä). Ajo: python fix_violetti_alpha.py <kansio> (muokkaa violetti_*.png paikallaan, varmuuskopio ulos-kansioon)."""
import sys, os, shutil, cv2, numpy as np
d = sys.argv[1]; bak = sys.argv[2] if len(sys.argv) > 2 else None
CELL = {"tanssi": (768, 672)}
def fix_cell(c):
    a = c[..., 3]
    solid = (a > 12).astype(np.uint8)
    if solid.sum() < 50: return c
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid, connectivity=8)
    keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); solid = (lab == keep).astype(np.uint8)
    closed = cv2.morphologyEx(solid, cv2.MORPH_CLOSE, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (21, 21)))
    # täytä suljetut aukot
    inv = (1 - closed).astype(np.uint8); ff = inv.copy(); h, w = ff.shape; mask = np.zeros((h + 2, w + 2), np.uint8)
    cv2.floodFill(ff, mask, (0, 0), 2)
    holes = (ff == 1)
    filled = closed.astype(bool) | holes
    weak = filled & (a < 250)                       # sisäpuolen haalistuneet pikselit
    core = cv2.erode(((a >= 250) & filled).astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool)   # luotettava väri
    band = cv2.dilate(filled.astype(np.uint8), np.ones((3, 3), np.uint8)).astype(bool) & ~core         # täytettävä + reunahapsu
    rgb = np.ascontiguousarray(c[..., :3])
    # reunahapsu ja aukot: väri lähimmästä luotettavasta pikselistä (etäisyysmuunnos + nimiöt)
    lum = cv2.cvtColor(rgb, cv2.COLOR_BGR2GRAY)
    src = core & (lum > 85)   # tummat ääriviivapikselit eivät kelpaa täyteväriksi
    dist, labels = cv2.distanceTransformWithLabels((~src).astype(np.uint8), cv2.DIST_L2, 5, labelType=cv2.DIST_LABEL_PIXEL)
    ys, xs = np.where(src)
    lab_to_xy = {}
    ly = np.zeros(labels.max() + 1, np.int32); lx = np.zeros(labels.max() + 1, np.int32)
    ly[labels[ys, xs]] = ys; lx[labels[ys, xs]] = xs
    near = rgb[ly[labels], lx[labels]]
    rgb2 = np.where(band[..., None], near, rgb)
    alpha = np.where(filled, 255, 0).astype(np.uint8)
    alpha = cv2.erode(alpha, np.ones((3, 3), np.uint8))        # 1 px sisään: valkoinen reunahapsu pois
    alpha = cv2.GaussianBlur(alpha, (3, 3), 0)
    return np.dstack([rgb2, alpha]).astype(np.uint8)
for f in sorted(os.listdir(d)):
    if not (f.startswith("violetti_") and f.endswith(".png")): continue
    p = os.path.join(d, f); name = f[9:-4]
    im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
    if bak: os.makedirs(bak, exist_ok=True); shutil.copy(p, os.path.join(bak, f))
    cw, ch = CELL.get(name, (640, 768)); H, W = im.shape[:2]
    for r in range(H // ch):
        for cc in range(W // cw):
            cell = im[r * ch:(r + 1) * ch, cc * cw:(cc + 1) * cw]
            if cell[..., 3].max() < 10: continue
            im[r * ch:(r + 1) * ch, cc * cw:(cc + 1) * cw] = fix_cell(cell.copy())
    cv2.imwrite(p, im); print(f, "ok")
