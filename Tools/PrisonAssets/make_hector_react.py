"""Hector: osuma (4 kuvaa vierekkäin), suojaus (6 kuvaa 3 x 2) ja kaatuminen (4 kuvaa 2 x 2) käyttäjän läpinäkyvistä kuvista
-> Assets/Sprites/Viholliset/hector_osuma.png, hector_suojaus.png, hector_kaatuminen.png (ruudut kuten muilla, hahmo ~640 px seisoessa).
Osuma ja suojaus: mittakaava seisovasta kuvasta (kuva 0). Kaatumisen arkissa ei ole seisovaa kuvaa, joten mittakaava haetaan lenkkareiden pinta-alasta
(sama hahmo: pinta-alan neliöjuuri suhteessa osuma-arkin seisovaan kuvaan). Lattiaviiva säilyy lähdekuvien ruuduissa.
Ajo: python Tools/PrisonAssets/make_hector_react.py <osuma.png> <suojaus.png> <kaatuminen.png> [ulos-kansio]"""
import os, sys
import numpy as np
from PIL import Image
HERE = os.path.dirname(os.path.abspath(__file__)); sys.path.insert(0, os.path.join(HERE, "..", "RexAssets"))
import make_rex_kicks2 as k2

LYING_FIX = 0.88   # makuuasento oli noin 12 % liian pitkä seisovaan verrattuna (käyttäjän havainto)

def hair(a):
    r, g, b, al = [a[..., i].astype(int) for i in range(4)]
    return float(((al > 128) & (r > 40) & (r < 115) & (g > 22) & (g < 80) & (b > 12) & (b < 70) & (r > g) & (g >= b)).sum())

def shoes(a):
    """Valkoisten lenkkarien pinta-ala: pitää hahmon koon paremmin kuin hiukset (testattu tunnetuilla seisovilla kuvilla: virhe ~5 %, hiuksilla jopa 16 %)."""
    r, g, b, al = [a[..., i].astype(int) for i in range(4)]
    return float(((al > 128) & (r > 205) & (g > 190) & (b > 165) & (r - b < 60)).sum())

def grid(img, cols, rows):
    W, H = img.size; cw, ch = W // cols, H // rows
    return [(c * cw, r * ch, (c + 1) * cw, (r + 1) * ch) for r in range(rows) for c in range(cols)]

def split_cells(img, cols, rows):
    """Ruudukko tyhjien raitojen mukaan (kuvissa hahmot eivät ole tasaisissa ruuduissa): jaetaan suurimpien tyhjien välien keskelle."""
    a = np.array(img.convert("RGBA"))[..., 3] > 20
    def cuts(proj, n):
        if n == 1: return [0, len(proj)]
        empty = ~proj; runs = []; i = 0
        while i < len(proj):
            if empty[i]:
                j = i
                while j < len(proj) and empty[j]: j += 1
                if i > 0 and j < len(proj): runs.append((j - i, (i + j) // 2))
                i = j
            else: i += 1
        mids = sorted(sorted(runs, reverse=True)[:n - 1], key=lambda t: t[1])
        return [0] + [m for _, m in mids] + [len(proj)]
    xc, yc = cuts(a.any(0), cols), cuts(a.any(1), rows)
    return [(xc[c], yc[r], xc[c + 1], yc[r + 1]) for r in range(rows) for c in range(cols)]

def main(p_hit, p_block, p_fall, outs):
    ih = Image.open(p_hit); k2.build(ih, grid(ih, 4, 1), 0, outs, "hector_osuma")
    ib = Image.open(p_block); k2.build(ib, grid(ib, 3, 2), 0, outs, "hector_suojaus")
    # kaatuminen: mittakaava lenkkareista
    a_h = np.array(ih.convert("RGBA")); c0 = grid(ih, 4, 1)[0]; stance = a_h[c0[1]:c0[3], c0[0]:c0[2]]
    ys, xs = np.where(stance[..., 3] > 20); k_hit = k2.TARGET / (ys.max() - ys.min() + 1)
    hair_h = np.mean([shoes(a_h[c[1]:c[3], c[0]:c[2]]) for c in grid(ih, 4, 1)])
    fa = Image.open(p_fall); a_f = np.array(fa.convert("RGBA")); cells = split_cells(fa, 2, 2)
    hair_f = np.mean([shoes(a_f[c[1]:c[3], c[0]:c[2]]) for c in cells])
    k = k_hit * (hair_h / hair_f) ** 0.5 * LYING_FIX; print("kaatuminen: mittakaava lenkkareista", round(k, 3), "(osuma", round(k_hit, 3), ")")
    crops = [a_f[c[1]:c[3], c[0]:c[2]] for c in cells]
    bbs = [k2.bbox(c) for c in crops]
    uy0, uy1 = min(b[2] for b in bbs), max(b[3] for b in bbs)   # yhteinen pystyalue (lattiaviiva)
    cw, ch = 1152, 768
    need_h = (uy1 - uy0) * k + 16; print("  tarvittava korkeus", round(need_h))
    if need_h > ch: ch = int(np.ceil(need_h / 128.0) * 128)
    sheet = Image.new("RGBA", (cw * 4, ch), (0, 0, 0, 0))
    for i, (c, b) in enumerate(zip(crops, bbs)):
        x0, x1 = b[0], b[1]
        im = Image.fromarray(c[uy0:uy1, x0:x1]).resize((max(1, round((x1 - x0) * k)), max(1, round((uy1 - uy0) * k))), Image.LANCZOS)
        px = int(round(576 - im.width / 2)); py = int(round(ch - 16 - im.height))   # vaaka: kuvan keskikohta pivotiin
        if px < 0 or px + im.width > cw or py < 0: print("  HUOM: leikkautuu", i, px, py, im.width)
        cell = Image.new("RGBA", (cw, ch), (0, 0, 0, 0)); cell.alpha_composite(im, (max(0, px), max(0, py)))
        sheet.alpha_composite(cell, (i * cw, 0))
    sheet.save(f"{outs}/hector_kaatuminen.png"); print("hector_kaatuminen", cw, "x", ch)

if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2], sys.argv[3], sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset")
