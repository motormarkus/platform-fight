"""Koe-olento (alien-pomo): vihreätaustaisesta videosta kuvasarjat Assets/Sprites/Viholliset/alien_<nimi>.png.
Video A (24 fps, 240 kuvaa): idle ~28-76, kävely ~76-122, laukka ~124-239 (jakso 16 kuvaa).
Nelijalkainen: leveä ruutu (1280), kiinteä mittakaava kaikille liikkeille, jalat 16 px ylös.
Ajo: python Tools/AlienAssets/make_alien_sheets.py <liike.mp4> [ulos-kansio] [raapiminen.mp4] [karjaisu.mov]"""
import os, sys
import numpy as np
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, os.path.join(HERE, "..", "KingAssets")); sys.path.insert(0, os.path.join(HERE, "..", "GymAssets"))
import make_king_sheets as m
import av
import cv2
from key_video import key_frame


def smooth_alpha(im):
    """Karvaiset reunat: mediaani poistaa porrastuksen, sitten pehmeä terävöitetty reuna."""
    a = cv2.medianBlur(im[..., 3], 5)
    a = cv2.GaussianBlur(a, (0, 0), 0.9)
    a = np.clip((a.astype(np.float32) - 40.0) * (255.0 / 175.0), 0, 255).astype(np.uint8)
    out = im.copy(); out[..., 3] = a
    return out


m.load = lambda path: [smooth_alpha(key_frame(f.to_ndarray(format="bgr24"))) for f in av.open(path).decode(video=0)]
_orig_rescale = m.rescale


def _rescale(rgba, f):
    if f <= 1.0: return _orig_rescale(rgba, f)
    a = rgba.astype(np.float32); al = a[..., 3:4] / 255.0; a[..., :3] *= al
    r = cv2.resize(a, (round(a.shape[1] * f), round(a.shape[0] * f)), interpolation=cv2.INTER_CUBIC)
    al = np.clip(r[..., 3:4] / 255.0, 0, 1); r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    r[..., 3:4] = al * 255.0
    return r.clip(0, 255).astype(np.uint8)


m.rescale = _rescale


def sil(im, mode):
    """Siluetti silmukkahakuun: leveä ikkuna (olento on ~700 px leveä)."""
    a = (im[..., 3] > 128).astype(np.float32); ax = int(m.anchor_x(im, mode))
    out = np.zeros((720, 1000), np.float32); x0 = ax - 500
    s0, s1 = max(0, x0), min(a.shape[1], x0 + 1000)
    out[:, s0 - x0:s1 - x0] = a[:720, s0:s1]
    return cv2.resize(out, (125, 90), interpolation=cv2.INTER_AREA)


m.sil = sil

K = 1.5          # videon olento ~340 px korkea -> ~510 px ruudussa
FLOOR = 685
m.DIMS.update({"a_idle": (1280, 780, 704), "a_kavely": (1280, 780, 704), "a_laukka": (1280, 780, 704),
               "a_karjaisu": (1536, 780, 704), "a_raapiminen": (1536, 780, 704)})


def main(vA, outs, vB=None, vC=None):
    A = m.load(vA)

    def sheet(name, idx, mode):
        m.setdims("a_" + name)
        cells = [m.cell(A[i], mode, K, FLOOR) for i in idx]
        m.save(cells, f"{outs}/alien_{name}.png"); print(name, len(cells), "kuvaa", idx[0], "-", idx[-1])

    idle = m.best_loop(A, 28, 78, 22, 48, "low"); sheet("idle", idle[::2], "low")
    walk = m.best_loop(A, 76, 124, 18, 40, "torso"); sheet("kavely", walk[::2], "torso")
    run = m.best_loop(A, 126, 239, 14, 34, "torso"); sheet("laukka", run, "torso")

    if vB:   # video B: karjaisu (kuvat 52-124: pää alas, häntä pystyyn), raapiminen (124-196: nousee takajaloille, kynsi-iskut)
        B = m.load(vB)

        def sheetB(name, idx, mode):
            m.setdims("a_" + name)
            cells = [m.cell(B[i], mode, K, FLOOR) for i in idx]
            m.save(cells, f"{outs}/alien_{name}.png"); print(name, len(cells), "kuvaa", idx[0], "-", idx[-1])
        sheetB("raapiminen", list(range(124, 197, 3)), "low")

    if vC:   # video C: karjaisu (97 kuvaa: sisäänveto 0-28, nousu 28-40, karjaisu 40-78, palautus 78-96); ääni samassa tiedostossa
        C = m.load(vC)
        m.setdims("a_karjaisu")
        idx = [0, 6, 12, 18, 24, 28, 31, 34, 37, 40, 44, 48, 52, 56, 60, 64, 68, 72, 76, 79, 82, 85, 88, 92, 96]
        m.save([m.cell(C[i], "low", K, FLOOR) for i in idx], f"{outs}/alien_karjaisu.png"); print("karjaisu", len(idx), "kuvaa; huippu indeksit 9-18 (kuvat 40-76)")


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2] if len(sys.argv) > 2 else "Assets/Sprites/Viholliset", sys.argv[3] if len(sys.argv) > 3 else None, sys.argv[4] if len(sys.argv) > 4 else None)
