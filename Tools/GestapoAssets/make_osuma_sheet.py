"""Gestapon osumareaktio (ei kaada): viiden kuvan rivi (läpinäkyvä tausta) -> Assets/Sprites/Viholliset/gestapo_osuma_kevyt.png (ruutu 896 x 768, 4 saraketta, jalat 16 px ylhäällä, hahmon keskikohta x 420).
Mittakaava: ensimmäisen kuvan korkeus = gestapo_idle (640 px). Käyttö: python make_osuma_sheet.py <kuvarivi.png> [ulos-kansio]"""
import sys, os, cv2, numpy as np
src = sys.argv[1]; out = sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset"
CW, CH, COLS, FOOT, CX = 896, 768, 4, 16, 420
im = cv2.imread(src, cv2.IMREAD_UNCHANGED)
n, lab, st, _ = cv2.connectedComponentsWithStats((im[..., 3] > 128).astype(np.uint8), connectivity=8)
comps = sorted([i for i in range(1, n) if st[i, 4] > 5000], key=lambda i: st[i, 0])
k = 640.0 / st[comps[0], 3]
sheet = np.zeros((CH * 2, CW * COLS, 4), np.uint8)
for j, i in enumerate(comps):
    x, y, w, h, _ = st[i]
    c = im[y:y + h, x:x + w].copy(); c[..., 3] = np.where(lab[y:y + h, x:x + w] == i, c[..., 3], 0)
    c = cv2.resize(c, (int(round(w * k)), int(round(h * k))), interpolation=cv2.INTER_AREA)
    ox = CX - c.shape[1] // 2; oy = CH - FOOT - c.shape[0]
    r, col = divmod(j, COLS)
    sheet[r * CH + oy:r * CH + oy + c.shape[0], col * CW + ox:col * CW + ox + c.shape[1]] = c
cv2.imwrite(os.path.join(out, "gestapo_osuma_kevyt.png"), sheet); print(len(comps), "kuvaa, mittakaava", round(k, 3))
