"""Luurankosheettien läpinäkyvät reiät: vihreän taustan poisto söi sinivihreät osat (päänauha, höyhenet, korut). Lähteenä backup_holes (alkuperäiset).
1) pienet suljetut reiät -> sinivihreä (luu: ympäristön väri), 2) sisäiset puoliläpinäkyvät -> peittävät, 3) pelkkä ääriviiva-höyhenpylväs -> täytetään sinivihreällä."""
import cv2, numpy as np, glob, os, shutil
ROOT = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..")
SP = os.path.join(ROOT, "Assets", "Sprites", "Viholliset"); BK = os.path.join(os.path.dirname(os.path.abspath(__file__)), "backup_holes")
FR = {'idle': (640, 768), 'kavely': (640, 768), 'juoksu': (640, 768), 'jab': (768, 768), 'takasuora': (768, 768), 'potku': (1024, 768), 'osuma': (896, 768)}
TEAL = np.array([135, 128, 30], np.float32)   # BGR (RGB 30,128,135)

def hsv(bgr): return cv2.cvtColor(bgr.reshape(-1, 1, 3).astype(np.uint8), cv2.COLOR_BGR2HSV).reshape(-1, 3)

for src in sorted(glob.glob(os.path.join(BK, "luuranko_*.png"))):
    f = os.path.basename(src); n = f[len("luuranko_"):-4]
    a = cv2.imread(src, cv2.IMREAD_UNCHANGED); H, W = a.shape[:2]; al = a[..., 3]
    m = (al < 128).astype(np.uint8)
    nl, lab, st, _ = cv2.connectedComponentsWithStats(m, connectivity=4)
    bg = lab[0, 0]
    big = [i for i in range(1, nl) if st[i, 4] >= 400]
    holes_ids = [i for i in range(1, nl) if i != bg and st[i, 4] < 400]
    holes = np.isin(lab, holes_ids).astype(np.uint8) * 255
    holes_d = cv2.dilate(holes, np.ones((3, 3), np.uint8))
    inp = cv2.inpaint(a[..., :3].copy(), holes_d, 3, cv2.INPAINT_TELEA)
    # luureikä (vaalea, matala kylläisyys ympärillä) -> inpaint; muuten sinivihreä
    out = a.copy()
    ring = cv2.dilate(holes_d, np.ones((9, 9), np.uint8)) & ~holes_d
    nl2, lab2 = cv2.connectedComponents((holes_d > 0).astype(np.uint8), connectivity=8)
    for i in range(1, nl2):
        sel = lab2 == i
        rg = (cv2.dilate(sel.astype(np.uint8), np.ones((9, 9), np.uint8)) > 0) & ~sel & (al >= 250)
        if rg.sum() < 5: bone = True
        else:
            px = a[..., :3][rg]; h = hsv(px); v = h[:, 2].mean(); s = h[:, 1].mean()
            bone = v > 165 and s < 90
        if bone: out[..., :3][sel] = inp[sel]
        else:
            # sinivihreä + hieman ympäristön sävyä (ääriviivat säilyvät)
            col = 0.8 * TEAL + 0.2 * inp[sel].astype(np.float32)
            out[..., :3][sel] = col.astype(np.uint8)
        out[..., 3][sel] = 255
    al2 = out[..., 3]
    # sisäiset puoliläpinäkyvät
    outside = np.isin(lab, big)
    dist = cv2.distanceTransform((~outside).astype(np.uint8), cv2.DIST_L2, 3)
    inter = (dist > 5) & (al2 < 255)
    low = inter & (al2 < 215)
    if low.any():
        mk = cv2.dilate(low.astype(np.uint8) * 255, np.ones((3, 3), np.uint8))
        o2 = cv2.inpaint(out[..., :3].copy(), mk, 3, cv2.INPAINT_TELEA); sel = (mk > 0) & inter
        out[..., :3][sel] = o2[sel]
    out[..., 3][inter] = 255
    # pelkkä ääriviiva-höyhenpylväs
    fixed = 0
    if n in FR:
        fw, fh = FR[n]
        for r in range(H // fh):
            for c in range(W // fw):
                cell = out[r * fh:(r + 1) * fh, c * fw:(c + 1) * fw]; ca = cell[..., 3]
                ys, xs = np.where(ca > 128)
                if len(ys) < 500: continue
                y0 = ys.min(); y1 = ys.max()
                hh = hsv(cell[..., :3].reshape(-1, 3)).reshape(fh, fw, 3)
                opaque = ca > 200
                teal = opaque & (hh[..., 0] > 80) & (hh[..., 0] < 105) & (hh[..., 1] > 90) & (hh[..., 2] > 80)
                gold = opaque & (hh[..., 0] > 15) & (hh[..., 0] < 35) & (hh[..., 1] > 130) & (hh[..., 2] > 150)
                gy = np.where(gold.any(1))[0]
                if len(gy) == 0: continue
                g0 = gy.min()
                if g0 - y0 < 12: continue
                reg = slice(y0, g0 - 1)
                if teal[reg].sum() > 120: continue           # höyhenet jo täynnä
                sub = (ca[reg] > 100).astype(np.uint8)
                pts = cv2.findNonZero(sub)
                if pts is None or len(pts) < 30: continue
                hull = cv2.convexHull(pts); fill = np.zeros_like(sub); cv2.fillConvexPoly(fill, hull, 1)
                fill = fill & (ca[reg] < 200).astype(np.uint8)
                ysf = np.where(fill)[0]
                if len(ysf) == 0: continue
                t = (ysf - ysf.min()) / max(1, ysf.max() - ysf.min())
                colr = (TEAL[None, :] + (1 - t)[:, None] * np.array([12, 18, 8], np.float32)).clip(0, 255).astype(np.uint8)
                sc = cell[reg]; sc[..., :3][fill > 0] = colr; sc[..., 3][fill > 0] = 255; fixed += 1
    cv2.imwrite(os.path.join(SP, f), out); print(f, len(holes_ids), "reikää,", fixed, "höyhenruutua")
