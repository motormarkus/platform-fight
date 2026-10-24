"""Pensalenkkarin lakki-sarjan (lenkkari_lakki.png) housut olivat liian sinisiä muihin Lenkkari-kuviin verrattuna.
Housujen (hahmon alaosan vaaleansininen farkku) väri siirretään lenkkari_lakki_suora.png:n housujen sävyyn kanavakohtaisella keskiarvo/hajonta-sovituksella.
Alkuperäinen: Tools/EnemyFixes/orig/lenkkari_lakki.png. Ajo: python Tools/EnemyFixes/fix_lenkkari_lakki_pants.py"""
import os
import numpy as np
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
SPR = os.path.join(ROOT, "Assets", "Sprites", "Viholliset")
CW, CH = 768, 448


def pants_mask(cell):
    a = cell[..., 3] > 200
    ys = np.where(a.any(1))[0]
    y0, y1 = ys.min(), ys.max()
    low = np.zeros_like(a); low[int(y0 + (y1 - y0) * 0.42):] = True       # vyötärön alapuolella
    r, g, b = cell[..., 0].astype(int), cell[..., 1].astype(int), cell[..., 2].astype(int)
    blue = (b > r + 30) & (r + g + b > 330)                                 # vaalea farkku (ei tummia ääriviivoja, ei oransseja repeämiä)
    return a & low & blue


def collect(path):
    im = np.array(Image.open(path).convert("RGBA"))
    px = []
    for i in range(im.shape[1] // CW):
        c = im[:, i * CW:(i + 1) * CW]
        if c[..., 3].max() == 0: continue
        px.append(c[pants_mask(c)][:, :3].astype(float))
    return np.concatenate(px)


def main():
    ref = collect(os.path.join(SPR, "lenkkari_lakki_suora.png"))
    src_path = os.path.join(ROOT, "Tools", "EnemyFixes", "orig", "lenkkari_lakki.png")
    src = collect(src_path)
    m_r, s_r, m_s, s_s = ref.mean(0), ref.std(0), src.mean(0), src.std(0)
    print("ref", m_r.round(1), "src", m_s.round(1))
    im = np.array(Image.open(src_path).convert("RGBA"))
    out = im.copy()
    for i in range(im.shape[1] // CW):
        c = im[:, i * CW:(i + 1) * CW]
        if c[..., 3].max() == 0: continue
        m = pants_mask(c)
        rgb = c[..., :3].astype(float)
        new = (rgb - m_s) / np.maximum(s_s, 1e-3) * s_r + m_r
        res = c.copy()
        res[..., :3][m] = np.clip(new[m], 0, 255).astype(np.uint8)
        out[:, i * CW:(i + 1) * CW] = res
    Image.fromarray(out, "RGBA").save(os.path.join(SPR, "lenkkari_lakki.png"))
    print("ok")


if __name__ == "__main__":
    main()
