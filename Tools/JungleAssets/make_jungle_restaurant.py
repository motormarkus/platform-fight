"""Viidakkoravintolan sisus: kolme generaattorin kuvaa (1672 x 941, alfa; ikkunat ja oviaukot läpinäkyviä) -> Assets/Sprites/Taustat/viidakko_ravintola_1..3.png
sekä tiskin etuosan kaistat viidakko_ravintola_tiski_1..3.png (Sohvi seisoo tiskin takana; kaistat piirtyvät hänen päälleen).
Alfa 240-254 -> 255, lähes läpinäkyvät pikselit (alfa < 16) puhdistetaan (ei punaista reunaa ikkunoihin), lattian sauma tasoitetaan (rivikohtainen kerroin + peilattu häivytys).
Ajo: python Tools/JungleAssets/make_jungle_restaurant.py <kuva 1> <kuva 2> <kuva 3>"""
import os, sys
import numpy as np, cv2
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_jungle_assets as J

ROW0 = 615          # lattia alkaa (tiskin alareuna ~600)
CROP_Y0, CROP_Y1 = 448, 606   # tiskin etuosan kaista (kansi ja etulevy)


def seam(left, right, L=650):
    r = right.astype(np.float32)
    le = left[ROW0:, -6:, :3].astype(np.float32).mean(axis=1); re_ = r[ROW0:, :6, :3].mean(axis=1)
    g = (le + 1) / (re_ + 1)
    g = cv2.GaussianBlur(g.reshape(-1, 1, 3), (0, 0), sigmaX=0.1, sigmaY=18).reshape(-1, 3)
    g = np.clip(g, 0.75, 1.35)
    x = np.arange(r.shape[1], dtype=np.float32); w = np.clip(1 - x / L, 0, 1) ** 1.5
    r[ROW0:, :, :3] *= 1 + (g[:, None, :] - 1) * w[None, :, None]
    out = right.copy(); out[..., :3] = np.clip(r[..., :3], 0, 255).astype(np.uint8)
    # peilattu häivytys ensimmäisillä 140 px:llä, jotta lattian saumassa ei näy katkoa
    B, row0 = 140, 655
    m = left[row0:, -B:, :3][:, ::-1].astype(np.float32)
    ww = (1 - np.arange(B, dtype=np.float32) / B)[None, :, None] ** 1.5
    out[row0:, :B, :3] = np.clip(m * ww + out[row0:, :B, :3].astype(np.float32) * (1 - ww), 0, 255).astype(np.uint8)
    return out


def main(p1, p2, p3):
    ims = [J.fit(J.bleed(J.solidify(J.rd(p)))) for p in (p1, p2, p3)]
    ims = [J.clear_glass(i) for i in ims]
    ims[1] = seam(ims[0], ims[1]); ims[2] = seam(ims[1], ims[2])
    for i, im in enumerate(ims, 1): J.save("viidakko_ravintola_%d.png" % i, im)
    # tiskin etuosa: kuva 1 oikea osa (x 1070-), kuva 2 koko leveys, kuva 3 vasen osa (-440)
    J.save("viidakko_ravintola_tiski_1.png", ims[0][CROP_Y0:CROP_Y1, 1070:])
    J.save("viidakko_ravintola_tiski_2.png", ims[1][CROP_Y0:CROP_Y1, :])
    J.save("viidakko_ravintola_tiski_3.png", ims[2][CROP_Y0:CROP_Y1, :440])
    print("ok")


if __name__ == "__main__":
    main(*sys.argv[1:4])
