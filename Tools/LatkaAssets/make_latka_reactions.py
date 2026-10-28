"""Lätkäfanin reaktiot yhdestä videosta (1080p, 30 fps): osuma, kaatuminen (ilma), maahan (makaaminen) ja ylösnousu. Ruutu 1280 x 704, hahmo keskellä (jalkojen keskikohta = ruudun keskikohta), ppu 200.
Kohdistus, jottei hahmo "kävele" animaatiossa (Enemy siirtää itse vihua): osuma = jalkojen keskikohta kiinteänä; ilmassa ja maassa = painopiste (silotettu), jolloin vartalo pysyy ruudun keskellä;
ylösnousussa painopiste liukuu takaisin jalkoihin, jotta viimeinen kuva täsmää perusasentoon. Maassa makaavan alareuna pidetään lattialla (perspektiivi muuttaa videossa ruudun alareunaa).
Ajo: python Tools/LatkaAssets/make_latka_reactions.py <osuma_kaatuminen.mov> <ulos-kansio>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from make_latka_sheets import load, feet, cell, save, rescale
CW, A = 1280, 640
SEG = {   # nimi: (ensimmäinen, viimeinen, askel)
    "osuma": (4, 30, 2), "kaatuminen": (31, 66, 1), "maahan": (67, 110, 2), "nousu": (111, 215, 2),
}
def smooth(v, n=7):
    v = np.asarray(v, float); p = np.pad(v, (n // 2, n // 2), mode="edge"); return np.convolve(p, np.ones(n) / n, mode="valid")
def ramp(i, a, b): return float(np.clip((i - a) / (b - a), 0, 1))
def main():
    video, out = sys.argv[1:3]; os.makedirs(out, exist_ok=True)
    fr = load(video); f0, a0, h0 = feet(fr[0]); k = 613 / h0; print("ruutuja", len(fr), "korkeus", h0, "skaalaus", round(k, 3))
    sc = [rescale(f, k) for f in fr]; fl0, fa0, _ = feet(sc[0])
    cx, bot, fx = [], [], []
    for f in sc:
        al = f[..., 3] > 128; ys, xs = np.where(al); cx.append(xs.mean()); bot.append(ys.max() + 1)
        low = al[ys.min() + int((ys.max() - ys.min()) * 0.8):]; _, lx = np.where(low); fx.append(lx.mean())
    cxs = smooth(cx)
    ax, fl = [], []
    for i in range(len(sc)):
        w = ramp(i, 32, 40) * (1 - ramp(i, 150, 185))          # 0 = jalat, 1 = painopiste
        ax.append((1 - w) * (fa0 if i < 100 else fx[i]) + w * cxs[i])
        wv = ramp(i, 62, 68) * (1 - ramp(i, 150, 185))         # 1 = alareuna lattialla
        fl.append(wv * bot[i] + (1 - wv) * fl0)
    for name, (a, b, st) in SEG.items():
        idx = list(range(a, b + 1, st)); cells = [cell(sc[i], ax[i], fl[i], CW, A) for i in idx]
        for i, c in zip(idx, cells):
            al = c[..., 3] > 128; ys, xs = np.where(al)
            if xs.min() <= 0 or xs.max() >= CW - 1 or ys.min() <= 0: print("  HUOM leikkautuu:", name, i, xs.min(), xs.max(), ys.min())
        save(cells, 4, os.path.join(out, "latkafani_" + name + ".png"))
main()
