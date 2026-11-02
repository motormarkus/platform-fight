"""Poistaa tekoälykuvien kimalteen (pienet kirkkaat täplät/kultahileet kivipinnoissa): kirkkaat paikalliset piikit korvataan ympäristön mediaaniväreillä,
lisäksi kirkkaiden lämpimien (kulta/kupari) pikseleiden hienokontrasti pehmenee. Alfa säilyy, läpinäkyvät reunat eivät tummu.
Ajo: python Tools/MinerAssets/deglitter.py <vahvuus 0-1> kuva1.png kuva2.png ...   (alkuperäiset varmuuskopioidaan Tools/MinerAssets/backup_glitter/)"""
import os, sys, shutil
import cv2, numpy as np


def lum(f): return 0.114 * f[..., 0] + 0.587 * f[..., 1] + 0.299 * f[..., 2]


def process(path, k, outdir):
    im = cv2.imread(path, cv2.IMREAD_UNCHANGED)
    has_a = im.ndim == 3 and im.shape[2] == 4
    bgr = im[..., :3].copy(); alpha = im[..., 3] if has_a else np.full(im.shape[:2], 255, np.uint8)
    f = bgr.astype(np.float32)
    inner = cv2.erode((alpha >= 250).astype(np.uint8), np.ones((9, 9), np.uint8)).astype(np.float32)   # ei reunoilla (läpinäkyvän vieressä)
    med = cv2.medianBlur(bgr, 7).astype(np.float32)
    soft = cv2.GaussianBlur(f, (0, 0), 2.2)
    L = lum(f); Lmed = lum(med)
    spike = np.clip((L - Lmed - 6.0) / 28.0, 0, 1)                   # kirkkaat täplät
    # kulta/kupari: lämmin ja kirkas -> hienokontrasti pois
    b, g, r = f[..., 0], f[..., 1], f[..., 2]
    warm = np.clip((r - b - 35.0) / 60.0, 0, 1) * np.clip((L - 110.0) / 70.0, 0, 1)
    wt = np.clip(spike * 1.0 + warm * 0.55, 0, 1) * k * inner
    wt = cv2.GaussianBlur(wt, (0, 0), 0.8)[..., None]
    out = f * (1 - wt) + (0.6 * med + 0.4 * soft) * wt
    # myös yleinen hienojyvä kevyesti (puolet vahvuudesta) sisäalueilla
    gl = (0.5 * k * inner)[..., None]
    out = out * (1 - gl * 0.35) + soft * (gl * 0.35)
    out = np.clip(out, 0, 255).astype(np.uint8)
    res = np.dstack([out, alpha]) if has_a else out
    os.makedirs(outdir, exist_ok=True)
    bk = os.path.join(outdir, os.path.basename(path))
    if not os.path.exists(bk): shutil.copy2(path, bk)
    cv2.imwrite(path, res)


if __name__ == "__main__":
    k = float(sys.argv[1]); here = os.path.dirname(os.path.abspath(__file__))
    for p in sys.argv[2:]:
        process(p, k, os.path.join(here, "backup_glitter")); print("käsitelty", p)
