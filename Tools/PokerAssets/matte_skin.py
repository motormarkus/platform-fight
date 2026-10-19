"""Vähentää ihon kiiltoa (vahanukkelook) pöytäkuvassa. Lukee alkuperäisen, kirjoittaa Resources-kuvan.
Käyttö: python Tools/PokerAssets/matte_skin.py [strength 0..1]"""
import sys, numpy as np, cv2
SRC = 'Tools/PokerAssets/src/TableScene-original.jpg'
DST = 'Assets/Resources/Poker/Artwork/TableScene.jpg'
strength = float(sys.argv[1]) if len(sys.argv) > 1 else .8
img = cv2.imread(SRC).astype(np.float32) / 255
H, W = img.shape[:2]; k = W / 2000.0   # laatikot annettu 2000 px leveässä esikatselussa
# Hahmojen ihoalueet (x0,y0,x1,y1) 2000 px esikatselussa: päät, kaulat, käsivarret/rinnat
boxes = [(150, 180, 460, 480), (440, 50, 960, 420), (1060, 70, 1560, 420), (1620, 180, 1990, 560)]
box = np.zeros((H, W), np.float32)
for x0, y0, x1, y1 in boxes: box[int(y0*k):int(y1*k), int(x0*k):int(x1*k)] = 1
box = cv2.GaussianBlur(box, (0, 0), 14 * k)
bgr = (img * 255).astype(np.uint8)
hsv = cv2.cvtColor(bgr, cv2.COLOR_BGR2HSV).astype(np.float32)
h, s, v = hsv[..., 0] * 2, hsv[..., 1] / 255, hsv[..., 2] / 255
# Ihosävy: lämmin (punainen-oranssi-keltainen), kohtalainen kylläisyys; mukaan myös kiiltävä lähes valkoinen kohta
warm = ((h < 50) | (h > 345)).astype(np.float32)
skin = warm * np.clip((s - .12) / .12, 0, 1) * np.clip((v - .25) / .15, 0, 1)
skin = cv2.GaussianBlur(skin, (0, 0), 2 * k)
skin = cv2.dilate(skin, np.ones((int(5 * k) | 1,) * 2)) * 0 + skin  # (säilytetään pehmeänä)
# kiiltokohdat sisältävät alueen: sulje reiät (kiilto on vaalea, matalakylläinen) ihon sisällä
region = cv2.GaussianBlur(cv2.morphologyEx(skin, cv2.MORPH_CLOSE, np.ones((int(31 * k) | 1,) * 2)), (0, 0), 6 * k)
mask = np.clip(region * 1.3, 0, 1) * box
# Paikallinen perustaso (ilman kiiltoa): iso blur ihoalueen painotuksella
w3 = np.repeat((skin * box)[..., None], 3, 2)
num = cv2.GaussianBlur(img * w3, (0, 0), 22 * k); den = cv2.GaussianBlur(w3[..., 0], (0, 0), 22 * k)[..., None] + 1e-4
base = num / den
L = img.mean(2, keepdims=True); Lb = base.mean(2, keepdims=True)
spec = np.clip((L - Lb - .05) / .16, 0, 1)          # kuinka paljon pikseli on kiiltävämpi kuin ympäristö
spec = cv2.GaussianBlur(spec, (0, 0), 1.2 * k)[..., None]
m = (mask[..., None] * spec * strength)
# Korvaa kiilto perusihosävyllä, mutta säilytä osa hienosta yksityiskohdasta
detail = img - cv2.GaussianBlur(img, (0, 0), 3 * k)
target = base * (L / (L + 1e-4)) * 0 + base + detail * .5
out = img * (1 - m) + target * m
# Pieni yleinen kontrastin pehmennys ihossa (pois vahamaisuus): hieman kylläisyyttä lisää tummennetuille kohdille
cv2.imwrite(DST, np.clip(out * 255, 0, 255).astype(np.uint8), [cv2.IMWRITE_JPEG_QUALITY, 95])
cv2.imwrite('Tools/PokerAssets/src/matte-mask-preview.png', (mask * 255).astype(np.uint8))
print('ok', W, H)
