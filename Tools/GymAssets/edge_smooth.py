"""Reunojen siistiminen key_frame-tuloksen jälkeen: alfan porrastus pois (kevyt blur + kontrastin nosto 0,5-rajan ympärillä)
ja reunapikseleille sisäpuolen väri (ei vihreää/taustan sävyä). Palauttaa RGBA (uint8)."""
import cv2, numpy as np

def smooth_edges(rgba, sigma=1.0, gain=1.7):
    a = rgba[..., 3].astype(np.float32) / 255.0
    ab = cv2.GaussianBlur(a, (0, 0), sigma)
    a2 = np.clip((ab - 0.5) * gain + 0.5, 0.0, 1.0)
    a2[a < 0.02] = np.minimum(a2[a < 0.02], 0.0 if sigma <= 0 else a2[a < 0.02])
    core = (cv2.erode((a > 0.97).astype(np.uint8), np.ones((3, 3), np.uint8))).astype(np.float32)
    rgb = rgba[..., :3].astype(np.float32)
    num = cv2.GaussianBlur(rgb * core[..., None], (0, 0), 2.0); den = cv2.GaussianBlur(core, (0, 0), 2.0)[..., None]
    inner = num / np.maximum(den, 1e-3)
    use = (a2 < 0.98)[..., None] & (den > 0.02)
    rgb = np.where(use, inner, rgb)
    out = np.dstack([rgb, a2 * 255.0]).clip(0, 255).astype(np.uint8)
    out[out[..., 3] < 6] = 0
    return out
