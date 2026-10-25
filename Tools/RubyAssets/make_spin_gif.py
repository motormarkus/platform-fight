"""Rubyn kierrepotku: 6 kuvaa (2 arkkia x 3) -> kohdistus -> GIF + yksittäiset kuvat spin_kick/. Ajo: python make_spin_gif.py"""
import os, numpy as np
from PIL import Image
import importlib.util
HERE = os.path.dirname(os.path.abspath(__file__))
spec = importlib.util.spec_from_file_location("axe", os.path.join(HERE, "make_axe_gif.py"))
U = r"C:\Users\marku\.claude\uploads\738327ec-6328-4b78-9791-c65803258fd9"
files = ["a2711f59", "2b9525b2", "0b16f30b"]
OUT = os.path.join(HERE, "spin_kick")
# kiihtyy osumaa kohti, osumassa hidastuu, paluu rauhallisempi
MS = [100, 80, 60, 45, 120, 170, 110, 130, 160]
SCALE = 0.55
TIGHTS = np.array([121.0, 77.0, 62.0])
FIX = [1] * 9

def split(im):
    """Kolme hahmoa kuvasta komponenttien mukaan (kuvat voivat mennä päällekkäin x-suunnassa)."""
    import cv2
    arr = np.array(im)
    m = (arr[..., 3] > 20).astype(np.uint8)
    n, lab, st, cen = cv2.connectedComponentsWithStats(cv2.dilate(m, np.ones((9, 9), np.uint8)))
    big = sorted(range(1, n), key=lambda i: -st[i, 4])[:3]
    big = sorted(big, key=lambda i: cen[i][0])
    out = [np.zeros_like(arr) for _ in big]
    for i in range(1, n):
        j = min(range(3), key=lambda k: abs(cen[i][0] - cen[big[k]][0])) if i not in big else big.index(i)
        sel = (lab == i) & (m > 0)
        out[j][sel] = arr[sel]
    return [Image.fromarray(o, "RGBA") for o in out]

def darken(fr):
    import cv2
    im = np.array(fr).astype(np.float32)
    r, g, b, a = im[..., 0], im[..., 1], im[..., 2], im[..., 3]
    m = (a > 200) & (r > 70) & (r < 170) & (r > g) & (g > b) & (r - b > 15) & (r - b < 70)
    m[: int(im.shape[0] * 0.35)] = False
    if m.sum() < 500: return fr
    gain = np.minimum(TIGHTS / np.median(im[m][:, :3], axis=0), 1.0)
    soft = cv2.GaussianBlur(m.astype(np.float32), (5, 5), 0)[..., None]
    im[..., :3] = im[..., :3] * (1 - soft + soft * gain)
    return Image.fromarray(np.clip(im, 0, 255).astype(np.uint8), "RGBA")

frames = []
for f in files:
    frames += split(Image.open(os.path.join(U, f + "-image.png")).convert("RGBA"))
os.makedirs(OUT, exist_ok=True)
items = []
for n, fr in enumerate(frames):
    a = np.array(fr)[..., 3] > 20
    ys, xs = np.where(a)
    fr = fr.crop((xs.min(), ys.min(), xs.max() + 1, ys.max() + 1)); a = a[ys.min():ys.max() + 1, xs.min():xs.max() + 1]
    low = a[int(a.shape[0] * 0.94):]
    fx = np.where(low)[1].mean()
    hr = np.array(fr).astype(int)
    hm = (hr[..., 3] > 20) & (hr[..., 0] > 150) & (hr[..., 1] < 90) & (hr[..., 2] < 70) & (hr[..., 0] - hr[..., 1] > 90)
    k = FIX[n] * 82.0 / (hm.sum() ** 0.5)   # koko tasataan hiusten pinta-alan mukaan, kuten kirvespotkussa (seisova asento = 82)
    fr = darken(fr)
    if k != 1: fr = fr.resize((round(fr.width * k), round(fr.height * k)), Image.LANCZOS); fx *= k
    rgb = np.array(fr).astype(int)
    hair = (rgb[..., 3] > 20) & (rgb[..., 0] > 150) & (rgb[..., 1] < 90) & (rgb[..., 2] < 70) & (rgb[..., 0] - rgb[..., 1] > 90)
    ys2, _ = np.where(hair)
    tor = rgb[..., 3] > 20; tor[: ys2.max() + 5] = False; tor[int(ys2.max() + 0.25 * fr.height):] = False
    tx = np.where(tor)[1].mean()
    fr.save(os.path.join(OUT, "spin_%02d.png" % n))
    items.append([fr, fx, tx])
d = np.mean([it[2] - it[1] for it in items[2:4]])
for i in (0, 1, 8): items[i][1] = items[i][2] - d       # kaksijalkaiset kuvat vartalon mukaan
items = [(a, b) for a, b, _ in items]
import json
json.dump([float(fx) for _, fx in items], open(os.path.join(HERE, 'spin_anchors.json'), 'w'))   # tukijalan x kunkin kuvan sisällä
L = max(fx for _, fx in items); R = max(fr.width - fx for fr, fx in items); H = max(fr.height for fr, _ in items)
CW, CH = int(L + R) + 20, H + 20
tiles = []
for fr, fx in items:
    c = Image.new("RGBA", (CW, CH), (40, 44, 52, 255))
    c.alpha_composite(fr, (int(L - fx) + 10, CH - 10 - fr.height))
    tiles.append(c.resize((int(CW * SCALE), int(CH * SCALE)), Image.LANCZOS).convert("RGB"))
tiles[0].save(os.path.join(HERE, "ruby_spin_kick.gif"), save_all=True, append_images=tiles[1:], duration=MS, loop=0)
big = Image.new("RGB", (tiles[0].width * 9, tiles[0].height))
for i, t in enumerate(tiles): big.paste(t, (i * t.width, 0))
big.save(os.path.join(HERE, "spin_sarja.png"))
print("ok", CW, CH, sum(MS), "ms")
