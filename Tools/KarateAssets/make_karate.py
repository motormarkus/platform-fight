"""Karatepelin pikselikuvat: leikkaa pelin omat liikekuvat (Unity-metojen ruudukoista), pienentaa, rajaa varit ja
lisaa tumman ulkoreunan. Tulos: Assets/Resources/Karate/<hahmo>_<anim>_NN.png + <hahmo>_info.txt (pivotY, kuvan koko).

Aja: python Tools/KarateAssets/make_karate.py   (vaatii Pillow, numpy)
"""
import os, re, sys
import numpy as np
from PIL import Image

ROOT = os.path.normpath(os.path.join(os.path.dirname(__file__), "..", ".."))
SPR = os.path.join(ROOT, "Assets", "Sprites")
OUT = os.path.join(ROOT, "Assets", "Resources", "Karate")
TARGET_H = 76          # seisovan hahmon korkeus pikseleina
PALETTE = 28           # varia / hahmo
OUTLINE = (18, 14, 24)

# anim -> (tiedosto, valinta). valinta: None = kaikki kuvat, lista = vain nama indeksit
F = {
    "rocco": dict(idle="idle", walk="kavely", punch="jab", kick="korkea_potku", special="kiertopotku",
                  hurt=("hahmo_spritesheet", [9]), fall="hero_heitetty", getup="hero_kipup"),
    "gustav": dict(idle="gustav_idle", walk="gustav_kavely", punch="gustav_jab", kick="gustav_etupotku",
                   special="gustav_kiertopotku", hurt="gustav_osuma", fall="gustav_kaatuminen",
                   getup="gustav_ylosnousu", block="gustav_suoja"),
    "lippis": dict(idle="lippis_idle", walk="lippis_kavely", punch="lippis_lyonti", kick="lippis_potku",
                   special="lippis_potku", hurt="lippis_osuma", fall="lippis_kaatuminen",
                   getup="lippis_ylosnousu", block="lippis_torjunta"),
    "lenkkari": dict(idle="lenkkari_idle", walk="lenkkari_kavely", punch="lenkkari_jab", kick="lenkkari_potku",
                     special="lenkkari_suora", hurt="lenkkari_osuma", fall="lenkkari_kaatuminen",
                     getup="lenkkari_ylosnousu", block="lenkkari_torjunta"),
    "horhe": dict(idle="horhe_idle", walk="horhe_kavely", punch="horhe_koukku", kick="horhe_potkut",
                  special="horhe_takakoukku", hurt="horhe_osuma", fall="horhe_kaatuminen",
                  getup="horhe_ylosnousu", block="horhe_torjunta"),
    "marcus": dict(idle="marcus_idle", walk="marcus_kavely", punch="marcus_lyonti1", kick="marcus_koukku",
                   special="marcus_suora", hurt="marcus_osuma", fall="marcus_kaatuminen1",
                   getup="marcus_ylosnousu1", block="marcus_suoja"),
    "deacon": dict(idle="deacon_idle", walk="deacon_kavely", punch="deacon_suorat", kick="deacon_koukut",
                   special="deacon_vartalokoukut", hurt="deacon_osuma", fall="deacon_heitetty",
                   getup="deacon_nousu", block="deacon_suoja"),
}


def find(name):
    for d in (SPR, os.path.join(SPR, "Viholliset")):
        p = os.path.join(d, name + ".png")
        if os.path.exists(p):
            return p
    raise FileNotFoundError(name)


def cells(name):
    p = find(name)
    meta = open(p + ".meta", encoding="utf-8").read()
    rects = re.findall(r"name: (\S+)\s+rect:\s+serializedVersion: 2\s+x: ([\d.\-]+)\s+y: ([\d.\-]+)\s+width: ([\d.\-]+)\s+height: ([\d.\-]+)", meta)
    rects = [(n, *(int(float(v)) for v in r)) for n, *r in rects]
    rects.sort(key=lambda r: int(re.search(r"(\d+)$", r[0]).group(1)))
    im = Image.open(p).convert("RGBA")
    H = im.height
    out = []
    for n, x, y, w, h in rects:
        out.append(im.crop((x, H - y - h, x + w, H - y)))
    return out


def alpha_bbox(a, thr=40):
    ys, xs = np.where(a > thr)
    if len(xs) == 0:
        return None
    return xs.min(), ys.min(), xs.max() + 1, ys.max() + 1


