"""Lentokenttä Thaitownin kadun jatkoksi: 3 katukuvaa (1672 x 941, läpinäkyvät taivas ja ikkunat) + 4 sisätilan kuvaa.
- katu: katkaistaan kuten Thaitownin katu (rakennus 0-622, jalkakäytävä 622-668, katu 668-941) -> thaitown_lentokentta_katu_N_yla/_kav/_ala.png
- ikkunamaskit: ylä-osan läpinäkyvät alueet, jotka eivät ole yhteydessä yläreunaan (taivas), eli ikkunat -> thaitown_lentokentta_katu_N_ikkunat.png (valkoinen = ikkuna)
- thaitown_lentokentta_tiski.png: baaritiskin etuosa (sisäkuva 2, x 665-1415, rivit 485-620)
- sisätila: yksittäin thaitown_lentokentta_sisa_1-4.png (terminaalin kulku) ja neljä kuvaa vierekkäin yhdeksi leveäksi kuvaksi -> thaitown_lentokentta_sisa.png (parallaksina ikkunoiden taakse)
Ajo (projektin juuresta): python Tools/AirportAssets/make_airport_images.py <katu1> <katu2> <katu3> <sisa1> <sisa2> <sisa3> <sisa4>
"""
import sys, os
import numpy as np, cv2
from PIL import Image

T = os.path.join(os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__)))), "Assets", "Sprites", "Taustat")
CUT, CURB = 622, 668


def holes(alpha_top):
    """Ikkunat: läpinäkyvät alueet, jotka eivät kosketa yläreunaa (taivas)."""
    transparent = (alpha_top < 128).astype(np.uint8)
    n, lab, st, _ = cv2.connectedComponentsWithStats(transparent, connectivity=4)
    out = np.zeros_like(transparent)
    for i in range(1, n):
        x, y, w, h, area = st[i]
        if y == 0 or area < 1500 or h < 28 or w < 40: continue   # rimojen renkaat, vaijerien raot ja taivaskolot pois: vain ikkunat
        out[lab == i] = 1
    # pieni laajennus: reunapikselit eivät jätä juovaa ikkunan ja kehyksen väliin
    return cv2.dilate(out, np.ones((3, 3), np.uint8))


def main(args):
    street, interior = args[:3], args[3:7]
    for k, p in enumerate(street):
        im = Image.open(p).convert("RGBA"); w, h = im.size
        base = os.path.join(T, "thaitown_lentokentta_katu_%d" % (k + 1))
        im.crop((0, 0, w, CUT)).save(base + "_yla.png")
        im.crop((0, CUT, w, CURB)).save(base + "_kav.png")
        im.crop((0, CURB, w, h)).save(base + "_ala.png")
        a = np.array(im)[:CUT, :, 3]
        m = holes(a)
        mask = np.zeros((CUT, w, 4), np.uint8); mask[m > 0] = (255, 255, 255, 255)
        Image.fromarray(mask, "RGBA").save(base + "_ikkunat.png")
        print("katu", k + 1, im.size, "ikkunapikseleitä", int(m.sum()))
    ims = [Image.open(p).convert("RGBA") for p in interior]
    for k, im in enumerate(ims): im.save(os.path.join(T, "thaitown_lentokentta_sisa_%d.png" % (k + 1)))   # yksittäin terminaalin sisätilaan
    W = sum(i.width for i in ims); H = max(i.height for i in ims)
    ims[1].crop((665, 485, 1415, 620)).save(os.path.join(T, "thaitown_lentokentta_tiski.png"))   # baaritiskin etuosa Sohvin eteen (kuva 2)
    wide = Image.new("RGBA", (W, H), (0, 0, 0, 0)); x = 0
    for i in ims: wide.paste(i, (x, 0)); x += i.width
    wide.save(os.path.join(T, "thaitown_lentokentta_sisa.png")); print("sisätila", wide.size)


if __name__ == "__main__":
    main(sys.argv[1:])
