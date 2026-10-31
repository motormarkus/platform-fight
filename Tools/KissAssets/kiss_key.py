"""Rocco + amatsoni, suudelmavideo (vihreä tausta): taustan poisto. Eroaa amazon_key.py:stä: pitää kaikki isot komponentit (kaksi hahmoa erillään)."""
import cv2, numpy as np
VIDEO = r"C:\Users\marku\.claude\uploads\3422f5da-b08d-4942-ae8f-cb9c1ccfe0f6\a95bd992-gemini_generated_video_F638F7C2.mp4"
def key_frame(bgr, min_area=3000):
    f = bgr.astype(np.float32); b, g, r = f[..., 0], f[..., 1], f[..., 2]
    gd = g - np.maximum(r, b)
    a = np.clip(1.0 - (gd - 22.0) / 40.0, 0.0, 1.0)
    n2, lab2, st2, _ = cv2.connectedComponentsWithStats((a > 0.5).astype(np.uint8), connectivity=8)
    keep = np.zeros(n2, bool)
    for k in range(1, n2):
        if st2[k, cv2.CC_STAT_AREA] >= min_area: keep[k] = True
    a[~keep[lab2]] = 0.0
    g2 = np.where(a < 0.999, np.minimum(g, np.maximum(r, b) + 4.0), g)
    near = cv2.dilate((a < 0.5).astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool)
    g2 = np.where(near, np.minimum(g2, np.maximum(r, b) + 4.0), g2)
    return np.dstack([b, g2, r, a * 255.0]).clip(0, 255).astype(np.uint8)   # BGRA
def frames():
    cap = cv2.VideoCapture(VIDEO); out = []
    while True:
        ok, f = cap.read()
        if not ok: break
        out.append(key_frame(f))
    return out
