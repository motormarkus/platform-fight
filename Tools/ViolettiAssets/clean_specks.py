"""Violetin kävely-, käännös- ja selinkuvien reunasiivous: irralliset roiskeet (pienet palat) pois, reunan valkoiset/harmaat pikselit pois.
Käyttö: python clean_specks.py <Viholliset-kansio> <sheet-nimet pilkulla, esim. kavely,kaanto,selin> [varmuuskopiokansio]"""
import sys, os, shutil, cv2, numpy as np
d, names = sys.argv[1], sys.argv[2].split(","); bak = sys.argv[3] if len(sys.argv) > 3 else None
CW, CH = 640, 768
def clean(t):
    a = t[..., 3]
    m = (a > 40).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    if n < 2: return t
    keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); core = (lab == keep)
    core = cv2.morphologyEx(core.astype(np.uint8), cv2.MORPH_OPEN, np.ones((3, 3), np.uint8)).astype(bool)   # ohuet irtonaiset kiinnitykset pois
    n, lab, st, _ = cv2.connectedComponentsWithStats(core.astype(np.uint8), connectivity=8)
    keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); core = (lab == keep)
    # reunavyöhyke (4 px sisään): vaaleat harmaat pikselit (tausta- tai ääriviivahapsu) pois
    inner = cv2.erode(core.astype(np.uint8), np.ones((9, 9), np.uint8)).astype(bool)
    edge = core & ~inner
    hsv = cv2.cvtColor(t[..., :3], cv2.COLOR_BGR2HSV)
    pale = (hsv[..., 2] > 150) & (hsv[..., 1] < 55)
    core &= ~(edge & pale)
    core = cv2.morphologyEx(core.astype(np.uint8), cv2.MORPH_OPEN, np.ones((3, 3), np.uint8)).astype(bool)
    n, lab, st, _ = cv2.connectedComponentsWithStats(core.astype(np.uint8), connectivity=8)
    keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); core = (lab == keep)
    out = t.copy(); out[..., 3] = np.where(core, a, 0)
    return out
for nm in names:
    p = os.path.join(d, "violetti_%s.png" % nm); im = cv2.imread(p, cv2.IMREAD_UNCHANGED)
    if bak: os.makedirs(bak, exist_ok=True); shutil.copy(p, os.path.join(bak, os.path.basename(p)))
    H, W = im.shape[:2]
    for r in range(H // CH):
        for c in range(W // CW):
            cell = im[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
            if cell[..., 3].max() < 10: continue
            im[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = clean(cell.copy())
    cv2.imwrite(p, im); print(nm, "ok")
