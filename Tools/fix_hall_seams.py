"""Juhlahallin kuvien saumojen tasoitus: rivikohtainen värierotus reunalla jaetaan kahden kuvan kesken ja häivytetään 140 px matkalla.
Käyttö: python fix_hall_seams.py <Taustat-kansio> [varmuuskopiokansio]. Kuvat muuri_juhlahalli_a..e.png (vierekkäin 1672 px välein)."""
import sys, os, shutil, cv2, numpy as np
d = sys.argv[1]; bak = sys.argv[2] if len(sys.argv) > 2 else None
names = "abcde"; W_RAMP = 140
paths = [os.path.join(d, "muuri_juhlahalli_%s.png" % n) for n in names]
ims = [cv2.imread(p, cv2.IMREAD_UNCHANGED) for p in paths]
if bak:
    os.makedirs(bak, exist_ok=True)
    for p in paths: shutil.copy(p, os.path.join(bak, os.path.basename(p)))
def alpha(im): return im[..., 3] if im.shape[2] == 4 else np.full(im.shape[:2], 255, np.uint8)
for k in range(4):
    L, R = ims[k], ims[k + 1]
    le = L[:, -3:, :3].astype(np.float32).mean(1); re = R[:, :3, :3].astype(np.float32).mean(1)
    ok = (alpha(L)[:, -3:].min(1) > 250) & (alpha(R)[:, :3].min(1) > 250)
    delta = re - le; delta[~ok] = np.nan
    y = np.arange(len(delta)); good = ~np.isnan(delta[:, 0])
    for c in range(3): delta[:, c] = np.interp(y, y[good], delta[good, c])
    delta = cv2.GaussianBlur(delta.reshape(-1, 1, 3), (1, 0), sigmaX=0.1, sigmaY=14).reshape(-1, 3)
    ramp = (1 - np.arange(W_RAMP, dtype=np.float32) / W_RAMP) ** 1.5
    for t in range(W_RAMP):
        L[:, -1 - t, :3] = np.clip(L[:, -1 - t, :3].astype(np.float32) + delta * 0.5 * ramp[t], 0, 255).astype(np.uint8)
        R[:, t, :3] = np.clip(R[:, t, :3].astype(np.float32) - delta * 0.5 * ramp[t], 0, 255).astype(np.uint8)
    print(names[k] + "|" + names[k + 1], "keskiarvo-ero ennen", np.abs(delta).mean().round(1))
for p, im in zip(paths, ims): cv2.imwrite(p, im)
