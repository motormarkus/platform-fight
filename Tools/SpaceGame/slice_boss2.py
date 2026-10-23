"""Tuhoaja: kolmen aluksen arkki (violetti tausta) -> boss2_0..2.png (sama kangas, keskitetty). Käyttö: slice_boss2.py <arkki> <ulos-kansio>"""
import sys, numpy as np, cv2
src, out = sys.argv[1], sys.argv[2]
im = cv2.imread(src)
rgb = cv2.cvtColor(im, cv2.COLOR_BGR2RGB)
hsv = cv2.cvtColor(rgb, cv2.COLOR_RGB2HSV).astype(np.float32)
h, s, v = hsv[..., 0], hsv[..., 1] / 255, hsv[..., 2]
bgm = ((h > 135) & (h < 162)).astype(np.float32) * np.clip((s - 0.45) / 0.25, 0, 1) * np.clip((v - 50) / 40, 0, 1)
a = 1 - bgm
a = cv2.GaussianBlur(a, (0, 0), 0.7); a = np.clip((a - 0.1) / 0.8, 0, 1)
f = rgb.astype(np.float32)
edge = (a > 0.02) & (a < 0.98)
bgc = np.array([123, 0, 125], np.float32)
aa = np.maximum(a[edge], 0.25)[:, None]
f[edge] = np.clip((f[edge] - (1 - aa) * bgc) / aa, 0, 255)
rgba = np.dstack([f, a * 255]).astype(np.uint8)
rows = (a > 0.5).sum(axis=1)
H = im.shape[0]
# raot rivien tyhjistä väleistä
gaps = [y for y in range(H) if rows[y] < 3]
bands, start = [], None
for y in range(H):
    if rows[y] >= 3 and start is None: start = y
    if rows[y] < 3 and start is not None:
        if y - start > 50: bands.append((start, y))
        start = None
if start is not None: bands.append((start, H))
print("bands", bands)
assert len(bands) == 3, "odotettiin 3 alusta"
boxes = []
for y0, y1 in bands:
    m = (a[y0:y1] > 0.1)
    xs = np.where(m.any(axis=0))[0]
    boxes.append((xs.min(), xs.max(), y0, y1))
# keskitys: kaikki samaan kankaaseen siten, että alusten silmä (kuvan pystyakselin keskipiste) osuu samaan kohtaan
cxs = [(b[0] + b[1]) / 2 for b in boxes]
bw = [b[1] - b[0] for b in boxes]
# ankkuri: ensimmäisen aluksen vasen/oikea reuna; sijoitus absoluuttisen x:n mukaan (alukset samalla x-akselilla)
L = min(b[0] for b in boxes); R = max(b[1] for b in boxes)
Hc = max(b[3] - b[2] for b in boxes)
W = R - L + 8; Hh = Hc + 8
for i, (x0, x1, y0, y1) in enumerate(boxes):
    can = np.zeros((Hh, W, 4), np.uint8)
    crop = rgba[y0:y1, L:R + 1]
    # pystysuunnassa ankkuri: laipan keskikohta = alueen keskipiste
    oy = (Hh - crop.shape[0]) // 2
    can[oy:oy + crop.shape[0], 4:4 + crop.shape[1]] = crop
    cv2.imwrite(f"{out}/boss2_{i}.png", cv2.cvtColor(can, cv2.COLOR_RGBA2BGRA))
    print(i, can.shape)
