"""Alppikylän kiitotie ja taustat generaattorin kuvista -> Assets/Sprites/Taustat/alppi_*.png.
Ajo: python Tools/SnowAssets/make_alpine_assets.py <hangaarit> <kiitotie> <kyla> <vuoret>
 hangaarit: 1536 x 864 (alfa, taivas läpinäkyvä), kiitotie: 1672 x 941 (alfa), kyla: kukkulan kylä (alfa), vuoret: vuoristo (peittävä).
Tulosteet: alppi_hangaarit.png, alppi_kiitotie_1.png (+ peilattu _2), alppi_kyla.png (kaksi vierekkäin, saumaton), alppi_vuoret.png (kuva + peilikuva, saumaton).
Läpinäkyvien alueiden musta reunasävy korvataan viereisen kuvan värillä (ei tummaa hohtoa taivasta vasten)."""
import os, sys
import numpy as np, cv2

OUT = os.path.join(os.path.dirname(__file__), "..", "..", "Assets", "Sprites", "Taustat")


def bleed(bgra, sigma=3.0):
    """Puoliläpinäkyvien reunapikselien väri otetaan läheltä peittävistä pikseleistä."""
    f = bgra.astype(np.float32); a = f[..., 3] / 255.0
    solid = (a > 0.97).astype(np.float32)
    num = cv2.GaussianBlur(f[..., :3] * solid[..., None], (0, 0), sigma)
    den = cv2.GaussianBlur(solid, (0, 0), sigma)[..., None]
    fill = num / np.maximum(den, 1e-3)
    w = np.clip((den - 0.02) / 0.1, 0, 1) * (a < 0.97)[..., None]
    f[..., :3] = f[..., :3] * (1 - w) + fill * w
    return f.clip(0, 255).astype(np.uint8)


def save(name, img):
    for _ in range(6):
        try:
            cv2.imwrite(os.path.join(OUT, name), img); return
        except Exception:
            import time; time.sleep(1)
    cv2.imwrite(os.path.join(OUT, name), img)


def main(hang_p, run_p, vil_p, mtn_p):
    rd = lambda p: cv2.imread(p, cv2.IMREAD_UNCHANGED)
    A = bleed(rd(hang_p)); B = bleed(rd(run_p)); V = bleed(rd(vil_p)); M = rd(mtn_p)
    # kuva 1 on 1536 px leveä: skaalataan ruudun levyiseksi (1672), korkeus 941
    A = cv2.resize(A, (1672, 941), interpolation=cv2.INTER_CUBIC)
    A_orig = A   # sauman vertailu tehdään peilaamattomalla (hangaarit vasemmalla)
    # kiitotie: valitaan se suunta, jossa vasen reuna sopii parhaiten hangaarikuvan oikeaan reunaan
    d_dir = np.abs(A_orig[:, -1, :3].astype(float) - B[:, 0, :3]).mean()
    d_flip = np.abs(A_orig[:, -1, :3].astype(float) - B[:, -1, :3]).mean()
    B1 = B if d_dir <= d_flip else B[:, ::-1].copy()
    print("sauma hangaarit -> kiitotie: suora", round(d_dir, 1), "peilattu", round(d_flip, 1))
    save("alppi_kiitotie_1.png", B1)
    save("alppi_hangaarit.png", A_orig[:, ::-1].copy())   # peilattu: hangaarit oikeassa päässä, peli etenee oikealle
    save("alppi_kiitotie_2.png", B1[:, ::-1].copy())      # peilikuva: sen vasen reuna = kuvan 1 oikea reuna, sauma täsmää
    # kylä: reunat ristiinhäivytetään (kuva lyhenee 60 px), sitten kaksi vierekkäin -> saumaton
    L = 60; W = V.shape[1]
    t = np.linspace(0, 1, L, dtype=np.float32)[None, :, None]
    Vf = V.astype(np.float32)
    head = Vf[:, :L] * t + Vf[:, W - L:] * (1 - t)
    V1 = np.concatenate([head, Vf[:, L:W - L]], axis=1).clip(0, 255).astype(np.uint8)
    VV = np.concatenate([V1, V1], axis=1)
    save("alppi_kyla.png", VV)
    # vuoret: kuva + peilikuva
    MM = np.concatenate([M, M[:, ::-1]], axis=1)
    save("alppi_vuoret.png", MM)
    print("ok")


if __name__ == "__main__":
    main(*sys.argv[1:5])
