"""Kahden vierekkäisen taustakuvan sauman värierotuksen tasoitus (rivikohtainen ero reunalla jaetaan kahden kuvan kesken ja häivytetään).
Käyttö: python fix_seam_pair.py <vasen.png> <oikea.png> [ramp-px] [varmuuskopiokansio]"""
import sys, os, shutil, cv2, numpy as np
lp, rp = sys.argv[1], sys.argv[2]
ramp_w = int(sys.argv[3]) if len(sys.argv) > 3 else 260
bak = sys.argv[4] if len(sys.argv) > 4 else None
if bak:
    os.makedirs(bak, exist_ok=True)
    for p in (lp, rp): shutil.copy(p, os.path.join(bak, os.path.basename(p)))
L = cv2.imread(lp, cv2.IMREAD_UNCHANGED); R = cv2.imread(rp, cv2.IMREAD_UNCHANGED)
def alpha(im): return im[..., 3] if im.shape[2] == 4 else np.full(im.shape[:2], 255, np.uint8)
le = L[:, -3:, :3].astype(np.float32).mean(1); re = R[:, :3, :3].astype(np.float32).mean(1)
ok = (alpha(L)[:, -3:].min(1) > 250) & (alpha(R)[:, :3].min(1) > 250)
delta = re - le; delta[~ok] = np.nan
y = np.arange(len(delta)); good = ~np.isnan(delta[:, 0])
for c in range(3): delta[:, c] = np.interp(y, y[good], delta[good, c])
print("ero ennen (keskiarvo RGB):", np.abs(delta).mean(0).round(1))
delta = cv2.GaussianBlur(delta.reshape(-1, 1, 3), (1, 0), sigmaX=0.1, sigmaY=10).reshape(-1, 3)
ramp = (1 - np.arange(ramp_w, dtype=np.float32) / ramp_w) ** 1.5
for t in range(ramp_w):
    L[:, -1 - t, :3] = np.clip(L[:, -1 - t, :3].astype(np.float32) + delta * 0.5 * ramp[t], 0, 255).astype(np.uint8)
    R[:, t, :3] = np.clip(R[:, t, :3].astype(np.float32) - delta * 0.5 * ramp[t], 0, 255).astype(np.uint8)
cv2.imwrite(lp, L); cv2.imwrite(rp, R)
