"""Rubyn kirvespotku: 12 kuvaa (4 arkkia x 3) -> kohdistus tukijalan mukaan -> GIF. Ajo: python make_axe_gif.py"""
import os, numpy as np
from PIL import Image
U = r"C:\Users\marku\.claude\uploads\738327ec-6328-4b78-9791-c65803258fd9"
files = ["38d415a9", "7b13bd41", "7dfb46f3", "ae5654ca"]
HERE = os.path.dirname(os.path.abspath(__file__))
OUT = os.path.join(HERE, "axe_kick")
# ms per frame: nosto nopea, hidastus huipulle, isku alas nopeasti, hidastus alhaalla
# käsin hienosäätö: potkun huipun kuvissa vartalo jäi pieneksi (jalan pituus vs. muut kuvat)
FIX = [1, 1, 0.93, 1.05, 1.06, 1.06, 1.19, 1.21, 1.23, 0.92, 1, 1]
MS = [70, 45, 45, 55, 80, 170, 80, 45, 35, 110, 130, 170]
SCALE = 0.55

TIGHTS = np.array([121.0, 77.0, 62.0])   # sukkahousujen tavoitesävy (huipun kuvien tummuus)

def darken_tights(fr):
    """Sukkahousut samaan sävyyn kaikissa kuvissa (osa kuvista oli vaaleampia)."""
    im = np.array(fr).astype(np.float32)
    r, g, b, a = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
    m = (a > 200) & (r > 70) & (r < 170) & (r > g) & (g > b) & (r - b > 15) & (r - b < 70)
    m[: int(im.shape[0] * 0.35)] = False
    if m.sum() < 500: return fr
    med = np.median(im[m][:, :3], axis=0)
    gain = np.minimum(TIGHTS / med, 1.0)               # vain tummennetaan
    import cv2
    soft = cv2.GaussianBlur(m.astype(np.float32), (5, 5), 0)[..., None]
    im[..., :3] = im[..., :3] * (1 - soft + soft * gain)
    return Image.fromarray(np.clip(im, 0, 255).astype(np.uint8), "RGBA")

def split(im):
    a = np.array(im)[..., 3] > 20
    col = a.any(0); W = len(col); gaps = []; i = 0
    while i < W:
        if not col[i]:
            j = i
            while j < W and not col[j]: j += 1
            if i > 0 and j < W: gaps.append((j - i, i, j))
            i = j
        else: i += 1
    g = sorted(sorted(gaps, reverse=True)[:2], key=lambda x: x[1])
    cuts = [0] + [(x[1] + x[2]) // 2 for x in g] + [W]
    return [im.crop((cuts[k], 0, cuts[k + 1], im.height)) for k in range(3)]

frames = []
for f in files:
    frames += split(Image.open(os.path.join(U, f + "-image.png")).convert("RGBA"))
os.makedirs(OUT, exist_ok=True)
items = []
for n, fr in enumerate(frames):
    a = np.array(fr)[..., 3] > 20
    ys, xs = np.where(a)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    fr = fr.crop((x0, y0, x1, y1)); a = a[y0:y1, x0:x1]
    h = a.shape[0]
    low = a[int(h * 0.94):]                       # tukijalan kenkä
    fx = np.where(low)[1].mean()
    rgb = np.array(fr).astype(int)
    hair = (rgb[..., 3] > 20) & (rgb[..., 0] > 150) & (rgb[..., 1] < 90) & (rgb[..., 2] < 70) & (rgb[..., 0] - rgb[..., 1] > 90)
    k = FIX[n] * 82.0 / (hair.sum() ** 0.5)               # koko tasataan hiusten pinta-alan mukaan
    fr = fr.resize((round(fr.width * k), round(fr.height * k)), Image.LANCZOS); fx *= k
    ys2, xs2 = np.where(hair); hx = xs2.mean() * k; hy = ys2.max() * k
    tor = np.array(fr)[..., 3] > 20; tor[: int(hy) + 5] = False; tor[int(hy + 0.25 * fr.height):] = False
    tx = np.where(tor)[1].mean()
    fr = darken_tights(fr)
    fr.save(os.path.join(OUT, "axe_%02d.png" % n))
    items.append([fr, fx, tx])
d = np.mean([it[2] - it[1] for it in items[1:3]])   # vartalon keskikohta tukijalan suhteen seisoessa
for i in (0, 9, 10, 11):
    items[i][1] = items[i][2] - d                    # kaksijalkaisissa kuvissa kohdistus vartalon mukaan
items = [(a, b) for a, b, _ in items]
import json
json.dump([float(fx) for _, fx in items], open(os.path.join(HERE, 'axe_anchors.json'), 'w'))   # tukijalan x kunkin kuvan sisällä
L = max(fx for _, fx in items); R = max(fr.width - fx for fr, fx in items); H = max(fr.height for fr, _ in items)
CW, CH = int(L + R) + 20, H + 20
tiles = []
for fr, fx in items:
    c = Image.new("RGBA", (CW, CH), (40, 44, 52, 255))
    c.alpha_composite(fr, (int(L - fx) + 10, CH - 10 - fr.height))
    c = c.resize((int(CW * SCALE), int(CH * SCALE)), Image.LANCZOS).convert("RGB")
    tiles.append(c)
tiles[0].save(os.path.join(HERE, "ruby_axe_kick.gif"), save_all=True, append_images=tiles[1:], duration=MS, loop=0, optimize=False)
print("ok", CW, CH, sum(MS), "ms")
