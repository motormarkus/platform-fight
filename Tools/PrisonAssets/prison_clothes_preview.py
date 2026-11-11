"""Esikatselu: vankivaatteet olemassa olevista vihuista värjäämällä (ei uusia kuvia): Kovis (vihu_idle), Lippis, Punkkari, Seilori.
Vaatteiden värit -> vankioranssi (varjostus säilyy). Ajo: python Tools/PrisonAssets/prison_clothes_preview.py <ulos.png>"""
import sys, colorsys
import numpy as np
from PIL import Image
V = "Assets/Sprites/Viholliset/"
ORANGE_H = 24 / 360.0

def hsv(a):
    rgb = a[..., :3].astype(np.float32) / 255.0
    mx, mn = rgb.max(-1), rgb.min(-1); d = mx - mn
    h = np.zeros_like(mx); r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    m = d > 1e-6
    h = np.where(m & (mx == r), ((g - b) / np.maximum(d, 1e-6)) % 6, h)
    h = np.where(m & (mx == g), (b - r) / np.maximum(d, 1e-6) + 2, h)
    h = np.where(m & (mx == b), (r - g) / np.maximum(d, 1e-6) + 4, h)
    return (h / 6.0) % 1.0, np.where(mx > 1e-6, d / np.maximum(mx, 1e-6), 0), mx

def to_rgb(h, s, v):
    i = np.floor(h * 6).astype(int) % 6; f = h * 6 - np.floor(h * 6)
    p, q, t = v * (1 - s), v * (1 - f * s), v * (1 - (1 - f) * s)
    r = np.choose(i, [v, q, p, p, t, v]); g = np.choose(i, [t, v, v, q, p, p]); b = np.choose(i, [p, p, t, v, v, q])
    return np.stack([r, g, b], -1)

def recolor(a, mask, vmap):
    """mask-alueen väri -> oranssi, kirkkaus vmap(v):n mukaan (varjostus säilyy)."""
    h, s, v = hsv(a)
    nh = np.where(mask, ORANGE_H, h); ns = np.where(mask, 0.92, s); nv = np.where(mask, np.clip(vmap(v), 0, 1), v)
    out = a.copy(); out[..., :3] = (to_rgb(nh, ns, nv) * 255).astype(np.uint8); return out

def cell(n): return np.array(Image.open(V + n + ".png").convert("RGBA").crop((0, 0, 512, 384)))

def body_box(a):
    ys, xs = np.where(a[..., 3] > 128); return ys.min(), ys.max()

def kovis():
    a = cell("vihu_idle"); h, s, v = hsv(a); y0, y1 = body_box(a); Y = (np.arange(384)[:, None] - y0) / (y1 - y0)
    blue = (a[..., 3] > 128) & (h > 0.5) & (h < 0.7) & (s > 0.3)
    return recolor(a, blue, lambda v: v * 1.9 + 0.1)

def lippis():
    a = cell("lippis_idle"); h, s, v = hsv(a); y0, y1 = body_box(a); Y = (np.arange(384)[:, None] - y0) / (y1 - y0)
    green = (a[..., 3] > 128) & (h > 0.15) & (h < 0.45) & (s > 0.25)
    dark = (a[..., 3] > 128) & (v < 0.28) & (s < 0.5) & (Y > 0.2) & (Y < 0.55)   # liivi (ei lippu, ei kengät)
    a = recolor(a, green, lambda v: v * 1.7 + 0.1)
    return recolor(a, dark, lambda v: v * 2.2 + 0.35)

def punk():
    a = cell("punk_idle"); h, s, v = hsv(a); y0, y1 = body_box(a); Y = (np.arange(384)[:, None] - y0) / (y1 - y0)
    skin = (h < 0.1) & (s > 0.3)
    jeans = (a[..., 3] > 128) & ~skin & (Y > 0.5) & (Y < 0.86) & (v > 0.12) & (s < 0.35)
    vest = (a[..., 3] > 128) & ~skin & (v < 0.3) & (Y > 0.17) & (Y < 0.55) & (s < 0.4)
    a = recolor(a, jeans, lambda v: v * 1.5 + 0.2)
    return recolor(a, vest, lambda v: v * 2.2 + 0.35)

def seilori():
    a = cell("seilori_idle"); h, s, v = hsv(a); y0, y1 = body_box(a); Y = (np.arange(384)[:, None] - y0) / (y1 - y0)
    white = (a[..., 3] > 128) & (s < 0.2) & (v > 0.45) & (Y > 0.14)
    navy = (a[..., 3] > 128) & (h > 0.55) & (h < 0.72) & (s > 0.35)
    cap = (a[..., 3] > 128) & (s < 0.25) & (v > 0.6) & (Y < 0.14)   # lakki -> hiukset (ruskea), ei lakkia
    a = recolor(a, white, lambda v: v * 1.0)
    a = recolor(a, navy, lambda v: v * 1.4 + 0.15)
    out = a.copy(); out[cap, 0], out[cap, 1], out[cap, 2] = 92, 62, 38
    return out

def main(out):
    names = ["Kovis", "Lippis", "Punkkari", "Seilori"]
    orig = [cell("vihu_idle"), cell("lippis_idle"), cell("punk_idle"), cell("seilori_idle")]
    new = [kovis(), lippis(), punk(), seilori()]
    W = 512 * 4; im = Image.new("RGBA", (W, 768), (70, 70, 70, 255))
    for i in range(4):
        im.alpha_composite(Image.fromarray(orig[i]), (512 * i, 0)); im.alpha_composite(Image.fromarray(new[i]), (512 * i, 384))
    im.convert("RGB").save(out)

if __name__ == "__main__":
    main(sys.argv[1])
