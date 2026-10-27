"""Lentoemäntä täysresoluutiolla: videosta ei pienennetä mitään (hahmo n. 600 px, ruutu 512 x 640, 8 saraketta, ppu 200 -> sama maailmakoko kuin vanhoissa 300 px / ppu 100 -kuvissa).
Seisominen: lentoemanta_idle.png (ping-pong, joka 4. ruutu), puhe: lentoemanta_puhe.png (joka 3. ruutu, kerran läpi). Kamera on kiinteä, joten hahmo pysyy samassa kohdassa ruudussa (ei tärinää).
Ajo (projektin juuresta): python Tools/AttendantAssets/make_attendant_hd.py <idle.mp4> <puhe.mp4> <ulos-kansio> [<kavely.mp4>]"""
import os, sys
import cv2, numpy as np

def key_frame(bgr, keep_effects=False):
    """Kirkas vihreä tausta: vain selvästi vihreä (G reilusti yli R:n ja B:n) on taustaa, joten sininen puku ei koskaan katoa (key_video.key_frame teki pukuun lohkoreikiä).
    Pehmeä alfa reunalla, vihreä hohto pois (despill). Suljetut taustataskut (käsivarren ja vartalon väli) pysyvät läpinäkyvinä."""
    f = bgr.astype(np.float32); b, g, r = f[..., 0], f[..., 1], f[..., 2]
    gd = g - np.maximum(r, b)                                    # vihreys
    a = np.clip(1.0 - (gd - 25.0) / 55.0, 0.0, 1.0)              # gd<=25: täysin hahmoa, gd>=80: täysin taustaa
    a[(g < 110)] = 1.0                                           # tummat pikselit (ääriviivat, puku) eivät ole taustaa
    # vain suurin yhtenäinen hahmo
    n2, lab2, st2, _ = cv2.connectedComponentsWithStats((a > 0.5).astype(np.uint8), connectivity=8)
    if n2 > 2:
        keep = 1 + int(np.argmax(st2[1:, cv2.CC_STAT_AREA]))
        if keep_effects:   # tehosteet (kipinät) mukaan: pienetkin yhtenäiset alueet säilytetään
            for i in range(1, n2):
                if i != keep and st2[i, cv2.CC_STAT_AREA] < 40: a[lab2 == i] = 0.0
        else: a[lab2 != keep] = 0.0
    # despill: reunapikselien vihreä rajoitetaan
    g2 = np.where(a < 0.999, np.minimum(g, np.maximum(r, b) + 6.0), g)
    out = np.dstack([r, g2, b, a * 255.0]).clip(0, 255).astype(np.uint8)
    return out

CW, CH, COLS, FOOT = 512, 640, 8, 8

def load(path):
    cap = cv2.VideoCapture(path); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        fr.append(key_frame(f))
    return fr

def sheet(frames, idx, out):
    ys, xs = np.where(frames[0][..., 3] > 128)
    floor = int(ys.max()) + 1
    low = frames[0][..., 3][int(ys.min() + (ys.max() - ys.min()) * 0.65):] > 128
    _, lx = np.where(low); cx = int(round(lx.mean()))
    x0, y0 = cx - CW // 2, floor - (CH - FOOT)
    rows = (len(idx) + COLS - 1) // COLS
    img = np.zeros((rows * CH, COLS * CW, 4), np.uint8)
    for n, i in enumerate(idx):
        f = frames[i]; cell = np.zeros((CH, CW, 4), np.uint8)
        sy0, sx0 = max(0, y0), max(0, x0)
        sy1, sx1 = min(f.shape[0], y0 + CH), min(f.shape[1], x0 + CW)
        cell[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = f[sy0:sy1, sx0:sx1]
        r, c = divmod(n, COLS)
        img[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = cell
    cv2.imwrite(out, cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA))
    print(os.path.basename(out), len(idx), "kuvaa", img.shape[1], "x", img.shape[0], "lattia", floor, "keskikohta", cx)

def walk_sheet(frames, out, minlen=24):
    """Kävelysilmukka: paikallaan kävelevästä videosta etsitään ruutupari (s, e), jossa asento toistuu (pienin ero), ja otetaan s..e-1 joka 2. ruutu.
    Hahmo keskitetään vartalon mukaan joka ruudussa (ei liu'u sivulle), jalat pysyvät lattialla (lattia = jalkojen alin kohta, tasoitettu)."""
    tx, fl = [], []
    for f in frames:
        a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
        t = a[ys.min() + int(h * 0.25):ys.min() + int(h * 0.6)]; _, x = np.where(t)
        tx.append(x.mean()); fl.append(ys.max() + 1)
    floor = int(np.median(fl))
    def mask(i):
        a = (frames[i][..., 3] > 128)[floor - (CH - FOOT):floor + FOOT]
        m = np.zeros((CH, CW), np.float32); xc = int(round(tx[i])); x0 = xc - CW // 2
        sx0, sx1 = max(0, x0), min(a.shape[1], x0 + CW); m[:a.shape[0], sx0 - x0:sx1 - x0] = a[:, sx0:sx1]
        return m
    M = [mask(i) for i in range(len(frames))]
    best = min(((np.abs(M[s] - M[e]).mean(), s, e) for s in range(56, 110) for e in range(s + minlen, min(len(frames), s + 72))))
    _, s, e = best
    idx = list(range(s, e, 2)); print("silmukka", s, e, "ero", round(float(best[0]), 4), len(idx), "kuvaa")
    rows = (len(idx) + COLS - 1) // COLS
    img = np.zeros((rows * CH, COLS * CW, 4), np.uint8)
    for n, i in enumerate(idx):
        f = frames[i]; cell = np.zeros((CH, CW, 4), np.uint8)
        x0 = int(round(tx[i])) - CW // 2; y0 = floor - (CH - FOOT)
        sy0, sx0 = max(0, y0), max(0, x0); sy1, sx1 = min(f.shape[0], y0 + CH), min(f.shape[1], x0 + CW)
        cell[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = f[sy0:sy1, sx0:sx1]
        r, c = divmod(n, COLS); img[r * CH:(r + 1) * CH, c * CW:(c + 1) * CW] = cell
    cv2.imwrite(out, cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA))
    print(os.path.basename(out), len(idx), "kuvaa", img.shape[1], "x", img.shape[0])

if __name__ == "__main__":
    idle = load(sys.argv[1]); talk = load(sys.argv[2]); out = sys.argv[3]
    os.makedirs(out, exist_ok=True)
    fw = list(range(0, len(idle), 4)); sheet(idle, fw + fw[-2:0:-1], os.path.join(out, "lentoemanta_idle.png"))
    sheet(talk, list(range(0, len(talk), 3)), os.path.join(out, "lentoemanta_puhe.png"))
    if len(sys.argv) > 4: walk_sheet(load(sys.argv[4]), os.path.join(out, "lentoemanta_kavely.png"))
