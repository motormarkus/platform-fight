"""Roccon ja Helvin paritanssi (3 videota 1920x1080, 30 fps; video 1 alkaa kauempaa toisistaan, 2 jatkaa, 3 loppuu suudelmaan).
Käyttö: python make_helvi_pair.py <ulostulo-kansio>   Tulos: tanssi_pari_1..3.png, ruutu 640 x 400, 10 saraketta x 10 riviä (joka kolmas ruutu = 10 fps).
Mittakaava: Helvi (oikea hahmo ruudussa 0) = 300 px, samoin kuin helvi_idle."""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.dirname(__file__))
from key_video import key_frame, border_color
from make_helvi_sheets import read_all, resize_premult
U = "C:/Users/marku/.claude/uploads/3b55fbe8-a93c-494a-9b49-32b59fdabe3e/"
SRC = ["c2ccc848-copy_F5218F86-5174-43ED-9FDA-784AC2C1BF91.mov", "ea4fe2b7-copy_78A8B8BE-E3AF-4FA8-A0FB-003C18E0ECBC.mov", "428720de-copy_49D08348-3EA1-476A-8E55-C7A104277D2F.mov",
       "8273bd09-copy_06BB7819-32B8-466B-89DE-E7D9AFA8CA86.mov"]   # 4: nosto syliin (alkaa kuten 3, soitetaan lopuksi myös takaperin = lasku)
CW, CH, COLS, TARGET_H, FEET, STEP = 640, 400, 10, 300.0, 384, 3

def key(bgr):
    k = key_frame(bgr, t_in=26.0, t_out=60.0, cyan_safe=True, multi=True)
    # suljetut taustan väriset aukot (hahmojen välissä): pois
    d = np.sqrt(((bgr.astype(np.float32) - border_color(bgr).astype(np.float32)) ** 2).sum(axis=2))
    near = (d < 30).astype(np.uint8)
    nn, lab, st, _ = cv2.connectedComponentsWithStats(near, connectivity=4)
    for i in range(1, nn):
        if st[i, cv2.CC_STAT_AREA] > 150: k[lab == i, 3] = 0
    r, g, b = [k[..., i].astype(int) for i in range(3)]
    k[(g - r > 14) & (g - b > 6), 3] = 0
    solid = (k[..., 3] > 128).astype(np.uint8)
    solid = cv2.morphologyEx(solid, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    n, lab, st, _ = cv2.connectedComponentsWithStats(solid, connectivity=8)
    keep = np.zeros_like(solid)
    if n > 1:
        big = st[1:, cv2.CC_STAT_AREA].max()
        for i in range(1, n):
            if st[i, cv2.CC_STAT_AREA] >= 0.12 * big: keep[lab == i] = 1
    allow = cv2.dilate(keep, np.ones((9, 9), np.uint8)); k[allow == 0, 3] = 0
    return k

def main(out, only=None):
    os.makedirs(out, exist_ok=True)
    ref = 862.0 if only else None   # Helvin korkeus ruudussa (sama kamera kaikissa videoissa)
    for n, f in enumerate(SRC, 1):
        if only and n not in only: continue
        keyed = [key(x) for x in read_all(U + f)[::STEP]]
        if ref is None:   # Helvi = oikeanpuoleinen hahmo ruudussa 0
            a = (keyed[0][..., 3] > 128).astype(np.uint8)
            nn, lab, st, _ = cv2.connectedComponentsWithStats(a, connectivity=8)
            two = sorted(range(1, nn), key=lambda i: -st[i, cv2.CC_STAT_AREA])[:2]
            hel = max(two, key=lambda i: st[i, cv2.CC_STAT_LEFT])
            ref = float(st[hel, cv2.CC_STAT_HEIGHT]); print("Helvin korkeus", ref)
        s0 = TARGET_H / ref
        cx = np.array([np.mean(np.where(k[..., 3] > 128)[1][[0, -1]] if False else [np.where(k[..., 3] > 128)[1].min(), np.where(k[..., 3] > 128)[1].max()]) for k in keyed])
        bot = np.array([float(np.where(k[..., 3] > 128)[0].max()) for k in keyed])
        def rm(a, w):
            h = w // 2; return np.array([np.median(a[max(0, i - h):i + h + 1]) for i in range(len(a))])
        cxs, base = rm(cx, 15), rm(bot, 9)
        sheet = np.zeros(((len(keyed) + COLS - 1) // COLS * CH, COLS * CW, 4), np.uint8)
        pad = 800
        for i, k in enumerate(keyed):
            sw, sh = int(round(CW / s0)), int(round(CH / s0))
            x0 = int(round(cxs[i] - sw / 2)); y1 = int(round(base[i] + (CH - FEET) / s0)); y0 = y1 - sh
            big = cv2.copyMakeBorder(k, pad, pad, pad, pad, cv2.BORDER_CONSTANT, value=(0, 0, 0, 0))
            cell = resize_premult(big[y0 + pad:y1 + pad, x0 + pad:x0 + sw + pad], CW, CH)
            sheet[(i // COLS) * CH:(i // COLS + 1) * CH, (i % COLS) * CW:(i % COLS + 1) * CW] = cell
        cv2.imwrite(os.path.join(out, "tanssi_pari_%d.png" % n), cv2.cvtColor(sheet, cv2.COLOR_RGBA2BGRA))
        print(n, len(keyed), "ruutua")
if __name__ == "__main__": main(sys.argv[1], [int(a) for a in sys.argv[2:]] or None)
