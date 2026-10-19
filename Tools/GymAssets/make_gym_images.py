"""Salin sisäkuvat (Assets/ArtSources/Gym/sali_boxing_N.png, alkuperäiset muuttumattomina) -> Assets/Sprites/Taustat/sali_boxing_N.png.
- ikkunoiden lasit (valkoiset, pisteviivoitetut) saavat saman läpinäkyvyyden kaikissa kuvissa, jotta ikkunaparallaksi näkyy läpi
- muu kuva täysin peittävä
- kuvan 2 ja sitä seuraavien vasen reuna häivytetään OVERLAP px:n matkalla (kuvat limittyvät, sauma ei näy); sama arvo GymOverlapPx BeatEmUpSetup.cs:ssä
Käyttö: python make_gym_images.py"""
import glob
import os
import cv2
import numpy as np

SRC, DST = "Assets/ArtSources/Gym", "Assets/Sprites/Taustat"
OVERLAP = 48
PANE_ALPHA = 0           # 0..255: lasi kokonaan tyhjä (ikkunanäkymä näkyy sellaisenaan)
MIN_PANE_AREA = 250       # myös köysien/tolppien jättämät pienet lasinpalat


def pane_mask(rgba):
    rgb = rgba[..., :3].astype(np.int16)
    bright = rgb.min(axis=2) > 200
    sat = (rgb.max(axis=2) - rgb.min(axis=2)) < 28
    m = (bright & sat).astype(np.uint8)
    m = cv2.morphologyEx(m, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(m, connectivity=8)
    out = np.zeros_like(m)
    for i in range(1, n):
        if st[i, cv2.CC_STAT_AREA] >= MIN_PANE_AREA:
            out[lab == i] = 1
    core = out.astype(bool)
    near = cv2.dilate(out, np.ones((31, 31), np.uint8)).astype(bool)   # lasin vaalea/varjostettu reuna mukaan
    grey = (rgb.max(axis=2) - rgb.min(axis=2)) < 45                    # ruskea kehys ja tiilet (värikkäitä) jäävät peittäviksi
    mask = core | (near & grey & (rgb.max(axis=2) > 130))
    # viimeinen siivous: lasin reunasta 4 px sisään kaikki harmaa/vaalea (kehyksen sisäreunan valkoiset jäämät)
    ring = cv2.dilate(mask.astype(np.uint8), np.ones((9, 9), np.uint8)).astype(bool)
    sat = rgb.max(axis=2) - rgb.min(axis=2)
    leftover = ring & (((sat < 60) & (rgb.max(axis=2) > 85)) | (rgb.max(axis=2) > 190))
    return mask | leftover


def main():
    for i, path in enumerate(sorted(glob.glob(os.path.join(SRC, "sali_boxing_*.png")))):
        im = cv2.cvtColor(cv2.imread(path, cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)
        pane = pane_mask(im)
        alpha = np.full(im.shape[:2], 255, np.float32)
        alpha[pane] = PANE_ALPHA
        if i > 0:
            t = np.clip(np.arange(im.shape[1]) / float(OVERLAP), 0, 1)
            t = t * t * (3 - 2 * t)
            alpha *= t[None, :]
        out = im.copy(); out[..., 3] = alpha.astype(np.uint8)
        name = os.path.basename(path)
        cv2.imwrite(os.path.join(DST, name), cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
        print(name, "lasia %.1f %% kuvasta" % (100.0 * pane.mean()))


VIEW_BLUR = 3.0   # ikkunanäkymän (talorivi, jalkakäytävä ja tie) sumennus: lasin takainen katu hieman pehmeä


def views():
    """Ikkunoista näkyvä katu sumennettuna (alkuperäiset ArtSources/Gym-kansiossa)."""
    for name in ("sali_ikkuna_talot.png", "sali_ikkuna_tie.png"):
        im = cv2.cvtColor(cv2.imread(os.path.join(SRC, name), cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA).astype(np.float32)
        a = im[..., 3:4] / 255.0
        pm = np.dstack([im[..., :3] * a, im[..., 3:4]])
        pm = cv2.GaussianBlur(pm, (0, 0), VIEW_BLUR)
        a2 = pm[..., 3:4] / 255.0
        rgb = np.where(a2 > 1e-3, pm[..., :3] / np.maximum(a2, 1e-3), 0)
        out = np.dstack([rgb, pm[..., 3:4]]).clip(0, 255).astype(np.uint8)
        cv2.imwrite(os.path.join(DST, name), cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
        print(name, "sumennettu")


def ring():
    """Kehän sisäkuva: lasit läpinäkyviksi (ei häivytystä), ikkunaparallaksi näkyy läpi."""
    path = os.path.join(SRC, "sali_kehakuva.png")
    im = cv2.cvtColor(cv2.imread(path, cv2.IMREAD_UNCHANGED), cv2.COLOR_BGRA2RGBA)
    pane = pane_mask(im)
    out = im.copy(); out[..., 3] = np.where(pane, PANE_ALPHA, 255).astype(np.uint8)
    cv2.imwrite(os.path.join(DST, "sali_kehakuva.png"), cv2.cvtColor(out, cv2.COLOR_RGBA2BGRA))
    print("sali_kehakuva.png lasia %.1f %% kuvasta" % (100.0 * pane.mean()))


if __name__ == "__main__":
    main()
    ring()
    views()
