"""Hotellin katto (Thaitown): Assets/ArtSources/Hotel/hotelli_katto_*.png (alkuperäiset, taivas läpinäkyvä) -> Assets/Sprites/Taustat/.
Katto on symmetrinen oven (rakennus) ympärillä: [peilattu laaja katto, OVI, laaja katto] -> thaitown_katto_paa_1..3.png.
Laaja katto (hotelli_katto_laaja.png: ilmastointilaite, helikopterikenttä ja kulma yhdessä kuvassa) skaalataan 941 px korkeaksi; jos
alkuperäinen on jo 941 px korkea, skaalaus on 1:1 (nyt toimitettu kuva oli 2000 x 667, joten se suurennetaan 1,41-kertaiseksi).
Vasen reuna häivytetään OVERLAP px:n matkalla (sama arvo BeatEmUpSetup.cs:ssä, PenthouseOverlapPx). Taivas pysyy läpinäkyvänä.
Käyttö: python Tools/HotelAssets/make_roof_images.py"""
import glob
import os
import cv2
import numpy as np

SRC, DST = "Assets/ArtSources/Hotel", "Assets/Sprites/Taustat"
OVERLAP = 48   # sama arvo BeatEmUpSetup.cs:ssä (PenthouseOverlapPx)


def load(name):
    return cv2.cvtColor(cv2.imread(os.path.join(SRC, f"hotelli_katto_{name}.png"), cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)


def fade_left(im):
    t = np.clip(np.arange(im.shape[1]) / float(OVERLAP), 0, 1); t = t * t * (3 - 2 * t)
    out = im.copy(); out[..., 3] = (im[..., 3].astype(np.float32) * t[None, :]).astype(np.uint8)
    return out


def save(name, im):
    cv2.imwrite(os.path.join(DST, name), cv2.cvtColor(im, cv2.COLOR_RGBA2BGRA))


def main():
    for old in glob.glob(os.path.join(DST, "thaitown_katto_*.png*")):   # aiempien versioiden kuvat
        os.remove(old)
    door = load("ovi")
    wide = load("laaja")
    if wide.shape[0] != 941:
        w = int(round(wide.shape[1] * 941.0 / wide.shape[0]))
        f = wide.astype(np.float32); al = f[..., 3:4] / 255.0; f[..., :3] *= al
        f = cv2.resize(f, (w, 941), interpolation=cv2.INTER_LANCZOS4)
        a2 = np.clip(f[..., 3:4], 0, 255) / 255.0
        f[..., :3] = np.where(a2 > 1e-3, f[..., :3] / np.maximum(a2, 1e-3), 0)
        wide = f.clip(0, 255).astype(np.uint8)
    mirror = lambda im: im[:, ::-1].copy()
    for i, im in enumerate([mirror(wide), door, wide], 1):
        save(f"thaitown_katto_paa_{i}.png", fade_left(im) if i > 1 else im)
    print("ok, laaja kuva", wide.shape[1], "x", wide.shape[0])


if __name__ == "__main__":
    main()
