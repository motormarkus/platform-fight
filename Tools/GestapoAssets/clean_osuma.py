"""Gestapon osumakuvien siivous: keltaoranssi jäänne (videon tehoste) pois ja irtonaiset palaset pois. Käyttö: python clean_osuma.py <gestapo_osuma.png>"""
import sys, cv2, numpy as np
p = sys.argv[1]; im = cv2.imread(p, cv2.IMREAD_UNCHANGED); CW, CH = 896, 768
H, W = im.shape[:2]
for r in range(H // CH):
    for c in range(W // CW):
        t = im[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW]
        if (t[..., 3] > 128).sum() < 200: continue
        hsv = cv2.cvtColor(t[..., :3], cv2.COLOR_BGR2HSV)
        orange = (t[..., 3] > 0) & (hsv[..., 0] >= 8) & (hsv[..., 0] <= 34) & (hsv[..., 1] > 120) & (hsv[..., 2] > 150)
        orange = cv2.dilate(orange.astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool)
        a = t[..., 3].copy(); a[orange] = 0
        n, lab, st, _ = cv2.connectedComponentsWithStats((a > 40).astype(np.uint8), connectivity=8)
        if n > 1:
            keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA])); a[lab != keep] = 0
        print("ruutu", r * (W // CW) + c, "poistettu", int(((t[..., 3] > 0) & (a == 0)).sum()), "px")
        t[..., 3] = a
cv2.imwrite(p, im)
