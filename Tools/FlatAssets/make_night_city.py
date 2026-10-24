"""Yökaupunki kämpän ikkunoihin: kolme parallaksikerrosta + ikkunalasit läpinäkyviksi taustoihin.

Ajo: python Tools/FlatAssets/make_night_city.py
Tuottaa Assets/Sprites/Taustat/:
  kampa_yo_taivas.png   (taivas, tähdet, kuu)       600x440, liikkuu vähiten
  kampa_yo_kaukana.png  (kaukainen siluetti)         700x440
  kampa_yo_lahella.png  (lähirivi, valaistut ikkunat) 800x440, liikkuu eniten
  kampa.png             (lasit läpinäkyviksi; alkuperäinen Tools/FlatAssets/orig/kampa.png)
  kampa_makuuhuone.png  (makuuhuone, ikkunalasit läpinäkyviksi)
Kuvat ovat samassa pikselimittakaavassa kuin kampa.png (941 px korkea), joten niillä on sama ppu.
"""
import os, random, shutil
import numpy as np
import cv2
from PIL import Image, ImageDraw

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Sprites", "Taustat")
ORIG = os.path.join(ROOT, "Tools", "FlatAssets", "orig")
os.makedirs(ORIG, exist_ok=True)
H = 440
SS = 2  # supersample

random.seed(7)


def lerp(a, b, t):
    return tuple(int(a[i] + (b[i] - a[i]) * t) for i in range(3))


