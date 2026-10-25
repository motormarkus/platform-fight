"""Hotellin katon kopteri videosta (kopteri.mov): tausta häipyy vihreästä lähes valkoiseksi, joten tausta arvioidaan jokaisesta ruudusta (kulmat)
ja poistetaan taustan väri koko kuvasta (roottorin välit; ohjaamon lasin vaalein osa jää läpinäkyväksi). Valitaan roottorin silmukka (alku ja loppu mahdollisimman samat),
kuvat rajataan yhteiseen kankaaseen (skidit alareunassa, keskipiste = pivot) ja tallennetaan Assets/Sprites/Rekvisiitta/KatonKopteri/katon_kopteri_NN.png.
Ajo: python Tools/HotelAssets/make_roof_heli.py <video.mov>"""
import sys, os
import numpy as np, cv2, av
from PIL import Image

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
OUT = os.path.join(ROOT, "Assets", "Sprites", "Rekvisiitta", "KatonKopteri")
T0, T1 = 14.0, 60.0          # taustan etäisyys: alle T0 läpinäkyvä, yli T1 täysin peittävä
TARGET_W = 720               # kankaan leveys pikseleinä
START = 100                  # tausta on tästä eteenpäin lähes valkoinen ja tasainen
MINLOOP, MAXLOOP = 20, 90


def key(rgb):
    h, w = rgb.shape[:2]
    f = rgb.astype(np.float32)
    corners = np.concatenate([f[:40, :40].reshape(-1, 3), f[:40, -40:].reshape(-1, 3), f[-40:, :40].reshape(-1, 3), f[-40:, -40:].reshape(-1, 3)])
    bg = np.median(corners, axis=0)
    dist = np.sqrt(((f - bg) ** 2).sum(2))
    # koko kuvan väriavain: roottorin ja skidien väliin jäävät taustan alueet eivät ole yhteydessä reunaan
    conn = dist < T1
    a = np.ones((h, w), np.float32)
    a[conn] = np.clip((dist[conn] - T0) / (T1 - T0), 0, 1)
    c = f.copy()
    m = conn & (a > 0.02)
    c[m] = (f[m] - (1 - a[m, None]) * bg) / a[m, None]
    c = np.clip(c, 0, 255)
    return np.dstack([c, a * 255]).astype(np.uint8)


def main(path):
    frames = []
    for fr in av.open(path).decode(video=0):
        frames.append(key(fr.to_ndarray(format="rgb24")))
    print("ruutuja", len(frames))
    seg = frames[START:]
    # silmukka: etsi i<j, joiden ruutu j vastaa ruutua i parhaiten
    small = [cv2.resize(f[..., 3], (240, 135)).astype(np.float32) for f in seg]
    best = (1e18, 0, MINLOOP)
    for i in range(0, len(seg) - MINLOOP):
        for j in range(i + MINLOOP, min(len(seg), i + MAXLOOP)):
            d = float(np.abs(small[i] - small[j]).mean())
            if d < best[0]: best = (d, i, j)
    d, i, j = best
    loop = seg[i:j]
    print("silmukka", i + START, j + START, "pituus", len(loop), "ero", round(d, 2))
    # yhteinen rajaus: kaikkien ruutujen alfan yhdistelmä
    union = np.zeros(loop[0].shape[:2], bool)
    for f in loop: union |= f[..., 3] > 20
    ys, xs = np.where(union)
    y0, y1, x0, x1 = ys.min(), ys.max() + 1, xs.min(), xs.max() + 1
    k = TARGET_W / (x1 - x0)
    os.makedirs(OUT, exist_ok=True)
    for old in os.listdir(OUT):
        if old.endswith(".png") or old.endswith(".png.meta"): os.remove(os.path.join(OUT, old))
    for n, f in enumerate(loop):
        im = Image.fromarray(f[y0:y1, x0:x1], "RGBA")
        im = im.resize((TARGET_W, int(round((y1 - y0) * k))), Image.LANCZOS)
        im.save(os.path.join(OUT, "katon_kopteri_%02d.png" % n))
    print("ok", len(loop), "kuvaa", TARGET_W, "x", int(round((y1 - y0) * k)))


if __name__ == "__main__":
    main(sys.argv[1])
