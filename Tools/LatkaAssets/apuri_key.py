"""Pomon apuri (jääkiekkoilija): tiukempi vihreän poisto, koska jäässä on vihertävä heijastus, roiskeet ja viivat.
Hahmon värit (laivastonsininen, viininpunainen, musta, kerma, valkoinen) ovat neutraaleja/ei-vihreitä; vihertävä (G > max(R,B)) on taustaa, heijastusta tai roiskeita."""
import cv2, numpy as np

def key(bgr, thr=7.0, soft=14.0, largest=True):
    f = bgr.astype(np.float32); b, g, r = f[..., 0], f[..., 1], f[..., 2]
    gd = g - np.maximum(r, b)
    a = np.clip(1.0 - (gd - thr) / soft, 0.0, 1.0)
    # pieni avaus: poistaa ohuet jääviivat ja roiskeet
    m = (a > 0.5).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (5, 5)))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    if n > 1 and largest: m = (lab == 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))).astype(np.uint8)
    # täytä vain pienet sisäiset aukot (visiiri); suuret taustataskut (jalkojen ja mailan väli) pysyvät läpinäkyvinä
    bgm = (m == 0).astype(np.uint8)
    n2, lab2, st2, _ = cv2.connectedComponentsWithStats(bgm, connectivity=4)
    ys0 = np.where(m.any(1))[0]; ytop = ys0.min() if len(ys0) else 0; hh = (ys0.max() - ytop) if len(ys0) else 0
    for k in range(1, n2):
        x, y, w, h, ar = st2[k]
        if y < ytop + 0.25 * hh and ar < 600 and x > 0 and y > 0 and x + w < m.shape[1] and y + h < m.shape[0]: m[lab2 == k] = 1
    # pehmeä, siisti reuna: binäärimaski sumennetaan ja terävöitetään (ei portaita), vihreä hohto pois reunakaistalta
    ab = cv2.GaussianBlur(m.astype(np.float32), (0, 0), 1.6)
    a = np.clip((ab - 0.5) * 3.0 + 0.5, 0.0, 1.0)
    k7 = np.ones((7, 7), np.uint8)
    band = (cv2.dilate(m, k7) > 0) & (cv2.erode(m, k7) == 0)
    g2 = np.where(band, np.minimum(g, np.maximum(r, b) + 2.0), g)
    return np.dstack([r, g2, b, a * 255.0]).clip(0, 255).astype(np.uint8)
