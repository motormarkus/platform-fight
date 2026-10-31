"""Amatsoni: videoiden (vihreä tausta, c-videossa tummempi) taustan poisto. Käyttö: import amazon_key; amazon_key.load(path)"""
import cv2, numpy as np
def key_frame(bgr):
    f = bgr.astype(np.float32); b, g, r = f[..., 0], f[..., 1], f[..., 2]
    gd = g - np.maximum(r, b)
    a = np.clip(1.0 - (gd - 22.0) / 40.0, 0.0, 1.0)
    n2, lab2, st2, _ = cv2.connectedComponentsWithStats((a > 0.5).astype(np.uint8), connectivity=8)
    if n2 > 2:
        keep = 1 + int(np.argmax(st2[1:, cv2.CC_STAT_AREA])); a[lab2 != keep] = 0.0
    g2 = np.where(a < 0.999, np.minimum(g, np.maximum(r, b) + 4.0), g)
    # reunan despill myös lähipikseleille
    near = cv2.dilate((a < 0.5).astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool)
    g2 = np.where(near, np.minimum(g2, np.maximum(r, b) + 4.0), g2)
    return np.dstack([r, g2, b, a * 255.0]).clip(0, 255).astype(np.uint8)
def load(path):
    cap = cv2.VideoCapture(path); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        fr.append(key_frame(f))
    return fr
