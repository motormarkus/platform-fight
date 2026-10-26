"""Koneen ikkunoiden tausta generaattorin kuvista: taivas (kone_taivas.png, vaakasuunnassa saumaton) ja pilvikerros mustalla taustalla (kone_pilvikerros.png, musta -> alfa, saumaton).
Reunan pieni epäsuhta tasoitetaan 80 px:n alueella, jotta kuva toistuu ilman saumaa.
Ajo: python Tools/AirportAssets/make_plane_sky.py <taivas.png> <pilvet.png>"""
import os, sys
import numpy as np, cv2
out = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
L = 80

def seamless(img):
    img = img.astype(np.float32); W = img.shape[1]
    m = (img[:, 0] + img[:, -1]) / 2.0
    d0, d1 = m - img[:, 0], m - img[:, -1]
    for i in range(L):
        k = (1.0 - i / L) ** 1.5
        img[:, i] += d0 * k
        img[:, W - 1 - i] += d1 * k
    return img

sky = seamless(cv2.imread(sys.argv[1], cv2.IMREAD_COLOR))
cv2.imwrite(os.path.join(out, "kone_taivas.png"), sky.clip(0, 255).astype(np.uint8))

cl = seamless(cv2.imread(sys.argv[2], cv2.IMREAD_COLOR))
a = cl.max(2) / 255.0
a = np.clip((a - 0.03) * 1.25, 0, 1)                       # mustan tausta pois, pehmeä reuna säilyy
col = np.where(a[..., None] > 0.01, cl / np.maximum(a[..., None], 0.05), 0).clip(0, 255)
rgba = np.dstack([col, a * 255.0]).astype(np.uint8)
cv2.imwrite(os.path.join(out, "kone_pilvikerros.png"), rgba)
print("ok", sky.shape, rgba.shape)
