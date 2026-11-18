"""Koodilla tehty jäälaikka (lattialle, ylhäältä-viistosti kuten kermaläikät): läpikuultava sinivalkoinen jää, halkeamat, kiilto ja huurteiset reunat.
Ajo: python Tools/IceAssets/make_ice.py [ulostulokansio]   (oletus Tools/IceAssets/ice_preview)"""
import os, sys
import numpy as np, cv2
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); OUT = sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, "ice_preview"); os.makedirs(OUT, exist_ok=True)
def fbm(h, w, rng, octaves=5, base=6):
    out = np.zeros((h, w), np.float32); amp = 1.0; tot = 0
    for o in range(octaves):
        s = base * 2 ** o; n = rng.random((max(2, int(s * h / w) + 2), s + 2)).astype(np.float32)
        out += amp * cv2.resize(n, (w, h), interpolation=cv2.INTER_CUBIC); tot += amp; amp *= .5
    out /= tot; return (out - out.min()) / (out.max() - out.min() + 1e-6)
def make(seed, W=1000, H=150):
    rng = np.random.default_rng(seed)
    yy, xx = np.mgrid[0:H, 0:W].astype(np.float32); cx, cy = W / 2, H / 2
    # epäsäännöllinen soikea reuna huurretikuilla
    ang = np.arctan2((yy - cy) / (H / 2), (xx - cx) / (W / 2)); r = np.sqrt(((xx - cx) / (W / 2)) ** 2 + ((yy - cy) / (H / 2)) ** 2)
    edge_noise = fbm(1, 720, rng, 4, 18)[0]; ang_i = ((ang + np.pi) / (2 * np.pi) * 719).astype(int)
    limit = .86 + .12 * (edge_noise[ang_i] - .5) * 2.2
    spikes = np.clip(np.sin(ang * 23 + seed) * np.sin(ang * 41 + seed * 2), 0, 1) * .05
    inside = np.clip((limit + spikes - r) / .09, 0, 1)
    # halkeamat: Voronoi-reunat
    pts = np.stack([rng.random(26) * W, rng.random(26) * H], 1); d = np.full((H, W), 1e9, np.float32); d2 = d.copy()
    for px, py in pts:
        dd = np.sqrt((xx - px) ** 2 + ((yy - py) * 3.2) ** 2)
        m = dd < d; d2 = np.where(m, d, np.minimum(d2, dd)); d = np.where(m, dd, d)
    crack = np.clip(1 - (d2 - d) / 7.0, 0, 1) ** 2
    crack *= np.clip(fbm(H, W, rng, 3, 5) * 2 - .45, 0, 1)                     # kaikki rajat eivät halkea
    n1 = fbm(H, W, rng, 5, 5); n2 = fbm(H, W, rng, 4, 14)
    base = np.stack([150 + 70 * n1, 200 + 45 * n1, 238 + 15 * n1], -1)          # sinivalkoinen jää
    base *= (.88 + .12 * n2)[..., None]
    # syvyys: alareuna tummempi, yläreuna vaaleampi
    depth = np.clip((yy - cy) / (H / 2), -1, 1); base *= (1 - .10 * np.clip(depth, 0, 1))[..., None]; base += (14 * np.clip(-depth, 0, 1))[..., None]
    # kiiltoviirut (vinot, pitkät) ja kimallus
    sx = (xx * .9 + yy * 2.2) / 38.0; streak = np.clip(np.sin(sx) * np.sin(sx * .37 + 1.3), 0, 1) ** 6 * (n2 > .5) * 1.0
    spark = (rng.random((H, W)) > .9985).astype(np.float32); spark = cv2.GaussianBlur(spark, (0, 0), 1.6) * 18
    col = base + 70 * streak[..., None] + 255 * np.clip(spark, 0, 1)[..., None]
    col = col * (1 - .55 * crack[..., None]) + np.array([245, 252, 255], np.float32) * (.55 * crack[..., None])    # halkeamat vaaleina viivoina
    # sisäreunan huurre: valkoisempi lähellä reunaa
    frost = np.clip((r - .62) / .3, 0, 1) * n1; col = col * (1 - .6 * frost[..., None]) + 250 * .6 * frost[..., None]
    alpha = inside * (.72 + .16 * n1 + .10 * streak + .12 * crack); alpha = np.clip(alpha, 0, 1)
    alpha = np.minimum(alpha, 1.0) * (1 - np.clip((r - .96) / .05, 0, 1))
    rgba = np.dstack([np.clip(col, 0, 255), alpha * 255]).astype(np.uint8)
    return Image.fromarray(rgba, "RGBA").resize((W, H), Image.LANCZOS)
sizes = [(1000, 110), (1000, 150), (1000, 130)]
for i, (w, h) in enumerate(sizes, 1):
    make(i * 17 + 3, w, h).save(os.path.join(OUT, "jaa_%d.png" % i))
print("ok", os.listdir(OUT))