def build(fname, anims):
    frames = {}
    for anim, spec in anims.items():
        sel = None
        if isinstance(spec, tuple):
            spec, sel = spec
        cs = cells(spec)
        if sel is not None:
            cs = [cs[i] for i in sel]
        frames[anim] = cs
    # mittakaava: idlen 1. kuvan korkeus
    idle0 = np.array(frames["idle"][0])
    bb = alpha_bbox(idle0[..., 3])
    scale = TARGET_H / (bb[3] - bb[1])
    feet = idle0.shape[0] - bb[3]   # jalkojen etaisyys kuvan alareunasta (lahdepikselit)
    small = {}
    for anim, cs in frames.items():
        small[anim] = []
        for c in cs:
            # nostetaan jalat kuvan alareunaan (feet-offset pois)
            c = c.crop((0, 0, c.width, c.height - feet)) if feet > 0 else c
            w, h = max(1, round(c.width * scale)), max(1, round(c.height * scale))
            a = np.array(c.resize((w, h), Image.BOX)).astype(np.uint8)
            small[anim].append(a)
    # yhteinen kangas: x symmetrinen keskipisteen suhteen, y ylhaalta suurimman kuvan mukaan
    halfw = 1; top = 1
    for cs in small.values():
        for a in cs:
            b = alpha_bbox(a[..., 3], 100)
            if b is None: continue
            cx = a.shape[1] / 2
            halfw = max(halfw, int(np.ceil(max(cx - b[0], b[2] - cx))))
            top = max(top, a.shape[0] - b[1])
    W = halfw * 2 + 4; Hh = top + 3
    # paletti kaikista hahmon kuvista
    pix = np.concatenate([a[a[..., 3] > 200][:, :3] for cs in small.values() for a in cs])
    rng = np.random.default_rng(1)
    if len(pix) > 60000: pix = pix[rng.choice(len(pix), 60000, replace=False)]
    pimg = Image.fromarray(pix.reshape(-1, 1, 3).astype(np.uint8)).quantize(colors=PALETTE, method=Image.MEDIANCUT)
    pal = pimg.getpalette()[: PALETTE * 3]
    palimg = Image.new("P", (1, 1)); palimg.putpalette(pal + [0] * (768 - len(pal)))
    result = {}
    for anim, cs in small.items():
        result[anim] = []
        for a in cs:
            canvas = np.zeros((Hh, W, 4), np.uint8)
            h, w = a.shape[:2]
            ox = W // 2 - w // 2
            # alareuna kuvan alareunaan; ylitys leikataan
            y0 = Hh - 2 - h
            sy0, dy0 = (0, y0) if y0 >= 0 else (-y0, 0)
            sx0, dx0 = (0, ox) if ox >= 0 else (-ox, 0)
            hh = min(h - sy0, Hh - dy0); ww = min(w - sx0, W - dx0)
            canvas[dy0:dy0 + hh, dx0:dx0 + ww] = a[sy0:sy0 + hh, sx0:sx0 + ww]
            mask = canvas[..., 3] > 110
            rgb = Image.fromarray(canvas[..., :3]).quantize(palette=palimg, dither=Image.NONE).convert("RGB")
            rgb = np.array(rgb)
            res = np.zeros((Hh, W, 4), np.uint8)
            res[mask, :3] = rgb[mask]; res[mask, 3] = 255
            # ulkoreuna
            m = np.pad(mask, 1)
            nb = m[:-2, 1:-1] | m[2:, 1:-1] | m[1:-1, :-2] | m[1:-1, 2:]
            edge = nb & ~mask
            res[edge, :3] = OUTLINE; res[edge, 3] = 255
            result[anim].append(Image.fromarray(res))
    return result, W, Hh


def main():
    os.makedirs(OUT, exist_ok=True)
    only = sys.argv[1:] or list(F)
    for fname in only:
        res, W, H = build(fname, F[fname])
        for old in os.listdir(OUT):
            if old.startswith(fname + "_") and old.endswith((".png", ".txt")):
                os.remove(os.path.join(OUT, old))
        counts = []
        for anim, ims in res.items():
            for i, im in enumerate(ims):
                im.save(os.path.join(OUT, f"{fname}_{anim}_{i:02d}.png"))
            counts.append(f"{anim}={len(ims)}")
        open(os.path.join(OUT, f"{fname}_info.txt"), "w").write(f"size={W}x{H}\n" + "\n".join(counts) + "\n")
        print(fname, W, H, " ".join(counts))


if __name__ == "__main__":
    main()
