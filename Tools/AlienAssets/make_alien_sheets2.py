"""Koe-olento, toinen erä: pusku (6 asennon kuva), häntähuitaisu (video, 1920x1080), osuma/kaatuminen/maahan/nousu (video, 1280x720).
Ajo: python Tools/AlienAssets/make_alien_sheets2.py <pusku.png> <hantahuitaisu.mov> <osuma_kaatuminen_nousu.mov> [ulos-kansio]
Mittakaava kuten make_alien_sheets.py (K=1.5 videolla A); 1080p-video on 1,5x suurempi -> K=1.0; pusku-kuvan mittakaava sovitetaan idle-asennon korkeuteen."""
import os, sys
import numpy as np
import cv2
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import make_alien_sheets as a          # asentaa m.load / m.rescale / m.sil
m = a.m
from key_video import key_frame

# nimi: (leveys, pivot x, korkeus); sama taulukko BeatEmUpSetup.SkelCell:ssä
SIZES = {"pusku": (1792, 780, 704), "hantahuitaisu": (1792, 900, 704), "osuma": (1536, 780, 832), "kaatuminen": (1536, 780, 832), "maahan": (1536, 780, 704), "nousu": (1536, 780, 832)}
for n, v in SIZES.items():
    m.DIMS["a_" + n] = (v[0], v[1], v[2])
IDLE_H = 317 * a.K        # idle-asennon korkeus ruudussa (video A, kuva 0: 367..684)


def bbox(im):
    ys, xs = np.where(im[..., 3] > 128); return xs.min(), xs.max() + 1, ys.min(), ys.max() + 1


def pusku(path, outs):
    img = cv2.imread(path)
    hh, ww = img.shape[0] // 2, img.shape[1] // 3
    # key_frame säilyttää vain suurimman hahmon: jokainen asento avainnetaan omasta ruudustaan (3 x 2)
    poses = [a.smooth_alpha(key_frame(img[r * hh:(r + 1) * hh, c * ww:(c + 1) * ww])) for r in range(2) for c in range(3)]
    h6 = bbox(poses[5]); kk = IDLE_H / (h6[3] - h6[2]); print("pusku: mittakaava", round(kk, 3))
    m.setdims("a_pusku")
    cells = []
    for p in poses:
        x0, x1, y0, y1 = bbox(p)
        cells.append(m.cell(p, "low", kk, y1))
    m.save(cells, f"{outs}/alien_pusku.png"); print("pusku", len(cells), "kuvaa (1-2 valmistelu, 3-4 syöksy/osuma, 5-6 palautuminen)")


def whip(path, outs):
    A = m.load(path)
    for f in A:   # liikeviiruista jäänyt vihertävä/turkoosi hohto pois (olennossa ei ole vihreää)
        r, g, b = [f[..., i].astype(np.int32) for i in range(3)]
        f[..., 3] = np.where((g > r + 8) & (g >= b - 20), 0, f[..., 3])
    print("häntähuitaisu", len(A), "kuvaa", A[0].shape)
    _, _, _, floor = bbox(A[0])
    m.setdims("a_hantahuitaisu")
    idx = list(range(8, 53, 2))
    m.save([m.cell(A[i], "low", 1.0, floor) for i in idx], f"{outs}/alien_hantahuitaisu.png")
    print("hantahuitaisu", len(idx), "kuvaa", idx[0], "-", idx[-1], "; häntä osuu kuvissa 24-38 = indeksit 8-15")


def fall(path, outs):
    B = m.load(path); print("osuma-video", len(B), "kuvaa")
    def sheet(name, idx, mode):
        m.setdims("a_" + name)
        cells = [m.cell(B[i], mode if isinstance(mode, str) else mode(i), a.K, a.FLOOR) for i in idx]
        m.save(cells, f"{outs}/alien_{name}.png"); print(name, len(cells), "kuvaa", idx[0], "-", idx[-1])
    sheet("osuma", [11, 12, 14, 16, 18, 20], "low")
    sheet("kaatuminen", [22, 24, 26, 28, 30, 31, 32, 33, 34, 36], lambda i: "low" if i <= 26 else "com")
    sheet("maahan", list(range(38, 68, 4)), "com")
    sheet("nousu", list(range(68, 97, 2)), lambda i: "com" if i < 86 else "low")


if __name__ == "__main__":
    out = sys.argv[4] if len(sys.argv) > 4 else "Assets/Sprites/Viholliset"
    pusku(sys.argv[1], out); whip(sys.argv[2], out); fall(sys.argv[3], out)
