# Kuvien saumojen tasaus: oikean ja vasemman reunasarakkeen värieron puolikas levitetään pehmeästi (W px) molemmille puolille. Alkuperäiset: Assets/ArtSources/Thaitown/.
import os, shutil, numpy as np
from PIL import Image
import cv2
T = "Assets/Sprites/Taustat/"
SRC = "Assets/ArtSources/Thaitown/"
W = 160
def load(n):
    bak = SRC + n
    if not os.path.exists(bak): shutil.copy(T + n, bak)      # alkuperäinen talteen (korjataan aina alkuperäisestä)
    return np.array(Image.open(bak).convert("RGBA")).astype(np.float32)
def seam(L, R, left_only=False):
    h = L.shape[0]
    lc = L[:, -3:, :3].mean(axis=1); rc = R[:, :3, :3].mean(axis=1)
    ok = (L[:, -3:, 3].min(axis=1) > 250) & (R[:, :3, 3].min(axis=1) > 250)
    d = (rc - lc)                         # kuinka paljon oikea on vasenta vaaleampi
    d[~ok] = 0
    d = cv2.GaussianBlur(d, (1, 0), 8) if False else np.stack([cv2.GaussianBlur(d[:, c].reshape(-1, 1), (0, 0), 8).ravel() for c in range(3)], axis=1)
    ramp = np.linspace(0, 1, W) ** 1.5    # 0 kaukana, 1 reunassa
    k = 1.0 if left_only else 0.5
    L[:, -W:, :3] += (d * k)[:, None, :] * ramp[None, :, None]
    if not left_only: R[:, :W, :3] -= (d * 0.5)[:, None, :] * ramp[::-1][None, :, None]
def save(n, A):
    Image.fromarray(np.clip(A, 0, 255).astype(np.uint8), "RGBA").save(T + n)
# Kadun uusien kuvien limitykset (px): oikeanpuoleisen kuvan vasen reuna häivytetään ja kuva siirretään vasemman päälle (sisältö täsmää: tolpat osuvat päällekkäin).
STREET_OVERLAP = {"thaitown_katu_vasen_2.png": 186, "thaitown_katu_vasen_1.png": 102}
groups = [
    (["thaitown_katu_vasen_4.png", "thaitown_katu_vasen_3.png", "thaitown_katu_vasen_2.png", "thaitown_katu_vasen_1.png", "thaitown_katu1.png"], True),
    (["thaitown_klubi_3.png", "thaitown_klubi_2.png", "thaitown_klubi_1.png"], False),
    (["thaitown_panttikonttori_1.png", "thaitown_panttikonttori_2.png"], False),
]
for names, lastfixed in groups:
    imgs = [load(n) for n in names]
    for i in range(len(imgs) - 1):
        ov = STREET_OVERLAP.get(names[i + 1], 0)
        if ov: continue                                   # limittyvä sauma: sisältö täsmää, ei värikorjausta
        seam(imgs[i], imgs[i + 1], left_only=(lastfixed and i == len(imgs) - 2))
    if names[0] == "thaitown_klubi_3.png":
        for i in range(1, len(imgs)):   # klubin kuvat limittyvät 48 px: oikeanpuoleisen kuvan vasen reuna häivytetään
            imgs[i][:, :48, 3] = np.linspace(0, 255, 48)[None, :]
    for i, n in enumerate(names):
        ov = STREET_OVERLAP.get(n, 0)
        if ov: imgs[i][:, :ov, 3] = np.linspace(0, 255, ov)[None, :]
        if n == "thaitown_katu1.png": continue          # asemakatua ei muuteta
        save(n, imgs[i])
# tiskin leikkeet uudestaan korjatuista sisäkuvista
Image.open(T + "thaitown_panttikonttori_1.png").convert("RGBA").crop((360, 424, 1672, 648)).save(T + "thaitown_pantti_tiski_1.png")
Image.open(T + "thaitown_panttikonttori_2.png").convert("RGBA").crop((0, 424, 1040, 648)).save(T + "thaitown_pantti_tiski_2.png")
Image.open(T + "thaitown_klubi_2.png").convert("RGBA").crop((735, 388, 1235, 552)).save(T + "thaitown_klubi_tiski.png")
print("valmis")
