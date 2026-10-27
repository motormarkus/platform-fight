"""Yakuza-pomo (punainen puku, klubin yläkerta): kolmesta vihreätaustaisesta videosta täysresoluutiokuvat (ei pienennystä; ppu 200).
  idle  (video 3, 1080p, skaalataan samaan kokoon kuin video 2), kavely (video 1), iskut (video 2: kolme lyöntiä, kaksi potkua ja kierrepotku).
Ruudut: idle ja kävely 512 x 704 (hahmo keskellä), iskut 896 x 704 (jalkojen keskikohta 300 px vasemmalta; pivot x 0,335 Unityssa), jalat 16 px ylös.
Ajo (projektin juuresta): python Tools/YakuzaAssets/make_yakuza_boss_sheets.py <kavely.mp4> <iskut.mp4> <idle.mov> <ulos>"""
import os, sys
import cv2, numpy as np
sys.path.insert(0, os.path.join(os.path.dirname(__file__), "..", "AttendantAssets"))
from make_attendant_hd import key_frame

CH, FOOT = 704, 16
CW_IDLE, CW_ATK, ANCHOR_ATK = 512, 896, 300

def load(path):
    cap = cv2.VideoCapture(path); fr = []
    while True:
        ok, f = cap.read()
        if not ok: break
        fr.append(key_frame(f))
    return fr

def feet(f):
    a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
    low = a[ys.min() + int(h * 0.8):]; _, lx = np.where(low)
    return ys.max() + 1, lx.mean(), ys.max() - ys.min() + 1

def rescale(f, k):
    if abs(k - 1) < 1e-3: return f
    a = f.astype(np.float32); a[..., :3] *= a[..., 3:4] / 255.0
    r = cv2.resize(a, (round(f.shape[1] * k), round(f.shape[0] * k)), interpolation=cv2.INTER_AREA)
    al = r[..., 3:4] / 255.0; r[..., :3] = np.where(al > 1e-3, r[..., :3] / np.maximum(al, 1e-3), 0)
    return r.clip(0, 255).astype(np.uint8)

def cell(f, ax, floor, cw, anchor_x):
    c = np.zeros((CH, cw, 4), np.uint8)
    x0 = int(round(ax)) - anchor_x; y0 = int(round(floor)) - (CH - FOOT)
    sy0, sx0 = max(0, y0), max(0, x0); sy1, sx1 = min(f.shape[0], y0 + CH), min(f.shape[1], x0 + cw)
    if sy1 > sy0 and sx1 > sx0: c[sy0 - y0:sy1 - y0, sx0 - x0:sx1 - x0] = f[sy0:sy1, sx0:sx1]
    return c

def save(cells, cols, path):
    rows = (len(cells) + cols - 1) // cols; ch, cw = cells[0].shape[:2]
    img = np.zeros((rows * ch, cols * cw, 4), np.uint8)
    for n, c in enumerate(cells):
        r, k = divmod(n, cols); img[r * ch:(r + 1) * ch, k * cw:(k + 1) * cw] = c
    cv2.imwrite(path, cv2.cvtColor(img, cv2.COLOR_RGBA2BGRA)); print(os.path.basename(path), len(cells), "kuvaa", img.shape[1], "x", img.shape[0])

def main():
    walk_v, atk_v, idle_v, out = sys.argv[1:5]
    os.makedirs(out, exist_ok=True)
    atk = load(atk_v); floor0, ax0, h0 = feet(atk[0])
    print("iskuvideo: lattia", floor0, "jalkojen keski", round(ax0), "korkeus", h0)
    # --- iskut (kiinteä ankkuri: kamera ei liiku, hahmo askeltaa kuvassa) ---
    CLIPS = {   # nimi: (ruudut, osumakuva-indeksi)
        "yakuzapomo_lyonti1": ([32, 34, 35, 36, 37, 38, 40, 42], 4),
        "yakuzapomo_lyonti2": ([42, 43, 44, 45, 46, 47, 48, 50], 3),
        "yakuzapomo_lyonti3": ([50, 52, 53, 54, 56, 58, 60, 62, 64, 66], 4),
        "yakuzapomo_potku1": ([68, 70, 72, 74, 76, 78, 80, 82, 84, 86, 88, 90, 92, 94, 96, 98], 7),
        "yakuzapomo_potku2": ([102, 104, 106, 108, 110, 112, 114, 116, 118, 120, 122, 124, 126, 128, 130, 132], 9),
        "yakuzapomo_kierre": ([168, 170, 172, 174, 176, 178, 180, 182, 184, 186, 188, 190, 192, 194, 196, 198], 7),
    }
    for name, (idx, imp) in CLIPS.items():
        cells = [cell(atk[i], ax0, floor0, CW_ATK, ANCHOR_ATK) for i in idx]
        # tarkista, että mikään ei leikkaudu: hahmon laatikko ruudun sisällä
        for i, c in zip(idx, cells):
            a = c[..., 3] > 128; ys, xs = np.where(a)
            if xs.min() <= 0 or xs.max() >= CW_ATK - 1 or ys.min() <= 0: print("  HUOM leikkautuu:", name, i, xs.min(), xs.max(), ys.min())
        save(cells, 3, os.path.join(out, name + ".png")); print("  osumakuva-indeksi", imp)
    # --- kävely: silmukka ---
    wv = load(walk_v)
    tx, fl = [], []
    for f in wv:
        a = f[..., 3] > 128; ys, xs = np.where(a); h = ys.max() - ys.min()
        t = a[ys.min() + int(h * 0.25):ys.min() + int(h * 0.6)]; _, x = np.where(t); tx.append(x.mean()); fl.append(ys.max() + 1)
    floor = int(np.median(fl))
    def mask(i):
        m = np.zeros((CH, CW_IDLE), np.float32); a = (wv[i][..., 3] > 128)[floor - (CH - FOOT):floor + FOOT]
        x0 = int(round(tx[i])) - CW_IDLE // 2; sx0, sx1 = max(0, x0), min(a.shape[1], x0 + CW_IDLE); m[:a.shape[0], sx0 - x0:sx1 - x0] = a[:, sx0:sx1]; return m
    M = [mask(i) for i in range(len(wv))]
    best = min(((np.abs(M[s] - M[e]).mean(), s, e) for s in range(10, 120) for e in range(s + 24, min(len(wv), s + 80))))
    _, s, e = best; widx = list(range(s, e)); print("kävelysilmukka", s, e, "ero", round(float(best[0]), 4), len(widx), "kuvaa")
    save([cell(wv[i], tx[i], floor, CW_IDLE, CW_IDLE // 2) for i in widx], 8, os.path.join(out, "yakuzapomo_kavely.png"))
    # --- idle (1080p -> sama koko kuin iskuvideon hahmo), ping-pong ---
    iv = load(idle_v); f0, a0, hh = feet(iv[0]); k = h0 / hh
    print("idle: skaalauskerroin", round(k, 3))
    ids = list(range(0, len(iv), 8)); ids = ids + ids[-2:0:-1]
    cells = []
    for i in ids:
        r = rescale(iv[i], k); cells.append(cell(r, a0 * k, f0 * k, CW_IDLE, CW_IDLE // 2))   # kiinteä ankkuri (ei tärinää)
    save(cells, 8, os.path.join(out, "yakuzapomo_idle.png"))

main()
