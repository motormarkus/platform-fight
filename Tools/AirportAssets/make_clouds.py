"""Koneen ikkunoiden taivas: yötaivas (kone_taivas.png, 1672 x 941, kaltevuus + tähdet) ja kolme pehmeää pilveä (kone_pilvi_1..3.png, läpinäkyvä tausta).
Ajo: python Tools/AirportAssets/make_clouds.py   (kirjoittaa Assets/Sprites/Taustat/)"""
import os
import numpy as np, cv2
out = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")
rng = np.random.default_rng(7)

# taivas: yläreunasta tumma sininen horisonttia kohti vaaleampi violetti-sininen, tähtiä ylhäällä
H, W = 941, 1672
t = np.linspace(0, 1, H)[:, None]
top = np.array([10, 16, 44]); mid = np.array([30, 44, 100]); low = np.array([86, 80, 140])
col = np.where(t < 0.6, top + (mid - top) * (t / 0.6), mid + (low - mid) * ((t - 0.6) / 0.4))[..., None] if False else None
sky = np.zeros((H, W, 3), np.float32)
for y in range(H):
    u = y / (H - 1)
    c = top + (mid - top) * (u / 0.6) if u < 0.6 else mid + (low - mid) * ((u - 0.6) / 0.4)
    sky[y, :] = c
for _ in range(220):
    x, y = rng.integers(0, W), int(rng.integers(0, int(H * 0.55)))
    b = rng.uniform(120, 255); r = rng.choice([1, 1, 2])
    cv2.circle(sky, (int(x), int(y)), int(r), (b, b, min(255, b + 20)), -1, cv2.LINE_AA)
sky = cv2.GaussianBlur(sky, (0, 0), 0.7)
cv2.imwrite(os.path.join(out, "kone_taivas.png"), cv2.cvtColor(sky.clip(0, 255).astype(np.uint8), cv2.COLOR_RGB2BGR))

# pilvet: sumea valkoinen-sininen, alapuoli tummempi
def cloud(w, h, seed):
    r = np.random.default_rng(seed)
    a = np.zeros((h, w), np.float32)
    n = 14
    for k in range(n):   # pöllyävä kumpupilvi: pienempiä palloja ylälaidassa, litteä pohja
        u = (k + 0.5) / n
        cx = (0.14 + 0.72 * u) * w + r.uniform(-0.03, 0.03) * w
        hump = 0.55 + 0.45 * np.sin(np.pi * u) ** 0.8
        rr = r.uniform(0.55, 1.0) * h * 0.30 * hump
        cy = h * 0.62 - rr * r.uniform(0.2, 0.9)
        cv2.circle(a, (int(cx), int(cy)), int(rr), 1.0, -1, cv2.LINE_AA)
    cv2.rectangle(a, (int(w * 0.14), int(h * 0.55)), (int(w * 0.86), int(h * 0.70)), 1.0, -1)
    a[int(h * 0.74):, :] = 0                                               # litteä pohja
    a = cv2.GaussianBlur(a, (0, 0), h * 0.05).clip(0, 1)
    yy = (np.arange(h)[:, None] / h)
    shade = 1.0 - 0.32 * np.clip((yy - 0.35) / 0.4, 0, 1)                 # alapuoli tummempi, yläreunassa kuutamo
    rgb = np.dstack([np.full((h, w), 222), np.full((h, w), 230), np.full((h, w), 250)]).astype(np.float32) * shade[..., None]
    return np.dstack([rgb[..., 2], rgb[..., 1], rgb[..., 0], (a * 235)]).clip(0, 255).astype(np.uint8)   # BGRA

for i, (w, h) in enumerate(((760, 220), (560, 170), (940, 250)), 1):
    cv2.imwrite(os.path.join(out, f"kone_pilvi_{i}.png"), cloud(w, h, 100 + i)); print("pilvi", i, w, h)
print("ok")