def sky(w):
    im = Image.new("RGB", (w * SS, H * SS))
    d = ImageDraw.Draw(im)
    top, mid, hor = (8, 7, 30), (30, 18, 66), (96, 44, 110)
    for y in range(H * SS):
        t = y / (H * SS - 1)
        c = lerp(top, mid, t / 0.6) if t < 0.6 else lerp(mid, hor, (t - 0.6) / 0.4)
        d.line([(0, y), (w * SS, y)], fill=c)
    # tähdet
    for _ in range(95):
        x, y = random.randint(0, w * SS - 1), random.randint(0, int(H * SS * 0.7))
        r = random.choice([1, 1, 1, 2, 2, 3])
        c = random.choice([(235, 230, 255), (255, 240, 200), (190, 180, 255)])
        d.ellipse([x - r, y - r, x + r, y + r], fill=c)
    for _ in range(7):  # isommat ristitähdet
        x, y = random.randint(40, w * SS - 40), random.randint(20, int(H * SS * 0.5))
        for dx, dy in ((1, 0), (0, 1)):
            d.line([(x - 7 * dx, y - 7 * dy), (x + 7 * dx, y + 7 * dy)], fill=(255, 250, 230), width=2)
    # kuu (kuunsirppi, kuten seinätaulussa)
    mx, my, mr = int(w * SS * 0.30), int(H * SS * 0.25), 46 * SS // 2
    glow = Image.new("RGB", im.size, (0, 0, 0))
    gd = ImageDraw.Draw(glow)
    gd.ellipse([mx - mr * 3, my - mr * 3, mx + mr * 3, my + mr * 3], fill=(40, 30, 70))
    glow = glow.filter(__import__("PIL.ImageFilter", fromlist=["x"]).GaussianBlur(mr))
    im = Image.fromarray(np.clip(np.array(im).astype(int) + np.array(glow).astype(int), 0, 255).astype(np.uint8))
    d = ImageDraw.Draw(im)
    full = Image.new("L", im.size, 0); ImageDraw.Draw(full).ellipse([mx - mr, my - mr, mx + mr, my + mr], fill=255)
    bite = Image.new("L", im.size, 0); ImageDraw.Draw(bite).ellipse([mx - mr + 22, my - mr - 8, mx + mr + 22, my + mr - 8], fill=255)
    cres = Image.fromarray(np.clip(np.array(full).astype(int) - np.array(bite).astype(int), 0, 255).astype(np.uint8))
    rim = cres.filter(__import__("PIL.ImageFilter", fromlist=["x"]).MaxFilter(7))
    im.paste((20, 12, 40), mask=rim)
    im.paste((232, 222, 200), mask=cres)
    # ohut pilvi
    cl = Image.new("RGBA", im.size, (0, 0, 0, 0))
    cd = ImageDraw.Draw(cl)
    for cx, cy, cw in ((int(w * SS * 0.70), int(H * SS * 0.42), 150), (int(w * SS * 0.12), int(H * SS * 0.55), 110)):
        for k in range(5):
            cd.ellipse([cx + k * cw // 6 - 30, cy - 10 - (k % 2) * 8, cx + k * cw // 6 + 50, cy + 14], fill=(70, 40, 100, 120))
    im = Image.alpha_composite(im.convert("RGBA"), cl.filter(__import__("PIL.ImageFilter", fromlist=["x"]).GaussianBlur(4))).convert("RGB")
    return im.resize((w, H), Image.LANCZOS)


def skyline(w, base, hmin, hmax, body, edge, win_cols, win_p, wmin, wmax, seed, extras=False):
    random.seed(seed)
    im = Image.new("RGBA", (w * SS, H * SS), (0, 0, 0, 0))
    d = ImageDraw.Draw(im)
    x = -random.randint(0, 40)
    while x < w:
        bw = random.randint(wmin, wmax)
        bh = random.randint(hmin, hmax)
        x0, x1, y0, y1 = x * SS, (x + bw) * SS, (base - bh) * SS, H * SS + 4
        d.rectangle([x0, y0, x1, y1], fill=body, outline=edge, width=3 if extras else 2)
        # kattovarusteet
        if random.random() < 0.55:
            ax = x0 + random.randint(8, max(9, x1 - x0 - 8))
            d.line([(ax, y0), (ax, y0 - random.randint(14, 34) * SS // 2)], fill=edge, width=3)
        if random.random() < 0.35:
            d.rectangle([x0 + 10, y0 - 10 * SS // 2, x0 + 10 + 20 * SS // 2, y0], fill=body, outline=edge, width=2)
        # ikkunat
        step = 11 if extras else 9
        for wy in range(y0 + 14, y1 - 8, step * SS):
            for wx in range(x0 + 8, x1 - 12, int(step * 0.9) * SS):
                if random.random() < win_p:
                    c = random.choice(win_cols)
                    d.rectangle([wx, wy, wx + 4 * SS, wy + 5 * SS], fill=c)
        x += bw - random.randint(0, 3)
    if extras:
        # neonkyltti
        sx, sy = int(w * SS * 0.58), int((base - hmax + 40) * SS)
        d.rectangle([sx, sy, sx + 74 * SS // 2, sy + 22 * SS // 2], fill=(40, 10, 60), outline=(10, 4, 20), width=3)
        d.rectangle([sx + 6, sy + 6, sx + 74 * SS // 2 - 6, sy + 22 * SS // 2 - 6], outline=(255, 90, 200), width=3)
        d.line([(sx + 14, sy + 11 * SS // 2 + 2), (sx + 74 * SS // 2 - 14, sy + 11 * SS // 2 + 2)], fill=(120, 240, 255), width=3)
    return im.resize((w, H), Image.LANCZOS)


def make_layers():
    sky(600).save(os.path.join(OUT, "kampa_yo_taivas.png"))
    far = skyline(700, 330, 70, 190, (28, 20, 58, 255), (12, 8, 30, 255),
                  [(200, 150, 80, 255), (170, 110, 190, 255), (120, 90, 170, 255)], 0.10, 36, 70, 11)
    # vaalea horisonttihohde taakse
    far.save(os.path.join(OUT, "kampa_yo_kaukana.png"))
    near = skyline(800, 420, 110, 300, (17, 13, 38, 255), (6, 4, 14, 255),
                   [(255, 205, 110, 255), (255, 170, 80, 255), (190, 130, 230, 255), (120, 220, 235, 255)], 0.22, 52, 96, 23, extras=True)
    near.save(os.path.join(OUT, "kampa_yo_lahella.png"))


def save_orig(name):
    src = os.path.join(OUT, name)
    dst = os.path.join(ORIG, name)
    if not os.path.exists(dst):
        shutil.copy(src, dst)
    return dst


def hole_dark(name_in, name_out, box, white=False):
    """Tekee ikkunalasit läpinäkyviksi (musta tai valkoinen lasi, ohut kehys säilyy)."""
    src = save_orig(name_in) if os.path.dirname(name_in) == "" else name_in
    im = cv2.imread(src, cv2.IMREAD_COLOR)
    x0, y0, x1, y1 = box
    sub = im[y0:y1, x0:x1].astype(int)
    m = (sub.min(2) > 245) if white else (sub.max(2) < 45)
    m = m.astype(np.uint8) * 255
    k = np.ones((7, 7), np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_OPEN, k)          # ohuet viivat (kehys) pois
    m = cv2.dilate(m, np.ones((3, 3), np.uint8))         # lasin reunan tumma raja pois (jäämät)
    m = cv2.GaussianBlur(m, (3, 3), 0)
    a = np.full(im.shape[:2], 255, np.uint8)
    a[y0:y1, x0:x1] = 255 - m
    rgba = cv2.cvtColor(im, cv2.COLOR_BGR2BGRA)
    rgba[..., 3] = a
    cv2.imwrite(os.path.join(OUT, name_out), rgba)
    print(name_out, "läpinäkyviä pikseleitä:", int((a < 128).sum()))


if __name__ == "__main__":
    # (omat kerrokset korvattu valmiilla kuvalla kampa_yo_kaupunki.png; make_layers() ei enää käytössä)
    hole_dark("kampa.png", "kampa.png", (2845, 45, 3275, 455))
    # makuuhuoneen kuva: lähde annetaan polkuna ympäristömuuttujassa (valkoiset ikkunalasit)
    bed = os.environ.get("MAKUUHUONE_SRC")
    if bed:
        shutil.copy(bed, os.path.join(ORIG, "kampa_makuuhuone_src.png"))
        hole_dark(os.path.join(ORIG, "kampa_makuuhuone_src.png"), "kampa_makuuhuone.png", (640, 60, 1030, 375), white=True)
