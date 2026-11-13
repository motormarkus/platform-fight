"""Henkilökunnan huoneen kuvien (vankila_henkilokunta_1/2) sauma: oikean kuvan yläreunan läpinäkyvä kaista täytetään (sky näkyi viivana) ja
saumaan syntynyt kirkkauseroa tasataan pehmeästi (vasen kuva tummenee/oikea vaalenee ~ puolet erosta, vaimenee 220 px:ssä).
Alkuperäiset: Tools/PrisonAssets/alkuperaiset. Ajo: python Tools/PrisonAssets/fix_staff_seam.py"""
import os, numpy as np
from PIL import Image
D = "Assets/Sprites/Taustat"; O = "Tools/PrisonAssets/alkuperaiset"
def blur1d(v, s):
    r = int(s * 3); k = np.exp(-0.5 * (np.arange(-r, r + 1) / s) ** 2); k /= k.sum()
    pad = np.concatenate([np.repeat(v[:1], r, 0), v, np.repeat(v[-1:], r, 0)], 0)
    return np.stack([np.convolve(pad[:, c], k, "valid") for c in range(v.shape[1])], 1)
A = np.array(Image.open(os.path.join(O, "vankila_henkilokunta_1.png")).convert("RGBA"), float)
B = np.array(Image.open(os.path.join(O, "vankila_henkilokunta_2.png")).convert("RGBA"), float)
# 1) yläreunan läpinäkyvä kaista (kaikkialla läpinäkyvä rivi) täytetään ensimmäisellä täydellä rivillä
n = 0
while n < B.shape[0] and B[n, :, 3].mean() < 200: n += 1   # puoliläpinäkyvät rivit (alfa ~128) kuvan yläreunassa
if n: B[:n] = B[n]; print("täytetty yläkaista (rivit)", n)
# 2) kirkkauseron tasaus saumassa
d = blur1d(A[:, -1, :3] - B[:, 0, :3], 5)               # (h,3): A - B
W = 220; x = np.arange(W)
for img, sign, cols in ((A, -0.5, slice(A.shape[1] - W, A.shape[1])), (B, 0.5, slice(0, W))):
    fall = np.exp(-(x if img is B else x[::-1]) / 70.0)  # vaimenee kauemmas saumasta
    img[:, cols, :3] += sign * d[:, None, :] * fall[None, :, None] * (1 if img is B else 1)
    img[:, :, :3] = np.clip(img[:, :, :3], 0, 255)
# A: vähennetään puolet erosta (A - d/2), B: lisätään puolet (B + d/2): sauma kohtaa keskellä
Image.fromarray(A.astype(np.uint8)).save(os.path.join(D, "vankila_henkilokunta_1.png"))
Image.fromarray(B.astype(np.uint8)).save(os.path.join(D, "vankila_henkilokunta_2.png"))
print("valmis; sauma-ero nyt", np.abs(A[:, -1, :3] - B[:, 0, :3]).mean().round(1))
