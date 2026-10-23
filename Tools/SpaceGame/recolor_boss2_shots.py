"""Tuhoajan ammukset: Kurittajan ammuskuvat värinvaihdettuna vihreiksi (e2s/e2b/e2disc/e2orb/e2beam). Käyttö: recolor_boss2_shots.py <TahtiPyssy-kansio>"""
import sys, numpy as np, cv2
d = sys.argv[1]
SETS = {"eshot_s": ("e2s", 3, 58), "eshot_b": ("e2b", 3, 54), "disc": ("e2disc", 6, 38), "orb": ("e2orb", 3, 84), "ebeam": ("e2beam", 3, 62)}
for src, (dst, n, target) in SETS.items():
    imgs = [cv2.imread(f"{d}/{src}_{i}.png", -1) for i in range(n)]
    hs = []
    for a in imgs:
        hsv = cv2.cvtColor(a[..., :3], cv2.COLOR_BGR2HSV)
        m = (a[..., 3] > 128) & (hsv[..., 1] > 90) & (hsv[..., 2] > 120)
        if m.any(): hs.append(np.angle(np.exp(1j * hsv[..., 0][m] * np.pi / 90).mean()) * 90 / np.pi % 180)
    mean = float(np.mean(hs)) if hs else 0
    shift = target - mean
    print(src, "keskisävy", round(mean), "->", target)
    for i, a in enumerate(imgs):
        hsv = cv2.cvtColor(a[..., :3], cv2.COLOR_BGR2HSV).astype(np.float32)
        w = np.clip((hsv[..., 1] - 25) / 60, 0, 1)   # harmaat/valkoiset (sydän) pysyvät valkoisina
        hsv[..., 0] = (hsv[..., 0] + shift * w) % 180
        out = cv2.cvtColor(hsv.astype(np.uint8), cv2.COLOR_HSV2BGR)
        cv2.imwrite(f"{d}/{dst}_{i}.png", np.dstack([out, a[..., 3]]))
