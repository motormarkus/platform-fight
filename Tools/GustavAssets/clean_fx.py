"""Iskutehosteiden (keltainen tähti potkun päässä, punainen kipinä osumassa) poisto avainnetuista ruuduista."""
import cv2, numpy as np

def clean_fx(rgba_bgra):
    im = rgba_bgra.copy()
    hsv = cv2.cvtColor(im[..., :3], cv2.COLOR_BGR2HSV)
    h, s, v = hsv[..., 0].astype(int), hsv[..., 1].astype(int), hsv[..., 2].astype(int)
    fx = (h >= 19) & (h <= 40) & (s > 80) & (v > 195)          # keltainen
    fx = cv2.dilate(fx.astype(np.uint8), np.ones((9, 9), np.uint8)) > 0
    im[fx, 3] = 0
    a = (im[..., 3] > 20).astype(np.uint8)
    # pienet suljetut reiät (maastokuvion vihreä tulkittiin taustaksi) täytetään: alfa takaisin täyteen
    inv = (a == 0).astype(np.uint8)
    n0, lab0, st0, _ = cv2.connectedComponentsWithStats(inv, connectivity=4)
    for i in range(1, n0):
        x, y, w, h, ar = st0[i]
        if ar < 2500 and x > 0 and y > 0 and x + w < a.shape[1] and y + h < a.shape[0]:
            hole = lab0 == i
            bgr = im[..., :3].astype(np.int32)
            light = (bgr.min(axis=2) >= 170) & ((bgr.max(axis=2) - bgr.min(axis=2)) < 70)   # vaalea (kerma/valkoinen) tausta ei saa palata reiästä hahmoon
            im[hole & ~light, 3] = 255
    # suljettuihin rakoihin (raajojen väliin) jääneet vaaleat taustalaikut (kerma/valkoinen) pois: Gustavilla ei ole isoja valkoisia osia (ranneside on kermanvärinen, min < 195)
    bgr = im[..., :3].astype(np.int32)
    mn, mx = bgr.min(axis=2), bgr.max(axis=2)
    res = ((mn > 195) & (mx - mn < 45) & (im[..., 3] > 200)).astype(np.uint8)
    nr, labr, str_, _ = cv2.connectedComponentsWithStats(res, connectivity=8)
    for k in range(1, nr):
        if str_[k][4] > 60:
            comp = (labr == k).astype(np.uint8)
            if cv2.distanceTransform(comp, cv2.DIST_L2, 3).max() < 4.5: continue   # ohut raita (rannesiteen valkoinen kuvio) säilyy; paksu laikka = tausta
            grow = (cv2.dilate(comp, np.ones((5, 5), np.uint8)) > 0) & (mn > 165)   # myös vaalea reunahohto
            im[grow, 3] = 0
    a = (im[..., 3] > 20).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(a, connectivity=8)
    if n > 2:
        k = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); im[lab != k, 3] = 0
    return im
