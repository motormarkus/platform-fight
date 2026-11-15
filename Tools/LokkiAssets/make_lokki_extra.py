"""Lokin lisakuvat valmiista lapinakyvista kuvista (5 hahmoa vierekkain): suoja (lokki_suoja) ja polvi-isku (lokki_polvi).
Kuvissa on reunoilla matala-alfaisia punaisia/keltaisia jaamia (alfa 1-31): ne leikataan pois (alfa-kynnys), vain suurin yhtenainen hahmo per ruutu jää,
sitten sama siisti reuna kuin muilla Lokin kuvilla (clean_rescale + hento ääriviiva). Mittakaava: 1. hahmon (taisteluasento) korkeus = 640 px.
Ajo: python Tools/LokkiAssets/make_lokki_extra.py <suoja.png> <polvi.png> [ulos]"""
import os, sys, json
import cv2, numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "NokkamiesAssets")); sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_nokkamies_sheets as nk
nk.INK_DEPTH, nk.INK_ERODE = 0.0, 0.0   # kuvat ovat valmiiksi tekoälyn piirtämiä, oma ääriviiva

TARGET_H = 640.0

def clean_alpha(rgba):
    a = rgba[..., 3].astype(np.float32)
    a2 = np.clip((a - 90.0) / 110.0, 0, 1)   # jäämät (alfa < 90) pois, reuna pehmeänä
    n, lab, st, _ = cv2.connectedComponentsWithStats((a2 > 0.5).astype(np.uint8), connectivity=8)
    if n > 2:
        keep = 1 + int(np.argmax(st[1:, cv2.CC_STAT_AREA]))
        near = cv2.dilate((lab == keep).astype(np.uint8), np.ones((5, 5), np.uint8)) > 0
        a2 = np.where(near, a2, 0)
    out = rgba.copy(); out[..., 3] = (a2 * 255).astype(np.uint8)
    return out

def split_figures(rgba):
    cols = (rgba[..., 3] > 128).sum(0) > 3; runs = []; s0 = None
    for x, c in enumerate(cols):
        if c and s0 is None: s0 = x
        if not c and s0 is not None:
            if x - s0 > 80: runs.append((s0, x))
            s0 = None
    if s0 is not None: runs.append((s0, len(cols)))
    return runs

def build(path, name, outs, dims):
    im = cv2.cvtColor(cv2.imread(path, cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)
    # jaamien siivous koko kuvasta: alfa-kynnys ennen jakoa (ruutujen rajat näkyvät sarakkeista)
    a = im[..., 3].astype(np.float32); im[..., 3] = np.where(a < 90, 0, a).astype(np.uint8)
    # rivit (kuvassa voi olla useita rivejä hahmoja): rivit ylhäältä alas, kussakin hahmot vasemmalta oikealle
    rows_on = (im[..., 3] > 128).sum(1) > 3; bands = []; y0 = None
    for y, on in enumerate(rows_on):
        if on and y0 is None: y0 = y
        if not on and y0 is not None:
            if y - y0 > 80: bands.append((y0, y))
            y0 = None
    if y0 is not None: bands.append((y0, len(rows_on)))
    figs = []
    for (ya, yb) in bands:
        band = im[max(0, ya - 20):min(im.shape[0], yb + 20)]
        for (x0, x1) in split_figures(band):
            sub = clean_alpha(band[:, max(0, x0 - 20):min(band.shape[1], x1 + 20)])
            ys, xs = np.where(sub[..., 3] > 128); figs.append(sub[ys.min():ys.max() + 1, xs.min():xs.max() + 1])
    print(name, "hahmoja", len(figs), [f.shape[:2] for f in figs])
    k = TARGET_H / figs[0].shape[0]
    s = nk.Sheet(name)
    for f in figs:
        fr = np.zeros((f.shape[0] + 40, f.shape[1] + 40, 4), np.uint8); fr[20:20 + f.shape[0], 20:20 + f.shape[1]] = f
        s.add(fr, "low", k, f.shape[0] + 20)
    s.save(outs, dims)
    os.replace(f"{outs}/nokkamies_{name}.png", f"{outs}/lokki_{name}.png")

def main(guard, knee, outs):
    dims = {}
    build(guard, "suoja", outs, dims); build(knee, "polvi", outs, dims)
    p = os.path.join(HERE, "dims.json")
    d = json.load(open(p)) if os.path.exists(p) else {}
    d.update(dims); json.dump(d, open(p, "w"), indent=1)

if __name__ == "__main__":
    a = sys.argv
    main(a[1], a[2], a[3] if len(a) > 3 else "Assets/Sprites/Viholliset")
